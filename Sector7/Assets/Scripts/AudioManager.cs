using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;
    
    public AudioSource sfxSource;
    public AudioSource musicSource;
    public AudioClip jumpSound; 
    public AudioClip clickSound; 
    public AudioClip pickupSound;
    public AudioClip doorSound;
    public AudioClip stompSound;
    public AudioClip hurtSound;
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void Play(AudioClip clip, float volume = 1f)
    {
    if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip, volume);
        }
    }

    // Для кнопок інтерфейсу: OnClick вміє викликати лише методи з 0 або 1 параметром,
    // а Play тепер має два (clip і volume), тому для кліку окремий метод без параметрів
    public void PlayClick()
    {
        Play(clickSound);
    }
}
