using System.Text.RegularExpressions;
using ClosedXML.Excel;

namespace MatOrganizer;

internal sealed record ProtocolSlot(int Day, int Rat)
{
    public string DisplayName => $"Día {Day} · Rata {Rat}";
}

internal sealed class MatFileEntry(string sourcePath)
{
    public string SourcePath { get; } = sourcePath;

    public ProtocolSlot? Slot { get; set; }

    public string FileName => Path.GetFileName(SourcePath);

    public override string ToString() => FileName;
}

internal sealed record RenameRecord(
    string SourcePath,
    string DestinationPath,
    ProtocolSlot Slot);

internal sealed record SessionNamingOptions(string Period, string Phase)
{
    private static readonly HashSet<string> AllowedPhases = new(StringComparer.Ordinal)
    {
        "dis", "C", "S", "CP"
    };

    public static SessionNamingOptions Create(string? monthYear, string? phase)
    {
        if (string.IsNullOrWhiteSpace(monthYear) ||
            !Regex.IsMatch(monthYear.Trim(), @"^(0[1-9]|1[0-2])-\d{2}$"))
        {
            throw new InvalidOperationException("Mes-año debe tener el formato MM-AA, por ejemplo 01-26.");
        }

        if (string.IsNullOrWhiteSpace(phase) || !AllowedPhases.Contains(phase.Trim()))
        {
            throw new InvalidOperationException("La fase debe ser dis, C, S o CP.");
        }

        return new SessionNamingOptions(monthYear.Trim().Replace("-", string.Empty), phase.Trim());
    }

    public static bool IsValid(string? monthYear, string? phase)
    {
        try
        {
            _ = Create(monthYear, phase);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}

internal sealed record CopyOptions(bool IncludeMat, bool IncludeMp4, bool IncludeMkv)
{
    public static CopyOptions MatOnly { get; } = new(IncludeMat: true, IncludeMp4: false, IncludeMkv: false);

    public bool HasSelectedTypes => IncludeMat || IncludeMp4 || IncludeMkv;

    public IReadOnlyList<string> SelectedExtensions =>
        [
            ..(IncludeMat ? new[] { ".mat" } : []),
            ..(IncludeMp4 ? new[] { ".mp4" } : []),
            ..(IncludeMkv ? new[] { ".mkv" } : [])
        ];
}

internal sealed record RenameResult(
    int FileCount,
    string ManifestPath,
    int MatCount,
    int Mp4Count,
    int MkvCount);

internal sealed record CopyProgress(
    long CompletedBytes,
    long TotalBytes,
    int CompletedFiles,
    int TotalFiles,
    int CurrentFileNumber,
    string CurrentFileName,
    long CurrentFileBytes,
    long CurrentFileLength);

internal static partial class MatOrganizerService
{
    private const string ManifestFileName = "relacion_renombrado.xlsx";

    [GeneratedRegex(@"d(?<day>\d+)(?:\.0+)?r(?<rat>\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex IdentifierPattern();

    public static bool TryGetSlotFromFileName(string fileName, out ProtocolSlot? slot)
    {
        var match = IdentifierPattern().Match(Path.GetFileNameWithoutExtension(fileName));
        if (match.Success &&
            int.TryParse(match.Groups["day"].Value, out var day) && day > 0 &&
            int.TryParse(match.Groups["rat"].Value, out var rat) && rat > 0)
        {
            slot = new ProtocolSlot(day, rat);
            return true;
        }

        slot = null;
        return false;
    }

    public static RenameResult CreateCopiesAndManifest(
        string outputDirectory,
        IReadOnlyCollection<MatFileEntry> entries,
        SessionNamingOptions namingOptions,
        CopyOptions? copyOptions = null) =>
        CreateCopiesAndManifestAsync(outputDirectory, entries, namingOptions, copyOptions)
            .GetAwaiter()
            .GetResult();

    public static async Task<RenameResult> CreateCopiesAndManifestAsync(
        string outputDirectory,
        IReadOnlyCollection<MatFileEntry> entries,
        SessionNamingOptions namingOptions,
        CopyOptions? copyOptions = null,
        IProgress<CopyProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var records = BuildRecords(outputDirectory, entries, namingOptions, copyOptions);
        var manifestPath = Path.Combine(outputDirectory, ManifestFileName);
        if (File.Exists(manifestPath))
        {
            throw new InvalidOperationException($"Ya existe {ManifestFileName} en la carpeta de salida.");
        }

        var temporaryFiles = new List<string>();
        var completedFiles = new List<string>();
        var temporaryManifest = Path.Combine(outputDirectory, $".matrename-{Guid.NewGuid():N}.xlsx");
        var fileLengths = records.Select(record => new FileInfo(record.SourcePath).Length).ToList();
        var totalBytes = fileLengths.Sum();
        long completedBytes = 0;

        try
        {
            WriteManifest(temporaryManifest, records);

            for (var index = 0; index < records.Count; index++)
            {
                var record = records[index];
                var temporaryCopy = Path.Combine(
                    outputDirectory,
                    $".{Path.GetFileName(record.DestinationPath)}.{Guid.NewGuid():N}.tmp");
                temporaryFiles.Add(temporaryCopy);
                await CopyFileAsync(
                    record,
                    temporaryCopy,
                    completedBytes,
                    totalBytes,
                    index,
                    records.Count,
                    fileLengths[index],
                    progress,
                    cancellationToken);
                completedBytes += fileLengths[index];
            }

            for (var index = 0; index < records.Count; index++)
            {
                File.Move(temporaryFiles[index], records[index].DestinationPath);
                completedFiles.Add(records[index].DestinationPath);
            }

            File.Move(temporaryManifest, manifestPath);
            return new RenameResult(
                records.Count,
                manifestPath,
                records.Count(record => HasExtension(record.SourcePath, ".mat")),
                records.Count(record => HasExtension(record.SourcePath, ".mp4")),
                records.Count(record => HasExtension(record.SourcePath, ".mkv")));
        }
        catch
        {
            foreach (var path in temporaryFiles.Append(temporaryManifest))
            {
                DeleteIfExists(path);
            }

            foreach (var path in completedFiles)
            {
                DeleteIfExists(path);
            }

            throw;
        }
    }

    public static IReadOnlyList<RenameRecord> BuildRecords(
        string outputDirectory,
        IReadOnlyCollection<MatFileEntry> entries,
        SessionNamingOptions namingOptions,
        CopyOptions? copyOptions = null)
    {
        if (string.IsNullOrWhiteSpace(outputDirectory) || !Directory.Exists(outputDirectory))
        {
            throw new InvalidOperationException("Selecciona una carpeta de salida válida.");
        }

        var assignedEntries = entries.Where(entry => entry.Slot is not null).ToList();
        if (assignedEntries.Count == 0)
        {
            throw new InvalidOperationException("Asigna por lo menos un archivo a un día y una rata.");
        }

        var selectedOptions = copyOptions ?? CopyOptions.MatOnly;
        if (!selectedOptions.HasSelectedTypes)
        {
            throw new InvalidOperationException("Selecciona por lo menos un tipo de archivo para copiar.");
        }

        var missingFile = assignedEntries.FirstOrDefault(entry => !File.Exists(entry.SourcePath));
        if (missingFile is not null)
        {
            throw new FileNotFoundException($"No se encontró {missingFile.FileName}.", missingFile.SourcePath);
        }

        var records = assignedEntries
            .SelectMany(entry => BuildRecordsForEntry(outputDirectory, entry, namingOptions, selectedOptions))
            .OrderBy(record => record.Slot.Rat)
            .ThenBy(record => record.Slot.Day)
            .ThenBy(record => Path.GetExtension(record.SourcePath), StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (records.Count == 0)
        {
            throw new InvalidOperationException("No se encontraron archivos del tipo seleccionado para copiar.");
        }

        var duplicate = records
            .GroupBy(record => record.DestinationPath, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException($"Dos archivos producirían el mismo nombre: {Path.GetFileName(duplicate.Key)}.");
        }

        var existing = records.FirstOrDefault(record => File.Exists(record.DestinationPath));
        if (existing is not null)
        {
            throw new InvalidOperationException($"Ya existe {Path.GetFileName(existing.DestinationPath)} en la carpeta de salida.");
        }

        return records;
    }

    private static IEnumerable<RenameRecord> BuildRecordsForEntry(
        string outputDirectory,
        MatFileEntry entry,
        SessionNamingOptions namingOptions,
        CopyOptions copyOptions)
    {
        var sourcePaths = new List<string>();
        foreach (var extension in copyOptions.SelectedExtensions)
        {
            if (string.Equals(Path.GetExtension(entry.SourcePath), extension, StringComparison.OrdinalIgnoreCase))
            {
                sourcePaths.Add(entry.SourcePath);
            }
            else
            {
                AddCompanionFile(entry.SourcePath, extension, sourcePaths);
            }
        }

        return sourcePaths
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(sourcePath => new RenameRecord(
                sourcePath,
                Path.Combine(outputDirectory, BuildFileName(Path.GetFileName(sourcePath), entry.Slot!, namingOptions)),
                entry.Slot!));
    }

    private static void AddCompanionFile(string matPath, string extension, ICollection<string> sourcePaths)
    {
        var directory = Path.GetDirectoryName(matPath);
        if (directory is null)
        {
            return;
        }

        var baseName = Path.GetFileNameWithoutExtension(matPath);
        var companion = Directory.EnumerateFiles(directory)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(path =>
                string.Equals(Path.GetFileNameWithoutExtension(path), baseName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(Path.GetExtension(path), extension, StringComparison.OrdinalIgnoreCase));
        if (companion is not null)
        {
            sourcePaths.Add(companion);
        }
    }

    private static string BuildFileName(string sourceFileName, ProtocolSlot slot, SessionNamingOptions namingOptions)
    {
        var extension = Path.GetExtension(sourceFileName);
        return $"exp_{namingOptions.Period}_{namingOptions.Phase}_d{slot.Day}r{slot.Rat}{extension}";
    }

    private static async Task CopyFileAsync(
        RenameRecord record,
        string temporaryCopy,
        long completedBytesBeforeFile,
        long totalBytes,
        int fileIndex,
        int totalFiles,
        long fileLength,
        IProgress<CopyProgress>? progress,
        CancellationToken cancellationToken)
    {
        const int bufferSize = 1024 * 1024;
        long copiedBytes = 0;

        void Report(int completedFiles) => progress?.Report(new CopyProgress(
            completedBytesBeforeFile + copiedBytes,
            totalBytes,
            completedFiles,
            totalFiles,
            fileIndex + 1,
            Path.GetFileName(record.SourcePath),
            copiedBytes,
            fileLength));

        Report(fileIndex);
        await using var source = new FileStream(record.SourcePath, new FileStreamOptions
        {
            Mode = FileMode.Open,
            Access = FileAccess.Read,
            Share = FileShare.Read,
            BufferSize = bufferSize,
            Options = FileOptions.Asynchronous | FileOptions.SequentialScan
        });
        await using var destination = new FileStream(temporaryCopy, new FileStreamOptions
        {
            Mode = FileMode.CreateNew,
            Access = FileAccess.Write,
            Share = FileShare.None,
            BufferSize = bufferSize,
            Options = FileOptions.Asynchronous
        });

        var buffer = new byte[bufferSize];
        while (true)
        {
            var bytesRead = await source.ReadAsync(buffer, cancellationToken);
            if (bytesRead == 0)
            {
                break;
            }

            await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            copiedBytes += bytesRead;
            Report(fileIndex);
        }

        await destination.FlushAsync(cancellationToken);
        Report(fileIndex + 1);
    }

    private static void WriteManifest(string path, IReadOnlyList<RenameRecord> records)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Relación");
        sheet.Cell(1, 1).Value = "Archivo original";
        sheet.Cell(1, 2).Value = "Archivo nuevo";
        sheet.Cell(1, 3).Value = "Día asignado";
        sheet.Cell(1, 4).Value = "Rata asignada";
        sheet.Cell(1, 5).Value = "Tipo";
        sheet.Cell(1, 6).Value = "Estado";
        sheet.Range(1, 1, 1, 6).Style.Font.Bold = true;

        for (var row = 0; row < records.Count; row++)
        {
            var record = records[row];
            var worksheetRow = row + 2;
            sheet.Cell(worksheetRow, 1).Value = Path.GetFileName(record.SourcePath);
            sheet.Cell(worksheetRow, 2).Value = Path.GetFileName(record.DestinationPath);
            sheet.Cell(worksheetRow, 3).Value = record.Slot.Day;
            sheet.Cell(worksheetRow, 4).Value = record.Slot.Rat;
            sheet.Cell(worksheetRow, 5).Value = Path.GetExtension(record.SourcePath).TrimStart('.').ToUpperInvariant();
            sheet.Cell(worksheetRow, 6).Value = "Creado";
        }

        sheet.Columns().AdjustToContents();
        workbook.SaveAs(path);
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static bool HasExtension(string filePath, string extension) =>
        string.Equals(Path.GetExtension(filePath), extension, StringComparison.OrdinalIgnoreCase);
}
