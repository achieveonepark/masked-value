# Unity verification project

Package reference: `Packages/manifest.json` resolves the repository root with `file:../../..`.

Copy `Tests~/Verification/VerificationSuite.cs` and `Samples~/ClassFields/PlayerProfileCodec.cs` into `Assets/Editor` before opening. The build helper `Tools~/prepare-tests.ps1` performs that copy.

Open the project in Unity 6000.5.10f1 and choose `Tools > Masked Values > Run Verification`, or run:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.10f1\Editor\Unity.exe' -batchmode -nographics -projectPath 'C:\Projects\fff\Tests~\UnityProject' -executeMethod MaskedValuesVerification.Run -logFile 'C:\Projects\fff\Artifacts~\unity.log'
```

This checks package compilation, unmanaged Unity structs, behavioral assertions, managed allocations and comparative timing in the actual Editor. It does not replace an IL2CPP player/device benchmark.
