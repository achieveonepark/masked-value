# Masked Values

[![openupm](https://img.shields.io/npm/v/com.achieve.masked-values?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.achieve.masked-values/)
[![openupm](https://img.shields.io/badge/dynamic/json?color=brightgreen&label=downloads&query=%24.downloads&suffix=%2Fmonth&url=https%3A%2F%2Fpackage.openupm.com%2Fdownloads%2Fpoint%2Flast-month%2Fcom.achieve.masked-values)](https://openupm.com/packages/com.achieve.masked-values/)

[한국어](README.ko.md)

Mask gameplay values in memory and detect ordinary memory tampering with `GuardedValue<T>`. Supports numbers, unmanaged structs, strings, and data with custom codecs. **Unity 2022.3+**, with automatic Inspector support in **Unity 6**. No external package dependencies.

## Installation

### Git URL

In **Window > Package Manager**, select **+ > Add package from git URL** and paste:

```text
https://github.com/achieveonepark/masked-value.git#v1.0.1
```

### OpenUPM

With the [OpenUPM CLI](https://openupm.com/docs/getting-started-cli.html), run this in your Unity project folder:

```sh
openupm add com.achieve.masked-values
```

<details>
<summary>Install through a scoped registry without the CLI</summary>

Merge the following entries into `Packages/manifest.json`, keeping your existing registries and dependencies:

```json
{
  "scopedRegistries": [
    {
      "name": "OpenUPM",
      "url": "https://package.openupm.com",
      "scopes": ["com.achieve.masked-values"]
    }
  ],
  "dependencies": {
    "com.achieve.masked-values": "1.0.1"
  }
}
```

</details>

## Quick start

Add this component to a GameObject. In Unity 6, its fields also appear in the Inspector.

```csharp
using Achieve.MaskedValues;
using UnityEngine;

public sealed class PlayerState : MonoBehaviour
{
    public GuardedValue<int> health = 100;
    public MaskedValue<float> moveSpeed = 5f;
    public MaskedString playerName = "Player";

    public void TakeDamage(int amount) => health -= amount;
}
```

### Choose a type

| Type | When to use it |
| --- | --- |
| `GuardedValue<T>` | Health, currency, or other unmanaged values that need masking and tamper detection. |
| `MaskedValue<T>` | Numbers, enums, or unmanaged structs that need masking without integrity checks. |
| `MaskedString` | Strings. Use `ValueEquals` to compare or `CopyTo` to read into a reusable buffer. |
| `MaskedData<T>` | Data containing references; supply an explicit `ISpanValueCodec<T>`. See the Class Fields sample. |

### Read, write, and calculate

```csharp
GuardedValue<int> health = 100;
health -= 10;
health += 5;
health *= 2;
health /= 2;
int currentHealth = health;       // Implicit read, with integrity validation.
health.Value = 80;               // Explicit write, also validated.
bool isAlive = health > 0;
```

Numeric wrappers also support `%`, `%=`, unary signs, and `++`/`--`. Results retain the wrapper type: `var result = health + 10` is a `GuardedValue<int>`; use `int result = health + 10` or `health.Value + 10` for a plain number. Integer addition, subtraction, and multiplication wrap on overflow, including inside a caller's `checked` block. Use `.Value` when you need the underlying type's own arithmetic rules or custom struct operations.

`GuardedValue<T>.RefreshMask()` changes the masked representation while keeping the value. Call it explicitly when needed; reads already validate integrity.

## Inspector

In **Unity 6**, public fields work directly. Private fields use Unity's normal `[SerializeField]`. No companion initial-value field, linking attribute, or `Awake` is needed.

| Mode | What you can do |
| --- | --- |
| Edit | Set initial values, save scenes/prefabs, and use Undo. |
| Play | View and edit current values. Changes are discarded when Play mode ends. |

For runtime-only component fields, use `[System.NonSerialized]`. For custom Inspector value structs, add `[System.Serializable]` and follow Unity serialization rules. Unity 2022.3 uses the optional `[MaskedValueField]` bridge instead of automatic fields.

See the [Inspector guide](Documentation~/Inspector.md) for the older-version example, assembly references, and codec-backed fields.

## Handle tampering

`GuardedValue<T>` refuses to return or overwrite a value when its integrity check fails. `.Value` and `RefreshMask()` throw `InvalidOperationException`; `TryGetValue`, `TrySetValue`, `CheckIntegrity`, and `TryRefreshMask` return `false`. Stop the current game action on failure; do not use the failed output as valid state.

Subscribe to `GuardedValueDiagnostics.TamperingDetected` to respond. It reports the value type on the checking thread, **when the next check fails**, rather than at the exact external memory write. For important values you do not otherwise access, call `CheckIntegrity()` at a suitable interval.

To shut down on detection, connect the [ready-to-use listener example](Documentation~/Tampering.md#unity-listener-quit-on-detection). It signals a thread-safe flag, lets your game block further actions, and requests shutdown once on the main thread. In the Editor it stops Play mode. The package does not quit automatically without this integration.

## Samples

In **Package Manager > Masked Values > Samples**, import:

- **Class Fields** — runtime fields, guarded health, strings, structs, and a custom codec.
- **Inspector Fields** — Unity 6 fields for health, speed, position, player name, and custom stats. Add `InspectorFieldsExample` to a GameObject.

## Use with AI coding tools

Copy the included `Skills~/achieve-masked-values` folder into your project's `.agents/skills/` for Codex or `.claude/skills/` for Claude Code. Unity Skill Manager can collect the packaged skill for you.

- **Codex:** `$achieve-masked-values`
- **Claude Code:** `/achieve-masked-values`

The [skill](Skills~/achieve-masked-values/SKILL.md) guides field setup, Inspector use, memory/serialization limits, and a tamper listener that stops gameplay and requests shutdown on detection.

## Things to know

- Masking makes simple plaintext searches harder; it is not encryption or complete anti-cheat protection. Code patches, valid snapshot replay, and legitimate API writes remain possible. Keep authoritative online game state on the server.
- Unity 6 initial values remain plaintext configuration in memory and assets. Loading/cloning restores those defaults, not current gameplay state. `[NonSerialized]` is a serialization setting, not memory protection.
- Unmanaged reads, writes, and remasking allocate no managed memory after initialization. String reads/assignments and codecs can allocate; larger structs also cost more memory and processing. Synchronize concurrent access yourself.

More: [usage guide (Korean)](Documentation~/masked-values.md) · [tamper events and shutdown](Documentation~/Tampering.md) · [performance details](Documentation~/Performance.md)

## License

[MIT](LICENSE.md) © 2026 achieveonepark.
