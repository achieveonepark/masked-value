param([switch]$Check)

$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$taskSource = Join-Path $taskRoot 'Skills~/achieve-masked-values'
$taskSourceFiles = @(Get-ChildItem -LiteralPath $taskSource -File -Recurse)
$taskRelativeFiles = @($taskSourceFiles | ForEach-Object {
    $_.FullName.Substring($taskSource.Length).TrimStart([char[]]'\/')
})

foreach ($taskFolder in @('.agents/skills/achieve-masked-values', '.claude/skills/achieve-masked-values')) {
    $taskDestination = Join-Path $taskRoot $taskFolder
    foreach ($taskRelative in $taskRelativeFiles) {
        $taskSourcePath = Join-Path $taskSource $taskRelative
        $taskTargetPath = Join-Path $taskDestination $taskRelative
        if ($Check) {
            if (-not (Test-Path -LiteralPath $taskTargetPath -PathType Leaf)) {
                throw "Missing skill copy: $taskFolder/$taskRelative"
            }
            if ([IO.File]::ReadAllText($taskSourcePath) -cne [IO.File]::ReadAllText($taskTargetPath)) {
                throw "Outdated skill copy: $taskFolder/$taskRelative. Run Tools~/sync-skills.ps1."
            }
        }
        else {
            New-Item -ItemType Directory -Path (Split-Path -Parent $taskTargetPath) -Force | Out-Null
            Copy-Item -LiteralPath $taskSourcePath -Destination $taskTargetPath
        }
    }
    foreach ($taskExisting in (Get-ChildItem -LiteralPath $taskDestination -File -Recurse)) {
        $taskRelative = $taskExisting.FullName.Substring($taskDestination.Length).TrimStart([char[]]'\/')
        if ($taskRelativeFiles -cnotcontains $taskRelative) {
            throw "Unexpected skill copy file: $taskFolder/$taskRelative. Review it before removal."
        }
    }
}

Write-Output 'Masked Values skill copies match the Skills~ source.'
