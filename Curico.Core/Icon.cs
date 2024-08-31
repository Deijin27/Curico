using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.PixelFormats;

namespace Curico.Core;

public class Icon
{
    public IconFormat Format { get; set; }
    public List<IconImage> Images { get; } = [];

    public Icon()
    {
    }

    public Icon(BinaryReader br)
    {
        var startOffset = br.BaseStream.Position;
        var header = new Header(br);

        Format = (IconFormat)header.Format;

        var imageInfos = new IconImageInfo[header.ImageCount];
        for (int i = 0; i < imageInfos.Length; i++)
        {
            imageInfos[i] = new IconImageInfo(br);
        }

        for (int i = 0; i < imageInfos.Length; i++)
        {
            var info = imageInfos[i];
            br.BaseStream.Position = startOffset + info.OffsetToData;
            var data = br.ReadBytes(info.SizeImgAndMaskData);
        }

    }

    private struct Header
    {
        public const int Length = 6;

        public ushort Reserved;
        public ushort Format;
        public ushort ImageCount;

        public Header(BinaryReader br)
        {
            Reserved = br.ReadUInt16();
            Format = br.ReadUInt16();
            ImageCount = br.ReadUInt16();
        }

        public void WriteTo(BinaryWriter bw)
        {
            bw.Write(Reserved);
            bw.Write(Format);
            bw.Write(ImageCount);
        }
    }

    public void Save(string file)
    {
        using var bw = new BinaryWriter(File.Create(file));
        WriteTo(bw);
    }

    private void ValidateAndStandardiseImages()
    {
        Images.Sort((a, b) => b.Image.Width.CompareTo(a.Image.Width));

        // Validate image sizes
        var validSizes = new[] { 128, 96, 64, 48, 32 };
        if (!(Images.Select(x => x.Image.Width).SequenceEqual(validSizes) && Images.Select(x => x.Image.Height).SequenceEqual(validSizes)))
        {
            throw new Exception("You must provide one image of each size: 128x128, 96x96, 64x64, 48x48, 32x32");
        }

        if (Format == IconFormat.CUR)
        {
            // Validate cursor hotspots
            foreach (var image in Images)
            {
                if (image.Hotspot.X < 0 || image.Hotspot.Y < 0 || image.Hotspot.X >= image.Image.Width || image.Hotspot.Y >= image.Image.Height)
                {
                    throw new Exception($"Cursor hotspot was not in bounds for the {image.Image.Width}x{image.Image.Height} image");
                }
            }
        }

        // Make all the full transparency be black
        // Not necessary, but it's neat
        foreach (var image in Images)
        {
            NormalizeTransparency(image.Image);
        }
    }

    private static void NormalizeTransparency(Image<Rgba32> image)
    {
        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                foreach (ref var pixel in accessor.GetRowSpan(y))
                {
                    if (pixel.A == 0)
                    {
                        pixel.R = 0;
                        pixel.G = 0;
                        pixel.B = 0;
                    }
                }
            }
        });
        //for (int x = 0; x < image.Width; x++)
        //{
        //    for (int y = 0; y < image.Height; y++)
        //    {
        //        var pixel = image[x, y];
        //        if (pixel.A == 0)
        //        {
        //            image[x, y] = Color.Transparent;
        //        }
        //    }
        //}
    }

    public void WriteTo(BinaryWriter bw)
    {
        ValidateAndStandardiseImages();
        var startOffset = bw.BaseStream.Position;

        var header = new Header
        {
            Format = (ushort)Format,
            ImageCount = (ushort)Images.Count
        };

        header.WriteTo(bw);

        var imageInfosStart = bw.BaseStream.Position;

        bw.BaseStream.Seek(IconImageInfo.Length * Images.Count, SeekOrigin.Current);

        var imageInfos = new IconImageInfo[Images.Count];

        for (int i = 0; i < Images.Count; i++)
        {
            var image = Images[i];
            var imageInfo = new IconImageInfo
            {
                Width = (byte)image.Image.Width,
                Height = (byte)image.Image.Height,
            };
            if (Format == IconFormat.CUR)
            {
                imageInfo.Var1 = (ushort)image.Hotspot.X;
                imageInfo.Var2 = (ushort)image.Hotspot.Y;
            }
            else
            {
                imageInfo.Var1 = 1; // color planes
                imageInfo.Var2 = 0; // bits per pixel
            }

            var bitmapStartOffset = bw.BaseStream.Position;
            imageInfo.OffsetToData = (int)(bitmapStartOffset - startOffset);

            byte[]? mask = null;
            if (Format == IconFormat.CUR)
            {
                mask = CreateMask(image.Image);
            }

            // save image as bitmap
            using (var ms = new MemoryStream())
            {
                using var memBr = new BinaryReader(ms);
                using var memBw = new BinaryWriter(ms);

                image.Image.SaveAsBmp(memBw.BaseStream, new BmpEncoder() { BitsPerPixel = BmpBitsPerPixel.Pixel32 });
                const int bmpHeaderLen = 14;

                // The subheader, which starts immediately after the main header
                // needs to be modified slightly
                ms.Seek(bmpHeaderLen, SeekOrigin.Begin);
                var subHeader = new BitmapSubHeader(memBr);
                subHeader.Height *= 2; // needs to be doubled for some reason. Maybe because of the mask idk
                if (mask != null)
                {
                    // The size value needs to include the length of the mask
                    subHeader.SizeOfBitmap += (uint)mask.Length;
                }
                subHeader.HorzResolution = 0;
                subHeader.VertResolution = 0;
                ms.Seek(bmpHeaderLen, SeekOrigin.Begin);
                subHeader.WriteTo(memBw);

                // strip header and write to main stream
                var bmpBytes = ms.ToArray();
                bw.Write(bmpBytes, bmpHeaderLen, bmpBytes.Length - bmpHeaderLen);
            }

            if (mask != null)
            {
                bw.Write(mask);
            }

            var bitmapEndOffset = bw.BaseStream.Position;
            imageInfo.SizeImgAndMaskData = (int)(bitmapEndOffset - bitmapStartOffset);

            imageInfos[i] = imageInfo;
        }

        var end = bw.BaseStream.Position;
        bw.BaseStream.Position = imageInfosStart;
        foreach (var info in imageInfos)
        {
            info.WriteTo(bw);
        }
        bw.BaseStream.Position = end;
    }

    public struct BitmapSubHeader
    {
        /// <summary>
        /// Size of this header in bytes
        /// </summary>
        public uint Size;
        /// <summary>
        /// Image width in pixels
        /// </summary>
        public int Width;
        /// <summary>
        /// Image height in pixels
        /// </summary>
        public int Height;
        /// <summary>
        /// Number of color planes
        /// </summary>
        public ushort Planes;
        /// <summary>
        /// Number of bits per pixel
        /// </summary>
        public ushort BitsPerPixel;

        // Fields added for Windows 3.x follow this

        /// <summary>
        /// Compression methods used
        /// </summary>
        public uint Compression;

        /// <summary>
        /// Size of bitmap in bytes
        /// </summary>
        public uint SizeOfBitmap;

        /// <summary>
        /// Horizontal resolution in pixels per meter
        /// </summary>
        public int HorzResolution;

        /// <summary>
        /// Vertical resolution in pixels per meter
        /// </summary>
        public int VertResolution;

        /// <summary>
        /// Number of colors in the image
        /// </summary>
        public uint ColorsUsed;

        /// <summary>
        /// Minimum number of important color
        /// </summary>
        public uint ColorsImportant;

        public BitmapSubHeader(BinaryReader br)
        {
            Size = br.ReadUInt32();
            Width = br.ReadInt32();
            Height = br.ReadInt32();
            Planes = br.ReadUInt16();
            BitsPerPixel = br.ReadUInt16();
            Compression = br.ReadUInt32();
            SizeOfBitmap = br.ReadUInt32();
            HorzResolution = br.ReadInt32();
            VertResolution = br.ReadInt32();
            ColorsUsed = br.ReadUInt32();
            ColorsImportant = br.ReadUInt32();
        }

        public void WriteTo(BinaryWriter bw)
        {
            bw.Write(Size);
            bw.Write(Width);
            bw.Write(Height);
            bw.Write(Planes);
            bw.Write(BitsPerPixel);
            bw.Write(Compression);
            bw.Write(SizeOfBitmap);
            bw.Write(HorzResolution);
            bw.Write(VertResolution);
            bw.Write(ColorsUsed);
            bw.Write(ColorsImportant);
        }
    }

    public static byte[] CreateMask(Image<Rgba32> image)
    {
        if (image.Width != image.Height)
        {
            throw new Exception("Image must be square");
        }
        var maskRowSize = image.Width / 8;
        while (maskRowSize % 4 != 0)
        {
            maskRowSize++;
        }

        byte[] mask = new byte[image.Height * maskRowSize];
        image.ProcessPixelRows(accessor =>
        {
            for (int rowIndex = 0; rowIndex < accessor.Height; rowIndex++)
            {
                var trueRowIndex = accessor.Height - rowIndex - 1;
                var row = accessor.GetRowSpan(rowIndex);
                for (int colGroupOffset = 0; colGroupOffset < accessor.Width; colGroupOffset += 8)
                {
                    int maskByte = 0;
                    for (int colSubOffset = 0; colSubOffset < 8; colSubOffset++)
                    {
                        var trueColSubOffset = 8 - colSubOffset - 1;
                        ref var pixel = ref row[colGroupOffset + trueColSubOffset];
                        if (pixel.A == 0)
                        {
                            maskByte |= 1 << colSubOffset;
                        }
                    }
                    mask[trueRowIndex * maskRowSize + colGroupOffset / 8] = (byte)maskByte;
                }
            }
        });
        return mask;
    }
}
