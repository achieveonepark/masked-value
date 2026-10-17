using System;
using System.Reflection;
using Achieve.MaskedValues;
using UnityEngine;

// Runs the player-compiled callback code under .NET; no Unity native APIs.
// Actual Unity editor serialization is covered by InspectorUnityVerification.
internal static class PlayerSerializationVerification
{
    private static int Main()
    {
        try
        {
            Require(Initial(new GuardedValue<int>(99)).Equals(0), "Player constructors retain no plaintext authoring input");
            var guarded = (GuardedValue<int>)Restore(new GuardedValue<int>(99), 150);
            Require(guarded.Value == 150 && guarded.CheckIntegrity(), "Player restores guarded authoring value");
            Require(Initial(guarded).Equals(150), "Player retains authored defaults for Unity cloning");
            var masked = (MaskedValue<int>)Restore(new MaskedValue<int>(99), 200);
            Require(masked.Value == 200 && Initial(masked).Equals(200), "Player restores masked initial value");
            var text = (MaskedString)Restore(new MaskedString("Runtime"), "Authored");
            Require(text.Value == "Authored" && Initial(text).Equals("Authored"), "Player restores string initial value");
            var profile = (MaskedData<PlayerProfile>)Restore(new MaskedData<PlayerProfile>(
                new PlayerProfile { name = "Runtime", level = 1 }, PlayerProfileCodec.Instance),
                new PlayerProfile { name = "Authored", level = 5 });
            Require(profile.Value.name == "Authored" && profile.Value.level == 5 &&
                ((PlayerProfile)Initial(profile)).name == "Authored", "Player restores codec-backed initial value");
            ((ISerializationCallbackReceiver)(object)guarded).OnBeforeSerialize();
            Require(((GuardedValue<int>)Restore(guarded, Initial(guarded))).Value == 150,
                "Repeated serialization restores the same authored default");
            Console.WriteLine("Player-compiled serialization callback verification passed; native player execution remains separate.");
            return 0;
        }
        catch (Exception exception) { Console.Error.WriteLine(exception); return 1; }
    }

    private static object Restore(object value, object initial)
    {
        value.GetType().GetField("initialValue", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(value, initial);
        ((ISerializationCallbackReceiver)value).OnAfterDeserialize();
        return value;
    }
    private static object Initial(object value) => value.GetType().GetField("initialValue",
        BindingFlags.Instance | BindingFlags.NonPublic).GetValue(value);
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
