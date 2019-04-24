using System;
using System.Collections.Generic;

namespace Files.Gm1Converter
{
    class DecodedFile
    {

        #region Variables

        private Palette palette;

        private GM1FileHeader fileHeader;

        private List<TGXImage> images;

        private int actualPositionInByteArray = 0;

        #endregion

        #region Construtor

        public DecodedFile(byte[] array,String name)
        {

            this.fileHeader = new GM1FileHeader(array);
            this.fileHeader.Name = name;
            this.palette = new Palette(array);
            this.images = new List<TGXImage>();
            actualPositionInByteArray = (GM1FileHeader.fileHeaderSize + Palette.paletteSize); ;
            if (fileHeader.IDataType == (UInt32)GM1FileHeader.DataType.Animations)
            {
                CreateImagesFromAnimationFile(array);
            }

      
        }

        public byte[] GetNewGM1Bytes() {

            //testing
            palette.DebugTestPalette();



            List<byte> newFile = new List<byte>();
            var headerBytes = fileHeader.GetBytes();
            newFile.AddRange(headerBytes);
            newFile.AddRange(palette.ArrayPaletteByte);
            for (int i = 0; i < fileHeader.INumberOfPictureinFile; i++)
            {
                images[i].ConvertImageToByteArray();
                newFile.AddRange(BitConverter.GetBytes(images[i].OffsetinByteArray));
            }

            for (int i = 0; i < fileHeader.INumberOfPictureinFile; i++)
            {
                newFile.AddRange(BitConverter.GetBytes(images[i].SizeinByteArray));
            }

            for (int i = 0; i < fileHeader.INumberOfPictureinFile; i++)
            {
                newFile.AddRange(images[i].GetImageHeaderAsByteArray());
            }

            for (int i = 0; i < fileHeader.INumberOfPictureinFile; i++)
            {
                newFile.AddRange(images[i].ImgFileAsBytearray);
            }


            return newFile.ToArray();
        }

        private void CreateImagesFromAnimationFile(byte[] array)
        {
           
                CreateOffsetAndSizeInByteArrayList(array);
            
            
            //Image Header has a length of 16 bytes 

            for (int i = 0; i < this.fileHeader.INumberOfPictureinFile; i++)
            {

                images[i].Width = BitConverter.ToUInt16(array, actualPositionInByteArray + i * TGXImage.iImageHeaderSize + 0);
                images[i].Height = BitConverter.ToUInt16(array, actualPositionInByteArray + i * TGXImage.iImageHeaderSize + 2);
                images[i].OffsetX = BitConverter.ToUInt16(array, actualPositionInByteArray + i * TGXImage.iImageHeaderSize + 4);
                images[i].OffsetY = BitConverter.ToUInt16(array, actualPositionInByteArray + i * TGXImage.iImageHeaderSize + 6);
                images[i].ImagePart = array[actualPositionInByteArray + i * TGXImage.iImageHeaderSize + 8];
                images[i].SubParts = array[actualPositionInByteArray + i * TGXImage.iImageHeaderSize + 9];
                images[i].TileOffset = BitConverter.ToUInt16(array, actualPositionInByteArray + i * TGXImage.iImageHeaderSize + 10);
                images[i].Direction = array[actualPositionInByteArray + i * TGXImage.iImageHeaderSize + 12];
                images[i].HorizontalOffsetOfImage = array[actualPositionInByteArray + i * TGXImage.iImageHeaderSize + 13];
                images[i].BuildingWidth = array[actualPositionInByteArray + i * TGXImage.iImageHeaderSize + 14];
                images[i].AnimatedColor = array[actualPositionInByteArray + i * TGXImage.iImageHeaderSize + 15];

            }

            actualPositionInByteArray += (int)this.fileHeader.INumberOfPictureinFile * TGXImage.iImageHeaderSize;

            

            foreach (var image in images)
            {
                    image.ImgFileAsBytearray = new byte[(int)image.SizeinByteArray];
                    Buffer.BlockCopy(array, actualPositionInByteArray + (int)image.OffsetinByteArray, image.ImgFileAsBytearray, 0, (int)image.SizeinByteArray);
            }


            //Dekodiere alle Farben aus testzwecken
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < fileHeader.INumberOfPictureinFile; j++)
                {
                    var dummy = new TGXImage();
                    dummy.Width = images[j].Width;
                    dummy.Height = images[j].Height;
                    dummy.OffsetX = images[j].OffsetX;
                    dummy.OffsetY = images[j].OffsetY;
                    dummy.ImagePart = images[j].ImagePart;
                    dummy.SubParts = images[j].SubParts;
                    dummy.TileOffset = images[j].TileOffset;
                    dummy.Direction = images[j].Direction;
                    dummy.HorizontalOffsetOfImage = images[j].HorizontalOffsetOfImage;
                    dummy.BuildingWidth = images[j].BuildingWidth;
                    dummy.AnimatedColor = images[j].AnimatedColor;
                    dummy.ImgFileAsBytearray = images[j].ImgFileAsBytearray;
                    images.Add(dummy);
                }
            }

            for (uint i = 0; i < images.Count; i++)
            {
                images[(int)i].CreateImageFromByteArray(palette, fileHeader, i/fileHeader.INumberOfPictureinFile);
            }


        }

        private void CreateOffsetAndSizeInByteArrayList(byte[] array)
        {
            for (int i = 0; i < this.fileHeader.INumberOfPictureinFile; i++)
            {
                var image = new TGXImage();
                image.OffsetinByteArray = BitConverter.ToUInt32(array, actualPositionInByteArray + i * 4);
                images.Add(image);
            }
            actualPositionInByteArray += (int)this.fileHeader.INumberOfPictureinFile * 4;


            for (int i = 0; i < this.fileHeader.INumberOfPictureinFile; i++)
            {

                images[i].SizeinByteArray = BitConverter.ToUInt32(array, actualPositionInByteArray + i * 4);

            }
            actualPositionInByteArray += (int)this.fileHeader.INumberOfPictureinFile * 4;


        }

        #endregion

        #region GetterSetter

        internal GM1FileHeader FileHeader { get => fileHeader; }
        internal Palette Palette { get => palette; }
        internal List<TGXImage> Images { get => images; }

        #endregion

    }
}
