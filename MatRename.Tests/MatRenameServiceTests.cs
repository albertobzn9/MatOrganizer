using ClosedXML.Excel;

namespace MatRename.Tests;

public sealed class MatRenameServiceTests : IDisposable
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

        var result = MatRenameService.CreateCopiesAndManifest(
            outputDirectory,
            [
                new MatFileEntry(namedSource) { Slot = new ProtocolSlot(1, 1) },
                new MatFileEntry(unnamedSource) { Slot = new ProtocolSlot(2, 1) }
            ]);

        Assert.Equal(2, result.FileCount);
        Assert.Equal(namedBytes, File.ReadAllBytes(namedSource));
        Assert.Equal(unnamedBytes, File.ReadAllBytes(unnamedSource));
        Assert.Equal(namedBytes, File.ReadAllBytes(Path.Combine(outputDirectory, "exp_d1r1.mat")));
        Assert.Equal(unnamedBytes, File.ReadAllBytes(Path.Combine(outputDirectory, "registro_d2r1.mat")));
        Assert.True(File.Exists(result.ManifestPath));

        using var workbook = new XLWorkbook(result.ManifestPath);
        var sheet = workbook.Worksheet("Relación");
        Assert.Equal("Archivo original", sheet.Cell(1, 1).GetString());
        Assert.Equal("Archivo nuevo", sheet.Cell(1, 2).GetString());
        Assert.Equal("exp_d2r1.mat", sheet.Cell(2, 1).GetString());
        Assert.Equal("exp_d1r1.mat", sheet.Cell(2, 2).GetString());
        Assert.Equal(1, sheet.Cell(2, 3).GetValue<int>());
        Assert.Equal(1, sheet.Cell(2, 4).GetValue<int>());
    }

    [Theory]
    [InlineData("exp_d3r2.mat", 3, 2)]
    [InlineData("exp_d3.0r2.mat", 3, 2)]
    public void TryGetSlotFromFileName_reads_the_day_and_rat(string fileName, int expectedDay, int expectedRat)
    {
        var found = MatRenameService.TryGetSlotFromFileName(fileName, out var slot);

        Assert.True(found);
        Assert.Equal(new ProtocolSlot(expectedDay, expectedRat), slot);
    }

    public void Dispose()
    {
        if (Directory.Exists(_temporaryDirectory))
        {
            Directory.Delete(_temporaryDirectory, recursive: true);
        }
    }
}
