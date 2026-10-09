$taskRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$taskEditor = Join-Path $taskRoot 'Tests~\UnityProject\Assets\Editor'
Copy-Item -LiteralPath (Join-Path $taskRoot 'Tests~\Verification\VerificationSuite.cs') -Destination $taskEditor
Copy-Item -LiteralPath (Join-Path $taskRoot 'Samples~\ClassFields\PlayerProfileCodec.cs') -Destination $taskEditor
Copy-Item -LiteralPath (Join-Path $taskRoot 'Samples~\ClassFields\MaskedFieldsExample.cs') -Destination $taskEditor
New-Item -ItemType Directory -Path (Join-Path $taskRoot 'Artifacts~') -Force | Out-Null
