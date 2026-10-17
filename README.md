# Masked Values

[한국어](README.ko.md)

Low-allocation runtime value masking and optional lightweight tamper detection for **Unity 2022.3 or later**. Supports unmanaged values, strings, and reference-containing data through explicit codecs. No external package dependencies.

## Installation

### Git URL

In **Window > Package Manager**, choose **+ > Add package from git URL**:

```text
https://github.com/achieveonepark/masked-value.git#v1.0.0
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
    "com.achieve.masked-values": "1.0.0"
  }
}
```

## Quick start

```csharp
using System;
using Achieve.MaskedValues;
using UnityEngine;

public sealed class PlayerState : MonoBehaviour
{
    [NonSerialized] private GuardedValue<int> health;
    [NonSerialized] private MaskedValue<float> speed;

    private void Awake()
    {
        health = 100;
        speed = 5f;
    }

    public void TakeDamage(int amount) => health.Value -= amount;
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

## Runtime behavior and limits

These types are runtime-only. Use `[NonSerialized]` on Unity component fields and initialize them in `Awake` or another runtime entry point. They do not provide Inspector editing, Unity serialization, save-file persistence, or restoration after a domain reload. Synchronize concurrent access externally.

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
& './Tools~/validate-package.ps1' -Tag v1.0.0
& './Tools~/pack.ps1'
```

The archive is written to `Artifacts~/`. Tests, tools, CI configuration, and generated artifacts are excluded. See the [release guide](https://github.com/achieveonepark/masked-value/blob/main/.github/RELEASING.md) for tagging and OpenUPM submission, and the [Unity verification project](https://github.com/achieveonepark/masked-value/blob/main/Tests~/UnityProject/README.md) for Editor checks.

## License

[MIT](LICENSE.md) © 2026 achieveonepark.
