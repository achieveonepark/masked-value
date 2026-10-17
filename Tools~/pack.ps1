$ErrorActionPreference = 'Stop'
$taskRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$taskArtifacts = Join-Path $taskRoot 'Artifacts~'
New-Item -ItemType Directory -Path $taskArtifacts -Force | Out-Null

# Use the same package.json allowlist as OpenUPM's npm pack pipeline.
Push-Location -LiteralPath $taskRoot
try {
    $taskPackJson = & npm pack --json --ignore-scripts --pack-destination $taskArtifacts
    if ($LASTEXITCODE -ne 0) { throw 'Package archive creation failed.' }
    $taskPack = @($taskPackJson | ConvertFrom-Json)[0]
    Write-Output (Join-Path $taskArtifacts $taskPack.filename)
}
finally {
    Pop-Location
}
