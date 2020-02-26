using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gm1KonverterCrossPlatform.TexturePack
{
    public class IncludedFile
    {
        public enum Filetype { TGX,GM1};

        public Filetype Type;
        public String FileName;
        public byte[] FileData;
    }
}
