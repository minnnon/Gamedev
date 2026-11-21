using UnityEngine;
using UnityEngine.UI;

// Головне меню: нова гра, продовження зі збереження, вихід, гучність.
public class MainMenu : MonoBehaviour
{
    [SerializeField] private Button continueButton;
    [SerializeField] private Text saveInfo;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    private void Start()
    {
        bool has = SaveSystem.HasSave;
        if (continueButton != null) continueButton.interactable = has;
        if (saveInfo != null) saveInfo.text = has ? SaveSystem.ReadInfo() : "Збережень ще немає";

        if (AudioManager.Instance != null)
        {
            if (musicSlider != null) musicSlider.SetValueWithoutNotify(AudioManager.Instance.MusicVolume);
            if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(AudioManager.Instance.SfxVolume);
        }
    }

    public void NewGame() { Click(); GameManager.Instance.NewGame(); }
    public void Continue() { Click(); GameManager.Instance.ContinueGame(); }
    public void Quit() { Click(); GameManager.Instance.Quit(); }

    public void OnMusicSlider(float v) { if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(v); }
    public void OnSfxSlider(float v)
    {
        if (AudioManager.Instance == null) return;
        AudioManager.Instance.SetSfxVolume(v);
        AudioManager.Play("ui_click", 0.8f, 0f);
    }

    private static void Click() { AudioManager.Play("ui_click", 0.7f, 0f); }
}
