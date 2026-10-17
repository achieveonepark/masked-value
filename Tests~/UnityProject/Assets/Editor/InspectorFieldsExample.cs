using System;
using Achieve.MaskedValues;
using UnityEngine;

public sealed class InspectorFieldsExample : MonoBehaviour
{
    // Unity 6+: ordinary serialized fields, with no linking attribute or Awake.
    public GuardedValue<int> health = 100;
    public MaskedValue<float> moveSpeed = 5f;
    public MaskedValue<Vector3> spawnPosition = new Vector3(1f, 2f, 3f);
    public MaskedString playerName = "Player";
    public MaskedValue<InspectorPlayerStats> stats = new InspectorPlayerStats { level = 1, damage = 12.5f };

    public int Health => health.Value;
    public float Speed => moveSpeed.Value;

    public void TakeDamage(int amount) => health.Value = Math.Max(0, health.Value - amount);
}

[Serializable]
public struct InspectorPlayerStats
{
    public int level;
    public float damage;
}
