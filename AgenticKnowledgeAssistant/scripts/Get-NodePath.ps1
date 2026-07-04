$repoRoot = Split-Path -Parent $PSScriptRoot
$localNode = Get-ChildItem -Path (Join-Path $repoRoot ".tools") -Directory -Filter "node-*-win-x64" -ErrorAction SilentlyContinue |
    Sort-Object Name -Descending |
    Select-Object -First 1

if ($localNode) {
    $env:Path = "$($localNode.FullName);$env:Path"
}

$node = Get-Command node.exe -ErrorAction SilentlyContinue
$npm = Get-Command npm.cmd -ErrorAction SilentlyContinue

if (-not $node -or -not $npm) {
    Write-Error "Node.js/npm were not found. Install Node.js LTS or place a portable Node build under .tools."
    exit 1
}

[PSCustomObject]@{
    Node = $node.Source
    Npm = $npm.Source
}
