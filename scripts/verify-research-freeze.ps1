$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $PSScriptRoot
$expected = @{
    'evaluation/corpus-v3-final-holdout/manifest.json' = '596AA1F667D5A87C70D375A610046AD5C08E0FADD15EF4D143B30507C19AF3F5'
    'evaluation/corpus-v3-final-holdout/FILES-SHA256.txt' = 'D1FF8CED80070E0B997E4FAB3668FC600D52EA1FB1EAEDD82B31F9FD42F38E5F'
    'evaluation/corpus-v3-final-holdout/CORPUS-SPEC.md' = 'A820508FF03D52747344C4855F3EF14BD0E2A25F90A0015DEC69048FBA0EE597'
}

$failed = $false
foreach ($relativePath in $expected.Keys) {
    $path = Join-Path $projectRoot $relativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        Write-Error "Frozen research artifact is missing: $relativePath" -ErrorAction Continue
        $failed = $true
        continue
    }

    $actual = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
    if ($actual -ne $expected[$relativePath]) {
        Write-Error "Frozen research artifact changed: $relativePath (expected $($expected[$relativePath]), actual $actual)" -ErrorAction Continue
        $failed = $true
    }
    else {
        Write-Host "OK  $relativePath"
    }
}

if ($failed) { exit 1 }
Write-Host 'Research 6 frozen artifacts verified.'
