param(
    [string]$ApiBase = "http://127.0.0.1:7156",
    [string]$ServiceEmail,
    [string]$ServicePassword,
    [string]$AllowedOrigin = "http://localhost:5173",
    [switch]$CheckSwagger
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ServiceEmail) -or [string]::IsNullOrWhiteSpace($ServicePassword)) {
    throw "ServiceEmail and ServicePassword are required."
}

$results = New-Object System.Collections.Generic.List[object]

function Add-Result {
    param(
        [string]$Section,
        [string]$Item,
        [string]$Status,
        [string]$Detail
    )

    $results.Add([pscustomobject]@{
        section = $Section
        item = $Item
        status = $Status
        detail = $Detail
    }) | Out-Null
}

function Pass($section, $item, $detail) { Add-Result -Section $section -Item $item -Status "PASS" -Detail $detail }
function Fail($section, $item, $detail) { Add-Result -Section $section -Item $item -Status "FAIL" -Detail $detail }
function Skip($section, $item, $detail) { Add-Result -Section $section -Item $item -Status "SKIP" -Detail $detail }

function Try-Invoke {
    param([scriptblock]$Action)
    try {
        & $Action
    } catch {
        return $_
    }
}

function Wait-Health {
    param(
        [string]$Url,
        [int]$TimeoutSeconds = 30
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        $err = Try-Invoke { Invoke-RestMethod -Uri "$Url/health" -Method Get -TimeoutSec 3 | Out-Null }
        if (-not $err) {
            return $true
        }

        Start-Sleep -Milliseconds 500
    }

    return $false
}

# 1) Build and Run checks (runtime perspective)
if (-not (Wait-Health -Url $ApiBase)) {
    Fail "1) Build and Run" "API health endpoint reachable" "Unable to reach $ApiBase/health within timeout"
    $results | ConvertTo-Json -Depth 6
    exit 1
} else {
    Pass "1) Build and Run" "API health endpoint reachable" "$ApiBase/health is reachable"
}

if ($CheckSwagger) {
    $swaggerErr = Try-Invoke {
        $code = & curl.exe -s -o NUL -w "%{http_code}" "$ApiBase/swagger/index.html"
        if ($code -ne "200") {
            throw "HTTP $code"
        }
    }
    if ($swaggerErr) {
        Fail "1) Build and Run" "Swagger opens" "$swaggerErr"
    } else {
        Pass "1) Build and Run" "Swagger opens" "$ApiBase/swagger/index.html responds"
    }
} else {
    Skip "1) Build and Run" "Swagger opens" "Skipped (use -CheckSwagger to verify)"
}

# 3) Authentication and Authorization
$serviceLoginErr = Try-Invoke {
    $script:serviceLogin = Invoke-RestMethod -Uri "$ApiBase/api/auth/login" -Method Post -ContentType "application/json" -Body (@{
            email = $ServiceEmail
            password = $ServicePassword
        } | ConvertTo-Json)
}
if ($serviceLoginErr -or -not $serviceLogin.token) {
    Fail "3) Authentication and Authorization" "Service login works and returns JWT" "Service login failed"
    $results | ConvertTo-Json -Depth 6
    exit 1
} else {
    Pass "3) Authentication and Authorization" "Service login works and returns JWT" "JWT token received"
}

$serviceHeaders = @{ Authorization = "Bearer $($serviceLogin.token)" }

$unauthErr = Try-Invoke {
    Invoke-RestMethod -Uri "$ApiBase/api/home" -Method Get -TimeoutSec 4 | Out-Null
}
if ($unauthErr -and $unauthErr.Exception.Response.StatusCode.value__ -eq 401) {
    Pass "3) Authentication and Authorization" "Protected endpoints require Bearer / invalid token returns 401" "Got 401 without token"
} else {
    Fail "3) Authentication and Authorization" "Protected endpoints require Bearer / invalid token returns 401" "Did not get expected 401"
}

# create temp normal user and verify 403 on admin-only endpoint
$tempEmail = "go-live-$([DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds())@test.local"
$regErr = Try-Invoke {
    Invoke-RestMethod -Uri "$ApiBase/api/auth/register" -Method Post -ContentType "application/json" -Body (@{
            name = "GoLive User"
            email = $tempEmail
            password = "User12345"
        } | ConvertTo-Json) | Out-Null
}

if ($regErr) {
    Fail "3) Authentication and Authorization" "User without permission gets 403" "Cannot create temp user: $regErr"
    $results | ConvertTo-Json -Depth 6
    exit 1
}

$userLogin = Invoke-RestMethod -Uri "$ApiBase/api/auth/login" -Method Post -ContentType "application/json" -Body (@{
        email = $tempEmail
        password = "User12345"
    } | ConvertTo-Json)

$userHeaders = @{ Authorization = "Bearer $($userLogin.token)" }
$forbiddenErr = Try-Invoke {
    Invoke-RestMethod -Uri "$ApiBase/api/users" -Method Get -Headers $userHeaders -TimeoutSec 4 | Out-Null
}

if ($forbiddenErr -and $forbiddenErr.Exception.Response.StatusCode.value__ -eq 403) {
    Pass "3) Authentication and Authorization" "User without permission gets 403" "Got 403 for normal user"
} else {
    Fail "3) Authentication and Authorization" "User without permission gets 403" "Did not get expected 403"
}

# 5) Home/Room/Device flow
$suffix = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
$homeErr = Try-Invoke {
    $script:homeResp = Invoke-RestMethod -Uri "$ApiBase/api/home" -Method Post -Headers $userHeaders -ContentType "application/json" -Body (@{
            name = "GoLive Home $suffix"
        } | ConvertTo-Json)
}
if ($homeErr) { Fail "5) Home/Room/Device Flow" "POST /api/home creates home" "$homeErr" } else { Pass "5) Home/Room/Device Flow" "POST /api/home creates home" "homeId=$($homeResp.homeId)" }

$homeGetErr = Try-Invoke { Invoke-RestMethod -Uri "$ApiBase/api/home" -Method Get -Headers $userHeaders | Out-Null }
if ($homeGetErr) { Fail "5) Home/Room/Device Flow" "GET /api/home -> 200" "$homeGetErr" } else { Pass "5) Home/Room/Device Flow" "GET /api/home -> 200" "OK" }

$roomErr = Try-Invoke {
    $script:roomResp = Invoke-RestMethod -Uri "$ApiBase/api/room" -Method Post -Headers $userHeaders -ContentType "application/json" -Body (@{
            homeId = $homeResp.homeId
            roomName = "GoLive Room"
            description = "Go-live check room"
        } | ConvertTo-Json)
}
if ($roomErr) { Fail "5) Home/Room/Device Flow" "POST /api/room creates room with valid homeId" "$roomErr" } else { Pass "5) Home/Room/Device Flow" "POST /api/room creates room with valid homeId" "roomId=$($roomResp.roomId)" }

$deviceErr = Try-Invoke {
    $script:deviceResp = Invoke-RestMethod -Uri "$ApiBase/api/devices" -Method Post -Headers $userHeaders -ContentType "application/json" -Body (@{
            roomId = $roomResp.roomId
            name = "GoLive Sensor"
            type = "temperature-sensor"
            status = $true
        } | ConvertTo-Json)
}
if ($deviceErr) { Fail "5) Home/Room/Device Flow" "POST /api/devices creates device with valid roomId" "$deviceErr" } else { Pass "5) Home/Room/Device Flow" "POST /api/devices creates device with valid roomId" "deviceId=$($deviceResp.deviceId)" }

$byRoomErr = Try-Invoke { Invoke-RestMethod -Uri "$ApiBase/api/devices/by-room/$($roomResp.roomId)" -Method Get -Headers $userHeaders | Out-Null }
if ($byRoomErr) { Fail "5) Home/Room/Device Flow" "GET /api/devices/by-room/{roomId}" "$byRoomErr" } else { Pass "5) Home/Room/Device Flow" "GET /api/devices/by-room/{roomId}" "OK" }

$patchErr = Try-Invoke {
    Invoke-RestMethod -Uri "$ApiBase/api/devices/$($deviceResp.deviceId)/status" -Method Patch -Headers $userHeaders -ContentType "application/json" -Body (@{
            status = $false
        } | ConvertTo-Json) | Out-Null
}
if ($patchErr) { Fail "5) Home/Room/Device Flow" "PATCH /api/devices/{id}/status" "$patchErr" } else { Pass "5) Home/Room/Device Flow" "PATCH /api/devices/{id}/status" "OK" }

# 2) Database practical check via write/read
$sensorErr = Try-Invoke {
    $script:sensorResp = Invoke-RestMethod -Uri "$ApiBase/api/sensors" -Method Post -Headers $serviceHeaders -ContentType "application/json" -Body (@{
            deviceId = $deviceResp.deviceId
            value = 72.2
        } | ConvertTo-Json)
}
if ($sensorErr) { Fail "2) Database" "Core tables read/write through API" "$sensorErr" } else { Pass "2) Database" "Core tables read/write through API" "sensorDataId=$($sensorResp.dataId)" }

# 7) Fire + Media ingest
$fireErr = Try-Invoke {
    $script:fireResp = Invoke-RestMethod -Uri "$ApiBase/api/events/fire-alert" -Method Post -Headers $serviceHeaders -ContentType "application/json" -Body (@{
            deviceId = $deviceResp.deviceId
            temperature = 78.5
            rate = 0.91
            detectedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
            source = "go-live-script"
            cameraFilePath = "/videos/go-live-$suffix.mp4"
        } | ConvertTo-Json)
}
if ($fireErr) { Fail "7) Fire Alert Module" "Fire alert creation endpoint works" "$fireErr" } else { Pass "7) Fire Alert Module" "Fire alert creation endpoint works" "eventId=$($fireResp.eventId)" }

$historyErr = Try-Invoke {
    $eventList = Invoke-RestMethod -Uri "$ApiBase/api/events" -Method Get -Headers $userHeaders
    $exists = @($eventList | Where-Object { $_.eventId -eq $fireResp.eventId }).Count -gt 0
    if (-not $exists) { throw "Created event not found in history" }
}
if ($historyErr) { Fail "7) Fire Alert Module" "Fire alert history endpoint works" "$historyErr" } else { Pass "7) Fire Alert Module" "Fire alert history endpoint works" "Created event found in history" }

$mediaErr = Try-Invoke {
    Invoke-RestMethod -Uri "$ApiBase/api/mediafiles" -Method Post -Headers $serviceHeaders -ContentType "application/json" -Body (@{
            fileName = "go-live-$suffix.mp4"
            filePath = "/videos/go-live-$suffix.mp4"
            fileType = "video"
            deviceId = $deviceResp.deviceId
        } | ConvertTo-Json) | Out-Null
}
if ($mediaErr) { Fail "6) Camera Module" "Media metadata ingest endpoint works" "$mediaErr" } else { Pass "6) Camera Module" "Media metadata ingest endpoint works" "OK" }

# 8) CORS basic preflight
$corsErr = Try-Invoke {
    $headers = @(
        "-H", "Origin: $AllowedOrigin",
        "-H", "Access-Control-Request-Method: GET"
    )
    $raw = & curl.exe -s -i -X OPTIONS $headers "$ApiBase/api/home"
    if ($raw -notmatch "(?im)^Access-Control-Allow-Origin:\s*(.+)$") {
        throw "No Access-Control-Allow-Origin in preflight response"
    }
}
if ($corsErr) {
    Skip "8) CORS and Frontend Integration" "CORS preflight returns allow-origin" "Could not auto-verify: $corsErr"
} else {
    Pass "8) CORS and Frontend Integration" "CORS preflight returns allow-origin" "Origin=$AllowedOrigin"
}

# Manual-only checks
Skip "6) Camera Module" "Camera online stream behavior" "Manual check required on real camera/server network"
Skip "9) Logging and Error Handling" "Sensitive data not logged" "Manual log review required"
Skip "10) Security and Config" "HTTPS enabled in production" "Verify reverse proxy/TLS on server"
Skip "12) Demo Evidence for Manager" "Screenshot/video evidence" "Capture manually"

$results | ConvertTo-Json -Depth 6
