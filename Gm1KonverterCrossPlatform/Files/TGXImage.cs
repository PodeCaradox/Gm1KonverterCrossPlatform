using System;
using System.Collections.Generic;
using System.Drawing;
using Avalonia.Media.Imaging;
using HelperClasses.Gm1Converter;

namespace Files.Gm1Converter
{
    class TGXImage
    {
        public static readonly int iImageHeaderSize = 16;

        #region Variables
        private byte[] imgFileAsBytearray;
        private ushort width;
        private ushort height;
        private ushort offsetX;
        private ushort offsetY;
        private byte imagePart;
        private byte subParts;
        private ushort tileOffset;
        private byte direction;
        private byte horizontalOffsetOfImage;
        private byte buildingWidth;
        private byte animatedColor;
        private UInt32 offsetinByteArray;
        private UInt32 sizeinByteArray;
        #endregion

        #region Construtor
        public TGXImage()
        {

        }
        #endregion

        #region GetterSetter
        public UInt16 Width { get => width; set => width = value; }
        public UInt16 Height { get => height; set => height = value; }
        public UInt16 OffsetX { get => offsetX; set => offsetX = value; }
        public UInt16 OffsetY { get => offsetY; set => offsetY = value; }



        /// <summary>
        /// 0 denotes the start of a new collection
        /// </summary>
        public byte ImagePart { get => imagePart; set => imagePart = value; }

        /// <summary>
        /// Number of following parts in the collection
        /// </summary>
        public byte SubParts { get => subParts; set => subParts = value; }

     

        /// <summary>
        /// Vertical offset of the tile object on large surface
        /// </summary>
        public UInt16 TileOffset { get => tileOffset; set => tileOffset = value; }

        /// <summary>
        /// left,right, center... used for buildings only. 
        /// </summary>
        public byte Direction { get => direction; set => direction = value; }

        /// <summary>
        /// Initial horizontal offset of image. 
        /// </summary>
        public byte HorizontalOffsetOfImage { get => horizontalOffsetOfImage; set => horizontalOffsetOfImage = value; }

        /// <summary>
        /// Width of building part.
        /// </summary>
        public byte BuildingWidth { get => buildingWidth; set => buildingWidth = value; }

        /// <summary>
        /// Color, used for animated units only. 
        /// </summary>
        public byte AnimatedColor { get => animatedColor; set => animatedColor = value; }
        public UInt32 OffsetinByteArray { get => offsetinByteArray; set => offsetinByteArray = value; }
        public UInt32 SizeinByteArray { get => sizeinByteArray; set => sizeinByteArray = value; }
        public byte[] ImgFileAsBytearray { get => imgFileAsBytearray; set => imgFileAsBytearray = value; }


        public WriteableBitmap bmp;
        #endregion

        #region Methods


        //unsafe
        public unsafe void CreateImageFromByteArray(Palette palette)
        {
          
            
            bmp = new WriteableBitmap(new Avalonia.PixelSize(width, height), new Avalonia.Vector(100, 100), Avalonia.Platform.PixelFormat.Bgra8888);// Bgra8888 is device-native and much faster.
            
            using (var buf = bmp.Lock())
            {
                int x = 0;
                int y = 0;
                byte r, g, b, a;
                for (int bytePos = 0; bytePos < imgFileAsBytearray.Length;)
                {
            

                    byte token = imgFileAsBytearray[bytePos];
                    byte tokentype = (byte)(token >> 5);
                    byte length = (byte)((token & 31) + 1);

                    //transparent
                    UInt32 colorByte = Utility.TransparentColorByte;

                    bytePos++;
                    byte index;
                  
                    ushort pixelColor;
                    switch (tokentype)
                    {
                        case 0://Stream-of-pixels 

                            for (byte i = 0; i < length; i++)
                            {
                                index = imgFileAsBytearray[bytePos];
                                bytePos++;
                                pixelColor = palette.ArrayPaletten[palette.ActualPalette, index];
                                Utility.ReadColor(pixelColor, out r, out g, out b, out a);

                     
                                colorByte = (UInt32)(b | (g << 8) | (r << 16) | (a << 24));
                                var ptr = (uint*)buf.Address;
                                ptr += (uint)((width * y) + x);
                                *ptr = colorByte;
                                x++;
                               
                            }
                            break;
                        case 4://Newline
                            for (byte i = 0; i < this.width; i++)
                            {
                                if (x >= this.width || y >= this.height)
                                    break;

                                //Bgra8888
                               
                                var ptr = (uint*)buf.Address;
                                ptr += (uint)((width * y) + x);
                                *ptr = colorByte;
                                x++;
                               
                            }

                            y++;
                            if (y > this.height) break;
                            x = 0;
                            break;
                        case 2://Repeating pixels 

                            index = imgFileAsBytearray[bytePos];
                            bytePos++;
                            pixelColor = palette.ArrayPaletten[palette.ActualPalette, index];

                            Utility.ReadColor(pixelColor, out r, out g, out b, out a);
                            colorByte = (uint)(b | (g << 8) | (r << 16) | (a << 24));
                            for (byte i = 0; i < length; i++)
                            {
                                var ptr = (uint*)buf.Address;
                                ptr += (uint)((width * y) + x);
                                *ptr = colorByte;
                                x++;
                                
                            }
                            break;
                        case 1://Transparent-Pixel-String 

                            for (byte i = 0; i < length; i++)
                            {

                                
                                var ptr = (uint*)buf.Address;
                                ptr += (uint)((width * y) + x);
                                *ptr = colorByte;
                                x++;
                               
                            }
                            break;
                        default:
                            break;

                    }

                }

            }

           

        }
       
        internal void ConvertImageToByteArray(Palette palette)
        {
            var colors = Utility.ImgToColors(bmp,width,height);
            imgFileAsBytearray = Utility.ImgToGM1ByteArray(colors,width,height, imgFileAsBytearray, palette);
            sizeinByteArray = (uint)imgFileAsBytearray.Length;
        }

        internal byte[] GetImageHeaderAsByteArray()
        {
            List<byte> array = new List<byte>();
            array.AddRange(BitConverter.GetBytes(width));
            array.AddRange(BitConverter.GetBytes(height));
            array.AddRange(BitConverter.GetBytes(offsetX));
            array.AddRange(BitConverter.GetBytes(offsetY));
            array.Add(imagePart);
            array.Add(subParts);
            array.AddRange(BitConverter.GetBytes(tileOffset));
            array.Add(direction);
            array.Add(horizontalOffsetOfImage);
            array.Add(buildingWidth);
            array.Add(animatedColor);
            return array.ToArray();
        }


        #endregion
    }
}
