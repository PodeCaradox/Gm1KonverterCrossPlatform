namespace Gm1KonverterCrossPlatform.TexturePack
{
    public class IncludedFile
    {
        public enum Filetype { TGX, GM1 };

        public Filetype Type;
        public string FileName;
        public byte[] FileData;
    }
}
