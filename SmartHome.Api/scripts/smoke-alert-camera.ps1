param(
    [string]$ApiBase = "http://127.0.0.1:7156",
    [string]$ServiceEmail,
    [string]$ServicePassword
)

$ErrorActionPreference = "Stop"

function StepOk($name) {
    Write-Host "[PASS] $name"
}

if ([string]::IsNullOrWhiteSpace($ServiceEmail) -or [string]::IsNullOrWhiteSpace($ServicePassword)) {
    throw "ServiceEmail and ServicePassword are required."
}

function WaitHealth($url, $timeoutSeconds = 20) {
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
    throw "API health check failed at $ApiBase/health"
}
StepOk "API health"

# Create a temp normal user for Home/Room/Device ownership
$userEmail = "smoke-$([DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds())@test.local"
$userPassword = "User12345"

$regBody = @{ name = "Smoke User"; email = $userEmail; password = $userPassword } | ConvertTo-Json
Invoke-RestMethod -Uri "$ApiBase/api/auth/register" -Method Post -ContentType "application/json" -Body $regBody | Out-Null
StepOk "Register temp user"

$userLoginBody = @{ email = $userEmail; password = $userPassword } | ConvertTo-Json
$userLogin = Invoke-RestMethod -Uri "$ApiBase/api/auth/login" -Method Post -ContentType "application/json" -Body $userLoginBody
if (-not $userLogin.token) {
    throw "User login returned empty token"
}
StepOk "User login"

$userHeaders = @{ Authorization = "Bearer $($userLogin.token)" }

$serviceLoginBody = @{ email = $ServiceEmail; password = $ServicePassword } | ConvertTo-Json
$serviceLogin = Invoke-RestMethod -Uri "$ApiBase/api/auth/login" -Method Post -ContentType "application/json" -Body $serviceLoginBody
if (-not $serviceLogin.token) {
    throw "Service login returned empty token"
}
StepOk "Service login"

$serviceHeaders = @{ Authorization = "Bearer $($serviceLogin.token)" }
$suffix = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()

$homeBody = @{ name = "Smoke Home $suffix" } | ConvertTo-Json
$homeResp = Invoke-RestMethod -Uri "$ApiBase/api/home" -Method Post -Headers $userHeaders -ContentType "application/json" -Body $homeBody
StepOk "Create home"

$roomBody = @{ homeId = $homeResp.homeId; roomName = "Smoke Room"; description = "Local smoke test" } | ConvertTo-Json
$roomResp = Invoke-RestMethod -Uri "$ApiBase/api/room" -Method Post -Headers $userHeaders -ContentType "application/json" -Body $roomBody
StepOk "Create room"

$deviceBody = @{ roomId = $roomResp.roomId; name = "Smoke Temp Sensor"; type = "temperature-sensor"; status = $true } | ConvertTo-Json
$deviceResp = Invoke-RestMethod -Uri "$ApiBase/api/devices" -Method Post -Headers $userHeaders -ContentType "application/json" -Body $deviceBody
StepOk "Create device"

$sensorBody = @{ deviceId = $deviceResp.deviceId; value = 72.5 } | ConvertTo-Json
$sensorResp = Invoke-RestMethod -Uri "$ApiBase/api/sensors" -Method Post -Headers $serviceHeaders -ContentType "application/json" -Body $sensorBody
StepOk "Ingest sensor"

$fireBody = @{
    deviceId = $deviceResp.deviceId
    temperature = 76.3
    rate = 0.88
    detectedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
    source = "smoke-script"
    cameraFilePath = "/videos/smoke-$suffix.mp4"
} | ConvertTo-Json
$fireResp = Invoke-RestMethod -Uri "$ApiBase/api/events/fire-alert" -Method Post -Headers $serviceHeaders -ContentType "application/json" -Body $fireBody
StepOk "Ingest fire alert"

$mediaBody = @{ fileName = "smoke-$suffix.mp4"; filePath = "/videos/smoke-$suffix.mp4"; fileType = "video"; deviceId = $deviceResp.deviceId } | ConvertTo-Json
$mediaResp = Invoke-RestMethod -Uri "$ApiBase/api/mediafiles" -Method Post -Headers $serviceHeaders -ContentType "application/json" -Body $mediaBody
StepOk "Ingest media metadata"

$events = Invoke-RestMethod -Uri "$ApiBase/api/events" -Method Get -Headers $userHeaders
$found = @($events | Where-Object { $_.eventId -eq $fireResp.eventId }).Count -gt 0
if (-not $found) {
    throw "Created fire event not found in event list"
}
StepOk "Verify event list"

[pscustomobject]@{
    homeId = $homeResp.homeId
    roomId = $roomResp.roomId
    deviceId = $deviceResp.deviceId
    sensorDataId = $sensorResp.dataId
    fireEventId = $fireResp.eventId
    mediaId = $mediaResp.id
} | ConvertTo-Json -Depth 4
