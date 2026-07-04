$repoRoot = Split-Path -Parent $PSScriptRoot
$nodeInfo = & (Join-Path $PSScriptRoot "Get-NodePath.ps1")

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Host "Using Node: $($nodeInfo.Node)"
Write-Host "Using npm: $($nodeInfo.Npm)"

Set-Location (Join-Path $repoRoot "Frontend\agentic-knowledge-ui")

& $nodeInfo.Npm install
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& $nodeInfo.Npm run dev
