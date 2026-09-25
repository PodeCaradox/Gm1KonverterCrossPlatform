using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Gm1KonverterCrossPlatform.Core.BuildingOffsets;

namespace Gm1KonverterCrossPlatform.Core.Ucp
{
    /// <summary>
    /// A UCP3 module that sets the drawing offsets of the castle buildings while the game runs, instead of
    /// patching Stronghold Crusader.exe and Stronghold_Crusader_Extreme.exe. The values can be changed in the
    /// UCP3 GUI (options.yml); this program only sets their start values.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The generated init.lua finds each offset with a byte pattern (AOB) taken from the executables in the
    /// Stronghold folder, so Crusader and Extreme are found at their own addresses. The offset bytes themselves
    /// are wildcards, so executables patched by older versions of this program are found as well. Each pattern
    /// is extended until it occurs only once.
    /// </para>
    /// <para>
    /// The offsets of Stronghold Crusader.exe are 912 bytes before those of Stronghold_Crusader_Extreme.exe, and the
    /// code around them contains different memory addresses. The known patterns (<see cref="KnownCastleOffsets"/>)
    /// fit both games; the position of an offset in a local executable is measured with them.
    /// </para>
    /// <para>
    /// An offset is only written if it was set with this program or changed in the UCP3 GUI. All other offsets
    /// keep the value of the running game, which may differ between Crusader and Extreme.
    /// </para>
    /// <para>
    /// Plugins cannot write game memory, that is why this is a module. UCP3 only loads unsigned modules
    /// when it is started without security ("--ucp-no-security").
    /// </para>
    /// </remarks>
    public sealed class UcpOffsetModule : IBuildingOffsetTarget
    {
        public const string OffsetsFileName = "offsets.json";
        public const string OptionsFileName = "options.yml";

        private const int ContextStep = 8;
        private const int MaxContext = 512;

        private static readonly KeyValuePair<string, string>[] Dependencies =
        {
            new KeyValuePair<string, string>("framework", "^3.0.0"),
        };

        private readonly IReadOnlyList<StrongholdExecutable> executables;
        private readonly Dictionary<(StrongholdExecutable Executable, OffsetAddress Address), int> shifts =
            new Dictionary<(StrongholdExecutable Executable, OffsetAddress Address), int>();

        private UcpOffsetModule(UcpExtensionInfo info, string zipPath, IReadOnlyList<StrongholdExecutable> executables)
        {
            Info = info;
            ZipPath = zipPath;
            this.executables = executables;
        }

        public UcpExtensionInfo Info { get; }

        /// <summary>
        /// The module file <c>ucp/modules/&lt;name&gt;-&lt;version&gt;.zip</c>. The UCP3 GUI of a normal UCP3 installation
        /// only lists modules packed as zip.
        /// </summary>
        public string ZipPath { get; }

        /// <summary>True once the module was written.</summary>
        public bool Exists => File.Exists(ZipPath);

        public bool HasExecutables => executables.Count > 0;

        /// <summary>The offsets set with this program (start values in the UCP3 GUI).</summary>
        public IReadOnlyDictionary<int, BuildingOffset> Offsets => ReadOffsets(ZipPath);

        /// <summary>The module that belongs to the texture mod <paramref name="textureMod"/>.</summary>
        public static UcpExtensionInfo InfoFor(UcpExtensionInfo textureMod)
        {
            if (textureMod == null) throw new ArgumentNullException(nameof(textureMod));
            return new UcpExtensionInfo(textureMod.Name + "-Offsets", textureMod.Version, textureMod.DisplayName + " (Offsets)", textureMod.Author);
        }

        /// <summary>
        /// Opens the module <paramref name="info"/>. If it only exists in another version, or as a folder written by an
        /// earlier version of this program, its offsets are taken over and the old module is removed, so UCP3 never
        /// sees two modules with the same name.
        /// </summary>
        /// <param name="executables">The executables of the Stronghold folder, see <see cref="StrongholdExecutable.LoadAll"/>.</param>
        public static UcpOffsetModule Open(UcpFolder ucp, UcpExtensionInfo info, IReadOnlyList<StrongholdExecutable> executables)
        {
            if (ucp == null) throw new ArgumentNullException(nameof(ucp));
            if (info == null) throw new ArgumentNullException(nameof(info));
            if (executables == null) throw new ArgumentNullException(nameof(executables));

            var module = new UcpOffsetModule(info, ucp.ModuleZip(info), executables);
            if (!module.Exists)
            {
                module.TakeOverOldModule(ucp);
            }

            return module;
        }

        /// <summary>True for every castle offset: they are known for both games, executables are optional.</summary>
        public bool Supports(int imageIndex) => CastleOffsetAddresses.TryGet(imageIndex, out _);

        /// <summary>
        /// The offset set with this program, otherwise the offset stored in the executable, otherwise the value of
        /// the unmodified game.
        /// </summary>
        public bool TryRead(int imageIndex, out BuildingOffset offset)
        {
            offset = default;
            if (!CastleOffsetAddresses.TryGet(imageIndex, out var address))
            {
                return false;
            }

            return Offsets.TryGetValue(imageIndex, out offset) || TryReadOriginal(CastleOffsetAddresses.GroupOf(address), out offset);
        }

        /// <exception cref="InvalidDataException">No executable contains a unique pattern for the offset (unknown version).</exception>
        public void Write(int imageIndex, BuildingOffset offset) => WriteAll(new[] { new KeyValuePair<int, BuildingOffset>(imageIndex, offset) });

        /// <summary>
        /// Writes the module with every offset that can be located, so all of them can be changed in the UCP3 GUI.
        /// </summary>
        /// <exception cref="InvalidDataException">No offset could be located (unknown version).</exception>
        public void Create() => WriteAll(Array.Empty<KeyValuePair<int, BuildingOffset>>());

        /// <summary>Sets several offsets at once. Nothing is written if one of them cannot be set.</summary>
        /// <exception cref="InvalidDataException">No executable contains a unique pattern for an offset (unknown version).</exception>
        public void WriteAll(IEnumerable<KeyValuePair<int, BuildingOffset>> offsets)
        {
            if (offsets == null) throw new ArgumentNullException(nameof(offsets));

            var all = new SortedDictionary<int, BuildingOffset>(Offsets.ToDictionary(entry => entry.Key, entry => entry.Value));
            foreach (var entry in offsets)
            {
                if (!CastleOffsetAddresses.TryGet(entry.Key, out _))
                {
                    throw new ArgumentOutOfRangeException(nameof(offsets), entry.Key, "This image has no offset in the executable.");
                }

                all[entry.Key] = entry.Value;
            }

            // locating the offsets fails for unknown executables, before anything is written
            var entries = CreateEntries(all);
            if (entries.Count == 0)
            {
                throw new InvalidDataException("The building offsets could not be located in the executables, this version of Stronghold is not supported.");
            }

            WriteZip(all, entries);
        }

        /// <summary>Rewrites the module, e.g. after the name or author changed.</summary>
        public void UpdateDefinition()
        {
            var offsets = Offsets;
            WriteZip(offsets, CreateEntries(offsets));
        }

        public string CreateDefinition() => ExtensionFiles.CreateDefinition(Info, "module", "Castle building offsets, adjustable in the UCP3 GUI", Dependencies);

        /// <summary>
        /// One entry per offset address that can be located. Offsets set with this program must be located,
        /// others are left out if they cannot.
        /// </summary>
        /// <exception cref="InvalidDataException">An offset set with this program cannot be located.</exception>
        internal IReadOnlyList<ModuleEntry> CreateEntries(IReadOnlyDictionary<int, BuildingOffset> offsets)
        {
            var entries = new List<ModuleEntry>();
            foreach (var group in CastleOffsetAddresses.Groups)
            {
                // images sharing an address: the last one wins, like patching them one after another
                var setIndices = group.ImageIndices.Where(offsets.ContainsKey).ToList();
                bool set = setIndices.Count > 0;

                BuildingOffset start;
                if (set)
                {
                    start = offsets[setIndices[setIndices.Count - 1]];
                }
                else if (!TryReadOriginal(group, out start))
                {
                    continue;
                }

                IReadOnlyList<OffsetPatch> patches;
                try
                {
                    patches = CreatePatches(group);
                }
                catch (InvalidDataException) when (!set)
                {
                    continue;
                }

                entries.Add(new ModuleEntry(group, start, set, patches));
            }

            return entries;
        }

        /// <summary>The generated init.lua that writes the offsets into the running game.</summary>
        internal string CreateInitScript(IReadOnlyList<ModuleEntry> entries)
        {
            var lua = new StringBuilder();
            lua.Append("-- Generated by Gm1 Konverter. The file is rewritten whenever the module is saved again.\n");
            lua.Append("-- Sets the drawing offsets of castle buildings (anim_castle.gm1). The values can be changed in the\n");
            lua.Append("-- UCP3 GUI (options.yml), \"start\" holds the values set in the Gm1 Konverter. An offset is only\n");
            lua.Append("-- written if it was set in the Gm1 Konverter (\"fixed\") or changed in the UCP3 GUI, otherwise the\n");
            lua.Append("-- game keeps its own value. Each offset is found by a byte pattern of Stronghold Crusader.exe or\n");
            lua.Append("-- Stronghold_Crusader_Extreme.exe; x and y are the positions of the values in the pattern.\n");
            lua.Append("local offsets = {\n");
            foreach (var entry in entries)
            {
                lua.Append("  {\n");
                lua.Append("    name = ").Append(ScriptText.LuaString(entry.Name)).Append(",\n");
                lua.Append("    images = ").Append(ScriptText.LuaString(entry.Images)).Append(",\n");
                lua.Append("    start = { x = ").Append(Number(entry.Start.X)).Append(", y = ").Append(Number(entry.Start.Y)).Append(" },\n");
                lua.Append("    fixed = ").Append(entry.Fixed ? "true" : "false").Append(",\n");
                lua.Append("    singleByteY = ").Append(entry.Address.SingleByteY ? "true" : "false").Append(",\n");
                lua.Append("    variants = {\n");
                foreach (var patch in entry.Patches)
                {
                    lua.Append("      { pattern = ").Append(ScriptText.LuaString(patch.Pattern.ToString()))
                        .Append(", x = ").Append(Number(patch.RelativeX))
                        .Append(", y = ").Append(Number(patch.RelativeY)).Append(" },\n");
                }

                lua.Append("    },\n");
                lua.Append("  },\n");
            }

            lua.Append("}\n");
            lua.Append('\n');
            lua.Append(RuntimeScript.Replace("{{LOG_PREFIX}}", ScriptText.LuaString($"[{Info.Name}]: offset of image ")));
            return lua.ToString();
        }

        /// <summary>The options.yml shown by the UCP3 GUI: X and Y of every offset.</summary>
        internal string CreateOptions(IReadOnlyList<ModuleEntry> entries)
        {
            var yaml = new StringBuilder();
            yaml.Append("specification-version: 1.0.0\n");
            yaml.Append("options:\n");
            yaml.Append("  - display: GroupBox\n");
            yaml.Append("    header: ").Append(ScriptText.YamlString("Castle building offsets")).Append('\n');
            yaml.Append("    text: ").Append(ScriptText.YamlString(
                "Drawing offsets of the castle buildings (anim_castle.gm1). The start values come from the Gm1 Konverter. " +
                "Only values set there or changed here are written; all others keep the value of the game.")).Append('\n');
            yaml.Append("    hasHeader: true\n");
            yaml.Append("    category: [").Append(ScriptText.YamlString(Info.DisplayName)).Append("]\n");
            yaml.Append("    children:\n");
            foreach (var entry in entries)
            {
                string yMin = entry.Address.SingleByteY ? "-128" : int.MinValue.ToString(CultureInfo.InvariantCulture);
                string yMax = entry.Address.SingleByteY ? "127" : int.MaxValue.ToString(CultureInfo.InvariantCulture);

                yaml.Append("      - display: GroupBox\n");
                yaml.Append("        header: ").Append(ScriptText.YamlString((entry.Group.ImageIndices.Count > 1 ? "Images " : "Image ") + entry.Images)).Append('\n');
                yaml.Append("        text: ").Append(ScriptText.YamlString(entry.Fixed ? "Set in the Gm1 Konverter." : "Value of the game.")).Append('\n');
                yaml.Append("        children:\n");
                AppendNumber(yaml, $"{Info.Name}.{entry.Name}.x", "X", Number(entry.Start.X), "-128", "127");
                AppendNumber(yaml, $"{Info.Name}.{entry.Name}.y", "Y", Number(entry.Start.Y), yMin, yMax);
            }

            return yaml.ToString();
        }

        /// <summary>
        /// Patterns that find the offset in the games, with the positions of x and y relative to the pattern start.
        /// The known pattern (<see cref="KnownCastleOffsets"/>) fits both games; a pattern of a local executable is
        /// only added for an executable it does not fit, e.g. another game version.
        /// </summary>
        /// <exception cref="InvalidDataException">An executable does not contain a unique pattern.</exception>
        internal IReadOnlyList<OffsetPatch> CreatePatches(OffsetGroup group)
        {
            var address = group.Address;
            var patches = new List<OffsetPatch>();
            if (KnownCastleOffsets.TryGet(group, out var known))
            {
                var patch = new OffsetPatch(known.Pattern, address.X - known.RelativeX, address);

                // the known pattern must not hit a wrong place in any executable of this installation
                if (executables.All(executable => patch.FindsOnlyExpectedPlace(executable, ShiftOf(executable, address), mustMatch: false)))
                {
                    patches.Add(patch);
                }
            }

            foreach (var executable in executables)
            {
                int shift = ShiftOf(executable, address);
                if (!patches.Any(patch => patch.FindsOnlyExpectedPlace(executable, shift, mustMatch: true)))
                {
                    patches.Add(CreateUniquePatch(address, executable));
                }
            }

            return patches;
        }

        private static void AppendNumber(StringBuilder yaml, string url, string text, string value, string min, string max)
        {
            yaml.Append("          - url: ").Append(url).Append('\n');
            yaml.Append("            header: ").Append(ScriptText.YamlString(text)).Append('\n');
            yaml.Append("            text: ").Append(ScriptText.YamlString(text)).Append('\n');
            yaml.Append("            display: Number\n");
            yaml.Append("            contents:\n");
            yaml.Append("              type: integer\n");
            yaml.Append("              value: ").Append(value).Append('\n');
            yaml.Append("              min: ").Append(min).Append('\n');
            yaml.Append("              max: ").Append(max).Append('\n');
        }

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

        private static IReadOnlyDictionary<int, BuildingOffset> ReadOffsets(string zipPath)
        {
            if (!File.Exists(zipPath))
            {
                return new Dictionary<int, BuildingOffset>();
            }

            using var zip = ZipFile.OpenRead(zipPath);
            var entry = zip.GetEntry(OffsetsFileName);
            if (entry == null)
            {
                return new Dictionary<int, BuildingOffset>();
            }

            using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
            return BuildingOffsetStore.Parse(reader.ReadToEnd());
        }

        /// <summary>Takes over the offsets of this module in another version or of the folder of an earlier version.</summary>
        private void TakeOverOldModule(UcpFolder ucp)
        {
            var zips = ucp.FindModuleZips(Info.Name);
            if (zips.Count == 1)
            {
                var offsets = ReadOffsets(zips[0]);
                WriteZip(offsets, CreateEntries(offsets));
                File.Delete(zips[0]);
                return;
            }

            // earlier versions of this program wrote a folder, which the UCP3 GUI does not list
            var folders = ucp.FindModuleFolders(Info.Name).Where(IsGeneratedFolder).ToList();
            if (zips.Count == 0 && folders.Count == 1)
            {
                var offsets = BuildingOffsetStore.Load(Path.Combine(folders[0], OffsetsFileName)).Offsets;
                WriteZip(offsets, CreateEntries(offsets));
                Directory.Delete(folders[0], recursive: true);
            }
        }

        private static bool IsGeneratedFolder(string folder)
        {
            string initScript = Path.Combine(folder, ExtensionFiles.InitFileName);
            return File.Exists(Path.Combine(folder, OffsetsFileName))
                && File.Exists(initScript)
                && File.ReadAllText(initScript).StartsWith("-- Generated by Gm1 Konverter", StringComparison.Ordinal);
        }

        /// <summary>Writes the whole module as zip, replacing the old file only when the new one is complete.</summary>
        private void WriteZip(IReadOnlyDictionary<int, BuildingOffset> offsets, IReadOnlyList<ModuleEntry> entries)
        {
            using var buffer = new MemoryStream();
            using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
            {
                AddEntry(zip, ExtensionFiles.DefinitionFileName, CreateDefinition());
                AddEntry(zip, ExtensionFiles.InitFileName, CreateInitScript(entries));
                AddEntry(zip, OptionsFileName, CreateOptions(entries));
                AddEntry(zip, ExtensionFiles.DescriptionFile, CreateDescription(entries));
                AddEntry(zip, OffsetsFileName, BuildingOffsetStore.ToJson(offsets));
            }

            Directory.CreateDirectory(Path.GetDirectoryName(ZipPath)!);
            ExtensionFiles.WriteBytes(ZipPath, buffer.ToArray());
        }

        private static void AddEntry(ZipArchive zip, string name, string text)
        {
            using var writer = new StreamWriter(zip.CreateEntry(name).Open(), new UTF8Encoding(false));
            writer.Write(text);
        }

        /// <summary>The value in the local executables, otherwise the value of the unmodified game.</summary>
        private bool TryReadOriginal(OffsetGroup group, out BuildingOffset offset)
        {
            foreach (var executable in executables)
            {
                if (executable.TryRead(group.Address, ShiftOf(executable, group.Address), out offset))
                {
                    return true;
                }
            }

            if (KnownCastleOffsets.TryGet(group, out var known))
            {
                offset = known.Original;
                return true;
            }

            offset = default;
            return false;
        }

        /// <summary>
        /// The difference between the offset position in <paramref name="executable"/> and the Extreme address.
        /// </summary>
        internal int ShiftOf(StrongholdExecutable executable, OffsetAddress address)
        {
            if (!shifts.TryGetValue((executable, address), out int shift))
            {
                shift = MeasureShift(executable, address);
                shifts[(executable, address)] = shift;
            }

            return shift;
        }

        /// <summary>
        /// Finds the offset in <paramref name="executable"/> with the known pattern, which fits both games. Falls back
        /// to the documented difference (<see cref="StrongholdExecutable.AddressShift"/>) if the pattern is not found
        /// exactly once, e.g. in another game version.
        /// </summary>
        private static int MeasureShift(StrongholdExecutable executable, OffsetAddress address)
        {
            if (KnownCastleOffsets.TryGet(CastleOffsetAddresses.GroupOf(address), out var known))
            {
                var matches = known.Pattern.FindAll(executable.Bytes, 2);
                if (matches.Count == 1)
                {
                    return matches[0] + known.RelativeX - address.X;
                }
            }

            return executable.AddressShift;
        }

        /// <summary>The pattern of <paramref name="context"/> bytes around the offset, or null if it has no fixed byte.</summary>
        private static OffsetPatch? CreatePatch(StrongholdExecutable executable, int shift, OffsetAddress address, int context)
        {
            // Extreme addresses of the window, limited to the file
            int start = Math.Max(address.Start - context, -shift);
            int end = Math.Min(address.End + context, executable.Bytes.Length - shift);
            if (end <= start)
            {
                return null;
            }

            var pattern = AobPattern.Create(
                executable.Bytes.AsSpan(start + shift, end - start),
                i => CastleOffsetAddresses.VariableAddresses.Contains(start + i),
                out int trimmed);
            return pattern == null ? null : new OffsetPatch(pattern, start + trimmed, address);
        }

        private OffsetPatch CreateUniquePatch(OffsetAddress address, StrongholdExecutable own)
        {
            int ownShift = ShiftOf(own, address);
            if (!own.Contains(address, ownShift))
            {
                throw new InvalidDataException($"\"{own.Name}\" is too small for the building offsets, this version of Stronghold is not supported.");
            }

            for (int context = ContextStep; context <= MaxContext; context += ContextStep)
            {
                var patch = CreatePatch(own, ownShift, address, context);
                if (patch != null && executables.All(executable =>
                        patch.FindsOnlyExpectedPlace(executable, ShiftOf(executable, address), mustMatch: executable == own)))
                {
                    return patch;
                }
            }

            throw new InvalidDataException($"The building offsets could not be located uniquely in \"{own.Name}\", this version of Stronghold is not supported.");
        }

        private string CreateDescription(IReadOnlyList<ModuleEntry> entries)
        {
            var markdown = new StringBuilder();
            markdown.Append("# ").Append(ScriptText.SingleLine(Info.DisplayName)).Append('\n');
            markdown.Append('\n');
            markdown.Append("Drawing offsets of castle buildings (anim_castle.gm1) for Stronghold Crusader and Crusader Extreme. ");
            markdown.Append("The values can be changed in the options of this module; the start values come from the Gm1 Konverter.\n");
            markdown.Append('\n');
            markdown.Append("This module is not signed: start the game from the UCP3 GUI with \"Disable Security\" to use it.\n");
            markdown.Append('\n');
            markdown.Append("| Image | X | Y | |\n");
            markdown.Append("|---|---|---|---|\n");
            foreach (var entry in entries)
            {
                markdown.Append("| ").Append(entry.Images).Append(" | ").Append(entry.Start.X).Append(" | ").Append(entry.Start.Y)
                    .Append(" | ").Append(entry.Fixed ? "set in the Gm1 Konverter" : "game value").Append(" |\n");
            }

            return markdown.ToString();
        }

        /// <summary>
        /// The part of init.lua that does not depend on the offsets. Values are only written if they were set
        /// in the Gm1 Konverter or differ from the start value (changed in the UCP3 GUI).
        /// </summary>
        private const string RuntimeScript =
            "local function find(variants)\n" +
            "  for _, variant in ipairs(variants) do\n" +
            "    local address = core.scanForAOB(variant.pattern)\n" +
            "    if address ~= nil and address ~= 0 then\n" +
            "      return address, variant\n" +
            "    end\n" +
            "  end\n" +
            "  return nil, nil\n" +
            "end\n" +
            "\n" +
            "-- a whole number from the UCP3 config, or nil\n" +
            "local function toInteger(value)\n" +
            "  local number = tonumber(value)\n" +
            "  if number == nil or number ~= number then\n" +
            "    return nil\n" +
            "  end\n" +
            "  return number // 1\n" +
            "end\n" +
            "\n" +
            "local function clamp(value, min, max)\n" +
            "  if value < min then return min end\n" +
            "  if value > max then return max end\n" +
            "  return value\n" +
            "end\n" +
            "\n" +
            "-- little endian bytes of a signed value\n" +
            "local function bytesOf(value, count)\n" +
            "  local bytes = {}\n" +
            "  for i = 1, count do\n" +
            "    bytes[i] = (value >> (8 * (i - 1))) & 0xFF\n" +
            "  end\n" +
            "  return bytes\n" +
            "end\n" +
            "\n" +
            "-- the value to write, or nil to keep the value of the game\n" +
            "local function valueToWrite(configured, start, fixed)\n" +
            "  if configured ~= nil and configured ~= start then\n" +
            "    return configured\n" +
            "  end\n" +
            "  if fixed then\n" +
            "    return start\n" +
            "  end\n" +
            "  return nil\n" +
            "end\n" +
            "\n" +
            "return {\n" +
            "  enable = function(self, config)\n" +
            "    for _, offset in ipairs(offsets) do\n" +
            "      local values = {}\n" +
            "      if type(config) == \"table\" and type(config[offset.name]) == \"table\" then\n" +
            "        values = config[offset.name]\n" +
            "      end\n" +
            "\n" +
            "      local x = valueToWrite(toInteger(values.x), offset.start.x, offset.fixed)\n" +
            "      local y = valueToWrite(toInteger(values.y), offset.start.y, offset.fixed)\n" +
            "      if x ~= nil or y ~= nil then\n" +
            "        local address, variant = find(offset.variants)\n" +
            "        if address == nil then\n" +
            "          log(WARNING, {{LOG_PREFIX}} .. offset.images .. \" not found, this game version is not supported\")\n" +
            "        else\n" +
            "          if x ~= nil then\n" +
            "            core.writeCodeBytes(address + variant.x, bytesOf(clamp(x, -128, 127), 1))\n" +
            "          end\n" +
            "          if y ~= nil and offset.singleByteY then\n" +
            "            core.writeCodeBytes(address + variant.y, bytesOf(clamp(y, -128, 127), 1))\n" +
            "          elseif y ~= nil then\n" +
            "            core.writeCodeBytes(address + variant.y, bytesOf(clamp(y, -2147483648, 2147483647), 4))\n" +
            "          end\n" +
            "        end\n" +
            "      end\n" +
            "    end\n" +
            "  end,\n" +
            "\n" +
            "  disable = function(self, config)\n" +
            "  end,\n" +
            "}\n";
    }

    /// <summary>One offset of the module: address, start value and patterns.</summary>
    internal sealed class ModuleEntry
    {
        public ModuleEntry(OffsetGroup group, BuildingOffset start, bool isFixed, IReadOnlyList<OffsetPatch> patches)
        {
            Group = group;
            Start = start;
            Fixed = isFixed;
            Patches = patches;
        }

        public OffsetGroup Group { get; }

        public OffsetAddress Address => Group.Address;

        /// <summary>Option name in the UCP3 config, e.g. "image121".</summary>
        public string Name => Group.Name;

        /// <summary>E.g. "121, 122".</summary>
        public string Images => string.Join(", ", Group.ImageIndices);

        /// <summary>The value set in this program, otherwise the value of the executable.</summary>
        public BuildingOffset Start { get; }

        /// <summary>True if the value was set in this program and is always written.</summary>
        public bool Fixed { get; }

        public IReadOnlyList<OffsetPatch> Patches { get; }
    }

    /// <summary>A byte pattern and the positions of the offset values relative to its start.</summary>
    internal sealed class OffsetPatch
    {
        /// <param name="startAddress">Extreme address of the first pattern byte.</param>
        public OffsetPatch(AobPattern pattern, int startAddress, OffsetAddress address)
        {
            Pattern = pattern;
            StartAddress = startAddress;
            RelativeX = address.X - startAddress;
            RelativeY = address.Y - startAddress;
        }

        public AobPattern Pattern { get; }

        public int StartAddress { get; }

        public int RelativeX { get; }

        public int RelativeY { get; }

        /// <summary>
        /// True if the pattern matches <paramref name="executable"/> nowhere (allowed unless <paramref name="mustMatch"/>)
        /// or exactly once at the offset, which is <paramref name="shift"/> bytes away from the Extreme address.
        /// </summary>
        public bool FindsOnlyExpectedPlace(StrongholdExecutable executable, int shift, bool mustMatch)
        {
            var matches = Pattern.FindAll(executable.Bytes, 2);
            return matches.Count == 0
                ? !mustMatch
                : matches.Count == 1 && matches[0] == StartAddress + shift;
        }
    }
}
