using System.Collections.Generic;
using UnityEngine;

// Вішає звуки на події гравця й світу: удар, блок, поранення, кроки, двері, руни.
// GameManager додає цей компонент на гравця сам при завантаженні сцени.
public class AudioHooks : MonoBehaviour
{
    private PlayerController controller;
    private PlayerCombat combat;
    private PlayerBlock block;
    private Health health;
    private RuneGate gate;
    private readonly Dictionary<Door, bool> doors = new Dictionary<Door, bool>();
    private float stepTimer;
    private float quietUntil;
    private int lastHp;

    private void Start()
    {
        controller = GetComponent<PlayerController>();
        combat = GetComponent<PlayerCombat>();
        block = GetComponent<PlayerBlock>();
        health = GetComponent<Health>();
        gate = FindFirstObjectByType<RuneGate>();

        if (combat != null) { combat.Swung += OnSwung; combat.Landed += OnLanded; }
        if (block != null) { block.BlockedHit += OnBlocked; block.GuardBroken += OnGuardBroken; }
        if (health != null) { lastHp = health.Current; health.Changed += OnHealthChanged; }
        if (gate != null) { gate.Changed += OnRune; gate.Opened += OnGateOpened; }

        foreach (var d in FindObjectsByType<Door>(FindObjectsSortMode.None)) doors[d] = d.IsOpen;
        quietUntil = Time.time + 1.5f;    // перші секунди двері мовчать (стан ще виставляється)
    }

    private void OnDestroy()
    {
        if (combat != null) { combat.Swung -= OnSwung; combat.Landed -= OnLanded; }
        if (block != null) { block.BlockedHit -= OnBlocked; block.GuardBroken -= OnGuardBroken; }
        if (health != null) health.Changed -= OnHealthChanged;
        if (gate != null) { gate.Changed -= OnRune; gate.Opened -= OnGateOpened; }
    }

    private void OnSwung() { AudioManager.Play("sword_swing", 0.8f); }
    private void OnLanded() { AudioManager.Play("sword_hit"); }
    private void OnBlocked() { AudioManager.Play("block"); }
    private void OnGuardBroken() { AudioManager.Play("shield_break"); }
    private void OnRune() { AudioManager.Play("rune_insert"); }
    private void OnGateOpened() { AudioManager.Play("gate_open"); }

    private void OnHealthChanged(int current, int max)
    {
        if (current < lastHp) AudioManager.Play(current > 0 ? "hurt" : "player_die");
        lastHp = current;
    }

    private void Update()
    {
        // Кроки: частіше при бігу
        if (controller != null && GameManager.IsPlaying && controller.IsGrounded && controller.CurrentSpeed > 0.5f)
        {
            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0f)
            {
                AudioManager.Play("footstep_" + Random.Range(1, 5), 0.55f, 0.08f);
                stepTimer = controller.CurrentSpeed > 5f ? 0.3f : 0.46f;
            }
        }
        else stepTimer = 0.1f;

        // Двері: звук у момент, коли змінився стан
        foreach (var d in FindDoorKeys())
        {
            if (d == null) continue;
            if (doors[d] == d.IsOpen) continue;
            doors[d] = d.IsOpen;
            if (Time.time > quietUntil) AudioManager.PlayAt("door", d.transform.position, 1f);
        }
    }

    private readonly List<Door> keys = new List<Door>();
    private List<Door> FindDoorKeys()
    {
        keys.Clear();
        keys.AddRange(doors.Keys);
        return keys;
    }
}
