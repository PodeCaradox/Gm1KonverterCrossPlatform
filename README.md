
Gm1KonverterCrossPlatform
=======================
A tool to convert Stronghold and Stronghold Crusader .gm1 files to .png and vice versa.

English:
---------
Hello Guys,

I created a tool to import/export GM1 and TGX files for Windows and Linux.

If you have any questions, just add me on Discord: PodeCaradox#1397

To download the program just click on the link and download the Converter.zip on the assets symbol.

![Download](https://github.com/Gaaammmler/Gm1KonverterCrossPlatform/releases)

![img2](https://github.com/Gaaammmler/Gm1KonverterCrossPlatform/blob/master/GMConverterImages/img2.JPG)


During the first launch you will have to select your Stronghold GM1 Folder and a Workfolder folder under Options, you can also choose a different language: German/Russian/English

![img1](https://github.com/Gaaammmler/Gm1KonverterCrossPlatform/blob/master/GMConverterImages/img1.JPG)

For more info regarding specific filetypes click on the Info icon.

![img3](https://github.com/Gaaammmler/Gm1KonverterCrossPlatform/blob/master/GMConverterImages/img3.JPG)


Support for the tgx Files is also there:
Just click on the Button Gm1/GFX to change the preview list from both folder(gm/gfx).
![preview1](https://user-images.githubusercontent.com/5760157/65734537-a7d53580-e0d3-11e9-8e31-8ce2546aca53.JPG)


If you want Stronghold 1 graphics in Crusader:
![SH1 Graphics](https://github.com/Gaaammmler/Stronghold-Crusader-Sh1-Graphics)

Thanks to

![Lolasik011](https://github.com/Lolasik011) for the russian translation

![metalvoidzz](https://github.com/metalvoidzz) for his tutorial on how to decode GM1 Files

![StrongholdOverlordsMod](https://github.com/StrongholdOverlordsMod) Contributer for future updates.

Deutsch
---------

Hallo Leute ich habe angefangen einen GM1 Exporter/Importer für Windows und Linux zu programmieren.

Falls du fragen hast adde mich auf Discord: PodeCaradox#1397

Um das Programm zu Donwloaden klicke auf den Link und Downloade die Converter.zip unterhalb dem Assets Symbol.

![Download](https://github.com/Gaaammmler/Gm1KonverterCrossPlatform/releases)

![img2](https://github.com/Gaaammmler/Gm1KonverterCrossPlatform/blob/master/GMConverterImages/img2.JPG)

Wenn du das Programm zum ersten mal startest muss unter den Optionen der GM1 Ordner und der Arbeitsordner ausgewählt werden, außerdem kann die Sprache zwischen Englisch/Deutsch/Russisch geändert werden.

![img1](https://github.com/Gaaammmler/Gm1KonverterCrossPlatform/blob/master/GMConverterImages/img1.JPG)

Für mehr informationen zu einem Dateityp klicke auf das Infoicon.

![img3](https://github.com/Gaaammmler/Gm1KonverterCrossPlatform/blob/master/GMConverterImages/img3.JPG)


Support für die tgx Dateien ist auch vorhanden:
Klicke hierzu einfach auf den Gm1/GFX Button um die Ansicht zu ändern(gm/gfx)
![preview1](https://user-images.githubusercontent.com/5760157/65734537-a7d53580-e0d3-11e9-8e31-8ce2546aca53.JPG)



Falls du Stronghold 1 Grafiken in Crusader möchtest:
![SH1 Graphics](https://github.com/Gaaammmler/Stronghold-Crusader-Sh1-Graphics)

Thanks to

![Lolasik011](https://github.com/Lolasik011) for the russian translation

![metalvoidzz](https://github.com/metalvoidzz) for his Tutorial how to decode GM1 Files

![StrongholdOverlordsMod](https://github.com/StrongholdOverlordsMod) Contributer for future updates.

Larger images / Größere Bilder
---------

Images can be larger than the original: export the images, enlarge the canvas of `Images/ImageN.png`
in your image editor and import the images again. The big image (BigImage) keeps the original sizes.
Buildings must be `32 × n − 2` pixels wide (n = 1 to 15 diamonds per row); the ground diamonds stay at the bottom.

Bilder können größer als das Original sein: Bilder exportieren, die Leinwand von `Images/ImageN.png`
im Bildprogramm vergrößern und die Bilder wieder importieren. Beim großen Bild (BigImage) bleiben die
Originalgrößen erhalten. Gebäude müssen `32 × n − 2` Pixel breit sein (n = 1 bis 15 Rauten pro Reihe);
die Boden-Rauten bleiben unten.

UCP3 mod / UCP3-Mod
---------

"File → Save GM1/Tgx in UCP Mod" no longer overwrites the game files. The modified file is saved in a
[UCP3](https://github.com/UnofficialCrusaderPatch/UnofficialCrusaderPatch3) plugin in the Stronghold folder:

```
ucp/plugins/<Name>-<Version>/
  definition.yml          name, author, version, dependencies (framework, frontend, files)
  init.lua                modules.files:overrideFileWith("gm\\anim_castle.gm1", ...) for every file
  locale/description-en.md
  resources/gm/*.gm1
  resources/gfx/*.tgx
```

Activate the plugin in the UCP3 GUI on the "Content" tab (select it under "Available extensions" and click
"Activate"). "Remove … from UCP Mod (Original)" deletes the file from the plugin so
the game loads the original again (and restores a backup `<name>Save.gm1` from the work folder if an older
version of this program overwrote the game file). Name, author and version are set under "File → UCP Mod…";
a new version moves the existing plugin, a new name starts a new plugin. Files already in the plugin are
opened from there, so you keep editing your modded version.

Building offsets of the castle (`anim_castle.gm1`) are no longer patched into the executables either. They
live in the UCP3 module `ucp/modules/<Name>-Offsets-<Version>.zip` (a zip, because the UCP3 GUI of a normal UCP3
installation only lists modules packed as zip), which is written when `anim_castle.gm1` is saved into the UCP mod
and whenever "Change Offsets" / "Import Offsets From File" is used:

- Activate the module on the "Content" tab like the plugin. Every offset can then be changed on the
  "Customisations" tab of the UCP3 GUI (options of the module, `options.yml`). The values set in this
  program are the start values. An offset is only written if it was set here or changed in the UCP3 GUI; all
  other offsets keep the value of the game.
- It works for Stronghold Crusader and Crusader Extreme. The offsets of Crusader are 912 bytes before those of
  Extreme and the surrounding code contains different memory addresses, so the module uses byte patterns
  (AOB) that fit both games (`KnownCastleOffsets`, created from both executables with the offset values and the
  version-specific bytes as wildcards). The executables in your Stronghold folder are optional; if they are a
  different version, an extra pattern is created from them.
- The `init.lua` finds each offset with `core.scanForAOB` and writes it with `core.writeCodeBytes` when the game
  starts. Modules can change the game's memory, therefore UCP3 only loads this unsigned module when the game is
  started from the "Launch" tab of the UCP3 GUI with the option "Disable Security". Without it (also when the game
  is started through Steam or the exe, which always use secure mode) UCP3 stops with "FATAL: … failed to verify zip
  file … Missing security signature". Only modules of the UCP3 store are signed; a plugin cannot change the
  game's code, so there is no unsigned way to patch the offsets.

Die Gebäude-Offsets der Burg werden nicht mehr in die Exe geschrieben, sondern stehen im UCP3-Modul
`ucp/modules/<Name>-Offsets-<Version>.zip` (als Zip, weil die UCP3-Oberfläche Modul-Ordner nur bei
Entwickler-Versionen anzeigt). Es wird beim Speichern von `anim_castle.gm1` in die UCP-Mod angelegt.
Das Modul wird im Tab „Inhalt“ aktiviert (unter „Verfügbare Erweiterungen“ auswählen, „Aktivieren“), danach
lässt sich jeder Offset im Tab „Anpassungen“ ändern; das Tool liefert nur die
Startwerte. Geschrieben werden nur Werte, die hier gesetzt oder in der UCP3-Oberfläche geändert wurden. Das Modul
funktioniert für Crusader und Extreme (Crusader liegt 912 Bytes früher, die Muster passen auf beide Versionen).
Da es nicht signiert ist, lädt UCP3 es nur, wenn das Spiel im Tab „Starten“ mit der Option „Sicherheit deaktivieren“ gestartet wird.
Sonst (auch beim Start über Steam oder die Exe) bricht UCP3 mit „FATAL: … Missing security signature“ ab.

„Datei → GM1/Tgx im UCP-Mod speichern“ überschreibt die Spieldateien nicht mehr, sondern speichert die
geänderte Datei in einem UCP3-Plugin unter `ucp/plugins/<Name>-<Version>` im Stronghold-Ordner. Das Plugin
wird in der UCP3-Oberfläche im Tab „Inhalt“ aktiviert. „… aus UCP-Mod entfernen (Original)“ nimmt die Datei wieder heraus,
dann lädt das Spiel das Original. Name, Autor und Version stehen unter „Datei → UCP-Mod…“.

Development / Entwicklung
---------

The solution consists of three projects:

| Project | Content |
|---------|---------|
| `Gm1KonverterCrossPlatform` | Avalonia user interface (views, view models) |
| `Gm1KonverterCrossPlatform.Core` | File formats (.gm1, .tgx), codecs, import/export, settings – no UI dependencies |
| `Gm1KonverterCrossPlatform.Tests` | xUnit tests, including differential tests against the previous encoder/decoder (`Legacy` folder) |

Build and run the tests with the .NET 8 SDK:

```
dotnet build Gm1KonverterCrossPlatform.sln
dotnet test Gm1KonverterCrossPlatform.Tests
```

All projects target .NET 8. Publish the Windows build with the publish profile, which builds a
self-contained `win10-x64` app (users need no .NET installation):

```
dotnet publish Gm1KonverterCrossPlatform/Gm1KonverterCrossPlatform.csproj -p:PublishProfile=FolderProfile
```

With `win-x64` the ANGLE library `av_libglesv2.dll` is missing and Avalonia falls back to software rendering.

The file format must stay byte compatible with Stronghold. Changes to the codecs in
`Gm1KonverterCrossPlatform.Core/Codecs` have to keep the differential tests green.
