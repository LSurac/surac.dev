param(
  [string]$Grep = ""
)

# Runs the Playwright suite against a throwaway PostgreSQL container, the API and the production
# SSR build (docs/testing.md). Needs Docker, the .NET SDK and `npx playwright install chromium`.
$ErrorActionPreference = "Stop"
$webRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $webRoot
$containerName = "surac-dev-e2e-postgres"
$port = 55432

function Invoke-Native {
  param([string]$FilePath, [string[]]$Arguments, [string]$WorkingDirectory = $webRoot)
  Push-Location $WorkingDirectory
  try {
    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Command failed with exit code ${LASTEXITCODE}: $FilePath $($Arguments -join ' ')" }
  }
  finally { Pop-Location }
}

docker rm -f $containerName 2>$null | Out-Null
Invoke-Native "docker" @("run", "-d", "--name", $containerName, "-e", "POSTGRES_USER=e2e", "-e", "POSTGRES_PASSWORD=e2e", "-e", "POSTGRES_DB=suracdev_e2e", "-p", "${port}:5432", "postgres:17-alpine")

try {
  $deadline = (Get-Date).AddSeconds(60)
  do {
    Start-Sleep -Seconds 1
    docker exec $containerName pg_isready -U e2e -d suracdev_e2e 2>$null | Out-Null
    $ready = ($LASTEXITCODE -eq 0)
  } until ($ready -or (Get-Date) -gt $deadline)
  if (-not $ready) { throw "PostgreSQL did not become ready." }

  Invoke-Native "dotnet" @("build", "SuracDev.slnx", "--configuration", "Debug") $repoRoot
  Invoke-Native "npm" @("run", "build")

  $env:E2E_CONNECTION_STRING = "Host=localhost;Port=$port;Database=suracdev_e2e;Username=e2e;Password=e2e"
  $playwrightArgs = @("playwright", "test")
  if ($Grep) { $playwrightArgs += @("--grep", $Grep) }
  Invoke-Native "npx" $playwrightArgs
}
finally {
  docker rm -f $containerName 2>$null | Out-Null
  Remove-Item Env:E2E_CONNECTION_STRING -ErrorAction SilentlyContinue
}
