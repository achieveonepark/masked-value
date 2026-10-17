# Masked Values

[한국어](README.ko.md)

Low-allocation runtime value masking and optional lightweight tamper detection for **Unity 2022.3 or later**. Supports unmanaged values, strings, and reference-containing data through explicit codecs. No external package dependencies.

## Installation

### Git URL

In **Window > Package Manager**, choose **+ > Add package from git URL**:

```text
https://github.com/achieveonepark/masked-value.git#v1.2.0
```

### OpenUPM

Once the package is listed on [OpenUPM](https://openupm.com/packages/com.achieve.masked-values/), run this in your Unity project with the [OpenUPM CLI](https://openupm.com/docs/getting-started-cli.html):

```sh
openupm add com.achieve.masked-values
```

Alternatively, add the following scoped registry and dependency to your project's `Packages/manifest.json`. Merge these entries with your existing configuration:

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
    "com.achieve.masked-values": "1.2.0"
  }
}
```

## Quick start

```csharp
using Achieve.MaskedValues;
using UnityEngine;

public sealed class PlayerState : MonoBehaviour
{
    public GuardedValue<int> health = 100;
    public MaskedValue<float> speed = 5f;

    public void TakeDamage(int amount) => health -= amount;
}
```

| Type | Use |
| --- | --- |
| `MaskedValue<T>` | Mask primitives, enums, and unmanaged structs. |
| `GuardedValue<T>` | Mask unmanaged values and validate integrity on reads, writes, and explicit remasking. |
| `MaskedString` | Mask strings; use `CopyTo` and `ValueEquals` to avoid allocating on reads. |
| `MaskedData<T>` | Mask reference-containing data with an explicit `ISpanValueCodec<T>`. |

For guarded values, `TryGetValue`, `TrySetValue`, `CheckIntegrity`, and `TryRefreshMask` return `false` when validation fails. The `Value` getter/setter and `RefreshMask` throw `InvalidOperationException` on failed checks. Call `RefreshMask()` explicitly to change the masked representation while keeping the value.

Import **Class Fields** from the package's **Samples** tab in Package Manager for a complete example.

## Inspector support

In **Unity 6**, declare ordinary serialized fields using the existing types:

```csharp
public GuardedValue<int> health = 100;
public MaskedValue<float> moveSpeed = 5f;
public MaskedString playerName = "Player";
```

No companion field, linking attribute, or initialization method is required. Edit mode supports initial-value persistence and Undo; Play mode displays/edits live values temporarily. Reflection and editing buffers stay in the Editor. Unity 6 serialization adds an internal authoring field and restores masked state on load, without extra gameplay getter/setter checks. Larger values may use more memory. Unity 2022.3 retains the optional `[MaskedValueField]` bridge.

Import **Inspector Fields** for primitives, a Unity vector, a string, and a custom struct. See the [Inspector guide](Documentation~/Inspector.md) for assembly references, codec-backed data, performance, and limitations.

## Runtime behavior and limits

Unity 6 serializes authoring values and explicit serializable codecs; runtime storage, keys, and tags are never persisted. Loading/cloning restores authored defaults, rather than current gameplay state. Loaded initial values remain plain authoring data in memory and in assets. Player constructors do not retain runtime input as an authoring value. Use `[NonSerialized]` for runtime-only component fields. Standalone .NET layouts remain unchanged. Synchronize concurrent access externally.

Numeric wrappers support `+`, `-`, `*`, `/`, `%`, compound assignment, unary signs, and prefix/postfix `++`/`--`, including guarded/masked combinations. Supported types: `byte`, `sbyte`, `short`, `ushort`, `char`, `int`, `uint`, `long`, `ulong`, `float`, `double`, `decimal`. Results keep the wrapper type. Integral overflow wraps, integer division by zero and decimal overflow throw, and floating division follows C#. A caller's `checked` block does not make these operators checked. Guarded operands validate before access. Comparisons and integral bitwise expressions use implicit conversions. Unity vectors support addition/subtraction, colors also multiplication, and quaternions multiplication; other custom operations use `.Value`.

```csharp
health += 10;
health -= 5;
health *= 2;
health /= 2;
health %= 100;
health++;
```

`MaskedString` supports concatenation (which allocates). `MaskedData<T>` converts to `T` but requires an explicit codec when constructed. Arithmetic preserves the masked operand's authoring snapshot.

Masking uses reversible XOR operations. `GuardedValue<T>` adds non-cryptographic integrity checks to detect ordinary memory overwrites. Keys and masked values remain in the same process; code patches, key extraction, valid snapshot replay, full erasure to the default state, and changes through legitimate APIs remain possible. Authoritative game state should be validated by the server.

Unmanaged reads, writes, and guarded remasking allocate no managed memory after initialization. Strings and codec-backed data can allocate. Operation cost and storage grow with the value size; measure on your target platform.

See the [detailed usage guide (Korean)](Documentation~/masked-values.md) and [performance measurements](Documentation~/Performance.md) for API details and verification limits.

## AI skill

The package includes [`Skills~/achieve-masked-values`](Skills~/achieve-masked-values/SKILL.md). Unity Skill Manager collects it into the project's `.agents/skills` and `.claude/skills`. Invoke it with `$achieve-masked-values` in Codex or `/achieve-masked-values` in Claude Code.

## Development

Run the behavior and allocation checks with the .NET 8 SDK:

```sh
dotnet run --project 'Tests~/SmokeTests/SmokeTests.csproj' --configuration Release
```

With Node.js/npm and PowerShell installed, validate the package and build the same tarball layout used by OpenUPM:

```powershell
& './Tools~/validate-package.ps1' -Tag v1.2.0
& './Tools~/pack.ps1'
```

The archive is written to `Artifacts~/`. Tests, tools, CI configuration, and generated artifacts are excluded. See the [release guide](https://github.com/achieveonepark/masked-value/blob/main/.github/RELEASING.md) for tagging and OpenUPM submission, and the [Unity verification project](https://github.com/achieveonepark/masked-value/blob/main/Tests~/UnityProject/README.md) for Editor checks.

## License

[MIT](LICENSE.md) © 2026 achieveonepark.
