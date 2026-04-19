<#
.SYNOPSIS
    Builds BackpackMod locally and copies it into your Slime Rancher 2 Mods folder.

.DESCRIPTION
    Validates that the required MelonLoader / SR2 DLLs are present, runs
    dotnet restore + dotnet build, and then copies the resulting
    BackpackMod.dll into <SR2Path>\Mods\.

.PARAMETER SR2Path
    Path to the Slime Rancher 2 install root.
    Defaults to the Steam default: C:\Program Files (x86)\Steam\steamapps\common\Slime Rancher 2

.PARAMETER IL2CppAssembliesPath
    Path to the Il2CppAssemblies folder.
    Defaults to <SR2Path>\MelonLoader\Il2CppAssemblies.

.EXAMPLE
    # Build using the Steam default path
    .\scripts\build-local.ps1

.EXAMPLE
    # Build with a custom SR2 install location
    .\scripts\build-local.ps1 -SR2Path "D:\SteamLibrary\steamapps\common\Slime Rancher 2"
#>
[CmdletBinding()]
param(
    [string]$SR2Path = "C:\Program Files (x86)\Steam\steamapps\common\Slime Rancher 2",
    [string]$IL2CppAssembliesPath = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Resolve IL2CppAssembliesPath default
if ([string]::IsNullOrEmpty($IL2CppAssembliesPath)) {
    $IL2CppAssembliesPath = Join-Path $SR2Path "MelonLoader\Il2CppAssemblies"
}

$MLNet6Path = Join-Path $SR2Path "MelonLoader\net6"

Write-Host ""
Write-Host "=== BackpackMod Local Build ===" -ForegroundColor Cyan
Write-Host "SR2 install  : $SR2Path"
Write-Host "Il2CppAsm    : $IL2CppAssembliesPath"
Write-Host "MelonLoader  : $MLNet6Path"
Write-Host ""

# ── Validate required DLLs ──────────────────────────────────────────────────
$requiredDlls = @(
    @{ Label = "MelonLoader";            Path = Join-Path $MLNet6Path "MelonLoader.dll" },
    @{ Label = "Il2CppInterop.Runtime";  Path = Join-Path $MLNet6Path "Il2CppInterop.Runtime.dll" },
    @{ Label = "Il2Cppmscorlib";         Path = Join-Path $IL2CppAssembliesPath "Il2Cppmscorlib.dll" },
    @{ Label = "UnityEngine.CoreModule"; Path = Join-Path $IL2CppAssembliesPath "UnityEngine.CoreModule.dll" },
    @{ Label = "UnityEngine.IMGUIModule";        Path = Join-Path $IL2CppAssembliesPath "UnityEngine.IMGUIModule.dll" },
    @{ Label = "UnityEngine.InputLegacyModule";  Path = Join-Path $IL2CppAssembliesPath "UnityEngine.InputLegacyModule.dll" },
    @{ Label = "Assembly-CSharp";        Path = Join-Path $IL2CppAssembliesPath "Assembly-CSharp.dll" }
)

$missing = @()
foreach ($dll in $requiredDlls) {
    if (-not (Test-Path $dll.Path)) {
        $missing += "  MISSING  [$($dll.Label)]  ->  $($dll.Path)"
    }
}

if ($missing.Count -gt 0) {
    Write-Host "ERROR: One or more required DLLs could not be found:" -ForegroundColor Red
    $missing | ForEach-Object { Write-Host $_ -ForegroundColor Red }
    Write-Host ""
    Write-Host "Make sure MelonLoader is installed for Slime Rancher 2 and that you have" -ForegroundColor Yellow
    Write-Host "launched the game at least once so MelonLoader generates the Il2Cpp assemblies." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "If your SR2 is installed in a non-default location, pass -SR2Path:" -ForegroundColor Yellow
    Write-Host '  .\scripts\build-local.ps1 -SR2Path "D:\SteamLibrary\steamapps\common\Slime Rancher 2"' -ForegroundColor Yellow
    exit 1
}

Write-Host "All required DLLs found." -ForegroundColor Green
Write-Host ""

# ── Optional Il2CppMonomiPark wrappers (informational only) ─────────────────
$wrapperDlls = @(
    "Il2CppMonomiPark.SlimeRancher.Player.dll",
    "Il2CppMonomiPark.SlimeRancher.Player.PlayerItems.dll"
)
$missingWrappers = @()
foreach ($w in $wrapperDlls) {
    $p = Join-Path $IL2CppAssembliesPath $w
    if (-not (Test-Path $p)) {
        $missingWrappers += $w
    }
}
if ($missingWrappers.Count -gt 0) {
    Write-Host "NOTE: The following Il2CppMonomiPark wrapper DLLs were not found:" -ForegroundColor Yellow
    $missingWrappers | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
    Write-Host "  They are optional at compile time when Assembly-CSharp.dll contains the types," -ForegroundColor Yellow
    Write-Host "  but if you get 'type not found' errors, adjust the DLL names in BackpackMod.csproj." -ForegroundColor Yellow
    Write-Host ""
}

# ── Navigate to repo root ────────────────────────────────────────────────────
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
try {
    # ── dotnet restore ───────────────────────────────────────────────────────
    Write-Host "--- dotnet restore ---" -ForegroundColor Cyan
    dotnet restore BackpackMod.csproj /p:SR2Path="$SR2Path" /p:IL2CppAssembliesPath="$IL2CppAssembliesPath"
    if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed (exit $LASTEXITCODE)." }

    # ── dotnet build ─────────────────────────────────────────────────────────
    Write-Host ""
    Write-Host "--- dotnet build -c Release ---" -ForegroundColor Cyan
    dotnet build BackpackMod.csproj -c Release --no-restore `
        /p:SR2Path="$SR2Path" `
        /p:IL2CppAssembliesPath="$IL2CppAssembliesPath"
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
    Write-Host "Mod DLL : $builtDll"
    Write-Host "Installed to: $(Join-Path $modsFolder 'BackpackMod.dll')"
    Write-Host ""
    Write-Host "Launch Slime Rancher 2 and press B to open your backpack." -ForegroundColor Cyan
}
finally {
    Pop-Location
}
