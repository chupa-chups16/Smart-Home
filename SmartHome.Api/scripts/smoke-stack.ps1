param(
    [string]$ApiBase = "http://127.0.0.1:7156",
    [string]$AlertFireBase = "http://127.0.0.1:5020",
    [string]$GatewayPath = "C:\Users\Nhat\Downloads\New WinRAR archive\alertfire\iot-gateway",
    [string]$ServiceEmail,
    [string]$ServicePassword,
    [string]$AlertFireIngestApiKey = "DEV_ONLY_CHANGE_ME",
    [int]$MqttPort = 0
)

$ErrorActionPreference = "Stop"

function StepOk($name) {
    Write-Host "[PASS] $name"
}

function WaitHealth($url, $timeoutSeconds = 30) {
    $deadline = (Get-Date).AddSeconds($timeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            Invoke-RestMethod -Uri "$url/health" -Method Get -TimeoutSec 2 | Out-Null
            return $true
        } catch {
            Start-Sleep -Milliseconds 500
        }
    }
    return $false
}

if (-not (WaitHealth -url $ApiBase)) {
    throw "SmartHome.Api health check failed at $ApiBase/health"
}
StepOk "SmartHome.Api health"

if (-not (WaitHealth -url $AlertFireBase)) {
    throw "AlertFire health check failed at $AlertFireBase/health"
}
StepOk "AlertFire health"

if (-not (Test-Path $GatewayPath)) {
    throw "Gateway path not found: $GatewayPath"
}

if ([string]::IsNullOrWhiteSpace($ServiceEmail) -or [string]::IsNullOrWhiteSpace($ServicePassword)) {
    throw "ServiceEmail and ServicePassword are required."
}

if ($MqttPort -le 0) {
    if ($env:MQTT_PORT) {
        $MqttPort = [int]$env:MQTT_PORT
    } else {
        $MqttPort = 1883
    }
}

# Create a temp owner user for Home/Room/Device ownership
$suffix = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
$userEmail = "smoke-$suffix@test.local"
$userPassword = "User12345"

Invoke-RestMethod -Uri "$ApiBase/api/auth/register" -Method Post -ContentType "application/json" -Body (@{
        name = "Smoke User"
        email = $userEmail
        password = $userPassword
    } | ConvertTo-Json) | Out-Null
StepOk "Register temp user"

$userLogin = Invoke-RestMethod -Uri "$ApiBase/api/auth/login" -Method Post -ContentType "application/json" -Body (@{
        email = $userEmail
        password = $userPassword
    } | ConvertTo-Json)
if (-not $userLogin.token) {
    throw "User login returned empty token"
}
StepOk "User login"

$userHeaders = @{ Authorization = "Bearer $($userLogin.token)" }

# Create home/room/device
$homeResp = Invoke-RestMethod -Uri "$ApiBase/api/home" -Method Post -Headers $userHeaders -ContentType "application/json" -Body (@{
        name = "Smoke Home $suffix"
    } | ConvertTo-Json)
StepOk "Create home"

$roomResp = Invoke-RestMethod -Uri "$ApiBase/api/room" -Method Post -Headers $userHeaders -ContentType "application/json" -Body (@{
        homeId = $homeResp.homeId
        roomName = "Smoke Room"
        description = "Stack smoke test"
    } | ConvertTo-Json)
StepOk "Create room"

$deviceResp = Invoke-RestMethod -Uri "$ApiBase/api/devices" -Method Post -Headers $userHeaders -ContentType "application/json" -Body (@{
        roomId = $roomResp.roomId
        name = "Smoke Temp Sensor"
        type = "temperature-sensor"
        status = $true
    } | ConvertTo-Json)
StepOk "Create device"

# Publish 2 MQTT messages via gateway's node_modules (validates broker + gateway forward + fire detection).
$publish = @"
const mqtt = require('mqtt');
const deviceId = Number(process.argv[2]);
const port = Number(process.env.MQTT_PORT || process.argv[3] || 1883);
const client = mqtt.connect('mqtt://127.0.0.1:' + port);
const failTimer = setTimeout(() => {
  console.error('MQTT connect timeout');
  process.exit(1);
}, 5000);
client.on('connect', () => {
  clearTimeout(failTimer);
  client.publish('sensor', JSON.stringify({ deviceId, temperature: 50 }));
  setTimeout(() => {
    client.publish('sensor', JSON.stringify({ deviceId, temperature: 80 }));
    setTimeout(() => client.end(), 200);
  }, 1200);
});
client.on('error', (err) => {
  console.error('MQTT error:', err && err.message ? err.message : String(err));
});
"@

Push-Location $GatewayPath
$env:MQTT_PORT = "$MqttPort"
$publish | node - $deviceResp.deviceId $MqttPort | Out-Null
Pop-Location
StepOk "Publish MQTT readings"

Start-Sleep -Seconds 4

# Verify sensors were stored and fire event created for the owner.
$sensors = Invoke-RestMethod -Uri "$ApiBase/api/sensors/by-device/$($deviceResp.deviceId)" -Method Get -Headers $userHeaders
if (@($sensors).Count -lt 1) {
    throw "No sensor records found for deviceId=$($deviceResp.deviceId)"
}
StepOk "Verify sensor records"

$events = Invoke-RestMethod -Uri "$ApiBase/api/events" -Method Get -Headers $userHeaders
$fireCount = @($events | Where-Object { $_.title -like "Fire Alert*" }).Count
if ($fireCount -lt 1) {
    throw "Fire alert event not found in /api/events"
}
StepOk "Verify fire event"

# Verify alertfire ingest endpoint auth is enforced (401 when key is wrong).
try {
    Invoke-RestMethod -Uri "$AlertFireBase/api/mqtt" -Method Post -ContentType "application/json" -Headers @{ "X-AlertFire-Key" = "wrong" } -Body (@{ deviceId = $deviceResp.deviceId; temperature = 30 } | ConvertTo-Json) | Out-Null
    throw "Expected 401 from alertfire when API key is wrong"
} catch {
    if ($_.Exception.Response.StatusCode.value__ -ne 401) {
        throw "Expected 401 from alertfire when API key is wrong, got $($_.Exception.Response.StatusCode.value__)"
    }
}
StepOk "Verify alertfire API key"

[pscustomobject]@{
    userEmail = $userEmail
    homeId = $homeResp.homeId
    roomId = $roomResp.roomId
    deviceId = $deviceResp.deviceId
    sensorCount = @($sensors).Count
    fireEventCount = $fireCount
} | ConvertTo-Json -Depth 4
