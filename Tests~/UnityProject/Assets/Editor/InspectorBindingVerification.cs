using System;
using System.Collections.Generic;
using System.Reflection;
using Achieve.MaskedValues;
using Achieve.MaskedValues.Editor;

internal static class InspectorBindingVerification
{
    internal static void Run()
    {
        var owner = new Owner();
        var health = new MaskedValueBinding("initialHealth", "health");
        Require((int)health.Read(owner) == 100, "Read guarded runtime value");
        health.Write(owner, 75);
        Require(owner.Health == 75, "Write back boxed guarded value");
        Require(owner.initialHealth == 100, "Keep initial value independent");

        var inherited = new MaskedValueBinding("initialInherited", "inherited");
        inherited.Write(owner, 17);
        Require(owner.Inherited == 17, "Find a private base-class field");

        var nested = new MaskedValueBinding("nested.initialValue", "value");
        nested.Write(owner, 23);
        Require(owner.nested.value.Value == 23, "Write back nested struct owner");
        var list = new MaskedValueBinding("items.Array.data[0].initialValue", "value");
        list.Write(owner, 31);
        Require(owner.items[0].value.Value == 31, "Write back struct inside list");
        var array = new MaskedValueBinding("array.Array.data[0].initialValue", "value");
        array.Write(owner, 37);
        Require(owner.array[0].value.Value == 37, "Write back struct inside array");
        var deep = new MaskedValueBinding("groups.Array.data[0].items.Array.data[0].initialValue", "value");
        deep.Write(owner, 41);
        Require(owner.groups[0].items[0].value.Value == 41, "Write back nested collection owners");

        var text = new MaskedValueBinding("initialName", "name");
        text.Write(owner, "Updated");
        Require((string)text.Read(owner) == "Updated", "Write masked string");
        text.Write(owner, null);
        Require(text.Read(owner) == null, "Preserve null masked string");
        var mode = new MaskedValueBinding("initialMode", "mode");
        mode.Write(owner, 2);
        Require((Mode)mode.Read(owner) == Mode.Second, "Convert serialized enum integer");
        var character = new MaskedValueBinding("initialChar", "character");
        character.Write(owner, (ushort)'Z');
        Require((char)character.Read(owner) == 'Z', "Convert serialized character");
        var data = new MaskedValueBinding("initialData", "data");
        data.Write(owner, new PlayerProfile { name = "Codec", level = 5 });
        Require(((PlayerProfile)data.Read(owner)).level == 5, "Write codec-backed data");
        var custom = new MaskedValueBinding("initialStats", "stats");
        custom.Write(owner, new Stats { level = 9 });
        Stats editedStats = (Stats)custom.Read(owner);
        Require(editedStats.level == 9 && editedStats.hidden == 91, "Preserve nonserialized custom struct fields");
        custom.WriteChanges(owner, new Stats { level = 1 }, new Stats { level = 1, damage = 4.5f });
        editedStats = (Stats)custom.Read(owner);
        Require(editedStats.level == 9 && editedStats.damage == 4.5f && editedStats.hidden == 91,
            "A deferred edit preserves live changes to untouched struct fields");

        object corrupt = new GuardedValue<int>(55);
        typeof(GuardedValue<int>).GetField("storage", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(corrupt, 123456);
        typeof(Owner).GetField("health", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(owner, corrupt);
        RequireThrows<InvalidOperationException>(() => health.Read(owner), "Reject corrupt read");
        RequireThrows<InvalidOperationException>(() => health.Write(owner, 10), "Reject corrupt write");
        object unchanged = typeof(Owner).GetField("health", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
        Require(corrupt.Equals(unchanged), "Corrupt state is not overwritten or re-tagged");
        RequireThrows<InvalidOperationException>(() => new MaskedValueBinding("initialHealth", "missing").Read(owner), "Missing binding");
        RequireThrows<InvalidOperationException>(() => new MaskedValueBinding("initialHealth", "initialHealth").Read(owner), "Reject plain field binding");
        RequireThrows<InvalidOperationException>(() => new MaskedValueBinding("initialHealth", "readonlyValue").Write(owner, 10), "Reject readonly binding");
        RequireThrows<InvalidOperationException>(() => new MaskedValueBinding("items.Array.data[99].initialValue", "value").Read(owner), "Reject invalid collection index");
        Console.WriteLine("Inspector binding verification passed: runtime edits, initial-value isolation, inheritance, nested structs/collections, strings, enums, codecs, and corrupt-state rejection.");
    }

    private static void Require(bool condition, string name)
    {
        if (!condition) throw new Exception("Inspector binding check failed: " + name);
    }

    private static void RequireThrows<T>(Action action, string name) where T : Exception
    {
        try { action(); }
        catch (TargetInvocationException exception) when (exception.InnerException is T) { return; }
        catch (T) { return; }
        throw new Exception("Inspector binding check did not throw: " + name);
    }

    private enum Mode { First = 1, Second = 2 }
    private class BaseOwner
    {
        private MaskedValue<int> inherited = 3;
        public int Inherited => inherited.Value;
    }

#pragma warning disable CS0414, CS0649
    private sealed class Owner : BaseOwner
    {
        public int initialHealth = 100;
        private GuardedValue<int> health = 100;
        public int Health => health.Value;
        private readonly MaskedValue<int> readonlyValue = 1;
        private MaskedString name = "Original";
        private MaskedValue<Mode> mode = Mode.First;
        private MaskedValue<char> character = 'A';
        private MaskedValue<Stats> stats = new Stats { level = 1, hidden = 91 };
        private MaskedData<PlayerProfile> data = new MaskedData<PlayerProfile>(
            new PlayerProfile { name = "Initial", level = 1 }, PlayerProfileCodec.Instance);
        public Nested nested;
        public List<Nested> items = new List<Nested> { default };
        public Nested[] array = new Nested[1];
        public Group[] groups = { new Group { items = new List<Nested> { default } } };
    }

    private struct Group { public List<Nested> items; }
    private struct Nested { public int initialValue; public MaskedValue<int> value; }
    [Serializable]
    private struct Stats { public int level; public float damage; [NonSerialized] public int hidden; }
#pragma warning restore CS0414, CS0649
}
