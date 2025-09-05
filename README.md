# Terbin

Command-line tool to help create and manage Farlands mods.

## Requirements

- .NET 9 SDK
- Windows x64 (the provided `build.bat` publishes for `win-x64`)

## Build and run

- Build a self-contained binary:
  ```powershell
  .\build.bat
  ```
- Run the CLI (after build):
  ```powershell
  .\Terbin\bin\Release\net9.0\win-x64\publish\terbin.exe <command> [args]
  ```

Tip: Add the `publish` folder to PATH to invoke `terbin` from anywhere.

## Quick start

Set up a new mod end-to-end with one command:

```powershell
terbin setup -y
```

This will:
1) Ask (or auto-accept with `-y`) for Farlands install path, store it in `.terbin`.
2) Create `manifest.json` (auto-confirmed with `-y`).
3) Generate the project `<Name>.csproj` and run `dotnet restore`.
4) Copy Farlands Managed DLLs into local `libs` (excludes `System.*` and `mscorlib.*`).
5) Generate `plugin.cs` from the manifest.

Alternatively, run step-by-step:

```powershell
terbin config fpath <path>   # or: terbin config fpath (interactive prompt)
terbin manifest [-y]         # creates manifest.json via dialog
terbin gen                   # generates <Name>.csproj and restores packages
terbin inf                   # copies game DLLs into ./libs (excludes System.* and mscorlib.*)
terbin bman                  # generates plugin.cs from manifest
terbin build                 # runs bman then dotnet build on <Name>.csproj
```

## Commands

- `info`
  - Shows Terbin and current project info (manifest path, existence, etc.).

- `help` [command]
  - Shows general usage and the list of available commands, or details for a specific command.

- `manifest` [`-y` | `--yes`]
  - Checks for `manifest.json`; if not present, offers to create it.
  - With `-y/--yes`, it auto-confirms and skips yes/no prompts.

- `gen`
  - Generates `<Name>.csproj` targeting `net35` with basic dependencies.
  - Runs `dotnet restore` for the generated project.

- `config`
  - Configures local Terbin options (saved in `.terbin`).
  - Subcommand: `fpath <path>` sets the Farlands install directory.
    - If `<path>` is omitted, Terbin will prompt: “Enter Farlands path:”.

- `inf`
  - Copies DLLs from `<FarlandsPath>\Farlands_Data\Managed` into `./libs`.
  - Skips assemblies starting with `System.` and `mscorlib.`.

- `bman`
  - Generates `plugin.cs` from the current `manifest.json`.
  - Plugin template uses BepInEx and logs a startup message.

- `build`
  - Generates `plugin.cs` (same as `bman`) and then runs `dotnet build` on `<Name>.csproj`.
  - Errors if the project file does not exist (run `gen` first).

- `setup` [`-y` | `--yes`]
  - Runs the main steps in order: `config fpath` → `manifest` → `gen` → `inf` → `bman`.
  - Propagates `-y/--yes` so confirmations are auto-accepted.
  - Reloads `.terbin` and `manifest.json` after each relevant step so later steps see fresh values.


## Logging

All commands/dialogs use a common logger:
- Sections with headers for readability.
- Colored levels: info, success, warning, error.
- Consistent prompts: `Ask` (input) and `Confirm` (yes/no).

## Configuration file

Terbin stores local settings in a JSON file named `.terbin` at the project root.

Example structure:
```json
{
  "FarlandsPath": "C:\\Games\\Farlands"
}
```
The file is written automatically when you set `FarlandsPath`.

## Manifest

The mod manifest (`manifest.json`) contains:

```json
{
  "Name": "...",
  "GUID": "...",
  "Versions": ["1.0.0"],
  "url": "https://github.com/user/repo",
  "Dependencies": ["fm.fcm"]
}
```

It is created via the interactive dialog (`terbin manifest`) and used by `gen`, `bman`, and `build`.

## Troubleshooting

- Unknown command
  - Run `terbin info` to see available commands.

- `manifest.json` missing
  - Run `terbin manifest -y` to create it quickly.

- Farlands path not set
  - Run `terbin config fpath <path>` or `terbin config fpath` and follow the prompt.

- Project already exists
  - `gen` will warn if `<Name>.csproj` already exists; delete it or adjust the manifest name.

- Build fails
  - Ensure you ran `gen` and that `<Name>.csproj` exists.
  - Check errors printed by `dotnet build` for missing dependencies or code issues.

