$ErrorActionPreference = 'Stop'
$taskRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$taskArtifacts = Join-Path $taskRoot 'Artifacts~'
$taskStaging = Join-Path $taskArtifacts ('staging-' + [Guid]::NewGuid().ToString('N'))
$taskPackage = Join-Path $taskStaging 'package'
New-Item -ItemType Directory -Path $taskPackage -Force | Out-Null

$taskEntries = @('package.json','package.json.meta','README.md','README.md.meta',
    'CHANGELOG.md','CHANGELOG.md.meta','Runtime','Runtime.meta','Samples~','Documentation~')
foreach ($taskEntry in $taskEntries) {
    Copy-Item -LiteralPath (Join-Path $taskRoot $taskEntry) -Destination $taskPackage -Recurse
}
$taskManifest = Get-Content -LiteralPath (Join-Path $taskPackage 'package.json') -Raw | ConvertFrom-Json
$taskArchive = Join-Path $taskArtifacts ($taskManifest.name + '-' + $taskManifest.version + '.tgz')
& tar -czf $taskArchive -C $taskStaging package
if ($LASTEXITCODE -ne 0) { throw 'Package archive creation failed.' }
Write-Output $taskArchive
