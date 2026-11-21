using UnityEngine;

// Вогнище-чекпоінт: по E зберігає гру, лікує лицаря і запам'ятовує точку відродження.
public class Checkpoint : MonoBehaviour, IInteractable
{
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 0f, -2.2f);   // де з'явиться лицар після смерті

    public string Prompt => "Зберегти гру";

    public void Interact(GameObject interactor)
    {
        var respawn = interactor.GetComponent<PlayerRespawn>();
        if (respawn != null) respawn.SetRespawnPosition(transform.TransformPoint(spawnOffset), transform.eulerAngles.y);

        var health = interactor.GetComponent<Health>();
        if (health != null) health.Heal(health.Max);

        bool ok = SaveSystem.Save();
        HUD.ShowMessage(ok ? "Прогрес збережено" : "Не вдалося зберегти");
        if (ok) AudioManager.Play("save");
    }
}
