# MatOrganizer

<img src="src/MatOrganizer/Assets/matorganizer-logo.png" alt="MatOrganizer logo" width="96" />

MatOrganizer is a cross-platform desktop app for organizing behavioral-session files by day and rat. It creates renamed copies of MATLAB data and video files, then writes an Excel manifest that records the original and new filenames.

## Features

- Assign each session to a day and rat interactively.
- Load `.mat`, `.mp4`, or `.mkv` files independently.
- Copy matching file types that share the same source basename.
- Generate standard session names such as `exp_0126_dis_d1r4.mat`.
- Support the `cs`, `cp`, and `dis` phases.
- Track both current-file and overall copy progress using actual transferred bytes.
- Preserve every original file.

## Download

Download the latest self-contained build from the [Releases page](https://github.com/albertobzn9/MatOrganizer/releases/latest).

- **macOS Apple Silicon:** DMG installer or portable ZIP.
- **Windows x64:** portable EXE or ZIP.

MATLAB and the .NET runtime are not required to run a release build.

## File naming

Source filenames can be arbitrary. Before creating copies, enter a month-year value as either `01-26` or `0126`, select a phase, and assign each file to a day and rat. The generated name always uses the compact period format:

```text
exp_MMAA_phase_d#r#.extension
```

For example, `01-26`, phase `dis`, day 1, rat 4, and an MP4 source file produce:

```text
exp_0126_dis_d1r4.mp4
```

## Development

Requirements: the .NET 10 SDK. Avalonia provides the cross-platform desktop interface.

```bash
dotnet restore MatOrganizer.sln
dotnet run --project src/MatOrganizer/MatOrganizer.csproj
dotnet test MatOrganizer.sln
```

## Repository layout

| Path | Purpose |
| --- | --- |
| `src/MatOrganizer` | Avalonia application source, views, assets, and project configuration. |
| `tests/MatOrganizer.Tests` | Unit tests for file naming, copying, manifests, and progress reporting. |
| `packaging/macos` | macOS application bundle metadata. |
| `scripts` | Local release-packaging scripts. |

## Packaging a release

On macOS, run the following from the repository root with a new version number:

```bash
./scripts/package-release.sh 1.2.0
```

The script produces a macOS Apple Silicon DMG and ZIP, plus a self-contained Windows x64 EXE and ZIP under `artifacts/release/`.
