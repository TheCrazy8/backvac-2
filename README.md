# BackpackMod for Slime Rancher 2

A MelonLoader mod that adds a 20-slot backpack inventory to Slime Rancher 2.  
Press **B** in-game to open or close it.

## Features

- 20 backpack slots (5 columns × 4 rows)
- **Deposit All** – move everything from your vac-pack into the backpack
- **Withdraw All** – move everything from the backpack back into the vac-pack
- **Take 1** button on each slot for precise transfers

---

## Prerequisites

| Requirement | Notes |
|---|---|
| Slime Rancher 2 (Steam) | Must be installed and launched at least once with MelonLoader active |
| [MelonLoader](https://melonwiki.xyz/) | Install for SR2; launch the game once so it generates the `Il2CppAssemblies` folder |
| [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) | Required to build the mod |

> **No game DLLs are committed to this repository.** The build script copies them
> from your local SR2 install into a gitignored `Libs/` folder automatically.

---

## Building Locally (Windows, Steam default path)

### Quick start (recommended)

Open PowerShell in the repository root and run:

```powershell
.\scripts\build-local.ps1
```

The script will:
1. Copy all DLLs from `<SR2Path>\MelonLoader\Il2CppAssemblies\` into the repo's `Libs\` folder.
2. Run `dotnet restore` and `dotnet build -c Release`.
3. Copy `BackpackMod.dll` into `<SR2Path>\Mods\`, creating the folder if needed.

### Custom SR2 install path

If your game is installed somewhere other than the Steam default:

```powershell
.\scripts\build-local.ps1 -SR2Path "D:\SteamLibrary\steamapps\common\Slime Rancher 2"
```

### Manual build (without the script)

First, copy the DLLs manually:

```powershell
$sr2 = "C:\Program Files (x86)\Steam\steamapps\common\Slime Rancher 2"
Copy-Item "$sr2\MelonLoader\Il2CppAssemblies\*.dll" .\Libs\ -Force
```

Then build:

```powershell
dotnet restore BackpackMod.csproj
dotnet build BackpackMod.csproj -c Release
```

The built DLL will be at `bin\Release\net6.0\BackpackMod.dll`.  
Copy it to `<SR2 install>\Mods\` manually.

### Troubleshooting

| Error | Fix |
|---|---|
| `Il2CppAssemblies folder not found` | Install MelonLoader for SR2 and launch the game once so it generates the assemblies |
| `type not found` compile errors | Make sure you ran the build script (or copied DLLs to `Libs\`) before building |
| Mod not loading in-game | Ensure MelonLoader is installed correctly and `BackpackMod.dll` is in the `Mods\` folder |

---

## Installation (pre-built release)

1. Install [MelonLoader](https://melonwiki.xyz/) for Slime Rancher 2.
2. Download `BackpackMod.zip` from the [Releases](../../releases) page.
3. Extract `BackpackMod.dll` into `<Slime Rancher 2 install>\Mods\`.
4. Launch the game and press **B**.

---

## Development

```
BackpackMod.csproj   – MSBuild project; references NuGet packages + local Libs/ DLLs
BackpackMod.cs       – MelonLoader mod entry point
BackpackUI.cs        – IMGUI backpack window + vac-pack inventory bridge
BackpackSlot.cs      – Single backpack slot model
scripts/
  build-local.ps1    – One-click local build + install helper
Libs/                – Gitignored; populated by build-local.ps1 from your SR2 install
```

