using System;
using Achieve.MaskedValues;
using UnityEngine;

public sealed class InspectorFieldsExample : MonoBehaviour
{
    [SerializeField, MaskedValueField(nameof(health))]
    private int initialHealth = 100;

    [SerializeField, MaskedValueField(nameof(speed))]
    private float initialSpeed = 5f;

    [SerializeField, MaskedValueField(nameof(spawnPosition))]
    private Vector3 initialSpawnPosition = new Vector3(1f, 2f, 3f);

    [SerializeField, MaskedValueField(nameof(playerName))]
    private string initialPlayerName = "Player";

    [SerializeField, MaskedValueField(nameof(stats))]
    private InspectorPlayerStats initialStats = new InspectorPlayerStats { level = 1, damage = 12.5f };

    [NonSerialized] private GuardedValue<int> health;
    [NonSerialized] private MaskedValue<float> speed;
    [NonSerialized] private MaskedValue<Vector3> spawnPosition;
    [NonSerialized] private MaskedString playerName;
    [NonSerialized] private MaskedValue<InspectorPlayerStats> stats;

    public int Health => health.Value;
    public float Speed => speed.Value;

    private void Awake()
    {
        // One-time assignment. Gameplay uses the original runtime types directly.
        health = initialHealth;
        speed = initialSpeed;
        spawnPosition = initialSpawnPosition;
        playerName = initialPlayerName;
        stats = initialStats;
    }

    public void TakeDamage(int amount) => health.Value = Math.Max(0, health.Value - amount);
}

[Serializable]
public struct InspectorPlayerStats
{
    public int level;
    public float damage;
}
