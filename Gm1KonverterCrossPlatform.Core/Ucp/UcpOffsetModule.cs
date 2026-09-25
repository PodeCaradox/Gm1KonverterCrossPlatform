using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Gm1KonverterCrossPlatform.Core.BuildingOffsets;

namespace Gm1KonverterCrossPlatform.Core.Ucp
{
    /// <summary>
    /// A UCP3 module that sets the drawing offsets of the castle buildings while the game runs, instead of
    /// patching Stronghold Crusader.exe and Stronghold_Crusader_Extreme.exe.
    /// </summary>
    /// <remarks>
    /// The generated init.lua finds each offset with a byte pattern (AOB) taken from the executables in the
    /// Stronghold folder. The offset bytes themselves are wildcards, so executables patched by older versions
    /// of this program are found as well. Each pattern is extended until it occurs only once.
    /// Plugins cannot write game memory, that is why this is a module. UCP3 only loads unsigned modules
    /// when it is started without security ("--ucp-no-security").
    /// </remarks>
    public sealed class UcpOffsetModule : IBuildingOffsetTarget
    {
        public const string OffsetsFileName = "offsets.json";

        private const int ContextStep = 8;
        private const int MaxContext = 512;

        private static readonly KeyValuePair<string, string>[] Dependencies =
        {
            new KeyValuePair<string, string>("framework", "^3.0.0"),
        };

        private readonly IReadOnlyList<StrongholdExecutable> executables;

        private UcpOffsetModule(UcpExtensionInfo info, string folder, IReadOnlyList<StrongholdExecutable> executables)
        {
            Info = info;
            Folder = folder;
            this.executables = executables;
        }

        public UcpExtensionInfo Info { get; }

        /// <summary>The module folder <c>ucp/modules/&lt;name&gt;-&lt;version&gt;</c>.</summary>
        public string Folder { get; }

        /// <summary>True once an offset was written.</summary>
        public bool Exists => Directory.Exists(Folder);

        public bool HasExecutables => executables.Count > 0;

        /// <summary>The offsets set by this module.</summary>
        public IReadOnlyDictionary<int, BuildingOffset> Offsets => BuildingOffsetStore.Load(OffsetsFile).Offsets;

        private string OffsetsFile => Path.Combine(Folder, OffsetsFileName);

        /// <summary>The module that belongs to the texture mod <paramref name="textureMod"/>.</summary>
        public static UcpExtensionInfo InfoFor(UcpExtensionInfo textureMod)
        {
            if (textureMod == null) throw new ArgumentNullException(nameof(textureMod));
            return new UcpExtensionInfo(textureMod.Name + "-Offsets", textureMod.Version, textureMod.DisplayName + " (Offsets)", textureMod.Author);
        }

        /// <summary>
        /// Opens the module <paramref name="info"/>. Like <see cref="UcpTexturePlugin.Open"/>, the folder of another
        /// version is renamed to the new version.
        /// </summary>
        /// <param name="executables">The executables of the Stronghold folder, see <see cref="StrongholdExecutable.LoadAll"/>.</param>
        public static UcpOffsetModule Open(UcpFolder ucp, UcpExtensionInfo info, IReadOnlyList<StrongholdExecutable> executables)
        {
            if (ucp == null) throw new ArgumentNullException(nameof(ucp));
            if (info == null) throw new ArgumentNullException(nameof(info));
            if (executables == null) throw new ArgumentNullException(nameof(executables));

            var module = new UcpOffsetModule(info, ucp.ModuleFolder(info), executables);
            if (ExtensionFiles.MoveOtherVersion(module.Folder, ucp.FindModuleFolders(info.Name)) && module.HasExecutables)
            {
                module.UpdateDefinition();
            }

            return module;
        }

        public bool Supports(int imageIndex) => HasExecutables && CastleOffsetAddresses.TryGet(imageIndex, out _);

        /// <summary>The offset set by this module, otherwise the offset stored in the executable.</summary>
        public bool TryRead(int imageIndex, out BuildingOffset offset)
        {
            offset = default;
            if (!CastleOffsetAddresses.TryGet(imageIndex, out var address))
            {
                return false;
            }

            if (Offsets.TryGetValue(imageIndex, out offset))
            {
                return true;
            }

            foreach (var executable in executables)
            {
                if (executable.TryRead(address, out offset))
                {
                    return true;
                }
            }

            return false;
        }

        /// <exception cref="InvalidDataException">No executable contains a unique pattern for the offset (unknown version).</exception>
        public void Write(int imageIndex, BuildingOffset offset) => WriteAll(new[] { new KeyValuePair<int, BuildingOffset>(imageIndex, offset) });

        /// <summary>Sets several offsets at once. Nothing is written if one of them cannot be set.</summary>
        /// <exception cref="InvalidDataException">No executable contains a unique pattern for an offset (unknown version).</exception>
        public void WriteAll(IEnumerable<KeyValuePair<int, BuildingOffset>> offsets)
        {
            if (offsets == null) throw new ArgumentNullException(nameof(offsets));
            if (!HasExecutables) throw new InvalidOperationException("No Stronghold executable was found.");

            var all = new SortedDictionary<int, BuildingOffset>(Offsets.ToDictionary(entry => entry.Key, entry => entry.Value));
            foreach (var entry in offsets)
            {
                if (!CastleOffsetAddresses.TryGet(entry.Key, out _))
                {
                    throw new ArgumentOutOfRangeException(nameof(offsets), entry.Key, "This image has no offset in the executable.");
                }

                all[entry.Key] = entry.Value;
            }

            // creating the script fails for unknown executables, before anything is written
            string script = CreateInitScript(all);
            ExtensionFiles.Write(Folder, CreateDefinition(), script, CreateDescription(all));
            BuildingOffsetStore.Load(OffsetsFile).SetAll(all);
        }

        /// <summary>Rewrites definition.yml, init.lua and the description, e.g. after the name or author changed.</summary>
        public void UpdateDefinition()
        {
            var offsets = Offsets;
            ExtensionFiles.Write(Folder, CreateDefinition(), CreateInitScript(offsets), CreateDescription(offsets));
        }

        public string CreateDefinition() => ExtensionFiles.CreateDefinition(Info, "module", "Castle building offsets set with the Gm1 Konverter", Dependencies);

        /// <summary>The generated init.lua that writes <paramref name="offsets"/> into the running game.</summary>
        public string CreateInitScript(IReadOnlyDictionary<int, BuildingOffset> offsets)
        {
            if (offsets == null) throw new ArgumentNullException(nameof(offsets));

            var lua = new StringBuilder();
            lua.Append("-- Generated by Gm1 Konverter. The file is rewritten whenever an offset is changed.\n");
            lua.Append("-- Sets the drawing offsets of castle buildings (anim_castle.gm1). Each offset is found by a byte\n");
            lua.Append("-- pattern of Stronghold Crusader.exe or Stronghold_Crusader_Extreme.exe; the bytes are written\n");
            lua.Append("-- relative to the start of the pattern.\n");
            lua.Append("local patches = {\n");
            foreach (var entry in offsets.OrderBy(entry => entry.Key))
            {
                if (!CastleOffsetAddresses.TryGet(entry.Key, out var address))
                {
                    // e.g. an edited offsets.json
                    continue;
                }

                lua.Append("  {\n");
                lua.Append("    image = ").Append(entry.Key).Append(",\n");
                lua.Append("    variants = {\n");
                foreach (var patch in CreatePatches(address, entry.Value))
                {
                    lua.Append("      { pattern = ").Append(ScriptText.LuaString(patch.Pattern.ToString())).Append(", writes = {");
                    lua.Append(string.Join(",", patch.Writes.Select(write =>
                        $" {{ {write.Address}, {{ {string.Join(", ", write.Bytes.Select(b => $"0x{b:X2}"))} }} }}")));
                    lua.Append(" } },\n");
                }

                lua.Append("    },\n");
                lua.Append("  },\n");
            }

            lua.Append("}\n");
            lua.Append('\n');
            lua.Append("local function find(variants)\n");
            lua.Append("  for _, variant in ipairs(variants) do\n");
            lua.Append("    local address = core.scanForAOB(variant.pattern)\n");
            lua.Append("    if address ~= nil and address ~= 0 then\n");
            lua.Append("      return address, variant\n");
            lua.Append("    end\n");
            lua.Append("  end\n");
            lua.Append("  return nil, nil\n");
            lua.Append("end\n");
            lua.Append('\n');
            lua.Append("return {\n");
            lua.Append("  enable = function(self, config)\n");
            lua.Append("    for _, patch in ipairs(patches) do\n");
            lua.Append("      local address, variant = find(patch.variants)\n");
            lua.Append("      if address == nil then\n");
            lua.Append("        log(WARNING, ").Append(ScriptText.LuaString($"[{Info.Name}]: offset of image ")).Append(" .. patch.image .. \" not found, this game version is not supported\")\n");
            lua.Append("      else\n");
            lua.Append("        for _, write in ipairs(variant.writes) do\n");
            lua.Append("          core.writeCodeBytes(address + write[1], write[2])\n");
            lua.Append("        end\n");
            lua.Append("      end\n");
            lua.Append("    end\n");
            lua.Append("  end,\n");
            lua.Append('\n');
            lua.Append("  disable = function(self, config)\n");
            lua.Append("  end,\n");
            lua.Append("}\n");
            return lua.ToString();
        }

        /// <summary>
        /// Patterns that find the offset in every executable, with the writes relative to the pattern start.
        /// One pattern usually fits both executables.
        /// </summary>
        /// <exception cref="InvalidDataException">An executable does not contain a unique pattern.</exception>
        internal IReadOnlyList<OffsetPatch> CreatePatches(OffsetAddress address, BuildingOffset offset)
        {
            var patches = new List<OffsetPatch>();
            foreach (var executable in executables)
            {
                if (!patches.Any(patch => patch.FindsOnlyExpectedPlace(executable, mustMatch: true)))
                {
                    patches.Add(CreateUniquePatch(address, offset, executable));
                }
            }

            return patches;
        }

        private OffsetPatch CreateUniquePatch(OffsetAddress address, BuildingOffset offset, StrongholdExecutable own)
        {
            if (!own.Contains(address))
            {
                throw new InvalidDataException($"\"{own.Name}\" is too small for the building offsets, this version of Stronghold is not supported.");
            }

            for (int context = ContextStep; context <= MaxContext; context += ContextStep)
            {
                // Extreme addresses of the window, limited to the file
                int start = Math.Max(address.Start - context, -own.AddressShift);
                int end = Math.Min(address.End + context, own.Bytes.Length - own.AddressShift);

                var pattern = AobPattern.Create(
                    own.Bytes.AsSpan(start + own.AddressShift, end - start),
                    i => CastleOffsetAddresses.VariableAddresses.Contains(start + i),
                    out int trimmed);
                if (pattern == null)
                {
                    continue;
                }

                var patch = new OffsetPatch(pattern, start + trimmed, address.GetWrites(offset));
                if (executables.All(executable => patch.FindsOnlyExpectedPlace(executable, mustMatch: executable == own)))
                {
                    return patch;
                }
            }

            throw new InvalidDataException($"The building offsets could not be located uniquely in \"{own.Name}\", this version of Stronghold is not supported.");
        }

        private string CreateDescription(IReadOnlyDictionary<int, BuildingOffset> offsets)
        {
            var markdown = new StringBuilder();
            markdown.Append("# ").Append(ScriptText.SingleLine(Info.DisplayName)).Append('\n');
            markdown.Append('\n');
            markdown.Append("Drawing offsets of castle buildings (anim_castle.gm1) set with the Gm1 Konverter.\n");
            markdown.Append('\n');
            markdown.Append("This module is not signed: start UCP3 without security to use it.\n");
            markdown.Append('\n');
            markdown.Append("| Image | X | Y |\n");
            markdown.Append("|---|---|---|\n");
            foreach (var entry in offsets.OrderBy(entry => entry.Key))
            {
                markdown.Append("| ").Append(entry.Key).Append(" | ").Append(entry.Value.X).Append(" | ").Append(entry.Value.Y).Append(" |\n");
            }

            return markdown.ToString();
        }
    }

    /// <summary>A byte pattern and the bytes to write relative to its start.</summary>
    internal sealed class OffsetPatch
    {
        /// <param name="startAddress">Extreme address of the first pattern byte.</param>
        public OffsetPatch(AobPattern pattern, int startAddress, IReadOnlyList<OffsetWrite> writes)
        {
            Pattern = pattern;
            StartAddress = startAddress;
            Writes = writes.Select(write => new OffsetWrite(write.Address - startAddress, write.Bytes.ToArray())).ToList();
        }

        public AobPattern Pattern { get; }

        public int StartAddress { get; }

        /// <summary>Writes with addresses relative to the pattern start.</summary>
        public IReadOnlyList<OffsetWrite> Writes { get; }

        /// <summary>
        /// True if the pattern matches <paramref name="executable"/> nowhere (allowed unless <paramref name="mustMatch"/>)
        /// or exactly once at the offset.
        /// </summary>
        public bool FindsOnlyExpectedPlace(StrongholdExecutable executable, bool mustMatch)
        {
            var matches = Pattern.FindAll(executable.Bytes, 2);
            return matches.Count == 0
                ? !mustMatch
                : matches.Count == 1 && matches[0] == StartAddress + executable.AddressShift;
        }
    }
}
