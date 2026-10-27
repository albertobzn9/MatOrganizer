using ClosedXML.Excel;

namespace MatOrganizer.Tests;

public sealed class MatOrganizerServiceTests : IDisposable
{
    private readonly string _temporaryDirectory = Path.Combine(Path.GetTempPath(), $"matrename-test-{Guid.NewGuid():N}");

    [Fact]
    public void CreateCopiesAndManifest_preserves_sources_and_records_the_new_names()
    {
        var sourceDirectory = Directory.CreateDirectory(Path.Combine(_temporaryDirectory, "origen")).FullName;
        var outputDirectory = Directory.CreateDirectory(Path.Combine(_temporaryDirectory, "salida")).FullName;
        var namedSource = Path.Combine(sourceDirectory, "exp_d2r1.mat");
        var unnamedSource = Path.Combine(sourceDirectory, "registro.mat");
        var namedBytes = new byte[] { 1, 2, 3, 4 };
        var unnamedBytes = new byte[] { 5, 6, 7, 8 };
        File.WriteAllBytes(namedSource, namedBytes);
        File.WriteAllBytes(unnamedSource, unnamedBytes);

        var result = MatOrganizerService.CreateCopiesAndManifest(
            outputDirectory,
            [
                new MatFileEntry(namedSource) { Slot = new ProtocolSlot(1, 1) },
                new MatFileEntry(unnamedSource) { Slot = new ProtocolSlot(2, 1) }
            ],
            SessionNamingOptions.Create("05-26", "dis"));

        Assert.Equal(2, result.FileCount);
        Assert.Equal(namedBytes, File.ReadAllBytes(namedSource));
        Assert.Equal(unnamedBytes, File.ReadAllBytes(unnamedSource));
        Assert.Equal(namedBytes, File.ReadAllBytes(Path.Combine(outputDirectory, "exp_0526_dis_d1r1.mat")));
        Assert.Equal(unnamedBytes, File.ReadAllBytes(Path.Combine(outputDirectory, "exp_0526_dis_d2r1.mat")));
        Assert.True(File.Exists(result.ManifestPath));

        using var workbook = new XLWorkbook(result.ManifestPath);
        var sheet = workbook.Worksheet("Relación");
        Assert.Equal("Archivo original", sheet.Cell(1, 1).GetString());
        Assert.Equal("Archivo nuevo", sheet.Cell(1, 2).GetString());
        Assert.Equal("exp_d2r1.mat", sheet.Cell(2, 1).GetString());
        Assert.Equal("exp_0526_dis_d1r1.mat", sheet.Cell(2, 2).GetString());
        Assert.Equal(1, sheet.Cell(2, 3).GetValue<int>());
        Assert.Equal(1, sheet.Cell(2, 4).GetValue<int>());
        Assert.Equal("MAT", sheet.Cell(2, 5).GetString());
        Assert.Equal("Creado", sheet.Cell(2, 6).GetString());
    }

    [Fact]
    public void CreateCopiesAndManifest_copies_selected_companion_videos_using_the_same_new_session_name()
    {
        var sourceDirectory = Directory.CreateDirectory(Path.Combine(_temporaryDirectory, "origen")).FullName;
        var outputDirectory = Directory.CreateDirectory(Path.Combine(_temporaryDirectory, "salida")).FullName;
        var matPath = Path.Combine(sourceDirectory, "exp_0526_dis_d2r1.mat");
        var mp4Path = Path.Combine(sourceDirectory, "exp_0526_dis_d2r1.mp4");
        var mkvPath = Path.Combine(sourceDirectory, "exp_0526_dis_d2r1.MKV");
        File.WriteAllBytes(matPath, [1]);
        File.WriteAllBytes(mp4Path, [2, 3]);
        File.WriteAllBytes(mkvPath, [4, 5, 6]);

        var result = MatOrganizerService.CreateCopiesAndManifest(
            outputDirectory,
            [new MatFileEntry(matPath) { Slot = new ProtocolSlot(1, 4) }],
            SessionNamingOptions.Create("05-26", "dis"),
            new CopyOptions(IncludeMat: true, IncludeMp4: true, IncludeMkv: true));

        Assert.Equal(3, result.FileCount);
        Assert.Equal(1, result.MatCount);
        Assert.Equal(1, result.Mp4Count);
        Assert.Equal(1, result.MkvCount);
        Assert.Equal(new byte[] { 1 }, File.ReadAllBytes(Path.Combine(outputDirectory, "exp_0526_dis_d1r4.mat")));
        Assert.Equal(new byte[] { 2, 3 }, File.ReadAllBytes(Path.Combine(outputDirectory, "exp_0526_dis_d1r4.mp4")));
        Assert.Equal(new byte[] { 4, 5, 6 }, File.ReadAllBytes(Path.Combine(outputDirectory, "exp_0526_dis_d1r4.MKV")));
        Assert.Equal(new byte[] { 1 }, File.ReadAllBytes(matPath));
        Assert.Equal(new byte[] { 2, 3 }, File.ReadAllBytes(mp4Path));
        Assert.Equal(new byte[] { 4, 5, 6 }, File.ReadAllBytes(mkvPath));

        using var workbook = new XLWorkbook(result.ManifestPath);
        var types = workbook.Worksheet("Relación").Column(5).CellsUsed().Select(cell => cell.GetString());
        Assert.Equal(new[] { "Tipo", "MAT", "MKV", "MP4" }, types);
    }

    [Fact]
    public void BuildRecords_skips_missing_or_unselected_companion_types()
    {
        var sourceDirectory = Directory.CreateDirectory(Path.Combine(_temporaryDirectory, "origen")).FullName;
        var outputDirectory = Directory.CreateDirectory(Path.Combine(_temporaryDirectory, "salida")).FullName;
        var matPath = Path.Combine(sourceDirectory, "exp_d2r1.mat");
        var mp4Path = Path.Combine(sourceDirectory, "exp_d2r1.mp4");
        File.WriteAllBytes(matPath, [1]);
        File.WriteAllBytes(mp4Path, [2]);

        var records = MatOrganizerService.BuildRecords(
            outputDirectory,
            [new MatFileEntry(matPath) { Slot = new ProtocolSlot(1, 1) }],
            SessionNamingOptions.Create("05-26", "CP"),
            new CopyOptions(IncludeMat: false, IncludeMp4: true, IncludeMkv: true));

        var record = Assert.Single(records);
        Assert.Equal(mp4Path, record.SourcePath);
        Assert.Equal(Path.Combine(outputDirectory, "exp_0526_CP_d1r1.mp4"), record.DestinationPath);
    }

    [Fact]
    public void BuildRecords_reports_when_selected_media_companions_do_not_exist()
    {
        var sourceDirectory = Directory.CreateDirectory(Path.Combine(_temporaryDirectory, "origen")).FullName;
        var outputDirectory = Directory.CreateDirectory(Path.Combine(_temporaryDirectory, "salida")).FullName;
        var matPath = Path.Combine(sourceDirectory, "exp_d2r1.mat");
        File.WriteAllBytes(matPath, [1]);

        var exception = Assert.Throws<InvalidOperationException>(() => MatOrganizerService.BuildRecords(
            outputDirectory,
            [new MatFileEntry(matPath) { Slot = new ProtocolSlot(1, 1) }],
            SessionNamingOptions.Create("05-26", "CP"),
            new CopyOptions(IncludeMat: false, IncludeMp4: true, IncludeMkv: true)));

        Assert.Contains("No se encontraron", exception.Message);
    }

    [Theory]
    [InlineData("exp_d3r2.mat", 3, 2)]
    [InlineData("exp_d3.0r2.mat", 3, 2)]
    public void TryGetSlotFromFileName_reads_the_day_and_rat(string fileName, int expectedDay, int expectedRat)
    {
        var found = MatOrganizerService.TryGetSlotFromFileName(fileName, out var slot);

        Assert.True(found);
        Assert.Equal(new ProtocolSlot(expectedDay, expectedRat), slot);
    }

    [Fact]
    public async Task CreateCopiesAndManifestAsync_renames_a_directly_selected_video_and_reports_byte_progress()
    {
        var sourceDirectory = Directory.CreateDirectory(Path.Combine(_temporaryDirectory, "origen")).FullName;
        var outputDirectory = Directory.CreateDirectory(Path.Combine(_temporaryDirectory, "salida")).FullName;
        var videoPath = Path.Combine(sourceDirectory, "cualquier_nombre.mp4");
        var videoBytes = new byte[1_500_000];
        videoBytes[^1] = 42;
        File.WriteAllBytes(videoPath, videoBytes);
        var progressUpdates = new List<CopyProgress>();

        var result = await MatOrganizerService.CreateCopiesAndManifestAsync(
            outputDirectory,
            [new MatFileEntry(videoPath) { Slot = new ProtocolSlot(3, 2) }],
            SessionNamingOptions.Create("01-26", "S"),
            new CopyOptions(IncludeMat: false, IncludeMp4: true, IncludeMkv: false),
            new InlineProgress<CopyProgress>(progressUpdates.Add));

        Assert.Equal(1, result.Mp4Count);
        Assert.Equal(videoBytes, File.ReadAllBytes(Path.Combine(outputDirectory, "exp_0126_S_d3r2.mp4")));
        Assert.Equal(videoBytes, File.ReadAllBytes(videoPath));
        Assert.NotEmpty(progressUpdates);
        Assert.All(progressUpdates, update => Assert.Equal(videoBytes.Length, update.TotalBytes));
        Assert.True(progressUpdates.Zip(progressUpdates.Skip(1), (first, second) => second.CompletedBytes >= first.CompletedBytes).All(value => value));
        var finalUpdate = progressUpdates.Last();
        Assert.Equal(videoBytes.Length, finalUpdate.CompletedBytes);
        Assert.Equal(1, finalUpdate.CompletedFiles);
        Assert.Equal(1, finalUpdate.TotalFiles);
    }

    [Theory]
    [InlineData("01-26", "dis", "0126", "dis")]
    [InlineData("10-22", "CP", "1022", "CP")]
    public void SessionNamingOptions_normalizes_the_period_without_the_hyphen(string monthYear, string phase, string expectedPeriod, string expectedPhase)
    {
        var naming = SessionNamingOptions.Create(monthYear, phase);

        Assert.Equal(expectedPeriod, naming.Period);
        Assert.Equal(expectedPhase, naming.Phase);
    }

    public void Dispose()
    {
        if (Directory.Exists(_temporaryDirectory))
        {
            Directory.Delete(_temporaryDirectory, recursive: true);
        }
    }

    private sealed class InlineProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }
}
