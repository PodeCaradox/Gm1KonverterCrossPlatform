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
using Gm1KonverterCrossPlatform.Core.Ucp;
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

        /// <summary>The UCP3 plugin that receives modified files instead of the Stronghold folder.</summary>
        public UcpExtensionInfo UcpMod => UcpExtensionInfo.FromUserInput(userConfig.UcpModName, userConfig.UcpModAuthor, userConfig.UcpModVersion);

        /// <summary>True if UCP3 is installed in the Stronghold folder.</summary>
        public bool IsUcpInstalled => TryGetStrongholdFolder() is StrongholdFolder folder && new UcpFolder(folder.Root).IsInstalled;

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

        /// <summary>
        /// Changes name, author and version of the UCP3 plugin. An existing plugin with the same name is
        /// updated (and moved to the new version); another name starts a new plugin.
        /// </summary>
        /// <exception cref="WorkflowException">The version is set but not like 1.0.0.</exception>
        public void SetUcpMod(string? displayName, string? author, string? version)
        {
            if (!string.IsNullOrWhiteSpace(version) && !UcpExtensionInfo.IsValidVersion(version))
            {
                throw new WorkflowException(Localization.GetText("UcpModInvalidVersion"));
            }

            userConfig.UcpModName = string.IsNullOrWhiteSpace(displayName) ? null : displayName!.Trim();
            userConfig.UcpModAuthor = string.IsNullOrWhiteSpace(author) ? null : author!.Trim();
            userConfig.UcpModVersion = UcpExtensionInfo.IsValidVersion(version) ? version!.Trim() : null;
            SaveUserConfig();
            this.RaisePropertyChanged(nameof(UcpMod));

            var strongholdFolder = TryGetStrongholdFolder();
            if (strongholdFolder != null)
            {
                var plugin = UcpTexturePlugin.Open(new UcpFolder(strongholdFolder.Root), UcpMod);
                if (plugin.Exists)
                {
                    plugin.UpdateDefinition();
                }

                var module = LoadOffsetTarget();
                if (module != null && module.Exists)
                {
                    module.UpdateDefinition();
                }

                if (IsCastleFileOpen)
                {
                    offsetTarget = module;
                    SelectImage(selectedItem);
                }
            }

            UpdateRestoreState();
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
            var modInstaller = CreateModInstaller(folder);
            var gameFile = new GameFile(GameFolder.Gm, fileName);
            var document = Gm1Document.Load(modInstaller.GetCurrentFile(gameFile));
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
            ReplaceWithSaveFile = modInstaller.CanRestore(gameFile);
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

        /// <summary>
        /// Saves the modified file in the UCP3 plugin; the game file stays unchanged. For anim_castle.gm1 the offsets
        /// module is written as well, so the building offsets can be adjusted in the UCP3 GUI.
        /// </summary>
        /// <returns>A message for the user.</returns>
        public string InstallGm1File()
        {
            var document = RequireGm1();
            var strongholdFolder = RequireStrongholdFolder();
            var modInstaller = CreateModInstaller(strongholdFolder);
            modInstaller.Install(new GameFile(GameFolder.Gm, document.FileName), document.ToBytes());
            string message = ModSavedMessage(modInstaller);

            if (CastleOffsetAddresses.AppliesTo(document.FileName))
            {
                message += Environment.NewLine + Environment.NewLine + CreateOffsetModuleForCastle(strongholdFolder);
            }

            ReopenGm1File(document);
            LoadWorkfolderFiles();
            return message;
        }

        /// <summary>Writes the offsets module next to the castle textures; a failure does not stop saving the textures.</summary>
        private string CreateOffsetModuleForCastle(StrongholdFolder strongholdFolder)
        {
            try
            {
                var module = OpenOffsetModule(strongholdFolder);
                module.Create();
                return string.Format(Localization.GetText("UcpOffsetsWithCastle"), module.Info.DisplayName);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is InvalidDataException)
            {
                Logger.LogException(e);
                return e.Message;
            }
        }

        /// <summary>Removes the file from the UCP3 plugin, so the game uses the original again.</summary>
        public void RestoreGm1File()
        {
            var document = RequireGm1();
            CreateModInstaller(RequireStrongholdFolder()).Restore(new GameFile(GameFolder.Gm, document.FileName));
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
            var modInstaller = CreateModInstaller(folder);
            var gameFile = new GameFile(GameFolder.Gfx, fileName);
            tgxDocument = TgxDocument.Load(modInstaller.GetCurrentFile(gameFile));
            this.RaisePropertyChanged(nameof(TgxDocument));

            TgxButtonExportEnabled = true;
            var workFolder = TryGetWorkFolder();
            TgxButtonImportEnabled = workFolder != null && File.Exists(workFolder.TgxImageFile(fileName));
            ReplaceWithSaveFileTgx = modInstaller.CanRestore(gameFile);

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

        /// <summary>Saves the modified file in the UCP3 plugin; the game file stays unchanged.</summary>
        /// <returns>A message for the user.</returns>
        public string InstallTgxFile()
        {
            var document = RequireTgx();
            var modInstaller = CreateModInstaller(RequireStrongholdFolder());
            modInstaller.Install(new GameFile(GameFolder.Gfx, document.FileName), document.ToBytes());

            ReplaceWithSaveFileTgx = true;
            LoadWorkfolderFiles();
            return ModSavedMessage(modInstaller);
        }

        /// <summary>Removes the file from the UCP3 plugin, so the game uses the original again.</summary>
        public void RestoreTgxFile()
        {
            var document = RequireTgx();
            CreateModInstaller(RequireStrongholdFolder()).Restore(new GameFile(GameFolder.Gfx, document.FileName));
            OpenTgxFile(document.FileName);
        }

        /// <summary>Opens the folder of the UCP3 plugin (or the plugins folder if nothing was saved yet).</summary>
        public void OpenUcpModFolder()
        {
            var plugin = CreateModInstaller(RequireStrongholdFolder()).Plugin;
            string folder = plugin.Exists ? plugin.Folder : Path.GetDirectoryName(plugin.Folder)!;
            Directory.CreateDirectory(folder);
            FolderLauncher.Open(folder);
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

        /// <summary>
        /// Sets the offset of the selected image in the UCP3 offsets module (the executables stay unchanged)
        /// and in Offsets.json of the work folder.
        /// </summary>
        /// <returns>A message for the user if the module was created, otherwise null.</returns>
        public string? ChangeSelectedOffset()
        {
            if (selectedOffsetImageIndex < 0)
            {
                return null;
            }

            var offset = new BuildingOffset(XOffset, YOffset);
            var module = RequireOffsetModule();
            bool created = !module.Exists;
            module.Write(selectedOffsetImageIndex, offset);
            offsetTarget = IsCastleFileOpen ? module : null;

            var workFolder = TryGetWorkFolder();
            if (workFolder != null)
            {
                BuildingOffsetStore.Load(workFolder.OffsetsFile).Set(selectedOffsetImageIndex, offset);
            }

            return created ? OffsetsSavedMessage(module) : null;
        }

        /// <summary>Sets all offsets of an offset file in the UCP3 offsets module.</summary>
        /// <returns>A message for the user.</returns>
        public string ApplyOffsetsFromFile(string path)
        {
            var module = RequireOffsetModule();
            var offsets = BuildingOffsetStore.Load(path).Offsets.Where(entry => module.Supports(entry.Key)).ToList();
            module.WriteAll(offsets);

            if (IsCastleFileOpen)
            {
                // Show the applied values instead of the ones read before.
                offsetTarget = module;
                SelectImage(selectedItem);
            }

            var workFolder = TryGetWorkFolder();
            if (workFolder != null && !PathsEqual(path, workFolder.OffsetsFile))
            {
                BuildingOffsetStore.Load(workFolder.OffsetsFile).SetAll(offsets);
            }

            return OffsetsSavedMessage(module);
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

        private TextureModInstaller CreateModInstaller(StrongholdFolder strongholdFolder)
        {
            return new TextureModInstaller(strongholdFolder, UcpMod, TryGetWorkFolder());
        }

        private static string ModSavedMessage(TextureModInstaller modInstaller)
        {
            string paragraph = Environment.NewLine + Environment.NewLine;
            string folder = Path.GetRelativePath(Path.GetDirectoryName(modInstaller.Ucp.Root)!, modInstaller.Plugin.Folder);
            string message = string.Format(Localization.GetText("UcpModSaved"), modInstaller.Plugin.Info.DisplayName)
                + Environment.NewLine + folder
                + paragraph + Localization.GetText("UcpModActivate");
            return modInstaller.Ucp.IsInstalled ? message : message + paragraph + Localization.GetText("UcpNotInstalled");
        }

        /// <summary>Enables "restore" for the open file if the current UCP3 plugin contains it.</summary>
        private void UpdateRestoreState()
        {
            var strongholdFolder = TryGetStrongholdFolder();
            if (strongholdFolder == null)
            {
                return;
            }

            var modInstaller = CreateModInstaller(strongholdFolder);
            if (gm1Document != null)
            {
                ReplaceWithSaveFile = modInstaller.CanRestore(new GameFile(GameFolder.Gm, gm1Document.FileName));
            }

            if (tgxDocument != null)
            {
                ReplaceWithSaveFileTgx = modInstaller.CanRestore(new GameFile(GameFolder.Gfx, tgxDocument.FileName));
            }
        }

        /// <summary>The UCP3 offsets module, or null if no Stronghold folder is set.</summary>
        private UcpOffsetModule? LoadOffsetTarget()
        {
            var folder = TryGetStrongholdFolder();
            if (folder == null)
            {
                return null;
            }

            try
            {
                return OpenOffsetModule(folder);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Logger.LogException(e);
                return null;
            }
        }

        /// <summary>The executables are optional: the offsets of both games are known (<see cref="KnownCastleOffsets"/>).</summary>
        private UcpOffsetModule RequireOffsetModule() => OpenOffsetModule(RequireStrongholdFolder());

        private UcpOffsetModule OpenOffsetModule(StrongholdFolder folder)
        {
            return UcpOffsetModule.Open(new UcpFolder(folder.Root), UcpOffsetModule.InfoFor(UcpMod), StrongholdExecutable.LoadAll(folder));
        }

        private string OffsetsSavedMessage(UcpOffsetModule module)
        {
            var ucpFolder = new UcpFolder(RequireStrongholdFolder().Root);
            string paragraph = Environment.NewLine + Environment.NewLine;
            string message = string.Format(Localization.GetText("UcpOffsetsSaved"), module.Info.DisplayName)
                + Environment.NewLine + Path.GetRelativePath(Path.GetDirectoryName(ucpFolder.Root)!, module.Folder)
                + paragraph + Localization.GetText("UcpOffsetsActivate");
            return ucpFolder.IsInstalled ? message : message + paragraph + Localization.GetText("UcpNotInstalled");
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
