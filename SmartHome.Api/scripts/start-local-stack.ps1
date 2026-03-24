param(
    [string]$ApiProjectPath = "C:\Users\Nhat\source\repos\SmartHome.Api\SmartHome.Api",
    [string]$GatewayPath = "C:\Users\Nhat\Downloads\New WinRAR archive\alertfire\iot-gateway",
    [string]$AlertFirePath = "C:\Users\Nhat\Downloads\New WinRAR archive\alertfire",
    [string]$ServiceEmail = "service@gmail.com",
    [string]$ServicePassword = "12345678",
    [string]$AlertFireIngestApiKey = "DEV_ONLY_CHANGE_ME",
    [string]$ApiBase = "http://127.0.0.1:7156",
    [string]$AlertFireBase = "http://127.0.0.1:5020",
    [switch]$StartAlertFire,
    [switch]$StartRecorder
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $ApiProjectPath)) {
    throw "Api project path not found: $ApiProjectPath"
}

if (-not (Test-Path $GatewayPath)) {
    throw "Gateway path not found: $GatewayPath"
}

if ($StartAlertFire -and -not (Test-Path $AlertFirePath)) {
    throw "AlertFire path not found: $AlertFirePath"
}

if ([string]::IsNullOrWhiteSpace($ServiceEmail) -or [string]::IsNullOrWhiteSpace($ServicePassword)) {
    throw "ServiceEmail and ServicePassword are required."
}

$runtimeDir = Join-Path $ApiProjectPath ".runtime"
if (-not (Test-Path $runtimeDir)) {
    New-Item -ItemType Directory -Path $runtimeDir | Out-Null
}

$stateFile = Join-Path $runtimeDir "local-stack.json"
if (Test-Path $stateFile) {
    throw "Stack appears to be running already. Run scripts\\stop-local-stack.ps1 first."
}

$env:BootstrapServiceAccount__Name = "Alert Camera Service"
$env:BootstrapServiceAccount__Email = $ServiceEmail
$env:BootstrapServiceAccount__Password = $ServicePassword
$env:ASPNETCORE_URLS = "http://0.0.0.0:7156"
$env:SMART_HOME_SERVICE_EMAIL = $ServiceEmail
$env:SMART_HOME_SERVICE_PASSWORD = $ServicePassword

function Test-PortFree([int]$Port) {
    try {
        $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Any, $Port)
        $listener.Start()
        $listener.Stop()
        return $true
    } catch {
        return $false
    }
}

# Avoid MQTT port conflicts (1883 is commonly used by other local brokers/tools).
if (-not $env:MQTT_PORT) {
    foreach ($p in 1883..1893) {
        if (Test-PortFree -Port $p) {
            $env:MQTT_PORT = "$p"
            break
        }
    }
}
if (-not $env:MQTT_PORT) {
    throw "No free MQTT port available in range 1883..1893. Set MQTT_PORT explicitly."
}

$apiOut = Join-Path $runtimeDir "smarthome-run.out.log"
$apiErr = Join-Path $runtimeDir "smarthome-run.err.log"
$api = Start-Process -FilePath "dotnet" -ArgumentList "run --no-build" -WorkingDirectory $ApiProjectPath -RedirectStandardOutput $apiOut -RedirectStandardError $apiErr -PassThru

if ($StartAlertFire) {
    $env:AlertFire__IngestApiKey = $AlertFireIngestApiKey
    $env:ASPNETCORE_URLS = "http://0.0.0.0:5020"

    # Gateway can push realtime events to alertfire (best-effort).
    $env:ALERTFIRE_REALTIME_API = "$AlertFireBase/api/mqtt"
    $env:ALERTFIRE_API_KEY = $AlertFireIngestApiKey

    $alertFireOut = Join-Path $runtimeDir "alertfire-run.out.log"
    $alertFireErr = Join-Path $runtimeDir "alertfire-run.err.log"
    $alertFire = Start-Process -FilePath "dotnet" -ArgumentList "run --no-build" -WorkingDirectory $AlertFirePath -RedirectStandardOutput $alertFireOut -RedirectStandardError $alertFireErr -PassThru
} else {
    $alertFire = $null
    $alertFireOut = $null
    $alertFireErr = $null
}

$gateway = Start-Process -FilePath "powershell" -ArgumentList "-ExecutionPolicy Bypass -File `"$GatewayPath\run-gateway-smarthome.ps1`"" -WorkingDirectory $GatewayPath -PassThru

$recorder = $null
if ($StartRecorder) {
    $recorder = Start-Process -FilePath "node" -ArgumentList "videoRecorder.js" -WorkingDirectory $GatewayPath -PassThru
}

$state = @{
    apiPid = $api.Id
    gatewayPid = $gateway.Id
    recorderPid = if ($recorder) { $recorder.Id } else { $null }
    alertFirePid = if ($alertFire) { $alertFire.Id } else { $null }
    mqttPort = [int]$env:MQTT_PORT
    apiOut = $apiOut
    apiErr = $apiErr
    alertFireOut = $alertFireOut
    alertFireErr = $alertFireErr
    apiBase = $ApiBase
    alertFireBase = $AlertFireBase
    alertFireIngestApiKey = if ($StartAlertFire) { $AlertFireIngestApiKey } else { $null }
    startedAtUtc = [DateTime]::UtcNow.ToString("o")
}

$state | ConvertTo-Json | Set-Content -Path $stateFile

Write-Host "Started local stack."
Write-Host "API PID: $($api.Id)"
if ($alertFire) {
    Write-Host "AlertFire PID: $($alertFire.Id)"
}
Write-Host "Gateway PID: $($gateway.Id)"
if ($recorder) {
    Write-Host "Recorder PID: $($recorder.Id)"
}
Write-Host "Use scripts\\stop-local-stack.ps1 to stop all."
