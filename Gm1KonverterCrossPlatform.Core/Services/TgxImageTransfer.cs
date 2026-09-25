using System;
using System.IO;
using Gm1KonverterCrossPlatform.Core.Documents;
using Gm1KonverterCrossPlatform.Core.IO;

namespace Gm1KonverterCrossPlatform.Core.Services
{
    /// <summary>Exports and imports the image of a standalone .tgx file.</summary>
    public sealed class TgxImageTransfer
    {
        private readonly WorkFolder workFolder;

        public TgxImageTransfer(WorkFolder workFolder)
        {
            this.workFolder = workFolder ?? throw new ArgumentNullException(nameof(workFolder));
        }

        public string ImageFile(TgxDocument document) => workFolder.TgxImageFile(document.FileName);

        /// <returns>The folder the image was written to.</returns>
        public string Export(TgxDocument document)
        {
            ImageFiles.SavePng(document.Render(), ImageFile(document));
            return workFolder.FileFolder(document.FileName);
        }

        public void Import(TgxDocument document)
        {
            string path = ImageFile(document);
            if (!File.Exists(path))
            {
                throw new WorkflowException($"\"{path}\" does not exist. Please export the TGX image first.");
            }

            document.Replace(Gm1Importer.LoadPng(path));
        }
    }
}
