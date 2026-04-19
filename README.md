# BackpackMod for Slime Rancher 2

A MelonLoader mod that adds a 20-slot backpack inventory to Slime Rancher 2.  
Press **B** in-game to open or close it.

## Features

- 20 backpack slots (5 columns × 4 rows)
- **Deposit All** – move everything from your vac-pack into the backpack
- **Withdraw All** – move everything from the backpack back into the vac-pack
- **Take 1** button on each slot for precise transfers

---

## Requirements

| Requirement | Notes |
|---|---|
| Slime Rancher 2 (Steam) | Game must be installed and launched at least once with MelonLoader |
| [MelonLoader](https://melonwiki.xyz/) | Install for SR2; launch the game once so it generates Il2Cpp assemblies |
| [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) | For building the mod locally |

---

## Building Locally (Windows, Steam default path)

> **Note:** The mod references DLLs from your local SR2 + MelonLoader install.
> No game DLLs are committed to this repository.

### Quick start (recommended)

Open PowerShell in the repository root and run:

```powershell
.\scripts\build-local.ps1
```

The script will:
1. Validate that all required DLLs exist in your SR2 install.
2. Run `dotnet restore` and `dotnet build -c Release`.
3. Copy `BackpackMod.dll` into `<SR2Path>\Mods\`, creating the folder if needed.

### Custom SR2 install path

If your game is installed somewhere other than the Steam default:

```powershell
.\scripts\build-local.ps1 -SR2Path "D:\SteamLibrary\steamapps\common\Slime Rancher 2"
```

### Manual build (without the script)

```powershell
dotnet restore BackpackMod.csproj
dotnet build BackpackMod.csproj -c Release
```

The built DLL will be at `bin\Release\net6.0\BackpackMod.dll`.  
Copy it to `<SR2 install>\Mods\` manually.

To override the SR2 path at build time:

```powershell
dotnet build BackpackMod.csproj -c Release /p:SR2Path="D:\Games\Slime Rancher 2"
```

### Troubleshooting

| Error | Fix |
|---|---|
| `MISSING [MelonLoader]` | Install MelonLoader for SR2 and launch the game once |
| `type not found: Ammo` (or similar Il2CppMonomiPark type) | `BackpackUI.cs` uses `Ammo` from `Il2CppMonomiPark.SlimeRancher.Player.PlayerItems`. The DLL containing this type is missing from `Il2CppAssemblies\`. Check the actual filenames there (look for `*MonomiPark*Player*`) and update the `_PlayerHint` / `_PlayerItemsHint` properties in `BackpackMod.csproj` if the name differs |
| DLL not in `Il2CppAssemblies\` | The assembly may be split differently in your game version; look for `*MonomiPark*Player*` files |

---

## Installation (pre-built release)

1. Install [MelonLoader](https://melonwiki.xyz/) for Slime Rancher 2.
2. Download `BackpackMod.zip` from the [Releases](../../releases) page.
3. Extract `BackpackMod.dll` into `<Slime Rancher 2 install>\Mods\`.
4. Launch the game and press **B**.

---

## Development

```
BackpackMod.csproj   – MSBuild project; references SR2 DLLs via SR2Path property
BackpackMod.cs       – MelonLoader mod entry point
BackpackUI.cs        – IMGUI backpack window + vac-pack inventory bridge
BackpackSlot.cs      – Single backpack slot model
scripts/
  build-local.ps1    – One-click local build + install helper
```
