using System;
using Achieve.MaskedValues;
using UnityEngine;

public sealed class MaskedFieldsExample : MonoBehaviour
{
    // Runtime only. Do not persist masks/keys into scenes, prefabs or save files.
    [NonSerialized] public GuardedValue<int> health = 100;
    [NonSerialized] public MaskedValue<float> speed = 5f;
    [NonSerialized] public MaskedValue<PlayerStats> stats;
    [NonSerialized] public MaskedValue<Vector3> spawnPosition;
    [NonSerialized] public MaskedString playerName = "Player";
    [NonSerialized] public MaskedData<PlayerProfile> profile;

    private void Awake()
    {
        stats.Value = new PlayerStats { level = 1, damage = 12.5f };
        spawnPosition.Value = new Vector3(1f, 2f, 3f);
        profile = new MaskedData<PlayerProfile>(
            new PlayerProfile { name = "Player", level = 1 }, PlayerProfileCodec.Instance);
    }

    private void Update()
    {
        // Explicit, infrequent remasking avoids charging every read for a refresh.
        // The tag is still checked on every guarded read and write.
        if ((Time.frameCount & 63) == 0)
            health.RefreshMask();
    }

    public void TakeDamage(int amount)
    {
        health.Value = Math.Max(0, health.Value - amount);
    }

    public void LevelUp()
    {
        // A struct getter returns a copy; edit the copy and assign it back.
        PlayerStats current = stats.Value;
        current.level++;
        stats.Value = current;
    }
}

public struct PlayerStats
{
    public int level;
    public float damage;
}
