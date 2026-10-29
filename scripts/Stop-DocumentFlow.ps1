[CmdletBinding()]
param()

$ErrorActionPreference = 'Continue'
$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$statePath = Join-Path $projectRoot '.run\processes.json'

function Stop-SavedProcess(
    [string]$Name,
    [object]$ProcessId
) {
    if ($null -eq $ProcessId -or -not (Get-Process -Id $ProcessId -ErrorAction SilentlyContinue)) {
        Write-Host "$Name was already stopped."
        return
    }

    & taskkill.exe /PID $ProcessId /T /F *> $null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "$Name stopped." -ForegroundColor Green
    }
    else {
        Write-Warning "$Name process tree could not be stopped cleanly."
    }
}

Set-Location -LiteralPath $projectRoot

$state = $null
if (Test-Path -LiteralPath $statePath) {
    try {
        $state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
    }
    catch {
        Write-Warning 'The DocumentFlow process state is invalid; no processes will be stopped by PID.'
    }
}

if ($null -eq $state) {
    Write-Host 'DocumentFlow process state was not found.'
}
else {
    Stop-SavedProcess 'API' $state.ApiPid
    Stop-SavedProcess 'Worker' $state.WorkerPid
    Stop-SavedProcess 'Frontend' $state.FrontendPid
}

$envFile = if (Test-Path -LiteralPath (Join-Path $projectRoot '.env')) {
    Join-Path $projectRoot '.env'
}
else {
    Join-Path $projectRoot '.env.example'
}

& docker compose --env-file $envFile stop postgres
if ($LASTEXITCODE -eq 0) {
    Write-Host 'PostgreSQL stopped. Its Docker volume was preserved.' -ForegroundColor Green
}
else {
    Write-Warning 'PostgreSQL was already stopped or Docker Compose could not stop it.'
}

if (Test-Path -LiteralPath $statePath) {
    Remove-Item -LiteralPath $statePath -Force
}

Write-Host 'DocumentFlow stop completed.'
exit 0
