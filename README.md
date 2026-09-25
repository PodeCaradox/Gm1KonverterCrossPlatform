
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

All projects target .NET 8. Publish the Windows build for `win10-x64` (as the publish profile does);
with `win-x64` the ANGLE library `av_libglesv2.dll` is missing and Avalonia falls back to software rendering.

The file format must stay byte compatible with Stronghold. Changes to the codecs in
`Gm1KonverterCrossPlatform.Core/Codecs` have to keep the differential tests green.
