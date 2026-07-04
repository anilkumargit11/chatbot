param(
    [string]$ProjectPath = "src\AgenticKnowledgeAssistant.API\AgenticKnowledgeAssistant.API.csproj",
    [string]$AppName = "AgenticKnowledgeAssistant.API"
)

$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

$processes = Get-CimInstance Win32_Process |
    Where-Object {
        $_.Name -eq "$AppName.exe" -or
        ($_.Name -eq "dotnet.exe" -and $_.CommandLine -match [regex]::Escape($AppName))
    }

if ($processes) {
    $processes | Select-Object ProcessId, Name, ExecutablePath, CommandLine | Format-Table -AutoSize
    foreach ($process in $processes) {
        Stop-Process -Id $process.ProcessId -Force
    }
    Write-Host "Stopped $($processes.Count) $AppName backend process(es)."
}
else {
    Write-Host "No $AppName backend process was running."
}

dotnet restore $ProjectPath
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet build $ProjectPath --no-restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet run --project $ProjectPath --no-build
