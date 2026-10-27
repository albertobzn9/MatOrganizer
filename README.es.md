# MatOrganizer

<img src="src/MatOrganizer/Assets/matorganizer-logo.png" alt="Logotipo de MatOrganizer" width="96" />

MatOrganizer es una aplicación de escritorio multiplataforma para organizar archivos de sesiones conductuales por día y rata. Crea copias renombradas de datos de MATLAB y archivos de video, y después genera un Excel que relaciona cada nombre original con su nombre nuevo.

[English](README.md) | [Español](README.es.md)

## Funciones

- Asigna cada sesión a un día y una rata de forma interactiva.
- Carga archivos `.mat`, `.mp4` o `.mkv` de manera independiente.
- Copia tipos de archivo coincidentes que compartan el mismo nombre base de origen.
- Genera nombres estándar de sesión, como `exp_0126_dis_d1r4.mat`.
- Incluye las fases `cs`, `cp` y `dis`.
- Muestra el progreso del archivo actual y del total usando los bytes realmente copiados.
- Conserva intactos todos los archivos originales.

## Descargas

Descarga la versión autocontenida más reciente desde la [página de Releases](https://github.com/albertobzn9/MatOrganizer/releases/latest).

- **macOS Apple Silicon:** instalador DMG o ZIP portable.
- **Windows x64:** EXE portable o ZIP.

No se necesita MATLAB ni el runtime de .NET para ejecutar una versión publicada.

## Nombres de archivo

Los nombres de los archivos de origen pueden ser cualquiera. Antes de crear las copias, escribe un mes-año como `01-26` o `0126`, selecciona una fase y asigna cada archivo a un día y una rata. El nombre generado siempre usa el período compacto:

```text
exp_MMAA_fase_d#r#.extensión
```

Por ejemplo, `01-26`, fase `dis`, día 1, rata 4 y un archivo MP4 de origen producen:

```text
exp_0126_dis_d1r4.mp4
```

## Desarrollo

Requisito: SDK de .NET 10. Avalonia proporciona la interfaz de escritorio multiplataforma.

```bash
dotnet restore MatOrganizer.sln
dotnet run --project src/MatOrganizer/MatOrganizer.csproj
dotnet test MatOrganizer.sln
```

## Estructura del repositorio

| Ruta | Propósito |
| --- | --- |
| `src/MatOrganizer` | Código de la aplicación Avalonia, vistas, recursos y configuración del proyecto. |
| `tests/MatOrganizer.Tests` | Pruebas unitarias de nombres, copias, manifiestos y progreso. |
| `packaging/macos` | Metadatos del paquete de aplicación para macOS. |
| `scripts` | Scripts locales para empaquetar releases. |

## Empaquetar un release

En macOS, ejecuta lo siguiente desde la raíz del repositorio usando un número de versión nuevo:

```bash
./scripts/package-release.sh 1.2.0
```

El script genera un DMG y ZIP para macOS Apple Silicon, además de un EXE y ZIP autocontenidos para Windows x64, dentro de `artifacts/release/`.
