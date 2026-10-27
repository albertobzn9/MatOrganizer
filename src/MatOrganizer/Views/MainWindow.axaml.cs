using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using MatOrganizer.Services;

namespace MatOrganizer.Views;

public partial class MainWindow : Window
{
    private readonly List<MatFileEntry> _entries = [];
    private readonly AppLocalizer _localizer;
    private int _dayCount = 10;
    private int _ratCount = 1;
    private string? _outputDirectory;
    private bool _applyingLanguage;

    public MainWindow()
    {
        InitializeComponent();
        _localizer = AppLocalizer.Current;
        _localizer.LanguageChanged += Localizer_LanguageChanged;
        Closed += (_, _) => _localizer.LanguageChanged -= Localizer_LanguageChanged;
        ApplyLocalizedText();
        StatusText.Text = _localizer.Get("InitialStatus");
        RefreshView();
    }

    private void Localizer_LanguageChanged(object? sender, EventArgs eventArgs)
    {
        ApplyLocalizedText();
        RefreshView();
    }

    private void ApplyLocalizedText()
    {
        SubtitleText.Text = _localizer.Get("Subtitle");
        InstructionsText.Text = _localizer.Get("Instructions");
        AddLabelText.Text = _localizer.Get("Add");
        SelectFilesButton.Content = _localizer.Format("AddFiles", GetSelectedInputFileType());
        DeselectButton.Content = _localizer.Get("Deselect");
        RemoveButton.Content = _localizer.Get("Remove");
        DaysLabelText.Text = _localizer.Get("Days");
        RatsLabelText.Text = _localizer.Get("Rats");
        UpdateGridButton.Content = _localizer.Get("UpdateGrid");
        MonthYearLabelText.Text = _localizer.Get("MonthYear");
        PhaseLabelText.Text = _localizer.Get("Phase");
        CopyLabelText.Text = _localizer.Get("Copy");
        ChooseOutputFolderButton.Content = _localizer.Get("ChooseOutputFolder");
        CreateButton.Content = _localizer.Get("CreateCopies");
        OutputFolderText.Text = _outputDirectory is null
            ? _localizer.Get("OutputFolderNotSelected")
            : _localizer.Format("OutputFolder", _outputDirectory);

        _applyingLanguage = true;
        LanguageToggleButton.IsChecked = _localizer.CurrentLanguage == AppLanguage.English;
        _applyingLanguage = false;
        UpdateLanguageToggleAppearance();
    }

    private void LanguageToggleButton_IsCheckedChanged(object? sender, RoutedEventArgs eventArgs)
    {
        if (!_applyingLanguage)
        {
            _localizer.SetLanguage(LanguageToggleButton.IsChecked == true
                ? AppLanguage.English
                : AppLanguage.Spanish);
        }
    }

    private void UpdateLanguageToggleAppearance()
    {
        var englishSelected = _localizer.CurrentLanguage == AppLanguage.English;
        EnglishLanguageText.FontWeight = englishSelected ? FontWeight.Bold : FontWeight.Normal;
        SpanishLanguageText.FontWeight = englishSelected ? FontWeight.Normal : FontWeight.Bold;
        EnglishLanguageText.Foreground = new SolidColorBrush(Color.Parse(englishSelected ? "#0F6CBD" : "#6B7280"));
        SpanishLanguageText.Foreground = new SolidColorBrush(Color.Parse(englishSelected ? "#6B7280" : "#0F6CBD"));
    }

    private async void SelectFilesButton_Click(object? sender, RoutedEventArgs eventArgs)
    {
        var extension = GetSelectedInputFileType();
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = _localizer.Format("AddFilesTitle", extension),
            AllowMultiple = true,
            FileTypeFilter =
            [
                new FilePickerFileType(_localizer.Format("FilesOfType", extension)) { Patterns = [$"*{extension}"] }
            ]
        });

        AddFiles(files.Select(file => file.TryGetLocalPath()).OfType<string>(), extension);
    }

    private void RemoveButton_Click(object? sender, RoutedEventArgs eventArgs)
    {
        if (UnassignedListBox.SelectedItem is not MatFileEntry entry)
        {
            return;
        }

        _entries.Remove(entry);
        StatusText.Text = _localizer.Format("FileRemoved", entry.FileName);
        RefreshView();
    }

    private void DeselectButton_Click(object? sender, RoutedEventArgs eventArgs)
    {
        UnassignedListBox.SelectedItem = null;
        StatusText.Text = _localizer.Get("NoFileSelected");
        UpdateButtons();
    }

    private void UpdateGridButton_Click(object? sender, RoutedEventArgs eventArgs)
    {
        if (!int.TryParse(DayCountTextBox.Text, out var days) || days < 1 ||
            !int.TryParse(RatCountTextBox.Text, out var rats) || rats < 1)
        {
            StatusText.Text = _localizer.Get("DaysRatsPositive");
            return;
        }

        if (_entries.Any(entry => entry.Slot is not null &&
                                  (entry.Slot.Day > days || entry.Slot.Rat > rats)))
        {
            StatusText.Text = _localizer.Get("GridCannotReduce");
            return;
        }

        _dayCount = days;
        _ratCount = rats;
        StatusText.Text = _localizer.Format("GridUpdated", _dayCount, _ratCount);
        RefreshView();
    }

    private async void ChooseOutputFolderButton_Click(object? sender, RoutedEventArgs eventArgs)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = _localizer.Get("ChooseOutputTitle"),
            AllowMultiple = false
        });
        var folder = folders.FirstOrDefault()?.TryGetLocalPath();
        if (folder is null)
        {
            return;
        }

        _outputDirectory = folder;
        OutputFolderText.Text = _localizer.Format("OutputFolder", _outputDirectory);
        StatusText.Text = _localizer.Get("OutputFolderSelected");
        UpdateButtons();
    }

    private async void CreateButton_Click(object? sender, RoutedEventArgs eventArgs)
    {
        if (_outputDirectory is null)
        {
            StatusText.Text = _localizer.Get("ChooseOutputFirst");
            return;
        }

        var copyOptions = GetCopyOptions();
        if (!copyOptions.HasSelectedTypes)
        {
            StatusText.Text = _localizer.Get("CopyAtLeastOne");
            return;
        }

        SessionNamingOptions namingOptions;
        try
        {
            namingOptions = SessionNamingOptions.Create(MonthYearTextBox.Text, GetSelectedPhase());
        }
        catch (InvalidOperationException exception)
        {
            StatusText.Text = exception.Message;
            return;
        }

        CreateButton.IsEnabled = false;
        ProgressPanel.IsVisible = true;
        CurrentFileProgressBar.Value = 0;
        TotalProgressBar.Value = 0;
        CurrentFileProgressText.Text = _localizer.Get("PreparingCopy");
        TotalProgressText.Text = _localizer.Get("TotalProgressInitial");
        try
        {
            var preview = await Task.Run(() => MatOrganizerService.BuildRecords(_outputDirectory, _entries, namingOptions, copyOptions));
            StatusText.Text = _localizer.Format("Copying", preview.Count);
            var progress = new Progress<CopyProgress>(UpdateCopyProgress);
            var result = await MatOrganizerService.CreateCopiesAndManifestAsync(
                _outputDirectory,
                _entries,
                namingOptions,
                copyOptions,
                progress);
            StatusText.Text = _localizer.Format("Completed", result.MatCount, result.Mp4Count, result.MkvCount);
        }
        catch (Exception exception)
        {
            StatusText.Text = _localizer.Format("CreateFailed", exception.Message);
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
            StatusText.Text = _localizer.Format("SelectedClickSlot", entry.FileName);
        }

        UpdateButtons();
    }

    private void CopyOption_IsCheckedChanged(object? sender, RoutedEventArgs eventArgs)
    {
        if (CopyMatCheckBox is null || CopyMp4CheckBox is null || CopyMkvCheckBox is null)
        {
            return;
        }

        UpdateButtons();
    }

    private void InputFileTypeComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs eventArgs)
    {
        if (SelectFilesButton is null || CopyMatCheckBox is null || CopyMp4CheckBox is null || CopyMkvCheckBox is null)
        {
            return;
        }

        var extension = GetSelectedInputFileType();
        SelectFilesButton.Content = _localizer.Format("AddFiles", extension);
        CopyMatCheckBox.IsChecked = extension == ".mat";
        CopyMp4CheckBox.IsChecked = extension == ".mp4";
        CopyMkvCheckBox.IsChecked = extension == ".mkv";
        StatusText.Text = _localizer.Format("InputTypeSelected", extension);
        UpdateButtons();
    }

    private void NamingOption_Changed(object? sender, RoutedEventArgs eventArgs) => UpdateButtons();

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
            StatusText.Text = _localizer.Format("ReturnedToList", assignedEntry.FileName);
            RefreshView();
            UnassignedListBox.SelectedItem = assignedEntry;
            return;
        }

        StatusText.Text = _localizer.Get("SelectLeftFirst");
    }

    private void MoveToSlot(MatFileEntry entry, ProtocolSlot destination)
    {
        var occupied = _entries.SingleOrDefault(candidate => candidate.Slot == destination);
        if (occupied is null || occupied == entry)
        {
            entry.Slot = destination;
            StatusText.Text = _localizer.Format("AssignedTo", entry.FileName, destination.DisplayName);
        }
        else if (entry.Slot is null)
        {
            occupied.Slot = null;
            entry.Slot = destination;
            StatusText.Text = _localizer.Format("AssignedWithReturn", entry.FileName, destination.DisplayName, occupied.FileName);
        }
        else
        {
            (entry.Slot, occupied.Slot) = (occupied.Slot, entry.Slot);
            StatusText.Text = _localizer.Get("SessionsSwapped");
        }

        RefreshView();
    }

    private void AddFiles(IEnumerable<string> paths, string selectedExtension)
    {
        var added = 0;
        var automaticallyAssigned = 0;
        foreach (var path in paths.Where(path => string.Equals(Path.GetExtension(path), selectedExtension, StringComparison.OrdinalIgnoreCase)))
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
                ? _localizer.Format("FilesAddedAuto", added, automaticallyAssigned)
                : _localizer.Format("FilesAdded", added, selectedExtension);
            RefreshView();
        }
    }

    private void RefreshView()
    {
        UnassignedListBox.ItemsSource = _entries.Where(entry => entry.Slot is null).OrderBy(entry => entry.FileName).ToList();
        UnassignedTitleText.Text = _localizer.Format("Unassigned", _entries.Count(entry => entry.Slot is null));
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

        AddCell(new TextBlock { Text = _localizer.Get("RatDay"), FontWeight = FontWeight.SemiBold, Margin = new Thickness(8) }, 0, 0);
        for (var day = 1; day <= _dayCount; day++)
        {
            var label = day == 10 ? _localizer.Get("Test") : _localizer.Format("Day", day);
            AddCell(new TextBlock { Text = label, FontWeight = FontWeight.SemiBold, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, Margin = new Thickness(8) }, 0, day);
        }

        for (var rat = 1; rat <= _ratCount; rat++)
        {
            AddCell(new TextBlock { Text = _localizer.Format("Rat", rat), FontWeight = FontWeight.SemiBold, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Margin = new Thickness(8) }, rat, 0);
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
            BorderBrush = Brushes.Gray,
            Background = entry is null ? Brushes.Transparent : Brushes.LightSteelBlue,
            Child = new TextBlock
            {
                Text = entry?.FileName ?? _localizer.Get("DropHere"),
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
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
        if (UnassignedListBox is null || DeselectButton is null || RemoveButton is null ||
            CreateButton is null || CopyMatCheckBox is null || CopyMp4CheckBox is null ||
            CopyMkvCheckBox is null || MonthYearTextBox is null || PhaseComboBox is null)
        {
            return;
        }

        var hasSelectedEntry = UnassignedListBox.SelectedItem is MatFileEntry;
        DeselectButton.IsEnabled = hasSelectedEntry;
        RemoveButton.IsEnabled = hasSelectedEntry;
        CreateButton.IsEnabled = _outputDirectory is not null &&
                                 _entries.Any(entry => entry.Slot is not null) &&
                                 GetCopyOptions().HasSelectedTypes &&
                                 SessionNamingOptions.IsValid(MonthYearTextBox.Text, GetSelectedPhase());
    }

    private CopyOptions GetCopyOptions() => new(
        CopyMatCheckBox.IsChecked == true,
        CopyMp4CheckBox.IsChecked == true,
        CopyMkvCheckBox.IsChecked == true);

    private string GetSelectedInputFileType() =>
        (InputFileTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? ".mat";

    private string GetSelectedPhase() =>
        (PhaseComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty;

    private void UpdateCopyProgress(CopyProgress progress)
    {
        var currentPercent = progress.CurrentFileLength == 0
            ? 100d
            : progress.CurrentFileBytes * 100d / progress.CurrentFileLength;
        var totalPercent = progress.TotalBytes == 0
            ? 100d
            : progress.CompletedBytes * 100d / progress.TotalBytes;

        CurrentFileProgressBar.Value = currentPercent;
        TotalProgressBar.Value = totalPercent;
        CurrentFileProgressText.Text = _localizer.Format(
            "CurrentFileProgress",
            progress.CurrentFileNumber,
            progress.TotalFiles,
            progress.CurrentFileName,
            FormatBytes(progress.CurrentFileBytes),
            FormatBytes(progress.CurrentFileLength));
        TotalProgressText.Text = _localizer.Format(
            "TotalProgress",
            progress.CompletedFiles,
            progress.TotalFiles,
            FormatBytes(progress.CompletedBytes),
            FormatBytes(progress.TotalBytes));
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var value = (double)bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return $"{value:0.##} {units[unit]}";
    }
}
