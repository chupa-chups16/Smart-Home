param(
    [string]$ApiProjectPath = "C:\Users\Nhat\source\repos\SmartHome.Api\SmartHome.Api",
    [string]$GatewayPath = "C:\Users\Nhat\Downloads\New WinRAR archive\alertfire\iot-gateway",
    [string]$AlertFirePath = "C:\Users\Nhat\Downloads\New WinRAR archive\alertfire",
    [string]$ServiceEmail = "service@gmail.com",
    [string]$ServicePassword = "12345678",
    [string]$AlertFireIngestApiKey = "DEV_ONLY_CHANGE_ME",
    [string]$ApiBase = "http://127.0.0.1:7156",
    [string]$AlertFireBase = "http://127.0.0.1:5020",
    [switch]$StartRecorder
)

$ErrorActionPreference = "Stop"

# Make start idempotent for local dev runs.
try {
    & "$PSScriptRoot\stop-local-stack.ps1" -ApiProjectPath $ApiProjectPath | Out-Null
} catch {
    # ignore
}

& "$PSScriptRoot\start-local-stack.ps1" `
    -ApiProjectPath $ApiProjectPath `
    -GatewayPath $GatewayPath `
    -AlertFirePath $AlertFirePath `
    -ServiceEmail $ServiceEmail `
    -ServicePassword $ServicePassword `
    -AlertFireIngestApiKey $AlertFireIngestApiKey `
    -ApiBase $ApiBase `
    -AlertFireBase $AlertFireBase `
    -StartAlertFire `
    -StartRecorder:$StartRecorder
