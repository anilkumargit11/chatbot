param(
    [string]$AppName = "AgenticKnowledgeAssistant.API"
)

$processes = Get-CimInstance Win32_Process |
    Where-Object {
        $_.Name -eq "$AppName.exe" -or
        ($_.Name -eq "dotnet.exe" -and $_.CommandLine -match [regex]::Escape($AppName))
    } |
    Select-Object ProcessId, Name, ExecutablePath, CommandLine

if (-not $processes) {
    Write-Host "No $AppName backend process is running."
    exit 0
}

$processes | Format-Table -AutoSize
