param([string]$Tag = '')

$ErrorActionPreference = 'Stop'
$taskRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$taskManifest = Get-Content -LiteralPath (Join-Path $taskRoot 'package.json') -Raw | ConvertFrom-Json
if ($taskManifest.name -cnotmatch '^[a-z0-9]+(?:\.[a-z0-9][a-z0-9-]*){2,}$') {
    throw 'Package name must use reverse domain notation.'
}
if ($taskManifest.version -cnotmatch '^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-(?:0|[1-9]\d*|\d*[A-Za-z-][0-9A-Za-z-]*)(?:\.(?:0|[1-9]\d*|\d*[A-Za-z-][0-9A-Za-z-]*))*)?$') {
    throw 'Package version must be a semantic version without build metadata.'
}
if ($Tag -and $Tag -cne ('v' + $taskManifest.version)) {
    throw "Release tag '$Tag' must match v$($taskManifest.version)."
}
if ($taskManifest.license -ne 'MIT' -or $taskManifest.private -eq $true) {
    throw 'The distribution must be public and declare its MIT license.'
}
foreach ($taskField in @('author', 'repository', 'bugs', 'documentationUrl', 'changelogUrl', 'licensesUrl')) {
    if (-not $taskManifest.$taskField) { throw "Missing package metadata: $taskField" }
}

Push-Location -LiteralPath $taskRoot
try {
    $taskPackJson = & npm pack --dry-run --json --ignore-scripts
    if ($LASTEXITCODE -ne 0) { throw 'npm pack validation failed.' }
    $taskPack = @($taskPackJson | ConvertFrom-Json)[0]
    $taskFiles = @($taskPack.files | ForEach-Object { $_.path })
    foreach ($taskRequired in @('package.json', 'package.json.meta', 'README.md', 'README.md.meta',
            'README.ko.md', 'README.ko.md.meta', 'LICENSE.md', 'LICENSE.md.meta',
            'CHANGELOG.md', 'CHANGELOG.md.meta', 'Runtime.meta',
            'Runtime/Achieve.MaskedValues.asmdef', 'Runtime/Achieve.MaskedValues.asmdef.meta',
            'Runtime/Unity/Achieve.MaskedValues.Unity.asmdef', 'Runtime/Unity/MaskedValueFieldAttribute.cs',
            'Editor.meta', 'Editor/Achieve.MaskedValues.Editor.asmdef', 'Editor/MaskedValueFieldDrawer.cs',
            'Samples~/InspectorFields/InspectorFieldsExample.cs',
            'Documentation~/Inspector.md',
            'Samples~/ClassFields/MaskedFieldsExample.cs', 'Documentation~/masked-values.md',
            'Documentation~/Performance.md', 'Skills~/achieve-masked-values/SKILL.md',
            'Skills~/achieve-masked-values/references/package-guide.md')) {
        if ($taskFiles -cnotcontains $taskRequired) { throw "Missing distribution file: $taskRequired" }
    }
    foreach ($taskFile in $taskFiles) {
        if ($taskFile -match '(^|/)(Tests~|Tools~|Artifacts~|\.github|bin|obj|Library|Temp)(/|$)') {
            throw "Development file included in distribution: $taskFile"
        }
        if ($taskFile -match '^(Runtime|Editor)/.*\.(cs|asmdef)$' -and $taskFiles -cnotcontains ($taskFile + '.meta')) {
            throw "Missing Unity metadata: $taskFile.meta"
        }
    }
    foreach ($taskSample in $taskManifest.samples) {
        if (-not @($taskFiles | Where-Object { $_.StartsWith($taskSample.path + '/') }).Count) {
            throw "Missing sample directory: $($taskSample.path)"
        }
    }
    Write-Output "Validated $($taskManifest.name)@$($taskManifest.version): $($taskFiles.Count) distribution files."
}
finally {
    Pop-Location
}
