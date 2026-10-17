# Unity verification project

Package reference: `Packages/manifest.json` resolves the repository root with `file:../../..`.

Run `Tools~/prepare-tests.ps1` before opening to copy the shared runtime/binding verification suites and both sample components into `Assets/Editor`.

Open the project in Unity 6000.5.10f1 and choose `Tools > Masked Values > Run Verification`, or run:

```powershell
$taskProject = (Resolve-Path './Tests~/UnityProject').Path
$taskLog = Join-Path (Get-Location) 'Artifacts~/unity.log'
& 'C:\Program Files\Unity\Hub\Editor\6000.5.10f1\Editor\Unity.exe' -batchmode -nographics -projectPath $taskProject -executeMethod MaskedValuesVerification.Run -logFile $taskLog
```

The project must be closed in other Editor instances for batch execution. This checks package compilation, unmanaged Unity structs, numeric operators, behavioral assertions, managed allocations, and comparative timing. Inspector checks exercise automatic Unity 6 field serialization/restoration, strings and explicit codecs, direct arrays/nested structs, native row height, editable isolated buffers, delayed edits, initial-value isolation, and Undo. The batch suite does not simulate mouse input; live Inspector checks are separate.

Editor verification passed on Unity 6000.5.10f1. Unity 2022.3 and IL2CPP/player/device execution require separate validation.
