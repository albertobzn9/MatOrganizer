using Avalonia;

namespace MatOrganizer.Services;

internal enum AppLanguage
{
    English,
    Spanish
}

internal sealed class AppLocalizer
{
    private static readonly AppLocalizer Fallback = new();

    private static readonly IReadOnlyDictionary<string, string> English = new Dictionary<string, string>
    {
        ["Subtitle"] = "Organize sessions by day and rat, create .mat, .mp4, or .mkv copies with a standard name, and generate their Excel manifest.",
        ["Instructions"] = "1. Select the file type and add files.  2. Select a file from the list and click its Day–Rat cell.  3. Enter month-year, phase, file types to copy, and an output folder. Originals are never modified.",
        ["Unassigned"] = "Unassigned ({0})",
        ["Add"] = "Add:",
        ["AddFiles"] = "Add {0} files…",
        ["Deselect"] = "Deselect",
        ["Remove"] = "Remove from list",
        ["Days"] = "Days:",
        ["Rats"] = "Rats:",
        ["UpdateGrid"] = "Update grid",
        ["MonthYear"] = "Month-year:",
        ["Phase"] = "Phase:",
        ["Copy"] = "Copy:",
        ["OutputFolderNotSelected"] = "Output folder: not selected",
        ["ChooseOutputFolder"] = "Choose output folder",
        ["CreateCopies"] = "Create copies and Excel",
        ["InitialStatus"] = "Add .mat files to begin.",
        ["AddFilesTitle"] = "Add {0} files",
        ["FilesOfType"] = "{0} files",
        ["FileRemoved"] = "{0} was removed from the list.",
        ["NoFileSelected"] = "No file is selected.",
        ["DaysRatsPositive"] = "Days and rats must be numbers greater than zero.",
        ["GridCannotReduce"] = "You cannot reduce the grid while files remain outside the new limit.",
        ["GridUpdated"] = "Grid updated: {0} day(s), {1} rat(s).",
        ["ChooseOutputTitle"] = "Choose output folder",
        ["OutputFolder"] = "Output folder: {0}",
        ["OutputFolderSelected"] = "Output folder selected.",
        ["ChooseOutputFirst"] = "Choose an output folder before creating files.",
        ["CopyAtLeastOne"] = "Select at least one file type to copy.",
        ["PreparingCopy"] = "Preparing the copy…",
        ["TotalProgressInitial"] = "Total progress: 0%",
        ["Copying"] = "Copying {0} file(s); videos may take some time…",
        ["Completed"] = "Complete: {0} .mat, {1} .mp4, {2} .mkv, and relacion_renombrado.xlsx created.",
        ["CreateFailed"] = "Could not complete creation: {0}",
        ["SelectedClickSlot"] = "{0} selected. Now click the corresponding Day–Rat cell.",
        ["InputTypeSelected"] = "Select {0} files; you can check more types below to also copy files with the same name.",
        ["ReturnedToList"] = "{0} returned to the list. Select it and click its new cell.",
        ["SelectLeftFirst"] = "Select a file from the left list first.",
        ["AssignedTo"] = "{0} was assigned to {1}.",
        ["AssignedWithReturn"] = "{0} was assigned to {1}; {2} returned to the unassigned list.",
        ["SessionsSwapped"] = "The two sessions swapped positions.",
        ["FilesAddedAuto"] = "Added {0} file(s); {1} were assigned automatically from their names.",
        ["FilesAdded"] = "Added {0} {1} file(s).",
        ["RatDay"] = "Rat / day",
        ["Test"] = "Test",
        ["Day"] = "Day {0}",
        ["Rat"] = "Rat {0}",
        ["DropHere"] = "Click here",
        ["Slot"] = "Day {0} · Rat {1}",
        ["CurrentFileProgress"] = "File {0} of {1}: {2} ({3} of {4})",
        ["TotalProgress"] = "Total progress: {0} of {1} file(s) · {2} of {3}",
        ["InvalidMonthYear"] = "Month-year must use MM-YY or MMYY, for example 01-26 or 0126.",
        ["InvalidPhase"] = "Phase must be cs, cp, or dis.",
        ["ManifestExists"] = "{0} already exists in the output folder.",
        ["ValidOutputFolder"] = "Select a valid output folder.",
        ["AssignAtLeastOne"] = "Assign at least one file to a day and rat.",
        ["MissingInputFile"] = "{0} was not found.",
        ["NoMatchingFiles"] = "No files of the selected type were found to copy.",
        ["DuplicateDestination"] = "Two files would produce the same name: {0}.",
        ["DestinationExists"] = "{0} already exists in the output folder."
    };

    private static readonly IReadOnlyDictionary<string, string> Spanish = new Dictionary<string, string>
    {
        ["Subtitle"] = "Organiza sesiones por día y rata, crea copias .mat, .mp4 o .mkv con un nombre estándar y genera su relación en Excel.",
        ["Instructions"] = "1. Elige el tipo de archivo y cárgalo.  2. Selecciona un archivo de la lista y haz clic en su celda Día–Rata.  3. Indica mes-año, fase, tipos a copiar y una carpeta de salida. Los originales nunca se modifican.",
        ["Unassigned"] = "Sin asignar ({0})",
        ["Add"] = "Agregar:",
        ["AddFiles"] = "Agregar archivos {0}…",
        ["Deselect"] = "Deseleccionar",
        ["Remove"] = "Quitar de la lista",
        ["Days"] = "Días:",
        ["Rats"] = "Ratas:",
        ["UpdateGrid"] = "Actualizar cuadrícula",
        ["MonthYear"] = "Mes-año:",
        ["Phase"] = "Fase:",
        ["Copy"] = "Copiar:",
        ["OutputFolderNotSelected"] = "Carpeta de salida: sin seleccionar",
        ["ChooseOutputFolder"] = "Seleccionar carpeta final",
        ["CreateCopies"] = "Crear copias y Excel",
        ["InitialStatus"] = "Agrega archivos .mat para comenzar.",
        ["AddFilesTitle"] = "Agregar archivos {0}",
        ["FilesOfType"] = "Archivos {0}",
        ["FileRemoved"] = "Se quitó {0} de la lista.",
        ["NoFileSelected"] = "No hay ningún archivo seleccionado.",
        ["DaysRatsPositive"] = "Días y ratas deben ser números mayores que cero.",
        ["GridCannotReduce"] = "No puedes reducir la cuadrícula mientras haya archivos fuera del nuevo límite.",
        ["GridUpdated"] = "Cuadrícula actualizada: {0} día(s), {1} rata(s).",
        ["ChooseOutputTitle"] = "Elegir carpeta de salida",
        ["OutputFolder"] = "Carpeta de salida: {0}",
        ["OutputFolderSelected"] = "Carpeta de salida seleccionada.",
        ["ChooseOutputFirst"] = "Elige una carpeta de salida antes de crear los archivos.",
        ["CopyAtLeastOne"] = "Selecciona por lo menos un tipo de archivo para copiar.",
        ["PreparingCopy"] = "Preparando la copia…",
        ["TotalProgressInitial"] = "Progreso total: 0%",
        ["Copying"] = "Copiando {0} archivo(s); los videos pueden tardar…",
        ["Completed"] = "Listo: {0} .mat, {1} .mp4, {2} .mkv y relación_renombrado.xlsx creados.",
        ["CreateFailed"] = "No se pudo completar la creación: {0}",
        ["SelectedClickSlot"] = "{0} seleccionado. Ahora haz clic en la celda Día–Rata que le corresponde.",
        ["InputTypeSelected"] = "Selecciona archivos {0}; puedes marcar más tipos abajo si también quieres copiar sus archivos con el mismo nombre.",
        ["ReturnedToList"] = "{0} volvió a la lista. Selecciónalo y haz clic en su nueva celda.",
        ["SelectLeftFirst"] = "Selecciona primero un archivo de la lista izquierda.",
        ["AssignedTo"] = "{0} se asignó a {1}.",
        ["AssignedWithReturn"] = "{0} se asignó a {1}; {2} volvió a la lista sin asignar.",
        ["SessionsSwapped"] = "Las dos sesiones intercambiaron posición.",
        ["FilesAddedAuto"] = "Se agregaron {0} archivo(s); {1} se asignaron automáticamente por su nombre.",
        ["FilesAdded"] = "Se agregaron {0} archivo(s) {1}.",
        ["RatDay"] = "Rata / día",
        ["Test"] = "Prueba",
        ["Day"] = "Día {0}",
        ["Rat"] = "Rata {0}",
        ["DropHere"] = "Haz clic aquí",
        ["Slot"] = "Día {0} · Rata {1}",
        ["CurrentFileProgress"] = "Archivo {0} de {1}: {2} ({3} de {4})",
        ["TotalProgress"] = "Progreso total: {0} de {1} archivo(s) · {2} de {3}",
        ["InvalidMonthYear"] = "Mes-año debe tener el formato MM-AA o MMAA, por ejemplo 01-26 o 0126.",
        ["InvalidPhase"] = "La fase debe ser cs, cp o dis.",
        ["ManifestExists"] = "Ya existe {0} en la carpeta de salida.",
        ["ValidOutputFolder"] = "Selecciona una carpeta de salida válida.",
        ["AssignAtLeastOne"] = "Asigna por lo menos un archivo a un día y una rata.",
        ["MissingInputFile"] = "No se encontró {0}.",
        ["NoMatchingFiles"] = "No se encontraron archivos del tipo seleccionado para copiar.",
        ["DuplicateDestination"] = "Dos archivos producirían el mismo nombre: {0}.",
        ["DestinationExists"] = "Ya existe {0} en la carpeta de salida."
    };

    public AppLanguage CurrentLanguage { get; private set; } = AppLanguage.English;

    public event EventHandler? LanguageChanged;

    public static AppLocalizer Current => (Application.Current as App)?.Localizer ?? Fallback;

    public string Get(string key) => (CurrentLanguage == AppLanguage.English ? English : Spanish)[key];

    public string Format(string key, params object[] values) => string.Format(Get(key), values);

    public void SetLanguage(AppLanguage language)
    {
        if (CurrentLanguage == language)
        {
            return;
        }

        CurrentLanguage = language;
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }
}
