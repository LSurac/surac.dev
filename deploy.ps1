param(
  [Parameter(Mandatory = $true)][string]$TargetHost,
  [string]$TargetUser = "deploy",
  [string]$RemotePath = "/opt/surac-dev",
  [string]$BackupPath = "/opt/surac-dev-backups",
  [switch]$SkipLocalBuild,
  [switch]$Production
)

# The single deploy path for humans and CI (docs/ops-and-deploy.md). Stages the working tree without
# build output and secrets, uploads it, keeps the host's .env and runs `docker compose up --build`.
# -Production adds the guards: clean tree, branch master, database dump before the deploy.
$ErrorActionPreference = "Stop"

function Invoke-Native {
  param([string]$FilePath, [string[]]$Arguments)
  & $FilePath @Arguments
  if ($LASTEXITCODE -ne 0) {
    throw "Command failed with exit code ${LASTEXITCODE}: $FilePath $($Arguments -join ' ')"
  }
}

if (-not (Test-Path "SuracDev.Api") -or -not (Test-Path "SuracDev.Web")) {
  throw "deploy.ps1 must run from the repository root."
}

$Target = "$TargetUser@$TargetHost"
$ArchiveName = "surac-dev-deploy.tar.gz"
$StagingPath = Join-Path $env:TEMP "surac-dev-deploy"
$ArchivePath = Join-Path $env:TEMP $ArchiveName

Write-Host "=== surac.dev deploy ===" -ForegroundColor Cyan
Write-Host "Target:     $Target"
Write-Host "RemotePath: $RemotePath"
Write-Host "Production: $Production"
Write-Host ""

if ($Production) {
  Write-Host "=== Production guards ===" -ForegroundColor Cyan
  $branch = (git rev-parse --abbrev-ref HEAD).Trim()
  if ($branch -ne "master") { throw "Production deploys only from master (current: $branch)." }
  if ((git status --porcelain).Length -gt 0) { throw "Working tree is not clean. Commit or stash first." }
  Write-Host "Branch master, tree clean."
}

if (-not $SkipLocalBuild) {
  Write-Host "=== Local gate ===" -ForegroundColor Cyan
  Invoke-Native "dotnet" @("build", ".\SuracDev.slnx")
  Invoke-Native "dotnet" @("test", ".\SuracDev.Api.Tests\SuracDev.Api.Tests.csproj", "--no-build")
  Push-Location ".\SuracDev.Web"
  try {
    Invoke-Native "npm" @("run", "lint")
    Invoke-Native "npm" @("run", "test:ci")
    Invoke-Native "npm" @("run", "build")
  }
  finally {
    Pop-Location
  }
}

Write-Host ""
Write-Host "=== Staging the working tree ===" -ForegroundColor Cyan
if (Test-Path $StagingPath) { Remove-Item -Recurse -Force $StagingPath }
if (Test-Path $ArchivePath) { Remove-Item -Force $ArchivePath }
New-Item -ItemType Directory -Force -Path $StagingPath | Out-Null

robocopy . $StagingPath /MIR /XD `
  .git .vs .vscode .claude .githooks node_modules dist .angular out-tsc bin obj TestResults coverage e2e-results e2e-report docs `
  /XF .env appsettings.Development.json npm-debug.log *.code-workspace | Out-Null
if ($LASTEXITCODE -gt 7) { throw "Robocopy failed with exit code $LASTEXITCODE" }

Invoke-Native "tar" @("-czf", $ArchivePath, "-C", $StagingPath, ".")

Write-Host ""
Write-Host "=== Remote preparation ===" -ForegroundColor Cyan
Invoke-Native "ssh" @(
  $Target,
  "if [ ! -d '$RemotePath' ] || [ ! -w '$RemotePath' ]; then sudo mkdir -p '$RemotePath' && sudo chown -R '$TargetUser':'$TargetUser' '$RemotePath'; fi; if [ ! -f '$RemotePath/.env' ]; then echo 'ERROR: $RemotePath/.env is missing. Create it from .env.example first.' >&2; exit 1; fi"
)

if ($Production) {
  Write-Host ""
  Write-Host "=== Database dump before the deploy ===" -ForegroundColor Cyan
  Invoke-Native "ssh" @(
    $Target,
    "set -e; mkdir -p '$BackupPath/pre-deploy'; cd '$RemotePath'; if docker compose ps --status running surac-dev-db >/dev/null 2>&1 && [ -n `"`$(docker compose ps -q surac-dev-db)`" ]; then docker compose exec -T surac-dev-db pg_dump -Fc -U suracdev suracdev > '$BackupPath/pre-deploy/suracdev-'`$(date +%Y%m%d-%H%M%S)'.dump'; echo 'dump written'; else echo 'no running database yet, skipping dump'; fi"
  )
}

Write-Host ""
Write-Host "=== Upload ===" -ForegroundColor Cyan
Invoke-Native "scp" @($ArchivePath, "$Target`:/tmp/$ArchiveName")

Write-Host ""
Write-Host "=== Remote deploy ===" -ForegroundColor Cyan
# gzip -t first: the stack folder is emptied only once the uploaded archive is there and intact
# (2026-11-09 a missing archive left production with nothing but its .env; the running containers kept serving).
Invoke-Native "ssh" @(
  $Target,
  "set -e; gzip -t '/tmp/$ArchiveName'; find '$RemotePath' -mindepth 1 -maxdepth 1 ! -name '.env' -exec rm -rf {} +; tar -xzf '/tmp/$ArchiveName' -C '$RemotePath'; rm -f '/tmp/$ArchiveName'; cd '$RemotePath'; docker compose up -d --build --wait; docker compose ps"
)

Write-Host ""
Write-Host "=== Done ===" -ForegroundColor Green
