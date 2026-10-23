using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace MatOrganizer;

public partial class MainWindow : Window
{
    private readonly List<MatFileEntry> _entries = [];
    private int _dayCount = 10;
    private int _ratCount = 1;
    private string? _outputDirectory;

    public MainWindow()
    {
        InitializeComponent();
        StatusText.Text = "Agrega archivos .mat para comenzar.";
        RefreshView();
    }

    private async void SelectFilesButton_Click(object? sender, RoutedEventArgs eventArgs)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Agregar archivos MATLAB",
            AllowMultiple = true,
            FileTypeFilter =
            [
                new FilePickerFileType("Archivos MATLAB") { Patterns = ["*.mat"] }
            ]
        });

        AddFiles(files.Select(file => file.TryGetLocalPath()).OfType<string>());
    }

    private void RemoveButton_Click(object? sender, RoutedEventArgs eventArgs)
    {
        if (UnassignedListBox.SelectedItem is not MatFileEntry entry)
        {
            return;
        }

        _entries.Remove(entry);
        StatusText.Text = $"Se quitó {entry.FileName} de la lista.";
        RefreshView();
    }

    private void DeselectButton_Click(object? sender, RoutedEventArgs eventArgs)
    {
        UnassignedListBox.SelectedItem = null;
        StatusText.Text = "No hay ningún archivo seleccionado.";
        UpdateButtons();
    }

    private void UpdateGridButton_Click(object? sender, RoutedEventArgs eventArgs)
    {
        if (!int.TryParse(DayCountTextBox.Text, out var days) || days < 1 ||
            !int.TryParse(RatCountTextBox.Text, out var rats) || rats < 1)
        {
            StatusText.Text = "Días y ratas deben ser números mayores que cero.";
            return;
        }

        if (_entries.Any(entry => entry.Slot is not null &&
                                  (entry.Slot.Day > days || entry.Slot.Rat > rats)))
        {
            StatusText.Text = "No puedes reducir la cuadrícula mientras haya archivos fuera del nuevo límite.";
            return;
        }

        _dayCount = days;
        _ratCount = rats;
        StatusText.Text = $"Cuadrícula actualizada: {_dayCount} día(s), {_ratCount} rata(s).";
        RefreshView();
    }

    private async void ChooseOutputFolderButton_Click(object? sender, RoutedEventArgs eventArgs)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Elegir carpeta de salida",
            AllowMultiple = false
        });
        var folder = folders.FirstOrDefault()?.TryGetLocalPath();
        if (folder is null)
        {
            return;
        }

        _outputDirectory = folder;
        OutputFolderText.Text = $"Carpeta de salida: {_outputDirectory}";
        StatusText.Text = "Carpeta de salida seleccionada.";
        UpdateButtons();
    }

    private async void CreateButton_Click(object? sender, RoutedEventArgs eventArgs)
    {
        if (_outputDirectory is null)
        {
            StatusText.Text = "Elige una carpeta de salida antes de crear los archivos.";
            return;
        }

        CreateButton.IsEnabled = false;
        StatusText.Text = "Creando copias .mat y el Excel de relación…";
        try
        {
            var result = await Task.Run(() => MatOrganizerService.CreateCopiesAndManifest(_outputDirectory, _entries));
            StatusText.Text = $"Listo: {result.FileCount} archivo(s) .mat y relación_renombrado.xlsx creados.";
        }
        catch (Exception exception)
        {
            StatusText.Text = $"No se pudo completar la creación: {exception.Message}";
        }
        finally
        {
            UpdateButtons();
        }
    }

    private void UnassignedListBox_SelectionChanged(object? sender, SelectionChangedEventArgs eventArgs)
    {
        if (UnassignedListBox.SelectedItem is MatFileEntry entry)
        {
            StatusText.Text = $"{entry.FileName} seleccionado. Ahora haz clic en la celda Día–Rata que le corresponde.";
        }

        UpdateButtons();
    }

    private void Slot_PointerPressed(object? sender, PointerPressedEventArgs eventArgs)
    {
        if (sender is not Border { Tag: ProtocolSlot slot })
        {
            return;
        }

        if (UnassignedListBox.SelectedItem is MatFileEntry selectedEntry)
        {
            MoveToSlot(selectedEntry, slot);
            return;
        }

        var assignedEntry = _entries.SingleOrDefault(entry => entry.Slot == slot);
        if (assignedEntry is not null)
        {
            assignedEntry.Slot = null;
            StatusText.Text = $"{assignedEntry.FileName} volvió a la lista. Selecciónalo y haz clic en su nueva celda.";
            RefreshView();
            UnassignedListBox.SelectedItem = assignedEntry;
            return;
        }

        StatusText.Text = "Selecciona primero un archivo de la lista izquierda.";
    }

    private void MoveToSlot(MatFileEntry entry, ProtocolSlot destination)
    {
        var occupied = _entries.SingleOrDefault(candidate => candidate.Slot == destination);
        if (occupied is null || occupied == entry)
        {
            entry.Slot = destination;
            StatusText.Text = $"{entry.FileName} se asignó a {destination.DisplayName}.";
        }
        else if (entry.Slot is null)
        {
            occupied.Slot = null;
            entry.Slot = destination;
            StatusText.Text = $"{entry.FileName} se asignó a {destination.DisplayName}; {occupied.FileName} volvió a la lista sin asignar.";
        }
        else
        {
            (entry.Slot, occupied.Slot) = (occupied.Slot, entry.Slot);
            StatusText.Text = "Las dos sesiones intercambiaron posición.";
        }

        RefreshView();
    }

    private void AddFiles(IEnumerable<string> paths)
    {
        var added = 0;
        var automaticallyAssigned = 0;
        foreach (var path in paths.Where(path => string.Equals(Path.GetExtension(path), ".mat", StringComparison.OrdinalIgnoreCase)))
        {
            if (_entries.Any(entry => string.Equals(entry.SourcePath, path, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var entry = new MatFileEntry(path);
            if (MatOrganizerService.TryGetSlotFromFileName(entry.FileName, out var slot) &&
                slot is not null &&
                slot.Day <= _dayCount &&
                slot.Rat <= _ratCount &&
                _entries.All(candidate => candidate.Slot != slot))
            {
                entry.Slot = slot;
                automaticallyAssigned++;
            }

            _entries.Add(entry);
            added++;
        }

        if (added > 0)
        {
            StatusText.Text = automaticallyAssigned > 0
                ? $"Se agregaron {added} archivo(s); {automaticallyAssigned} se asignaron automáticamente por su nombre."
                : $"Se agregaron {added} archivo(s) .mat.";
            RefreshView();
        }
    }

    private void RefreshView()
    {
        UnassignedListBox.ItemsSource = _entries.Where(entry => entry.Slot is null).OrderBy(entry => entry.FileName).ToList();
        UnassignedTitleText.Text = $"Sin asignar ({_entries.Count(entry => entry.Slot is null)})";
        RebuildAssignmentGrid();
        UpdateButtons();
    }

    private void RebuildAssignmentGrid()
    {
        AssignmentGrid.Children.Clear();
        AssignmentGrid.ColumnDefinitions.Clear();
        AssignmentGrid.RowDefinitions.Clear();

        AssignmentGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        for (var day = 1; day <= _dayCount; day++)
        {
            AssignmentGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(150)));
        }

        AssignmentGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        for (var rat = 1; rat <= _ratCount; rat++)
        {
            AssignmentGrid.RowDefinitions.Add(new RowDefinition(new GridLength(92)));
        }

        AddCell(new TextBlock { Text = "Rata / día", FontWeight = Avalonia.Media.FontWeight.SemiBold, Margin = new Thickness(8) }, 0, 0);
        for (var day = 1; day <= _dayCount; day++)
        {
            AddCell(new TextBlock { Text = day == 10 ? "Prueba" : $"Día {day}", FontWeight = Avalonia.Media.FontWeight.SemiBold, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, Margin = new Thickness(8) }, 0, day);
        }

        for (var rat = 1; rat <= _ratCount; rat++)
        {
            AddCell(new TextBlock { Text = $"Rata {rat}", FontWeight = Avalonia.Media.FontWeight.SemiBold, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Margin = new Thickness(8) }, rat, 0);
            for (var day = 1; day <= _dayCount; day++)
            {
                AddSlotCell(day, rat);
            }
        }
    }

    private void AddSlotCell(int day, int rat)
    {
        var slot = new ProtocolSlot(day, rat);
        var entry = _entries.SingleOrDefault(candidate => candidate.Slot == slot);
        var cell = new Border
        {
            Tag = slot,
            Padding = new Thickness(8),
            Margin = new Thickness(3),
            MinHeight = 68,
            CornerRadius = new CornerRadius(6),
            BorderThickness = new Thickness(1),
            BorderBrush = Avalonia.Media.Brushes.Gray,
            Background = entry is null ? Avalonia.Media.Brushes.Transparent : Avalonia.Media.Brushes.LightSteelBlue,
            Child = new TextBlock
            {
                Text = entry?.FileName ?? "Arrastra aquí",
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                TextAlignment = Avalonia.Media.TextAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            }
        };

        cell.PointerPressed += Slot_PointerPressed;

        AddCell(cell, rat, day);
    }

    private void AddCell(Control control, int row, int column)
    {
        Grid.SetRow(control, row);
        Grid.SetColumn(control, column);
        AssignmentGrid.Children.Add(control);
    }

    private void UpdateButtons()
    {
        var hasSelectedEntry = UnassignedListBox.SelectedItem is MatFileEntry;
        DeselectButton.IsEnabled = hasSelectedEntry;
        RemoveButton.IsEnabled = hasSelectedEntry;
        CreateButton.IsEnabled = _outputDirectory is not null && _entries.Any(entry => entry.Slot is not null);
    }
}
