[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$runDirectory = Join-Path $projectRoot '.run'
$statePath = Join-Path $runDirectory 'processes.json'
$startedProcesses = [System.Collections.Generic.List[System.Diagnostics.Process]]::new()
$postgresStarted = $false
$stateCreatedByThisRun = $false

function Write-Step([string]$Message) {
    Write-Host "`n==> $Message" -ForegroundColor Cyan
}

function Require-Command(
    [string]$Name,
    [string]$ErrorMessage
) {
    $command = Get-Command $Name -ErrorAction SilentlyContinue
    if ($null -eq $command) {
        throw $ErrorMessage
    }

    return $command
}

function Read-SimpleEnv([string]$Path) {
    $values = @{}
    foreach ($line in Get-Content -LiteralPath $Path) {
        $trimmed = $line.Trim()
        if ([string]::IsNullOrWhiteSpace($trimmed) -or $trimmed.StartsWith('#')) {
            continue
        }

        $separator = $trimmed.IndexOf('=')
        if ($separator -le 0) {
            continue
        }

        $key = $trimmed.Substring(0, $separator).Trim()
        $value = $trimmed.Substring($separator + 1).Trim()
        if (-not [string]::IsNullOrWhiteSpace($key)) {
            $values[$key] = $value
        }
    }

    return $values
}

function Test-SavedProcess([object]$State) {
    foreach ($property in 'ApiPid', 'WorkerPid', 'FrontendPid') {
        $pidValue = $State.$property
        if ($null -ne $pidValue -and (Get-Process -Id $pidValue -ErrorAction SilentlyContinue)) {
            return $true
        }
    }

    return $false
}

function Convert-ToEncodedCommand([string]$Command) {
    return [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($Command))
}

function Start-DocumentFlowProcess(
    [string]$Title,
    [string]$WorkingDirectory,
    [string]$Command
) {
    $windowCommand = "`$Host.UI.RawUI.WindowTitle = '$Title'; Set-Location -LiteralPath '$($WorkingDirectory.Replace("'", "''"))'; $Command"
    $process = Start-Process powershell.exe -ArgumentList @(
        '-NoProfile',
        '-NoExit',
        '-EncodedCommand',
        (Convert-ToEncodedCommand $windowCommand)
    ) -PassThru
    $startedProcesses.Add($process)
    return $process
}

function Start-FrontendProcess([string]$WorkingDirectory) {
    $process = Start-Process cmd.exe -ArgumentList @(
        '/d',
        '/k',
        'title DocumentFlow Frontend && npm run dev -- --host 0.0.0.0'
    ) -WorkingDirectory $WorkingDirectory -PassThru
    $startedProcesses.Add($process)
    return $process
}

function Wait-ForHttp(
    [string]$Url,
    [System.Diagnostics.Process]$Process,
    [string]$Name
) {
    Add-Type -AssemblyName System.Net.Http
    $waitTimer = [Diagnostics.Stopwatch]::StartNew()
    $httpHandler = [System.Net.Http.HttpClientHandler]::new()
    $httpHandler.UseProxy = $false
    $httpClient = [System.Net.Http.HttpClient]::new($httpHandler)
    $httpClient.Timeout = [TimeSpan]::FromSeconds(1)
    try {
        while ($waitTimer.Elapsed.TotalSeconds -lt 30) {
            if ($Process.HasExited) {
                throw "$Name process exited before becoming ready."
            }

            try {
                $response = $httpClient.GetAsync($Url).GetAwaiter().GetResult()
                if ([int]$response.StatusCode -eq 200) {
                    $response.Dispose()
                    return
                }
                $response.Dispose()
            }
            catch {
                # The service is still starting.
            }

            Start-Sleep -Milliseconds 500
        }
    }
    finally {
        $httpClient.Dispose()
    }

    throw "$Name did not become ready within 30 seconds."
}

function Stop-StartedProcesses {
    foreach ($process in $startedProcesses) {
        if (-not $process.HasExited) {
            & taskkill.exe /PID $process.Id /T /F *> $null
        }
    }
}

try {
    Set-Location -LiteralPath $projectRoot

    if (Test-Path -LiteralPath $statePath) {
        try {
            $existingState = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
        }
        catch {
            $existingState = $null
        }

        if ($null -ne $existingState -and (Test-SavedProcess $existingState)) {
            throw "DocumentFlow appears to be already running.`nUse STOP.bat before starting it again."
        }

        Write-Host 'Removing stale DocumentFlow process state.' -ForegroundColor Yellow
        Remove-Item -LiteralPath $statePath -Force
    }

    Write-Step 'Checking required tools'
    $dockerCommand = Require-Command 'docker' 'docker was not found. Install Docker Desktop.'
    $dotnetCommand = Require-Command 'dotnet' 'dotnet was not found. Install .NET 10 SDK.'
    $npmCommand = Require-Command 'npm' 'npm was not found. Install Node.js.'

    & $dotnetCommand.Source --version
    if ($LASTEXITCODE -ne 0) { throw 'dotnet --version failed.' }
    & $dockerCommand.Source --version
    if ($LASTEXITCODE -ne 0) { throw 'docker --version failed.' }
    & $npmCommand.Source --version
    if ($LASTEXITCODE -ne 0) { throw 'npm --version failed.' }

    & $dockerCommand.Source info *> $null
    if ($LASTEXITCODE -ne 0) {
        throw "Docker is installed but Docker Engine is not running.`nStart Docker Desktop and run RUN.bat again."
    }

    & $dotnetCommand.Source ef --version *> $null
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet-ef is required.`nInstall it with:`ndotnet tool install --global dotnet-ef"
    }

    $envPath = Join-Path $projectRoot '.env'
    if (-not (Test-Path -LiteralPath $envPath)) {
        Copy-Item -LiteralPath (Join-Path $projectRoot '.env.example') -Destination $envPath
        Write-Host '.env was created from .env.example.' -ForegroundColor Green
    }

    $settings = Read-SimpleEnv $envPath
    foreach ($requiredKey in 'POSTGRES_DB', 'POSTGRES_USER', 'POSTGRES_PASSWORD', 'POSTGRES_PORT') {
        if (-not $settings.ContainsKey($requiredKey) -or [string]::IsNullOrWhiteSpace($settings[$requiredKey])) {
            throw "$requiredKey is missing from .env."
        }
    }

    $storagePath = [System.IO.Path]::GetFullPath((Join-Path $projectRoot 'storage\documents'))
    New-Item -ItemType Directory -Force -Path $storagePath | Out-Null

    $env:POSTGRES_DB = $settings['POSTGRES_DB']
    $env:POSTGRES_USER = $settings['POSTGRES_USER']
    $env:POSTGRES_PASSWORD = $settings['POSTGRES_PASSWORD']
    $env:POSTGRES_PORT = $settings['POSTGRES_PORT']
    $env:ConnectionStrings__Postgres = "Host=localhost;Port=$($settings['POSTGRES_PORT']);Database=$($settings['POSTGRES_DB']);Username=$($settings['POSTGRES_USER']);Password=$($settings['POSTGRES_PASSWORD'])"
    $env:DocumentStorage__RootPath = $storagePath

    # Inherit auth settings in API without ever writing credentials to console.
    foreach ($authSetting in @(
        'Auth__Jwt__Issuer',
        'Auth__Jwt__Audience',
        'Auth__Jwt__SigningKey',
        'Auth__Jwt__LifetimeMinutes',
        'Auth__BootstrapAdmin__UserName',
        'Auth__BootstrapAdmin__Password',
        'Auth__BootstrapAdmin__DisplayName',
        'Auth__BootstrapAdmin__Email'
    )) {
        if ($settings.ContainsKey($authSetting) -and
            -not [string]::IsNullOrWhiteSpace($settings[$authSetting])) {
            Set-Item -Path "Env:$authSetting" -Value $settings[$authSetting]
        }
    }
    if ([string]::IsNullOrWhiteSpace($env:Auth__Jwt__SigningKey) -or
        [Text.Encoding]::UTF8.GetByteCount($env:Auth__Jwt__SigningKey) -lt 32) {
        throw 'Configure Auth__Jwt__SigningKey in .env or environment (at least 32 UTF-8 bytes).'
    }
    foreach ($requiredAuth in 'Auth__Jwt__Issuer', 'Auth__Jwt__Audience', 'Auth__Jwt__LifetimeMinutes') {
        if ([string]::IsNullOrWhiteSpace((Get-Item -Path "Env:$requiredAuth" -ErrorAction SilentlyContinue).Value)) {
            throw "Configure $requiredAuth in .env or environment."
        }
    }

    # Optional routing overrides are inherited by both API and Worker.
    # Appsettings supply defaults when these variables are absent.
    foreach ($routingSetting in @('Routing__Invoice', 'Routing__Act', 'Routing__Contract', 'Routing__Unknown')) {
        if ($settings.ContainsKey($routingSetting)) {
            Set-Item -Path "Env:$routingSetting" -Value $settings[$routingSetting]
        }
    }

    # Optional AI review settings are inherited by the API process. They remain
    # optional: neither Ollama nor a model is required for application startup.
    foreach ($optionalSetting in @(
        'AiReview__Enabled',
        'Ollama__BaseUrl',
        'Ollama__Model',
        'Ollama__TimeoutSeconds',
        'Ollama__Temperature',
        'Ollama__Seed'
    )) {
        if ($settings.ContainsKey($optionalSetting) -and
            -not [string]::IsNullOrWhiteSpace($settings[$optionalSetting])) {
            Set-Item -Path "Env:$optionalSetting" -Value $settings[$optionalSetting]
        }
    }

    $aiReviewEnabled = $false
    if ($settings.ContainsKey('AiReview__Enabled')) {
        [bool]::TryParse($settings['AiReview__Enabled'], [ref]$aiReviewEnabled) | Out-Null
    }

    $postgresPort = 0
    if (-not [int]::TryParse($settings['POSTGRES_PORT'], [ref]$postgresPort) -or
        $postgresPort -lt 1 -or $postgresPort -gt 65535) {
        throw 'POSTGRES_PORT must be a number between 1 and 65535.'
    }

    $conflictingListeners = @(Get-NetTCPConnection -LocalPort $postgresPort -State Listen -ErrorAction SilentlyContinue |
        Where-Object {
            $owner = Get-Process -Id $_.OwningProcess -ErrorAction SilentlyContinue
            $null -ne $owner -and
            $owner.ProcessName -notin @('com.docker.backend', 'wslrelay')
        })
    if ($conflictingListeners.Count -gt 0) {
        throw "Port $postgresPort is unavailable.`nChange POSTGRES_PORT in .env, for example to 15432,`nthen run RUN.bat again."
    }

    Write-Step 'Checking optional Tesseract OCR'
    $tesseractSetting = if ($settings.ContainsKey('Tesseract__ExecutablePath')) {
        $settings['Tesseract__ExecutablePath']
    } elseif (-not [string]::IsNullOrWhiteSpace($env:Tesseract__ExecutablePath)) {
        $env:Tesseract__ExecutablePath
    } else {
        'tesseract'
    }
    $env:Tesseract__ExecutablePath = $tesseractSetting
    $tesseractCommand = Get-Command $tesseractSetting -ErrorAction SilentlyContinue
    $ocrStatus = 'unavailable'
    if ($null -ne $tesseractCommand) {
        & $tesseractCommand.Source --version *> $null
        $languages = @(& $tesseractCommand.Source --list-langs 2>$null)
        if ($LASTEXITCODE -eq 0 -and $languages -contains 'rus' -and $languages -contains 'eng') {
            $ocrStatus = 'available (rus+eng)'
            Write-Host 'Tesseract OCR: available (rus+eng).' -ForegroundColor Green
        }
        else {
            Write-Warning "Tesseract is installed, but rus and eng language data were not both found.`nPDF files with a text layer will work, but image/scanned-document OCR may fail."
        }
    }
    else {
        Write-Warning "Tesseract OCR is unavailable.`nPDF files with a text layer will work, but image/scanned-document OCR will fail."
    }

    Write-Step 'Starting PostgreSQL'
    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $composeOutput = @(& $dockerCommand.Source compose up -d postgres 2>&1)
    $composeExitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorActionPreference
    if ($composeExitCode -ne 0) {
        $composeText = $composeOutput -join [Environment]::NewLine
        if ($composeText -match 'port is already allocated|address already in use|ports are not available|bind:') {
            throw "Port $($settings['POSTGRES_PORT']) is unavailable.`nChange POSTGRES_PORT in .env, for example to 15432,`nthen run RUN.bat again."
        }
        throw "docker compose up -d postgres failed.`n$composeText"
    }
    $postgresStarted = $true
    $composeOutput | Write-Host

    Write-Host "PostgreSQL: localhost:$($settings['POSTGRES_PORT']) / $($settings['POSTGRES_DB'])"
    $postgresReady = $false
    for ($attempt = 1; $attempt -le 30; $attempt++) {
        & $dockerCommand.Source compose exec -T postgres pg_isready -U $settings['POSTGRES_USER'] -d $settings['POSTGRES_DB'] *> $null
        if ($LASTEXITCODE -eq 0) {
            $postgresReady = $true
            break
        }
        Start-Sleep -Seconds 1
    }
    if (-not $postgresReady) {
        throw 'PostgreSQL did not become ready within 30 seconds.'
    }
    Write-Host 'PostgreSQL is ready.' -ForegroundColor Green

    Write-Step 'Applying database migrations'
    & $dotnetCommand.Source ef database update --project src/DocumentFlow.Infrastructure --startup-project src/DocumentFlow.Api
    if ($LASTEXITCODE -ne 0) { throw "Database migration failed with exit code $LASTEXITCODE." }

    Write-Step 'Building backend'
    & $dotnetCommand.Source build
    if ($LASTEXITCODE -ne 0) { throw "Backend build failed with exit code $LASTEXITCODE." }

    $frontendPath = Join-Path $projectRoot 'frontend\documentflow-web'
    if (-not (Test-Path -LiteralPath (Join-Path $frontendPath 'node_modules'))) {
        Write-Step 'Installing frontend dependencies'
        Push-Location $frontendPath
        try {
            & $npmCommand.Source ci
            if ($LASTEXITCODE -ne 0) { throw "npm ci failed with exit code $LASTEXITCODE." }
        }
        finally {
            Pop-Location
        }
    }

    New-Item -ItemType Directory -Force -Path $runDirectory | Out-Null

    Write-Step 'Starting API, Worker, and frontend'
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $apiProcess = Start-DocumentFlowProcess 'DocumentFlow API' $projectRoot '& dotnet run --no-build --project src/DocumentFlow.Api --launch-profile http'

    $env:DOTNET_ENVIRONMENT = 'Development'
    $workerProcess = Start-DocumentFlowProcess 'DocumentFlow Worker' $projectRoot '& dotnet run --no-build --project src/DocumentFlow.Worker'

    if ($settings.ContainsKey('VITE_API_BASE_URL') -and
        -not [string]::IsNullOrWhiteSpace($settings['VITE_API_BASE_URL'])) {
        $env:VITE_API_BASE_URL = $settings['VITE_API_BASE_URL']
    }
    elseif ([string]::IsNullOrWhiteSpace($env:VITE_API_BASE_URL)) {
        $env:VITE_API_BASE_URL = 'http://localhost:5214'
    }
    $frontendProcess = Start-FrontendProcess $frontendPath

    [ordered]@{
        ApiPid = $apiProcess.Id
        WorkerPid = $workerProcess.Id
        FrontendPid = $frontendProcess.Id
        StartedAt = [DateTime]::UtcNow.ToString('O')
    } | ConvertTo-Json | Set-Content -LiteralPath $statePath -Encoding UTF8
    $stateCreatedByThisRun = $true

    Write-Step 'Waiting for services'
    Wait-ForHttp 'http://127.0.0.1:5214/health/ready' $apiProcess 'API'
    Wait-ForHttp 'http://127.0.0.1:5173/documents' $frontendProcess 'Frontend'

    Start-Process 'http://localhost:5173/documents'

    Write-Host "`nDocumentFlow is running." -ForegroundColor Green
    Write-Host "`nPostgreSQL:`nlocalhost:$($settings['POSTGRES_PORT'])"
    Write-Host "`nAPI:`nhttp://localhost:5214"
    Write-Host "`nFrontend:`nhttp://localhost:5173"
    Write-Host "`nHealth:`nhttp://localhost:5214/health"
    Write-Host "`nStorage:`n$storagePath"
    Write-Host "`nOCR:`n$ocrStatus"
    Write-Host "`nAI-assisted review:`n$(if ($aiReviewEnabled) { 'enabled' } else { 'disabled' })"
    if ($aiReviewEnabled) {
        $ollamaEndpoint = if ($settings.ContainsKey('Ollama__BaseUrl') -and
            -not [string]::IsNullOrWhiteSpace($settings['Ollama__BaseUrl'])) {
            $settings['Ollama__BaseUrl']
        } else {
            'http://localhost:11434'
        }
        Write-Host "`nOllama endpoint:`n$ollamaEndpoint"
    }
    Write-Host "`nUse STOP.bat to stop DocumentFlow."
    exit 0
}
catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Stop-StartedProcesses
    if ($stateCreatedByThisRun -and (Test-Path -LiteralPath $statePath)) {
        Remove-Item -LiteralPath $statePath -Force
    }
    if ($postgresStarted) {
        Write-Warning 'PostgreSQL may still be running. Use STOP.bat to stop it without deleting data.'
    }
    exit 1
}
