<#
.SYNOPSIS
    Builds BackpackMod locally and copies it into your Slime Rancher 2 Mods folder.

.DESCRIPTION
    Copies all Il2Cpp wrapper DLLs from your SR2 MelonLoader install into the
    repo's Libs/ folder (gitignored), then runs dotnet restore + dotnet build,
    and finally copies the resulting BackpackMod.dll into <SR2Path>\Mods\.

.PARAMETER SR2Path
    Path to the Slime Rancher 2 install root.
    Defaults to the Steam default: C:\Program Files (x86)\Steam\steamapps\common\Slime Rancher 2

.EXAMPLE
    # Build using the Steam default path
    .\scripts\build-local.ps1

.EXAMPLE
    # Build with a custom SR2 install location
    .\scripts\build-local.ps1 -SR2Path "D:\SteamLibrary\steamapps\common\Slime Rancher 2"
#>
[CmdletBinding()]
param(
    [string]$SR2Path = "C:\Program Files (x86)\Steam\steamapps\common\Slime Rancher 2"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$IL2CppAssembliesPath = Join-Path $SR2Path "MelonLoader\Il2CppAssemblies"

Write-Host ""
Write-Host "=== BackpackMod Local Build ===" -ForegroundColor Cyan
Write-Host "SR2 install  : $SR2Path"
Write-Host "Il2CppAsm    : $IL2CppAssembliesPath"
Write-Host ""

# ── Validate the Il2CppAssemblies folder exists ─────────────────────────────
if (-not (Test-Path $IL2CppAssembliesPath)) {
    Write-Host "ERROR: Il2CppAssemblies folder not found:" -ForegroundColor Red
    Write-Host "  $IL2CppAssembliesPath" -ForegroundColor Red
    Write-Host ""
    Write-Host "Make sure MelonLoader is installed for Slime Rancher 2 and that you have" -ForegroundColor Yellow
    Write-Host "launched the game at least once so MelonLoader generates the Il2Cpp assemblies." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "If your SR2 is installed in a non-default location, pass -SR2Path:" -ForegroundColor Yellow
    Write-Host '  .\scripts\build-local.ps1 -SR2Path "D:\SteamLibrary\steamapps\common\Slime Rancher 2"' -ForegroundColor Yellow
    exit 1
}

# ── Navigate to repo root ────────────────────────────────────────────────────
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
try {
    # ── Copy all Il2Cpp wrapper DLLs into Libs/ ──────────────────────────────
    $libsPath = Join-Path $repoRoot "Libs"
    if (-not (Test-Path $libsPath)) {
        New-Item -ItemType Directory -Path $libsPath | Out-Null
    }

    Write-Host "--- Copying DLLs from Il2CppAssemblies to Libs/ ---" -ForegroundColor Cyan
    $dlls = Get-ChildItem -Path $IL2CppAssembliesPath -Filter "*.dll"
    if ($dlls.Count -eq 0) {
        Write-Host "ERROR: No DLLs found in $IL2CppAssembliesPath" -ForegroundColor Red
        exit 1
    }
    Copy-Item -Path (Join-Path $IL2CppAssembliesPath "*.dll") -Destination $libsPath -Force
    Write-Host "Copied $($dlls.Count) DLL(s) to Libs/" -ForegroundColor Green
    Write-Host ""

    # ── dotnet restore ───────────────────────────────────────────────────────
    Write-Host "--- dotnet restore ---" -ForegroundColor Cyan
    dotnet restore BackpackMod.csproj
    if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed (exit $LASTEXITCODE)." }

    # ── dotnet build ─────────────────────────────────────────────────────────
    Write-Host ""
    Write-Host "--- dotnet build -c Release ---" -ForegroundColor Cyan
    dotnet build BackpackMod.csproj -c Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed (exit $LASTEXITCODE)." }

    # ── Copy to Mods folder ──────────────────────────────────────────────────
    $builtDll  = Join-Path $repoRoot "bin\Release\net6.0\BackpackMod.dll"
    $modsFolder = Join-Path $SR2Path "Mods"

    if (-not (Test-Path $builtDll)) {
        throw "Expected build output not found: $builtDll"
    }

    if (-not (Test-Path $modsFolder)) {
        Write-Host ""
        Write-Host "Creating Mods folder: $modsFolder" -ForegroundColor Yellow
        New-Item -ItemType Directory -Path $modsFolder | Out-Null
    }

    Copy-Item $builtDll $modsFolder -Force
    Write-Host ""
    Write-Host "=== Build succeeded! ===" -ForegroundColor Green
    Write-Host "Mod DLL    : $builtDll"
    Write-Host "Installed  : $(Join-Path $modsFolder 'BackpackMod.dll')"
    Write-Host ""
    Write-Host "Launch Slime Rancher 2 and press B to open your backpack." -ForegroundColor Cyan
}
finally {
    Pop-Location
}

