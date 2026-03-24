param(
    [string]$ApiProjectPath = "C:\Users\Nhat\source\repos\SmartHome.Api\SmartHome.Api"
)

$ErrorActionPreference = "Stop"

$stateFile = Join-Path $ApiProjectPath ".runtime\local-stack.json"
if (-not (Test-Path $stateFile)) {
    Write-Host "No runtime state file found at $stateFile"
    exit 0
}

$state = Get-Content $stateFile | ConvertFrom-Json
$pids = @($state.apiPid, $state.alertFirePid, $state.gatewayPid, $state.recorderPid) | Where-Object { $_ }

foreach ($procId in $pids) {
    try {
        Stop-Process -Id $procId -Force -ErrorAction Stop
        Write-Host "Stopped PID $procId"
    } catch {
        Write-Host "PID $procId already stopped or inaccessible"
    }
}

Remove-Item $stateFile -Force
Write-Host "Local stack stopped."
