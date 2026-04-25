[CmdletBinding()]
param(
    [string]$Project = "SnakeGame/SnakeGame.csproj",
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$OutputRoot = "artifacts",
    [switch]$Package
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot $Project

if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "Project file not found: $projectPath"
}

$projectName = [System.IO.Path]::GetFileNameWithoutExtension($projectPath)
$publishDir = Join-Path $repoRoot (Join-Path $OutputRoot "publish/$Runtime")
$packageDir = Join-Path $repoRoot (Join-Path $OutputRoot "packages")
$packagePath = Join-Path $packageDir "$projectName-$Runtime.zip"

New-Item -ItemType Directory -Force -Path $publishDir | Out-Null

dotnet restore $projectPath
dotnet publish $projectPath `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    -o $publishDir

if ($Package.IsPresent) {
    New-Item -ItemType Directory -Force -Path $packageDir | Out-Null

    if (Test-Path -LiteralPath $packagePath) {
        Remove-Item -LiteralPath $packagePath -Force
    }

    Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $packagePath -Force
    Write-Host "Created package: $packagePath"
}

Write-Host "Published files: $publishDir"
