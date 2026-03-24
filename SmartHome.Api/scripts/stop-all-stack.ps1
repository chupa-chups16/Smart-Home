param(
    [string]$ApiProjectPath = "C:\Users\Nhat\source\repos\SmartHome.Api\SmartHome.Api"
)

$ErrorActionPreference = "Stop"

& "$PSScriptRoot\stop-local-stack.ps1" -ApiProjectPath $ApiProjectPath

