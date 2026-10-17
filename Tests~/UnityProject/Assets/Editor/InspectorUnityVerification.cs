using System;
using Achieve.MaskedValues;
using Achieve.MaskedValues.Editor;
using UnityEditor;
using UnityEngine;

internal static class InspectorUnityVerification
{
    internal static void Run()
    {
        var fixture = ScriptableObject.CreateInstance<MaskedInspectorFixture>();
        try
        {
            fixture.health = 75;
            using (var preview = new MaskedValueFieldDrawer.Preview(typeof(int), fixture.health.Value))
            {
                preview.Property.boxedValue = fixture.health.Value;
                Require(fixture.initialHealth == 100, "Preview must not change serialized initial value");
                preview.Property.intValue = 50;
                Require(fixture.health.Value == 75, "Editing the buffer must not apply automatically");
                new MaskedValueBinding("initialHealth", "health").Write(fixture, preview.Property.boxedValue);
                Require(fixture.health.Value == 50 && fixture.initialHealth == 100,
                    "Commit only the masked runtime value");
            }
            using (var preview = new MaskedValueFieldDrawer.Preview(typeof(MaskedInspectorStats), fixture.stats.Value))
            {
                preview.Property.boxedValue = new MaskedInspectorStats { level = 8, damage = 13.5f };
                Require(fixture.initialStats.level == 1, "Custom struct staging must not change the authoring field");
                new MaskedValueBinding("initialStats", "stats").Write(fixture, preview.Property.boxedValue);
                Require(fixture.stats.Value.level == 8 && fixture.stats.Value.hidden == 91,
                    "Custom struct editing must preserve nonserialized fields");
            }
            using (var preview = new MaskedValueFieldDrawer.Preview(typeof(int), 5))
            {
                preview.Property.intValue = 6;
                preview.Property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                Require(preview.HasPendingEdits && (int)preview.ReadEditedValue() == 6,
                    "Capture a deferred native-control edit applied to the scratch object");
                Require(fixture.initialHealth == 100, "Native apply must never touch authoring data");
                preview.Reset(7);
                Require(!preview.HasPendingEdits && (int)preview.ReadEditedValue() == 7,
                    "Refresh scratch state without generating a runtime write");
            }
            using (var preview = new MaskedValueFieldDrawer.Preview(typeof(Color), Color.red))
            {
                preview.Property.colorValue = Color.blue;
                preview.Property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                Require(preview.HasPendingEdits && (Color)preview.ReadEditedValue() == Color.blue,
                    "Capture a deferred color edit in the isolated buffer");
            }
            using (var preview = new MaskedValueFieldDrawer.Preview(typeof(PlayerProfile), fixture.profile.Value))
            {
                preview.Property.FindPropertyRelative("level").intValue = 7;
                new MaskedValueBinding("initialProfile", "profile").WriteChanges(fixture,
                    preview.OriginalValue, preview.ReadEditedValue());
                Require(fixture.profile.Value.level == 7 && fixture.profile.Value.name == "Player",
                    "Edit serializable codec-backed data through its existing codec");
            }
            using (var serialized = new SerializedObject(fixture))
            {
                serialized.FindProperty("initialHealth").intValue = 150;
                serialized.ApplyModifiedProperties();
                Require(fixture.initialHealth == 150 && fixture.health.Value == 50,
                    "Initial value editing must not change runtime values");
                Undo.PerformUndo();
                Require(fixture.initialHealth == 100, "Initial-value editing supports Undo");
            }
            string json = JsonUtility.ToJson(fixture);
            Require(json.Contains("initialHealth") && !json.Contains("storage") && !json.Contains("\"key\"") && !json.Contains("\"tag\""),
                "Only initial values are persisted, never runtime masks or tags");
            Debug.Log("MASKED_VALUES_INSPECTOR_VERIFICATION_PASSED");
        }
        finally
        {
            Undo.ClearUndo(fixture);
            UnityEngine.Object.DestroyImmediate(fixture);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}

public sealed class MaskedInspectorFixture : ScriptableObject
{
    [MaskedValueField(nameof(health))] public int initialHealth = 100;
    [MaskedValueField(nameof(stats))] public MaskedInspectorStats initialStats = new MaskedInspectorStats { level = 1 };
    [MaskedValueField(nameof(profile))] public PlayerProfile initialProfile;
    [NonSerialized] public GuardedValue<int> health;
    [NonSerialized] public MaskedValue<MaskedInspectorStats> stats = new MaskedInspectorStats { level = 1, hidden = 91 };
    [NonSerialized] public MaskedData<PlayerProfile> profile = new MaskedData<PlayerProfile>(
        new PlayerProfile { name = "Player", level = 1 }, PlayerProfileCodec.Instance);
}

[Serializable]
public struct MaskedInspectorStats
{
    public int level;
    public float damage;
    [NonSerialized] public int hidden;
}
