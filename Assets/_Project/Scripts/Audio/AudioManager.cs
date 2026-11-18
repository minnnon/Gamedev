using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

// Звук гри: фонова музика, ефекти й гучність.
// Гучність іде через AudioMixer "MainMixer" (параметри MusicVol і SfxVol у децибелах).
// Якщо мікшера в проєкті ще немає — працює запасний шлях через AudioSource.volume.
public class AudioManager : MonoBehaviour
{
    private const string MusicParam = "MusicVol";
    private const string SfxParam = "SfxVol";
    private const int PoolSize = 12;

    public static AudioManager Instance { get; private set; }

    public float MusicVolume { get; private set; } = 0.7f;
    public float SfxVolume { get; private set; } = 0.9f;

    private AudioMixer mixer;
    private AudioMixerGroup musicGroup, sfxGroup;
    private bool mixerReady;
    private AudioSource music;
    private readonly List<AudioSource> pool = new List<AudioSource>();
    private readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
    private int next;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Boot()
    {
        if (Instance != null) return;
        new GameObject("AudioManager").AddComponent<AudioManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        mixer = Resources.Load<AudioMixer>("MainMixer");
        if (mixer != null)
        {
            var m = mixer.FindMatchingGroups("Music");
            var s = mixer.FindMatchingGroups("SFX");
            if (m.Length > 0) musicGroup = m[0];
            if (s.Length > 0) sfxGroup = s[0];
        }

        music = gameObject.AddComponent<AudioSource>();
        music.loop = true;
        music.playOnAwake = false;
        music.spatialBlend = 0f;
        music.outputAudioMixerGroup = musicGroup;

        for (int i = 0; i < PoolSize; i++)
        {
            var go = new GameObject("Sfx_" + i);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.outputAudioMixerGroup = sfxGroup;
            pool.Add(src);
        }

        MusicVolume = PlayerPrefs.GetFloat("vol_music", 0.7f);
        SfxVolume = PlayerPrefs.GetFloat("vol_sfx", 0.9f);
        ApplyVolumes();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayMusic(scene.name == GameManager.MenuScene ? "music_menu" : "music_crypt");
        ApplyVolumes();   // мікшер міг ініціалізуватися вже після Awake
    }

    private static float ToDecibels(float v) { return Mathf.Log10(Mathf.Max(v, 0.0001f)) * 20f; }

    private void ApplyVolumes()
    {
        mixerReady = mixer != null && mixer.SetFloat(MusicParam, ToDecibels(MusicVolume)) && mixer.SetFloat(SfxParam, ToDecibels(SfxVolume));
        music.volume = mixerReady ? 1f : MusicVolume;
    }

    public void SetMusicVolume(float v)
    {
        MusicVolume = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat("vol_music", MusicVolume);
        ApplyVolumes();
    }

    public void SetSfxVolume(float v)
    {
        SfxVolume = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat("vol_sfx", SfxVolume);
        ApplyVolumes();
    }

    private void PlayMusic(string clipName)
    {
        var clip = GetClip(clipName);
        if (clip == null || music.clip == clip) return;
        music.clip = clip;
        music.Play();
    }

    private AudioClip GetClip(string clipName)
    {
        if (cache.TryGetValue(clipName, out var clip)) return clip;
        clip = Resources.Load<AudioClip>("Audio/" + clipName);
        if (clip == null) Debug.LogWarning("[Audio] Немає звуку: " + clipName);
        cache[clipName] = clip;
        return clip;
    }

    private AudioSource FreeSource()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            next = (next + 1) % pool.Count;
            if (!pool[next].isPlaying) return pool[next];
        }
        next = (next + 1) % pool.Count;
        return pool[next];     // усі зайняті — перебиваємо найстаріший
    }

    private void PlayInternal(string clipName, bool spatial, Vector3 position, float volume, float pitchRandom, float pitch = 1f)
    {
        var clip = GetClip(clipName);
        if (clip == null) return;
        var src = FreeSource();
        src.clip = clip;
        src.pitch = pitch + Random.Range(-pitchRandom, pitchRandom);
        src.volume = volume * (mixerReady ? 1f : SfxVolume);
        src.spatialBlend = spatial ? 1f : 0f;
        if (spatial)
        {
            src.transform.position = position;
            src.minDistance = 2f;
            src.maxDistance = 28f;
            src.rolloffMode = AudioRolloffMode.Linear;
        }
        src.Play();
    }

    // Звук без позиції (інтерфейс, звуки самого гравця)
    public static void Play(string clipName, float volume = 1f, float pitchRandom = 0.05f)
    {
        if (Instance != null) Instance.PlayInternal(clipName, false, Vector3.zero, volume, pitchRandom);
    }

    // Звук у точці світу (скелети, двері): гучність залежить від відстані до слухача
    public static void PlayAt(string clipName, Vector3 position, float volume = 1f, float pitchRandom = 0.06f, float pitch = 1f)
    {
        if (Instance != null) Instance.PlayInternal(clipName, true, position, volume, pitchRandom, pitch);
    }
}
