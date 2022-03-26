using Gm1KonverterCrossPlatform.Files;
using Gm1KonverterCrossPlatform.HelperClasses;
using Gm1KonverterCrossPlatform.Views;
using HelperClasses.Gm1Converter;
using System;
using System.Collections.Generic;

namespace Files.Gm1Converter
{
    class DecodedFile : IDisposable
    {
        #region Variables

        private GM1FileHeader fileHeader;
        private Palette palette;
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
            if (Logger.Loggeractiv) Logger.Log($"DecodeGm1File: {name}");
            
            fileArray = array;
            if (this.fileHeader == null)
            {
                // header is located at start of the file
                byte[] headerByteArray = new byte[GM1FileHeader.ByteSize];
                Array.Copy(array, 0, headerByteArray, 0, GM1FileHeader.ByteSize);
                this.fileHeader = new GM1FileHeader(headerByteArray);

                this.fileHeader.Name = name;

                if (fileHeader.IDataType == (uint)GM1FileHeader.DataType.Animations)
                {
                    // palette is located immediately after the header
                    byte[] paletteByteArray = new byte[Palette.ByteSize];
                    Array.Copy(array, GM1FileHeader.ByteSize, paletteByteArray, 0, Palette.ByteSize);
                    this.palette = new Palette(paletteByteArray);
                }
                if (Logger.Loggeractiv) Logger.Log($"DataType {(GM1FileHeader.DataType)fileHeader.IDataType}");
            }

            actualPositionInByteArray = (GM1FileHeader.ByteSize + Palette.ByteSize);
            this._TGXImage = new List<TGXImage>();
            this.tilesImage = new List<TilesImage>();
            //Supported Types
            try
            {
                switch ((GM1FileHeader.DataType)fileHeader.IDataType)
                {
                    case GM1FileHeader.DataType.Animations:
                    case GM1FileHeader.DataType.Interface:
                    case GM1FileHeader.DataType.TGXConstSize:
                    case GM1FileHeader.DataType.Font:
                        CreateImages(array);
                        return true;
                    case GM1FileHeader.DataType.NOCompression:
                    case GM1FileHeader.DataType.NOCompression1:
                        CreateNoCompressionImages(array, ((GM1FileHeader.DataType)fileHeader.IDataType == GM1FileHeader.DataType.NOCompression1) ? 0 : 7);
                        return true;
                    case GM1FileHeader.DataType.TilesObject:
                        CreateTileImage(array);
                        return true;
                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                if (Logger.Loggeractiv) Logger.Log("Exception:\n" + e.Message);
                MessageBoxWindow messageBox = new MessageBoxWindow(MessageBoxWindow.MessageTyp.Info, "Something went wrong: pls add a issue on the Github Page\n\nError:\n" + e.Message);
                messageBox.Show();
                return false;
            }

            return false;
        }

        /// <summary>
        /// Create the new GM1 File. File content:
        /// <para>1. FileHeader</para>
        /// <para>2. Palette</para>
        /// <para>3. OffsetList</para>
        /// <para>4. SizeList</para>
        /// <para>5. ImgHeaderList</para>
        /// <para>6. ImgsasByteList</para>
        /// </summary>
        public byte[] GetNewGM1Bytes()
        {
            if (Logger.Loggeractiv) Logger.Log("GetNewGM1Bytes");
            
            List<byte> newFile = new List<byte>();
            var headerBytes = fileHeader.GetBytes();
            newFile.AddRange(headerBytes);
            
            if (palette == null)
            {
                newFile.AddRange(new byte[Palette.ByteSize]);
            }
            else
            {
                newFile.AddRange(palette.GetBytes());
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
                newFile.AddRange(_TGXImage[i].Header.GetBytes());
            }

            for (int i = 0; i < fileHeader.INumberOfPictureinFile; i++)
            {
                newFile.AddRange(_TGXImage[i].ImgFileAsBytearray);
            }

            return newFile.ToArray();
        }

        public void Dispose()
        {
            if (tilesImage != null)
            {
                foreach (TilesImage image in tilesImage)
                {
                    image.Dispose();
                }
            }
        }

        /// <summary>
        /// Creates the IMG from the byte array, handles Animation, Interfaces, TGXConstSize, Font
        /// </summary>
        /// <param name="array">The GM1 File as byte Array</param>
        private void CreateImages(byte[] array)
        {
            if (Logger.Loggeractiv) Logger.Log("Create Images");
            CreateOffsetAndSizeInByteArrayList(array);
            CreateImgHeader(array);
            if (Logger.Loggeractiv) Logger.Log("CreateImageFromByteArray");

            for (uint i = 0; i < fileHeader.INumberOfPictureinFile; i++)
            {
                _TGXImage[(int)i].Bitmap = ImageConverter.GM1ByteArrayToImg(
                    _TGXImage[(int)i].ImgFileAsBytearray,
                    _TGXImage[(int)i].Header.Width,
                    _TGXImage[(int)i].Header.Height,
                    palette?.ColorTables[palette.ActualPalette]);
            }
        }

        private void CreateNoCompressionImages(byte[] array, int offset)
        {
            if (Logger.Loggeractiv) Logger.Log("CreateNoCompressionImages");
            CreateOffsetAndSizeInByteArrayList(array);
            CreateImgHeader(array);
            if (Logger.Loggeractiv) Logger.Log("CreateNoComppressionImageFromByteArray");
            for (uint i = 0; i < fileHeader.INumberOfPictureinFile; i++)
            {
                _TGXImage[(int)i].CreateNoComppressionImageFromByteArray(palette, offset);
            }
        }

        private List<TGXImage> newTileList = new List<TGXImage>();
        /// <summary>
        /// Convert Img To Tiled Diamond Images
        /// </summary>
        /// <param name="list"></param>
        internal void ConvertImgToTiles(List<ushort> list, ushort width, ushort height)
        {
            if (Logger.Loggeractiv) Logger.Log("ConvertImgToTiles");
            newTileList.AddRange(Utility.ConvertImgToTiles(list, width, height, ImagesTGX));
        }

        /// <summary>
        /// Creates the IMG Header
        /// </summary>
        /// <param name="array">The GM1 File as byte Array</param>
        private void CreateImgHeader(byte[] array)
        {
            if (Logger.Loggeractiv) Logger.Log("CreateImgHeader");
            
            for (int i = 0; i < this.fileHeader.INumberOfPictureinFile; i++)
            {
                byte[] imageHeaderByteArray = new byte[TGXImageHeader.ByteSize];
                Array.Copy(array, actualPositionInByteArray + (i * TGXImageHeader.ByteSize), imageHeaderByteArray, 0, TGXImageHeader.ByteSize);
                _TGXImage[i].Header = new TGXImageHeader(imageHeaderByteArray);
            }

            actualPositionInByteArray += (int)this.fileHeader.INumberOfPictureinFile * TGXImageHeader.ByteSize;

            foreach (var image in _TGXImage)
            {
                image.ImgFileAsBytearray = new byte[(int)image.SizeinByteArray];
                Buffer.BlockCopy(array, actualPositionInByteArray + (int)image.OffsetinByteArray, image.ImgFileAsBytearray, 0, (int)image.SizeinByteArray);
            }
        }

        /// <summary>
        /// Creates IMGS from TGX and Tile(the IMG consist out of many smaller IMGS)
        /// </summary>
        /// <param name="byteArray">The GM1 File as byte Array</param>
        private void CreateTileImage(byte[] byteArray)
        {
            if (Logger.Loggeractiv) Logger.Log("CreateTileImage");
            Dispose();
            CreateOffsetAndSizeInByteArrayList(byteArray);
            CreateImgHeader(byteArray);

            int offsetX = 0, offsetY = 0;
            int midx = 0;
            int width = 0;
            int counter = -1;
            int itemsPerRow = 1;
            int actualItemsPerRow = 0;
            int safeoffset = 0;
            bool halfReached = false;
            int partsBefore = 0;

            if (Logger.Loggeractiv) Logger.Log("CreateTileImage for Loop");
            for (int i = 0; i < _TGXImage.Count; i++)
            {
                if (_TGXImage[i].Header.ImagePart == 0)
                {
                    width = Utility.GetDiamondWidth(_TGXImage[i].Header.SubParts);

                    partsBefore += _TGXImage[i].Header.SubParts;

                    tilesImage.Add(new TilesImage(width * 30 + ((width - 1) * 2), width * 16 + _TGXImage[partsBefore - 1].Header.TileOffset + TilesImage.Puffer));//gap 2 pixels
                    counter++;
                    itemsPerRow = 1;
                    actualItemsPerRow = 0;
                    midx = (width / 2) * 30 + (width - 1) - ((width % 2 == 0) ? 15 : 0);
                    offsetY = tilesImage[counter].Height - 16;
                    offsetX = midx;
                    safeoffset = offsetX;
                    halfReached = false;
                }

                if (_TGXImage[i].ImgFileAsBytearray.Length > 512)
                {
                    int right = 0;
                    if (_TGXImage[i].Header.Direction == 3)
                    {
                        right = 14;
                    }
                    tilesImage[counter].AddImgTileOnTopToImg(_TGXImage[i].ImgFileAsBytearray, offsetX + right, offsetY - _TGXImage[i].Header.TileOffset);
                    if (tilesImage[counter].MinusHeight > offsetY - _TGXImage[i].Header.TileOffset)
                    {
                        tilesImage[counter].MinusHeight = offsetY - _TGXImage[i].Header.TileOffset;
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
        /// <param name="byteArray">The GM1 File as byte Array</param>
        private void CreateOffsetAndSizeInByteArrayList(byte[] byteArray)
        {
            if (Logger.Loggeractiv) Logger.Log("CreateOffsetAndSizeInByteArrayList");
            for (int i = 0; i < this.fileHeader.INumberOfPictureinFile; i++)
            {
                var image = new TGXImage();
                image.OffsetinByteArray = BitConverter.ToUInt32(byteArray, actualPositionInByteArray + i * 4);
                _TGXImage.Add(image);
            }
            actualPositionInByteArray += (int)this.fileHeader.INumberOfPictureinFile * 4;

            for (int i = 0; i < this.fileHeader.INumberOfPictureinFile; i++)
            {
                _TGXImage[i].SizeinByteArray = BitConverter.ToUInt32(byteArray, actualPositionInByteArray + i * 4);
            }
            actualPositionInByteArray += (int)this.fileHeader.INumberOfPictureinFile * 4;
        }

        public void SetNewTileList()
        {
            if (Logger.Loggeractiv) Logger.Log("SetNewTileList");
            //Check for animatedColor(not known how to set it in Stronghold 1 is not used in Stronghold Crusader)
            if (_TGXImage.Count <= newTileList.Count)
            for (int i = 0; i < _TGXImage.Count; i++)
            {
                newTileList[i].Header.AnimatedColor = _TGXImage[i].Header.AnimatedColor;
            }

            _TGXImage = newTileList;
            fileHeader.INumberOfPictureinFile = (uint)_TGXImage.Count;
            newTileList = new List<TGXImage>();
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
