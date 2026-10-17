using System;
using Achieve.MaskedValues;
using Achieve.MaskedValues.Editor;
using UnityEditor;
using UnityEngine;

internal static class InspectorUnityVerification
{
    internal static void Run()
    {
#if UNITY_6000_0_OR_NEWER
        VerifyAutomaticFields();
#endif
        var fixture = ScriptableObject.CreateInstance<MaskedInspectorFixture>();
        try
        {
            fixture.health = 75;
            using (var preview = new MaskedValueFieldDrawer.Preview(typeof(int), fixture.health.Value))
            {
                Require(preview.Property.editable, "The isolated runtime buffer must allow native control editing");
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

#if UNITY_6000_0_OR_NEWER
    private static void VerifyAutomaticFields()
    {
        var source = ScriptableObject.CreateInstance<AutomaticMaskedFixture>();
        var restored = ScriptableObject.CreateInstance<AutomaticMaskedFixture>();
        try
        {
            using (var serialized = new SerializedObject(source))
            {
                SerializedProperty health = serialized.FindProperty("health");
                Require(health != null, "A plain GuardedValue field is automatically serialized");
                health.FindPropertyRelative("initialValue").intValue = 150;
                serialized.FindProperty("speed.initialValue").floatValue = 7.5f;
                serialized.FindProperty("playerName.initialValue").stringValue = "Authored";
                serialized.FindProperty("enabled.initialValue").boolValue = true;
                serialized.FindProperty("position.initialValue").vector3Value = new Vector3(4, 5, 6);
                serialized.FindProperty("profile.initialValue.level").intValue = 8;
                serialized.ApplyModifiedProperties();
                // Match the height of a normal primitive property exactly.
                var drawer = new MaskedValueFieldDrawer();
                Require(drawer.GetPropertyHeight(health, GUIContent.none) == EditorGUIUtility.singleLineHeight,
                    "Automatic fields use normal Unity row height");
            }
            source.health.Value = 75;
            string json = JsonUtility.ToJson(source);
            Require(!json.Contains("storage") && !json.Contains("\"key\"") && !json.Contains("\"tag\""),
                "Automatic serialization excludes live masked bytes, keys, and tags");
            JsonUtility.FromJsonOverwrite(json, restored);
            Require(restored.health.Value == 150 && restored.health.CheckIntegrity(), "Deserialize the authored guarded value, not the live value");
            Require(restored.speed.Value == 7.5f && restored.playerName.Value == "Authored" && restored.enabled.Value,
                "Deserialize floats, strings, and booleans");
            Require(restored.position.Value == new Vector3(4, 5, 6), "Deserialize unmanaged Unity structs");
            var stringDrawer = new MaskedValueFieldDrawer();
            try
            {
                using (var serialized = new SerializedObject(restored))
                    Require((string)stringDrawer.CreatePreview(serialized.FindProperty("playerName")).ReadEditedValue() == "Authored",
                        "A string property may report isArray and must still support runtime editing");
            }
            finally { stringDrawer.ClearPreviews(); }
            Require(restored.profile.Value.level == 8 && restored.profile.Value.name == "Player",
                "Restore an explicitly serializable codec without runtime discovery");
            var direct = MaskedValueBinding.ForPath("health");
            direct.Write(restored, 40);
            Require(restored.health.Value == 40, "Runtime editing binds directly to the declared field");
            restored.health += 2;
            using (var serialized = new SerializedObject(restored))
                Require(serialized.FindProperty("health.initialValue").intValue == 150,
                    "Arithmetic preserves the authored snapshot");
            MaskedValueBinding.ForPath("values.Array.data[0]").Write(restored, 33);
            Require(restored.values[0].Value == 33, "Direct wrappers in arrays are editable");
            MaskedValueBinding.ForPath("nested.Array.data[0].health").Write(restored, 44);
            Require(restored.nested[0].health.Value == 44, "Direct wrappers in nested structs are written back");
            using (var serialized = new SerializedObject(restored))
            {
                serialized.FindProperty("health.initialValue").intValue = 200;
                serialized.ApplyModifiedProperties();
                Undo.PerformUndo();
                serialized.Update();
                Require(serialized.FindProperty("health.initialValue").intValue == 150, "Automatic initial values support Undo");
            }
            Debug.Log("MASKED_VALUES_AUTOMATIC_INSPECTOR_VERIFICATION_PASSED");
        }
        finally
        {
            Undo.ClearUndo(source); Undo.ClearUndo(restored);
            UnityEngine.Object.DestroyImmediate(source); UnityEngine.Object.DestroyImmediate(restored);
        }
    }
#endif
}

#if UNITY_6000_0_OR_NEWER
public sealed class AutomaticMaskedFixture : ScriptableObject
{
    public GuardedValue<int> health = 100;
    public MaskedValue<float> speed = 5f;
    public MaskedString playerName = "Player";
    public MaskedValue<bool> enabled = false;
    public MaskedValue<Vector3> position = new Vector3(1, 2, 3);
    public MaskedData<PlayerProfile> profile = new MaskedData<PlayerProfile>(
        new PlayerProfile { name = "Player", level = 1 }, PlayerProfileCodec.Instance);
    public GuardedValue<int>[] values = { new GuardedValue<int>(1) };
    public AutomaticNestedFields[] nested = { new AutomaticNestedFields { health = 2 } };
}
[Serializable] public struct AutomaticNestedFields { public GuardedValue<int> health; }
#endif

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
