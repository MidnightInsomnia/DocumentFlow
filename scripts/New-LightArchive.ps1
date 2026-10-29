[CmdletBinding()]
param(
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path (Split-Path -Parent $projectRoot) 'documentflow-light.zip'
}
$OutputPath = [IO.Path]::GetFullPath($OutputPath)

function Get-RelativeProjectPath([string]$FullPath) {
    return $FullPath.Substring($projectRoot.Length).TrimStart([char[]]@('\', '/')).Replace('\', '/')
}

$excludedDirectoryNames = [Collections.Generic.HashSet[string]]::new(
    [StringComparer]::OrdinalIgnoreCase)
foreach ($name in @(
    'bin', 'obj', '.git', '.vs', '.idea', '.vscode', 'node_modules', 'dist', 'storage',
    '.run', 'tmp', 'TestResults', 'artifacts', 'publish', 'coverage',
    'coverage-results', 'tessdata', '.ollama', 'postgres_data', '__pycache__'
)) { $excludedDirectoryNames.Add($name) | Out-Null }

function Test-Excluded([string]$FullPath) {
    $relative = Get-RelativeProjectPath $FullPath
    $segments = $relative.Split('/', [StringSplitOptions]::RemoveEmptyEntries)
    if ($segments.Count -gt 1) {
        foreach ($segment in $segments[0..($segments.Count - 2)]) {
            if ($excludedDirectoryNames.Contains($segment)) { return $true }
        }
    }
    if ($relative.StartsWith('evaluation/results/', [StringComparison]::OrdinalIgnoreCase)) { return $true }
    $name = [IO.Path]::GetFileName($relative)
    return $name -eq '.env' -or
        $name -eq '.DS_Store' -or
        $name -eq 'Thumbs.db' -or
        $name.EndsWith('.user', [StringComparison]::OrdinalIgnoreCase) -or
        $name.EndsWith('.suo', [StringComparison]::OrdinalIgnoreCase) -or
        $name.EndsWith('.zip', [StringComparison]::OrdinalIgnoreCase) -or
        $name.EndsWith('.log', [StringComparison]::OrdinalIgnoreCase) -or
        [IO.Path]::GetFullPath($FullPath) -eq $OutputPath
}

$outputDirectory = Split-Path -Parent $OutputPath
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
if (Test-Path -LiteralPath $OutputPath) {
    Remove-Item -LiteralPath $OutputPath -Force
}

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$stream = [IO.File]::Open($OutputPath, [IO.FileMode]::CreateNew)
$archive = [IO.Compression.ZipArchive]::new($stream, [IO.Compression.ZipArchiveMode]::Create)
try {
    $files = Get-ChildItem -LiteralPath $projectRoot -Recurse -File -Force |
        Where-Object { -not (Test-Excluded $_.FullName) }
    foreach ($file in $files) {
        $relative = Get-RelativeProjectPath $file.FullName
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
            $archive,
            $file.FullName,
            "documentflow/$relative",
            [IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
}
finally {
    $archive.Dispose()
    $stream.Dispose()
}

$verification = [IO.Compression.ZipFile]::OpenRead($OutputPath)
try {
    $forbidden = @($verification.Entries | Where-Object {
        $_.FullName -match '(^|/)(bin|obj|\.git|\.vs|\.idea|\.vscode|node_modules|dist|storage|\.run|tmp|TestResults|artifacts|publish|coverage|coverage-results|tessdata|\.ollama|postgres_data|__pycache__)(/|$)' -or
        $_.FullName -match '^documentflow/evaluation/results/' -or
        $_.FullName -match '(^|/)\.env$' -or
        $_.FullName -match '(^|/)(\.DS_Store|Thumbs\.db)$' -or
        $_.FullName -match '\.(zip|log|user|suo)$'
    })
    if ($forbidden.Count -ne 0) {
        throw "Archive verification failed: $($forbidden.Count) forbidden entries found."
    }
    Write-Host "Archive: $OutputPath"
    Write-Host "Entries: $($verification.Entries.Count)"
    Write-Host 'Forbidden entries: 0'
}
finally {
    $verification.Dispose()
}
