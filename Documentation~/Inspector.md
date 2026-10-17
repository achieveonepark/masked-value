# Inspector support

`MaskedValueFieldAttribute` connects a Unity-serialized initial value to a separate runtime masked field. It supports the normal Inspector without replacing your component's custom Editor.

## Usage

```csharp
using System;
using Achieve.MaskedValues;
using UnityEngine;

public sealed class PlayerState : MonoBehaviour
{
    [SerializeField, MaskedValueField(nameof(health))]
    private int initialHealth = 100;

    [NonSerialized] private GuardedValue<int> health;

    private void Awake() => health = initialHealth;

    public void TakeDamage(int amount) => health.Value -= amount;
}
```

The initial field and the masked `Value` must have exactly the same type. The attribute names a field on the same object or nested container, including private inherited fields. Initialize the runtime field explicitly in `Awake` or another suitable lifecycle method. The attribute does not initialize values or scan game objects at runtime.

Edit mode uses normal Unity serialization, so initial values support Undo, multi-object editing, scene saving, and prefab overrides. Play mode switches to the live masked value for objects participating in Play mode; prefab assets continue to edit their initial values. Runtime editing requires a single selected object and does not persist to the initial field. Runtime edits do not participate in Unity's serialized Undo history.

Import the **Inspector Fields** sample from Package Manager for `GuardedValue<int>`, `MaskedValue<float>`, `MaskedValue<Vector3>`, `MaskedString`, and a custom unmanaged struct. Custom initial-value structs must be `[Serializable]` and follow Unity's normal field serialization rules.

## Assembly definitions

The core assembly remains `Achieve.MaskedValues` and has no Unity engine references. If your component is in an assembly definition, add references to both:

- `Achieve.MaskedValues`
- `Achieve.MaskedValues.Unity`

The second assembly supplies the attribute. It contains no runtime update loop. The attribute is marked `[Conditional("UNITY_EDITOR")]`, so its uses are omitted from player metadata. `Achieve.MaskedValues.Editor` is restricted to the Editor and excluded from player builds.

## Codec-backed values

`MaskedData<T>` can be bound when `T` is also Unity-serializable. The runtime codec must be initialized by your own code before the Inspector reads it:

```csharp
[SerializeField, MaskedValueField(nameof(profile))]
private PlayerProfile initialProfile;

[NonSerialized] private MaskedData<PlayerProfile> profile;

private void Awake()
{
    profile = new MaskedData<PlayerProfile>(initialProfile, PlayerProfileCodec.Instance);
}
```

The Inspector does not discover or create codecs. Reading a codec-backed value invokes its decoder. Runtime edits use the existing codec's encoder. Custom classes and structs are edited through their Unity-serialized fields; nonserialized state is preserved when applying an edit.

## Performance and storage

The existing masked wrappers, their layouts, and their read/write implementations are unchanged. Gameplay accesses the same `.Value` APIs directly. Initializing from a serialized field performs a normal one-time assignment; there is no new lazy-initialization branch or reflection in gameplay access.

Reflection, boxing, and serialized editing buffers run in the Editor. Automatic Inspector refresh requests are limited to five per second. Inspecting strings or codec-backed values can allocate in the Editor because their normal getters can allocate. An open Inspector can therefore affect Editor profiling; measure player performance separately.

The serialized initial fields add their normal storage to the component and remain plain authoring data in scenes, prefabs, and builds. They do not contain changing live game state. If a retained initial value is unnecessary after initialization, your component can clear that field after copying it into the masked value. Inspector support is an authoring/debugging tool, not encryption of authored data.

Native controls use an isolated, hidden scratch object. Color pickers and other delayed controls can update it without applying live values to a scene or prefab. Only actual edits are passed to the masked setter; repainting does not remask or re-tag a value. Edits preserve nonserialized custom fields and live changes to untouched sibling fields.

## Limits and failure behavior

- Supported initial values must be serializable by Unity and accessible through `SerializedProperty.boxedValue`. Primitive numeric types, booleans, strings, enums, common Unity value types, and serializable custom structs/classes are supported. Runtime-only types such as `decimal` do not automatically gain Inspector support.
- Bind one value field. Array/list roots, fixed buffers, and `[SerializeReference]` initial fields are unsupported. A bound pair can live inside nested serialized structs, arrays, or lists; struct changes are copied back to the owner. Cyclic or deeply nested reference graphs are rejected.
- Keep masks/keys/tags nonserialized. Domain reload resets runtime wrappers. Your normal initialization and lifecycle code must restore them; this feature does not change Unity's behavior when domain/scene reload is disabled.
- Corrupt guarded values are rejected by their normal getter/setter checks. The Inspector shows an error and pauses reads until **Retry read**; it does not repair or overwrite the value. These checks can trigger the package's existing tampering diagnostic event. Exceptions from custom codecs are handled the same way.
- The Inspector runs on Unity's main thread. It does not add synchronization for gameplay code that changes masked values on worker threads.
- Custom Editors must draw the initial serialized property for its drawer to run. IMGUI and UI Toolkit hosts are supported. Other attributes or custom drawers can affect which drawer Unity selects for a field.

See Unity's [serialization rules](https://docs.unity3d.com/2022.3/Documentation/Manual/script-Serialization.html) and [`SerializedProperty.boxedValue` restrictions](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/SerializedProperty-boxedValue.html).
