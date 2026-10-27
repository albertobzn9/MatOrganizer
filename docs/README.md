# MatOrganizer technical documentation

This document describes the stable behavior of the application so the workflow can be understood and maintained without inferring it from the user interface.

## Purpose

MatOrganizer turns manually selected session files into **new, standardized copies**. It does not rename, move, edit, or delete the original files.

Supported source types are MATLAB data (`.mat`) and videos (`.mp4`, `.mkv`). Files may have any initial name.

## Naming contract

The user provides a period, one phase, and a day/rat assignment for each loaded session. The generated copy always follows this form:

```text
exp_MMAA_phase_d<day>r<rat>.<extension>
```

| Field | Accepted or generated value |
| --- | --- |
| `MMAA` | Input accepts `MM-AA` or `MMAA`; output removes the hyphen. |
| `phase` | One of `cs` (safe crossings), `cp` (dangerous crossings), or `dis` (discrimination). |
| `day` / `rat` | Positive integers assigned from the interface. |
| `extension` | The source extension: `.mat`, `.mp4`, or `.mkv`. |

For example, a source video assigned to period `01-26`, phase `dis`, day 1, rat 4 becomes `exp_0126_dis_d1r4.mp4`.

## Copy behavior

1. The user loads one source type at a time and assigns a day and rat to the files that should be processed.
2. The selected output types determine what is copied. If a loaded file has companions in the same folder with the same basename, selected companion types are included too.
3. Every output is copied first to a temporary file. Only after all copying succeeds are the temporary files promoted to their final names.
4. MatOrganizer writes `relacion_renombrado.xlsx` in the output folder. The `Relación` sheet lists the old and new filenames, day, rat, type, and creation status.

The app rejects an operation when no file is assigned, no output type is selected, a source file is missing, generated destinations collide, an output file already exists, or an output manifest already exists.

If an error or cancellation occurs during copying, temporary files and completed copies from that attempt are removed. Existing source files and prior output files are never overwritten.

## Code map

| Location | Responsibility |
| --- | --- |
| `src/MatOrganizer/Views` | Avalonia window layout and interaction handlers. |
| `src/MatOrganizer/Services/MatOrganizerService.cs` | Naming validation, record generation, byte-based copying, rollback, and Excel manifest generation. |
| `src/MatOrganizer/Services/AppLocalizer.cs` | English and Spanish user-interface text. English is the default. |
| `src/MatOrganizer/Assets` | Application icons and repository logo. |
| `tests/MatOrganizer.Tests` | Unit coverage for naming, media companions, manifests, and byte progress. |

## Build and tests

Install the .NET 10 SDK, then run from the repository root:

```bash
dotnet restore MatOrganizer.sln
dotnet test MatOrganizer.sln -c Release
dotnet run --project src/MatOrganizer/MatOrganizer.csproj
```

The release script lives at `scripts/package-release.sh`. It publishes self-contained builds, so end users do not need MATLAB or a separate .NET installation.
