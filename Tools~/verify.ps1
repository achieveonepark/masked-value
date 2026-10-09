param(
    [string]$UnityEditorPath = 'C:\Program Files\Unity\Hub\Editor\6000.5.10f1\Editor'
)

$ErrorActionPreference = 'Stop'
$taskRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$taskArtifacts = Join-Path $taskRoot 'Artifacts~'
$taskUnityData = Join-Path $UnityEditorPath 'Data'
$taskSdk = Join-Path $taskUnityData 'DotNetSdk'
$taskDotnet = Join-Path $taskSdk 'dotnet.exe'
$taskCompiler = (Get-ChildItem -LiteralPath (Join-Path $taskSdk 'sdk') -Directory |
    Sort-Object Name -Descending | Select-Object -First 1).FullName
$taskCompiler = Join-Path $taskCompiler 'Roslyn\bincore\csc.dll'
$taskReferencePack = (Get-ChildItem -LiteralPath (Join-Path $taskSdk 'packs\Microsoft.NETCore.App.Ref') -Directory |
    Sort-Object Name -Descending | Select-Object -First 1).FullName
$taskFramework = (Get-ChildItem -LiteralPath (Join-Path $taskReferencePack 'ref') -Directory |
    Sort-Object Name -Descending | Select-Object -First 1).FullName
$taskRuntimeVersion = (Get-ChildItem -LiteralPath (Join-Path $taskSdk 'shared\Microsoft.NETCore.App') -Directory |
    Sort-Object Name -Descending | Select-Object -First 1).Name

& (Join-Path $PSScriptRoot 'prepare-tests.ps1')
$taskReferences = @(Get-ChildItem -LiteralPath $taskFramework -Filter '*.dll' |
    ForEach-Object { '-r:' + $_.FullName })
$taskRuntimeSources = @(Get-ChildItem -LiteralPath (Join-Path $taskRoot 'Runtime') -Filter '*.cs' |
    ForEach-Object { $_.FullName })
$taskSources = $taskRuntimeSources + @(
    (Join-Path $taskRoot 'Samples~\ClassFields\PlayerProfileCodec.cs'),
    (Join-Path $taskRoot 'Tests~\Verification\VerificationSuite.cs'),
    (Join-Path $taskRoot 'Tests~\SmokeTests\Program.cs')
)
$taskExecutable = Join-Path $taskArtifacts 'SmokeTests.dll'
& $taskDotnet exec $taskCompiler -nologo -noconfig -nostdlib+ -unsafe+ -optimize+ -langversion:9 -target:exe "-out:$taskExecutable" $taskReferences $taskSources
if ($LASTEXITCODE -ne 0) { throw 'Smoke test compilation failed.' }

$taskConfiguration = @{
    runtimeOptions = @{
        framework = @{ name = 'Microsoft.NETCore.App'; version = $taskRuntimeVersion }
        configProperties = @{ 'System.Runtime.TieredCompilation' = $false }
    }
} | ConvertTo-Json -Depth 5
[System.IO.File]::WriteAllText((Join-Path $taskArtifacts 'SmokeTests.runtimeconfig.json'), $taskConfiguration)
& $taskDotnet exec $taskExecutable --report (Join-Path $taskArtifacts 'dotnet-verification.md')
if ($LASTEXITCODE -ne 0) { throw 'Smoke tests failed.' }

$taskStandard = '-r:' + (Join-Path $taskUnityData 'NetStandard\ref\2.1.0\netstandard.dll')
$taskRuntimeAssembly = Join-Path $taskArtifacts 'Achieve.MaskedValues.dll'
& $taskDotnet exec $taskCompiler -nologo -noconfig -nostdlib+ -unsafe+ -optimize+ -langversion:9 -target:library "-out:$taskRuntimeAssembly" $taskStandard $taskRuntimeSources
if ($LASTEXITCODE -ne 0) { throw 'Unity API runtime compilation failed.' }

$taskUnityReferences = @(
    $taskStandard,
    ('-r:' + $taskRuntimeAssembly),
    ('-r:' + (Join-Path $taskUnityData 'Managed\UnityEngine\UnityEngine.CoreModule.dll')),
    ('-r:' + (Join-Path $taskUnityData 'Managed\UnityEngine\UnityEditor.CoreModule.dll'))
)
$taskVerificationSources = @(Get-ChildItem -LiteralPath (Join-Path $taskRoot 'Tests~\UnityProject\Assets\Editor') -Filter '*.cs' |
    ForEach-Object { $_.FullName })
$taskVerificationAssembly = Join-Path $taskArtifacts 'Achieve.MaskedValues.Verification.dll'
& $taskDotnet exec $taskCompiler -nologo -noconfig -nostdlib+ -unsafe+ -optimize+ -langversion:9 -target:library "-out:$taskVerificationAssembly" $taskUnityReferences $taskVerificationSources
if ($LASTEXITCODE -ne 0) { throw 'Unity API sample/verification compilation failed.' }
Write-Output 'Unity .NET Standard 2.1 runtime and sample compilation passed.'
