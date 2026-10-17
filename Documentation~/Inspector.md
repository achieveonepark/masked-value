# Inspector support

In **Unity 6**, declare the existing masked types as ordinary fields:

```csharp
using Achieve.MaskedValues;
using UnityEngine;

public sealed class PlayerState : MonoBehaviour
{
    public GuardedValue<int> health = 100;
    public MaskedValue<float> moveSpeed = 5f;
    public MaskedValue<Vector3> spawnPosition = new Vector3(1, 2, 3);
    public MaskedString playerName = "Player";

    public void TakeDamage(int amount) => health -= amount;
}
```

No companion initial-value field, `MaskedValueField`, `NonSerialized`, or `Awake` is required. Private fields use Unity's normal `[SerializeField]`. Custom structs still need `[Serializable]` and Unity-serializable members. Import **Inspector Fields** from Package Manager for a working Unity 6 sample.

Edit mode uses native controls and row heights, with Undo, scene/prefab persistence, and multi-object editing. Play mode displays/edits live `.Value`; runtime edits preserve the authoring value and end with Play mode. Runtime Undo is not provided; select one object to edit live values.

## Serialization and performance

Unity 6 wrappers contain an internal authoring value. Serialization callbacks restore fresh masked state when loading or cloning. Keys, tags, and runtime storage are nonserialized. Loading/cloning restores authored defaults, including after a domain reload; it does not save current gameplay state. Unity's normal rules still apply when scene/domain reload is disabled.

Drawing, reflection, boxing, editing buffers, and refresh scheduling are confined to the Editor assembly. Gameplay getters/setters and integrity checks have no additional Inspector branch or reflection. Warm unmanaged access and numeric operators allocate no managed memory. Restoring strings or codecs can allocate. The authoring field can increase wrapper size, especially for larger structs; small values may fit existing padding. Standalone .NET layouts have no authoring field or Unity dependency.

Player constructors do not retain their input as plaintext authoring data. Loaded initial values remain plain authoring data in memory and in scene/prefab/build assets, so future clones can restore their defaults. The Editor retains constructor initial values for authoring. This does not encrypt assets or implement save-game persistence; runtime player JSON serialization does not preserve live values. Runtime-created objects without loaded authoring data clone with default initial values.

Live refresh is limited to five times per second. String/codec getters can allocate in the Editor, so measure player performance separately. Native controls use isolated scratch objects: delayed controls cannot write runtime values into scene/prefab authoring fields. Edits preserve nonserialized custom state and live changes to untouched sibling fields.

## Codec-backed data

```csharp
public MaskedData<PlayerProfile> profile = new MaskedData<PlayerProfile>(
    new PlayerProfile { name = "Player", level = 1 }, PlayerProfileCodec.Instance);
```

Both the value and codec implementation must be `[Serializable]` for direct Unity persistence. The codec is stored as a managed reference; its configuration must follow Unity serialization rules. Callbacks can run on serialization threads, so decoding must not use main-thread Unity APIs. Codecs are never discovered or created by reflection. Default `MaskedData<T>` still requires a codec before assignment. Runtime-only or nonserializable codecs can use the bridge below.

## Optional bridge and Unity 2022.3

Unity 2022.3 does not support serialization callbacks on structs. On that version, or for custom initialization, use the optional bridge:

```csharp
[SerializeField, MaskedValueField(nameof(health))]
private int initialHealth = 100;

[System.NonSerialized] private GuardedValue<int> health;

private void Awake() => health = initialHealth;
```

The initial field and masked `.Value` must have the same type and share an owner. Private inherited fields and pairs inside nested structs/lists/arrays are supported. Initialize the runtime field yourself. This attribute remains optional on Unity 6, including for manual codec initialization.

Components in assembly definitions need `Achieve.MaskedValues`; the optional bridge additionally needs `Achieve.MaskedValues.Unity`. `Achieve.MaskedValues.Editor` is Editor-only, and bridge attribute uses are excluded from player metadata.

## Limits

- Values must be Unity-serializable and accessible through `SerializedProperty.boxedValue`. Runtime types such as `decimal` do not automatically gain Inspector support.
- Direct wrappers can appear in arrays/lists and nested structs. A wrapper's value cannot itself be an array/list root, fixed buffer, or `[SerializeReference]` value. Cyclic/deep graphs are rejected.
- Corrupt guarded values are rejected by normal checks. The Inspector reports the error and pauses reads until **Retry read**; it never repairs corrupt state. Explicit deserialization creates fresh state from trusted authoring data and is not tamper protected.
- Custom Editors must draw the serialized property. IMGUI and UI Toolkit are supported; other drawers/attributes can affect drawer selection.
- Wrappers remain unsynchronized. Do not share mutable gameplay state or access Unity objects from serialization callbacks/codecs.

Serialization, Undo, collection binding, native row height, operators, and allocations were checked on Unity 6000.5.10f1. Unity 2022.3, other Unity 6 versions, IL2CPP, and devices need separate execution validation.

See Unity's [serialization callbacks](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/ISerializationCallbackReceiver.html), [2022.3 callback limitations](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/ISerializationCallbackReceiver.html), and [`SerializedProperty.boxedValue`](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/SerializedProperty-boxedValue.html).
