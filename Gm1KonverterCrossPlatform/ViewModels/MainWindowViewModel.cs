using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Avalonia.Media.Imaging;
using Gm1KonverterCrossPlatform.Core.BuildingOffsets;
using Gm1KonverterCrossPlatform.Core.Diagnostics;
using Gm1KonverterCrossPlatform.Core.Documents;
using Gm1KonverterCrossPlatform.Core.Files;
using Gm1KonverterCrossPlatform.Core.Imaging;
using Gm1KonverterCrossPlatform.Core.IO;
using Gm1KonverterCrossPlatform.Core.Services;
using Gm1KonverterCrossPlatform.Core.Settings;
using Gm1KonverterCrossPlatform.HelperClasses;
using ReactiveUI;

namespace Gm1KonverterCrossPlatform.ViewModels
{
    /// <summary>
    /// State of the main window and all operations on the opened file. Operations throw exceptions on
    /// errors (<see cref="WorkflowException"/> for expected problems); the view shows them to the user.
    /// </summary>
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly UserConfigStore configStore;
        private UserConfig userConfig = new UserConfig();

        private Language actualLanguage;
        private ColorTheme actualColorTheme;
        private bool openFolderAfterExport;
        private bool loggerActiv;

        private IReadOnlyList<string> workfolderFiles = Array.Empty<string>();
        private IReadOnlyList<string> strongholdFiles = Array.Empty<string>();
        private IReadOnlyList<string> gfxFiles = Array.Empty<string>();

        private Gm1Document? gm1Document;
        private TgxDocument? tgxDocument;
        private Gm1FileHeader? fileHeader;
        private TgxImageHeader? selectedImageHeader;
        private ObservableCollection<ImagePreviewItem> tgxImages = new ObservableCollection<ImagePreviewItem>();
        private WriteableBitmap? actuellColorTable;
        private Gm1DataType filetype;
        private bool fileSelected;
        private int actualPalette = 1;

        private bool buttonsEnabled;
        private bool importButtonEnabled;
        private bool colorButtonsEnabled;
        private bool orginalStrongholdAnimationButtonEnabled;
        private bool tgxButtonExportEnabled;
        private bool tgxButtonImportEnabled;
        private bool replaceWithSaveFile;
        private bool replaceWithSaveFileTgx;

        private int delay = 100;
        private int bigImageWidth = 900;
        private bool gm1PreviewTrue = true;
        private bool gfxPreviewTrue;
        private string toggleButtonName = "GM1";

        private IBuildingOffsetTarget? offsetTarget;
        private ImagePreviewItem? selectedItem;
        private int selectedOffsetImageIndex = -1;
        private bool offsetExpanderVisible;
        private sbyte xOffset;
        private int yOffset;

        public MainWindowViewModel()
            : this(new UserConfigStore(Config.AppDataPath))
        {
        }

        public MainWindowViewModel(UserConfigStore configStore)
        {
            this.configStore = configStore ?? throw new ArgumentNullException(nameof(configStore));
        }

        public Language ActualLanguage
        {
            get => actualLanguage;
            set
            {
                this.RaiseAndSetIfChanged(ref actualLanguage, value);
                HelperClasses.Languages.SelectLanguage(value);
                userConfig.Language = value;
                SaveUserConfig();
            }
        }

        public Language[] Languages => HelperClasses.Languages.All;

        public ColorTheme ActualColorTheme
        {
            get => actualColorTheme;
            set
            {
                this.RaiseAndSetIfChanged(ref actualColorTheme, value);
                HelperClasses.ColorThemes.SelectColorTheme(value);
                userConfig.ColorTheme = value;
                SaveUserConfig();
            }
        }

        public ColorTheme[] ColorThemes => HelperClasses.ColorThemes.All;

        public bool OpenFolderAfterExport
        {
            get => openFolderAfterExport;
            set
            {
                this.RaiseAndSetIfChanged(ref openFolderAfterExport, value);
                userConfig.OpenFolderAfterExport = value;
                SaveUserConfig();
            }
        }

        public bool LoggerActiv
        {
            get => loggerActiv;
            set
            {
                this.RaiseAndSetIfChanged(ref loggerActiv, value);
                Logger.IsEnabled = value;
                userConfig.ActivateLogger = value;
                SaveUserConfig();
            }
        }

        public string? CrusaderPath => userConfig.CrusaderPath;

        public string? WorkFolderPath => userConfig.WorkFolderPath;

        public IReadOnlyList<string> WorkfolderFiles
        {
            get => workfolderFiles;
            private set => this.RaiseAndSetIfChanged(ref workfolderFiles, value);
        }

        public IReadOnlyList<string> StrongholdFiles
        {
            get => strongholdFiles;
            private set => this.RaiseAndSetIfChanged(ref strongholdFiles, value);
        }

        public IReadOnlyList<string> GfxFiles
        {
            get => gfxFiles;
            private set => this.RaiseAndSetIfChanged(ref gfxFiles, value);
        }

        public bool Gm1PreviewTrue
        {
            get => gm1PreviewTrue;
            set
            {
                this.RaiseAndSetIfChanged(ref gm1PreviewTrue, value);
                GfxPreviewTrue = !value;
                ToggleButtonName = value ? "GM1" : "GFX";
            }
        }

        public bool GfxPreviewTrue
        {
            get => gfxPreviewTrue;
            set => this.RaiseAndSetIfChanged(ref gfxPreviewTrue, value);
        }

        public string ToggleButtonName
        {
            get => toggleButtonName;
            set => this.RaiseAndSetIfChanged(ref toggleButtonName, value);
        }

        public Gm1Document? Gm1Document => gm1Document;

        public TgxDocument? TgxDocument => tgxDocument;

        public bool FileSelected
        {
            get => fileSelected;
            private set => this.RaiseAndSetIfChanged(ref fileSelected, value);
        }

        public Gm1DataType Filetype
        {
            get => filetype;
            private set => this.RaiseAndSetIfChanged(ref filetype, value);
        }

        /// <summary>Header of the opened .gm1 file, shown in the header expander.</summary>
        public Gm1FileHeader? FileHeader
        {
            get => fileHeader;
            private set => this.RaiseAndSetIfChanged(ref fileHeader, value);
        }

        /// <summary>Header of the selected image, shown in the image header expander.</summary>
        public TgxImageHeader? SelectedImageHeader
        {
            get => selectedImageHeader;
            private set => this.RaiseAndSetIfChanged(ref selectedImageHeader, value);
        }

        public ObservableCollection<ImagePreviewItem> TGXImages
        {
            get => tgxImages;
            private set => this.RaiseAndSetIfChanged(ref tgxImages, value);
        }

        public WriteableBitmap? ActuellColorTable
        {
            get => actuellColorTable;
            private set => this.RaiseAndSetIfChanged(ref actuellColorTable, value);
        }

        /// <summary>1 based number of the selected color table.</summary>
        public int ActualPalette
        {
            get => actualPalette;
            private set => this.RaiseAndSetIfChanged(ref actualPalette, value);
        }

        public int Delay
        {
            get => delay;
            set => this.RaiseAndSetIfChanged(ref delay, value);
        }

        public int BigImageWidth
        {
            get => bigImageWidth;
            set => this.RaiseAndSetIfChanged(ref bigImageWidth, value);
        }

        public bool ButtonsEnabled
        {
            get => buttonsEnabled;
            private set => this.RaiseAndSetIfChanged(ref buttonsEnabled, value);
        }

        public bool ImportButtonEnabled
        {
            get => importButtonEnabled;
            private set => this.RaiseAndSetIfChanged(ref importButtonEnabled, value);
        }

        public bool ColorButtonsEnabled
        {
            get => colorButtonsEnabled;
            private set => this.RaiseAndSetIfChanged(ref colorButtonsEnabled, value);
        }

        public bool OrginalStrongholdAnimationButtonEnabled
        {
            get => orginalStrongholdAnimationButtonEnabled;
            private set => this.RaiseAndSetIfChanged(ref orginalStrongholdAnimationButtonEnabled, value);
        }

        public bool TgxButtonExportEnabled
        {
            get => tgxButtonExportEnabled;
            private set => this.RaiseAndSetIfChanged(ref tgxButtonExportEnabled, value);
        }

        public bool TgxButtonImportEnabled
        {
            get => tgxButtonImportEnabled;
            private set => this.RaiseAndSetIfChanged(ref tgxButtonImportEnabled, value);
        }

        public bool ReplaceWithSaveFile
        {
            get => replaceWithSaveFile;
            private set => this.RaiseAndSetIfChanged(ref replaceWithSaveFile, value);
        }

        public bool ReplaceWithSaveFileTgx
        {
            get => replaceWithSaveFileTgx;
            private set => this.RaiseAndSetIfChanged(ref replaceWithSaveFileTgx, value);
        }

        public bool OffsetExpanderVisible
        {
            get => offsetExpanderVisible;
            private set => this.RaiseAndSetIfChanged(ref offsetExpanderVisible, value);
        }

        public sbyte XOffset
        {
            get => xOffset;
            set => this.RaiseAndSetIfChanged(ref xOffset, value);
        }

        public int YOffset
        {
            get => yOffset;
            set => this.RaiseAndSetIfChanged(ref yOffset, value);
        }

        /// <summary>Offsets.json in the work folder, if it exists.</summary>
        public string? ExistingOffsetsFile
        {
            get
            {
                var workFolder = TryGetWorkFolder();
                return workFolder != null && File.Exists(workFolder.OffsetsFile) ? workFolder.OffsetsFile : null;
            }
        }

        /// <summary>Loads the user config and the file lists.</summary>
        public void Initialize()
        {
            userConfig = configStore.Load();

            Logger.IsEnabled = userConfig.ActivateLogger;
            HelperClasses.Languages.SelectLanguage(userConfig.Language);
            HelperClasses.ColorThemes.SelectColorTheme(userConfig.ColorTheme);

            actualLanguage = userConfig.Language;
            actualColorTheme = userConfig.ColorTheme;
            openFolderAfterExport = userConfig.OpenFolderAfterExport;
            loggerActiv = userConfig.ActivateLogger;
            this.RaisePropertyChanged(nameof(ActualLanguage));
            this.RaisePropertyChanged(nameof(ActualColorTheme));
            this.RaisePropertyChanged(nameof(OpenFolderAfterExport));
            this.RaisePropertyChanged(nameof(LoggerActiv));

            LoadStrongholdFiles();
            LoadWorkfolderFiles();
        }

        public void SetCrusaderPath(string path)
        {
            userConfig.CrusaderPath = StrongholdFolder.NormalizeRoot(path);
            SaveUserConfig();
            this.RaisePropertyChanged(nameof(CrusaderPath));
            LoadStrongholdFiles();
        }

        public void SetWorkFolderPath(string path)
        {
            userConfig.WorkFolderPath = path;
            SaveUserConfig();
            this.RaisePropertyChanged(nameof(WorkFolderPath));
            LoadWorkfolderFiles();
        }

        public void LoadStrongholdFiles()
        {
            var folder = TryGetStrongholdFolder();
            StrongholdFiles = folder?.GetGm1FileNames() ?? Array.Empty<string>();
            GfxFiles = folder?.GetTgxFileNames() ?? Array.Empty<string>();
        }

        public void LoadWorkfolderFiles()
        {
            var workFolder = TryGetWorkFolder();
            if (workFolder == null)
            {
                WorkfolderFiles = Array.Empty<string>();
                return;
            }

            Directory.CreateDirectory(workFolder.Root);
            WorkfolderFiles = Directory.GetDirectories(workFolder.Root)
                .Select(Path.GetFileName)
                .OfType<string>()
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <exception cref="InvalidDataException">The file is not a valid .gm1 file.</exception>
        public void OpenGm1File(string fileName)
        {
            Logger.Log($"Open GM1 file {fileName}");
            var folder = RequireStrongholdFolder();

            CloseFiles();
            var document = Gm1Document.Load(folder.Gm1File(fileName));
            gm1Document = document;
            this.RaisePropertyChanged(nameof(Gm1Document));

            FileSelected = true;
            Filetype = document.DataType;
            FileHeader = document.File.Header;

            if (!document.IsSupported)
            {
                throw new WorkflowException($"{(uint)document.DataType} {Localization.GetText("TilesarenotSupportedyet")}");
            }

            ButtonsEnabled = true;
            ImportButtonEnabled = true;
            ColorButtonsEnabled = document.HasColorTables;
            OrginalStrongholdAnimationButtonEnabled = document.HasColorTables;
            ReplaceWithSaveFile = BackupExists(fileName);
            ActualPalette = document.ColorTableIndex + 1;

            offsetTarget = CastleOffsetAddresses.AppliesTo(fileName) ? LoadOffsetTarget() : null;

            RefreshPreview();
        }

        public string ExportImages() => AfterExport(new Gm1Exporter(RequireWorkFolder()).ExportImages(RequireGm1()));

        public string ExportBigImage()
        {
            if (BigImageWidth <= 0)
            {
                throw new WorkflowException($"{Localization.GetText("ImageSize")}: {BigImageWidth} <= 0");
            }

            return AfterExport(new Gm1Exporter(RequireWorkFolder()).ExportBigImage(RequireGm1(), BigImageWidth));
        }

        public string ExportColorTables() => AfterExport(new Gm1Exporter(RequireWorkFolder()).ExportColorTables(RequireGm1()));

        public string ExportOriginalAnimation() => AfterExport(new Gm1Exporter(RequireWorkFolder()).ExportOriginalAnimation(RequireGm1()));

        public void ImportImages()
        {
            RunAndRefreshPreview(() => new Gm1Importer(RequireWorkFolder()).ImportImages(RequireGm1()));
        }

        public void ImportBigImage()
        {
            RunAndRefreshPreview(() => new Gm1Importer(RequireWorkFolder()).ImportBigImage(RequireGm1()));
        }

        public void ImportColorTables()
        {
            RunAndRefreshPreview(() => new Gm1Importer(RequireWorkFolder()).ImportColorTables(RequireGm1()));
        }

        public void ImportOriginalAnimation()
        {
            RunAndRefreshPreview(() => new Gm1Importer(RequireWorkFolder()).ImportOriginalAnimation(RequireGm1()));
        }

        /// <summary>Shows the next (+1) or previous (-1) color table. Imported images are kept.</summary>
        public void ChangeColorTable(int step)
        {
            var document = RequireGm1();
            document.ColorTableIndex = ((document.ColorTableIndex + step) % Palette.ColorTableCount + Palette.ColorTableCount) % Palette.ColorTableCount;
            ActualPalette = document.ColorTableIndex + 1;
            RefreshPreview();
        }

        /// <summary>A copy of the current color table for editing.</summary>
        public ColorTable CopyCurrentColorTable() => RequireGm1().CurrentColorTable.Copy();

        public void ReplaceCurrentColorTable(ColorTable colorTable)
        {
            var document = RequireGm1();
            document.File.Palette.ColorTables[document.ColorTableIndex] = colorTable ?? throw new ArgumentNullException(nameof(colorTable));
            RefreshPreview();
        }

        /// <summary>Writes the modified file into the Stronghold folder, keeping a backup of the original.</summary>
        public void InstallGm1File()
        {
            var document = RequireGm1();
            var workFolder = RequireWorkFolder();
            var strongholdFolder = RequireStrongholdFolder();

            GameFileInstaller.Install(
                strongholdFolder.Gm1File(document.FileName),
                document.ToBytes(),
                workFolder.BackupFile(document.FileName),
                workFolder.ModdedFile(document.FileName));

            ReopenGm1File(document);
            LoadWorkfolderFiles();
        }

        public void RestoreGm1File()
        {
            var document = RequireGm1();
            GameFileInstaller.Restore(RequireWorkFolder().BackupFile(document.FileName), RequireStrongholdFolder().Gm1File(document.FileName));
            ReopenGm1File(document);
        }

        public string ExportGif(IEnumerable<ImagePreviewItem> selectedItems)
        {
            var frames = selectedItems.Select(item => item.Pixels).ToList();
            if (frames.Count == 0)
            {
                throw new WorkflowException(Localization.GetText("SelectGif"));
            }

            string fileName = gm1Document?.FileName ?? tgxDocument?.FileName ?? throw new WorkflowException(Localization.GetText("SelectGif"));
            var workFolder = RequireWorkFolder();
            GifExporter.Save(frames, Delay, workFolder.GifFile(fileName));
            return AfterExport(workFolder.GifFolder(fileName));
        }

        /// <exception cref="InvalidDataException">The file is not a valid .tgx file.</exception>
        public void OpenTgxFile(string fileName)
        {
            Logger.Log($"Open TGX file {fileName}");
            var folder = RequireStrongholdFolder();

            CloseFiles();
            tgxDocument = TgxDocument.Load(folder.TgxFile(fileName));
            this.RaisePropertyChanged(nameof(TgxDocument));

            TgxButtonExportEnabled = true;
            var workFolder = TryGetWorkFolder();
            TgxButtonImportEnabled = workFolder != null && File.Exists(workFolder.TgxImageFile(fileName));
            ReplaceWithSaveFileTgx = BackupExists(fileName);

            RefreshPreview();
        }

        public string ExportTgxImage()
        {
            string folder = new TgxImageTransfer(RequireWorkFolder()).Export(RequireTgx());
            TgxButtonImportEnabled = true;
            return AfterExport(folder);
        }

        public void ImportTgxImage()
        {
            RunAndRefreshPreview(() => new TgxImageTransfer(RequireWorkFolder()).Import(RequireTgx()));
        }

        public void InstallTgxFile()
        {
            var document = RequireTgx();
            GameFileInstaller.Install(
                RequireStrongholdFolder().TgxFile(document.FileName),
                document.ToBytes(),
                RequireWorkFolder().BackupFile(document.FileName));

            ReplaceWithSaveFileTgx = true;
            LoadWorkfolderFiles();
        }

        public void RestoreTgxFile()
        {
            var document = RequireTgx();
            GameFileInstaller.Restore(RequireWorkFolder().BackupFile(document.FileName), RequireStrongholdFolder().TgxFile(document.FileName));
            OpenTgxFile(document.FileName);
        }

        public void SelectImage(ImagePreviewItem? item)
        {
            selectedItem = item;
            SelectedImageHeader = item?.Header;

            selectedOffsetImageIndex = -1;
            OffsetExpanderVisible = false;

            if (item == null || offsetTarget == null || !offsetTarget.Supports(item.Index))
            {
                return;
            }

            if (offsetTarget.TryRead(item.Index, out var offset))
            {
                XOffset = (sbyte)Math.Max(sbyte.MinValue, Math.Min(sbyte.MaxValue, offset.X));
                YOffset = offset.Y;
                selectedOffsetImageIndex = item.Index;
                OffsetExpanderVisible = true;
            }
        }

        /// <summary>Writes the offset of the selected image into the executables and Offsets.json.</summary>
        public void ChangeSelectedOffset()
        {
            if (selectedOffsetImageIndex < 0)
            {
                return;
            }

            var offset = new BuildingOffset(XOffset, YOffset);
            var patcher = RequireOffsetPatcher();
            patcher.Write(selectedOffsetImageIndex, offset);
            patcher.Save();
            offsetTarget = IsCastleFileOpen ? patcher : null;

            var workFolder = TryGetWorkFolder();
            if (workFolder != null)
            {
                BuildingOffsetStore.Load(workFolder.OffsetsFile).Set(selectedOffsetImageIndex, offset);
            }
        }

        /// <summary>Applies all offsets of an offset file to the executables.</summary>
        /// <returns>The number of applied offsets.</returns>
        public int ApplyOffsetsFromFile(string path)
        {
            var offsets = BuildingOffsetStore.Load(path).Offsets;
            var patcher = RequireOffsetPatcher();

            int applied = 0;
            foreach (var entry in offsets.Where(entry => patcher.Supports(entry.Key)))
            {
                patcher.Write(entry.Key, entry.Value);
                applied++;
            }

            patcher.Save();
            if (IsCastleFileOpen)
            {
                // Show the applied values instead of the ones read before.
                offsetTarget = patcher;
                SelectImage(selectedItem);
            }

            var workFolder = TryGetWorkFolder();
            if (workFolder != null && !PathsEqual(path, workFolder.OffsetsFile))
            {
                var store = BuildingOffsetStore.Load(workFolder.OffsetsFile);
                foreach (var entry in offsets.Where(entry => patcher.Supports(entry.Key)))
                {
                    store.Set(entry.Key, entry.Value);
                }
            }

            return applied;
        }

        public void OpenStrongholdFolder() => FolderLauncher.Open(RequireStrongholdFolder().Root);

        public void OpenWorkFolder() => FolderLauncher.Open(RequireWorkFolder().Root);

        public void OpenWorkFolderEntry(string name) => FolderLauncher.Open(Path.Combine(RequireWorkFolder().Root, name));

        public void OpenLogFolder() => FolderLauncher.Open(Logger.Directory);

        private bool IsCastleFileOpen => gm1Document != null && CastleOffsetAddresses.AppliesTo(gm1Document.FileName);

        private void RefreshPreview()
        {
            var items = new ObservableCollection<ImagePreviewItem>();

            if (gm1Document != null && gm1Document.IsSupported)
            {
                var images = gm1Document.RenderItems();
                for (int i = 0; i < images.Count; i++)
                {
                    items.Add(new ImagePreviewItem(i, images[i], gm1Document.GetFirstImageOfItem(i).Header));
                }

                ActuellColorTable = gm1Document.HasColorTables
                    ? BitmapFactory.Create(ColorTableImage.Render(gm1Document.CurrentColorTable))
                    : null;

                // The header values change on import; a new DataContext makes the bindings read them again.
                FileHeader = null;
                FileHeader = gm1Document.File.Header;
            }
            else if (tgxDocument != null)
            {
                items.Add(new ImagePreviewItem(0, tgxDocument.Render(), null));
            }

            TGXImages = items;
            SelectImage(null);
        }

        /// <summary>The preview always shows what would be written, even if an import fails.</summary>
        private void RunAndRefreshPreview(Action change)
        {
            try
            {
                change();
            }
            finally
            {
                RefreshPreview();
            }
        }

        private void CloseFiles()
        {
            gm1Document = null;
            tgxDocument = null;
            offsetTarget = null;
            this.RaisePropertyChanged(nameof(Gm1Document));
            this.RaisePropertyChanged(nameof(TgxDocument));

            FileSelected = false;
            FileHeader = null;
            ActuellColorTable = null;
            ActualPalette = 1;
            ButtonsEnabled = false;
            ImportButtonEnabled = false;
            ColorButtonsEnabled = false;
            OrginalStrongholdAnimationButtonEnabled = false;
            ReplaceWithSaveFile = false;
            TgxButtonExportEnabled = false;
            TgxButtonImportEnabled = false;
            ReplaceWithSaveFileTgx = false;
            TGXImages = new ObservableCollection<ImagePreviewItem>();
            SelectImage(null);
        }

        private void ReopenGm1File(Gm1Document previous)
        {
            OpenGm1File(previous.FileName);
            if (gm1Document != null && gm1Document.HasColorTables)
            {
                gm1Document.ColorTableIndex = previous.ColorTableIndex;
                ActualPalette = previous.ColorTableIndex + 1;
                RefreshPreview();
            }
        }

        private string AfterExport(string folder)
        {
            LoadWorkfolderFiles();
            if (OpenFolderAfterExport)
            {
                try
                {
                    FolderLauncher.Open(folder);
                }
                catch (Exception e)
                {
                    // The export itself succeeded.
                    Logger.LogException(e);
                }
            }

            return folder;
        }

        private bool BackupExists(string fileName)
        {
            var workFolder = TryGetWorkFolder();
            return workFolder != null && File.Exists(workFolder.BackupFile(fileName));
        }

        private IBuildingOffsetTarget? LoadOffsetTarget()
        {
            var folder = TryGetStrongholdFolder();
            if (folder == null)
            {
                return null;
            }

            try
            {
                var patcher = ExecutableOffsetPatcher.Load(folder);
                return patcher.HasExecutables ? patcher : null;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Logger.LogException(e);
                return null;
            }
        }

        private ExecutableOffsetPatcher RequireOffsetPatcher()
        {
            var folder = RequireStrongholdFolder();
            var patcher = ExecutableOffsetPatcher.Load(folder);
            if (!patcher.HasExecutables)
            {
                throw new WorkflowException($"\"{StrongholdFolder.CrusaderExecutable}\" / \"{StrongholdFolder.ExtremeExecutable}\": {folder.Root}");
            }

            return patcher;
        }

        private Gm1Document RequireGm1() => gm1Document ?? throw new WorkflowException(Localization.GetText("NoFileSelected"));

        private TgxDocument RequireTgx() => tgxDocument ?? throw new WorkflowException(Localization.GetText("NoFileSelected"));

        private WorkFolder RequireWorkFolder() => TryGetWorkFolder() ?? throw new WorkflowException(Localization.GetText("NoWorkfolderSelected"));

        private StrongholdFolder RequireStrongholdFolder() => TryGetStrongholdFolder() ?? throw new WorkflowException(Localization.GetText("NoStrongholdFolderSelected"));

        private WorkFolder? TryGetWorkFolder()
        {
            return string.IsNullOrWhiteSpace(userConfig.WorkFolderPath) ? null : new WorkFolder(userConfig.WorkFolderPath!);
        }

        private StrongholdFolder? TryGetStrongholdFolder()
        {
            return string.IsNullOrWhiteSpace(userConfig.CrusaderPath) ? null : new StrongholdFolder(userConfig.CrusaderPath!);
        }

        private void SaveUserConfig()
        {
            try
            {
                configStore.Save(userConfig);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Logger.LogException(e);
            }
        }

        private static bool PathsEqual(string first, string second)
        {
            return string.Equals(Path.GetFullPath(first), Path.GetFullPath(second), StringComparison.OrdinalIgnoreCase);
        }
    }
}
