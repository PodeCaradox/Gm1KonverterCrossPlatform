using Avalonia.Controls;
using Gm1KonverterCrossPlatform.Files;
using Gm1KonverterCrossPlatform.HelperClasses.Views;
using HelperClasses.Gm1Converter;
using System;
using System.Collections.Generic;

namespace Files.Gm1Converter
{
    class DecodedFile
    {

        #region Variables

        private Palette palette;

        private GM1FileHeader fileHeader;

        private List<TGXImage> _TGXImage;

        private List<TilesImage> tilesImage;

        private int actualPositionInByteArray = 0;

        private byte[] fileArray;

        #endregion

        #region Construtor

        public DecodedFile()
        {

        }

        #endregion


        #region Methods

        /// <summary>
        /// Decode the actual GM1 File to Imgs and the typical headers
        /// </summary>
        /// <param name="array">The GM1 File as Array</param>
        /// <param name="name">Name of the File</param>
        /// <returns></returns>
        public bool DecodeGm1File(byte[] array, String name)
        {
            fileArray = array;
            if (this.fileHeader == null)
            {
                this.fileHeader = new GM1FileHeader(array);
                this.fileHeader.Name = name;
                if (fileHeader.IDataType == (UInt32)GM1FileHeader.DataType.Animations)
                {
                    this.palette = new Palette(array);
                }
            }


            actualPositionInByteArray = (GM1FileHeader.fileHeaderSize + Palette.paletteSize);
            this._TGXImage = new List<TGXImage>();
            this.tilesImage = new List<TilesImage>();
            //Supported Types
            switch ((GM1FileHeader.DataType)fileHeader.IDataType)
            {
                case GM1FileHeader.DataType.Animations:
                case GM1FileHeader.DataType.Interface:
                case GM1FileHeader.DataType.TGXConstSize:
                case GM1FileHeader.DataType.Font:
                    CreateImages(array);
                    return true;
                case GM1FileHeader.DataType.TilesObject:
                    CreateTileImage(array);
                    return true;
                default:
                    break;
            }

            return false;
        }

        /// <summary>
        /// Create the new GM1 Files from IMGS and Headers(1. FileHeader,2. Palette,3. OffsetList,4. SizeList,5. ImgHeaderList,6. ImgsasByteList)
        /// </summary>
        /// <returns></returns>
        public byte[] GetNewGM1Bytes()
        {


            List<byte> newFile = new List<byte>();
            var headerBytes = fileHeader.GetBytes();
            newFile.AddRange(headerBytes);
            if (palette == null)
            {
                newFile.AddRange(new byte[Palette.paletteSize]);
            }
            else
            {
                palette.CalculateNewBytes();
                newFile.AddRange(palette.ArrayPaletteByte);
            }

            for (int i = 0; i < fileHeader.INumberOfPictureinFile; i++)
            {
                newFile.AddRange(BitConverter.GetBytes(_TGXImage[i].OffsetinByteArray));

            }

            for (int i = 0; i < fileHeader.INumberOfPictureinFile; i++)
            {
                newFile.AddRange(BitConverter.GetBytes(_TGXImage[i].SizeinByteArray));

            }

            for (int i = 0; i < fileHeader.INumberOfPictureinFile; i++)
            {
                newFile.AddRange(_TGXImage[i].GetImageHeaderAsByteArray());
            }

            for (int i = 0; i < fileHeader.INumberOfPictureinFile; i++)
            {
                newFile.AddRange(_TGXImage[i].ImgFileAsBytearray);
            }


            return newFile.ToArray();
        }

        /// <summary>
        /// Creates the IMG from the byte array, handles Animation,Interfaces,TGXConstSize, Font
        /// </summary>
        /// <param name="array">The GM1 File as byte Array</param>
        private void CreateImages(byte[] array)
        {

            CreateOffsetAndSizeInByteArrayList(array);
            CreateImgHeader(array);

            for (uint i = 0; i < fileHeader.INumberOfPictureinFile; i++)
            {
                _TGXImage[(int)i].CreateImageFromByteArray(palette);
            }
        }

        private List<TGXImage> newTileList = new List<TGXImage>();
        /// <summary>
        /// Convert Img To Tiled Diamond Images
        /// </summary>
        /// <param name="list"></param>
        internal void ConvertImgToTiles(List<ushort> list, ushort width, ushort height)
        {
            newTileList.AddRange(Utility.ConvertImgToTiles(list, width, height,ImagesTGX));
        }

        /// <summary>
        /// Creates the IMG Header
        /// </summary>
        /// <param name="array">The GM1 File as byte Array</param>
        private void CreateImgHeader(byte[] array)
        {
            //Image Header has a length of 16 bytes 
            for (int i = 0; i < this.fileHeader.INumberOfPictureinFile; i++)
            {

                _TGXImage[i].Width = BitConverter.ToUInt16(array, actualPositionInByteArray + i * TGXImage.iImageHeaderSize + 0);
                _TGXImage[i].Height = BitConverter.ToUInt16(array, actualPositionInByteArray + i * TGXImage.iImageHeaderSize + 2);
                _TGXImage[i].OffsetX = BitConverter.ToUInt16(array, actualPositionInByteArray + i * TGXImage.iImageHeaderSize + 4);
                _TGXImage[i].OffsetY = BitConverter.ToUInt16(array, actualPositionInByteArray + i * TGXImage.iImageHeaderSize + 6);
                _TGXImage[i].ImagePart = array[actualPositionInByteArray + i * TGXImage.iImageHeaderSize + 8];
                _TGXImage[i].SubParts = array[actualPositionInByteArray + i * TGXImage.iImageHeaderSize + 9];
                _TGXImage[i].TileOffset = BitConverter.ToUInt16(array, actualPositionInByteArray + i * TGXImage.iImageHeaderSize + 10);
                _TGXImage[i].Direction = array[actualPositionInByteArray + i * TGXImage.iImageHeaderSize + 12];
                _TGXImage[i].HorizontalOffsetOfImage = array[actualPositionInByteArray + i * TGXImage.iImageHeaderSize + 13];
                _TGXImage[i].BuildingWidth = array[actualPositionInByteArray + i * TGXImage.iImageHeaderSize + 14];
                _TGXImage[i].AnimatedColor = array[actualPositionInByteArray + i * TGXImage.iImageHeaderSize + 15];
            }
            actualPositionInByteArray += (int)this.fileHeader.INumberOfPictureinFile * TGXImage.iImageHeaderSize;

            foreach (var image in _TGXImage)
            {
                image.ImgFileAsBytearray = new byte[(int)image.SizeinByteArray];
                Buffer.BlockCopy(array, actualPositionInByteArray + (int)image.OffsetinByteArray, image.ImgFileAsBytearray, 0, (int)image.SizeinByteArray);
            }
        }

        /// <summary>
        /// Creates IMGS from TGX and Tile(the IMG consist out of many smaller IMGS)
        /// </summary>
        /// <param name="array">The GM1 File as byte Array</param>
        private void CreateTileImage(byte[] array)
        {
            CreateOffsetAndSizeInByteArrayList(array);
            CreateImgHeader(array);

            int offsetX = 0, offsetY = 0;
            int midx = 0;
            int width = 0;
            int counter = -1;
            int itemsPerRow = 1;
            int actualItemsPerRow = 0;
            int safeoffset = 0;
            bool halfReached = false;
            int partsBefore = 0;
            for (int i = 0; i < _TGXImage.Count; i++)
            {
                if (_TGXImage[i].ImagePart == 0)
                {
                    width = Utility.GetDiamondWidth(_TGXImage[i].SubParts);


                    partsBefore += _TGXImage[i].SubParts;

                    tilesImage.Add(new TilesImage(width * 30 + ((width - 1) * 2), width * 16 + _TGXImage[partsBefore - 1].TileOffset + TilesImage.Puffer));//gap 2 pixels
                    counter++;
                    itemsPerRow = 1;
                    actualItemsPerRow = 0;
                    midx = (width / 2) * 30 + (width - 1) - ((width%2==0)? 15 : 0);
                    offsetY = tilesImage[counter].Height - 16;
                    offsetX = midx;
                    safeoffset = offsetX;
                    halfReached = false;
                }


                if (_TGXImage[i].ImgFileAsBytearray.Length > 512 && (_TGXImage[i].Width > 30 || _TGXImage[i].Height > 16))
                {

                    int right = 0;
                    if (_TGXImage[i].Direction == 3)
                    {
                        right = 14;
                    }
                    tilesImage[counter].AddImgTileOnTopToImg(_TGXImage[i].ImgFileAsBytearray, offsetX + right, offsetY - _TGXImage[i].TileOffset);
                    if (tilesImage[counter].MinusHeight > offsetY - _TGXImage[i].TileOffset)
                    {
                        tilesImage[counter].MinusHeight = offsetY - _TGXImage[i].TileOffset;
                    }

                }
                tilesImage[counter].AddDiamondToImg(_TGXImage[i].ImgFileAsBytearray, offsetX, offsetY);




                offsetX += 32;
                actualItemsPerRow++;
                if (actualItemsPerRow == itemsPerRow)
                {
                    offsetX = safeoffset;

                    offsetY -= 8;
                    if (itemsPerRow < width)
                    {
                        if (halfReached)
                        {
                            itemsPerRow--;
                            offsetX += 16;
                        }
                        else
                        {
                            itemsPerRow++;
                            offsetX -= 16;
                        }
                    }
                    else
                    {
                        itemsPerRow--;
                        offsetX += 16;
                        halfReached = true;
                    }

                    safeoffset = offsetX;
                    actualItemsPerRow = 0;
                }





            }

            foreach (var image in tilesImage)
            {
                image.CreateImagefromList();
            }
        }

        /// <summary>
        ///  Create the offset and size Lists
        /// </summary>
        /// <param name="array">The GM1 File as byte Array</param>
        private void CreateOffsetAndSizeInByteArrayList(byte[] array)
        {
            for (int i = 0; i < this.fileHeader.INumberOfPictureinFile; i++)
            {
                var image = new TGXImage();
                image.OffsetinByteArray = BitConverter.ToUInt32(array, actualPositionInByteArray + i * 4);
                _TGXImage.Add(image);
            }
            actualPositionInByteArray += (int)this.fileHeader.INumberOfPictureinFile * 4;


            for (int i = 0; i < this.fileHeader.INumberOfPictureinFile; i++)
            {

                _TGXImage[i].SizeinByteArray = BitConverter.ToUInt32(array, actualPositionInByteArray + i * 4);

            }
            actualPositionInByteArray += (int)this.fileHeader.INumberOfPictureinFile * 4;


        }

        public void SetNewTileList()
        {
            _TGXImage = newTileList;
            fileHeader.INumberOfPictureinFile = (uint)_TGXImage.Count;
        }
        #endregion




        #region GetterSetter

        internal GM1FileHeader FileHeader { get => fileHeader; }
        internal Palette Palette { get => palette; }
        internal List<TGXImage> ImagesTGX { get => _TGXImage; }
        public byte[] FileArray { get => fileArray; set => fileArray = value; }
        internal List<TilesImage> TilesImages { get => tilesImage; set => tilesImage = value; }

        #endregion

    }
}
