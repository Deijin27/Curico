using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.PixelFormats;
using System.Runtime.InteropServices;

namespace Curico.Core;

public class Icon
{
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

    public IconFormat Format { get; set; }

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

    public void Save(string file)
    {
        using var bw = new BinaryWriter(File.Create(file));
        WriteTo(bw);
    }

    public void WriteTo(BinaryWriter bw)
    {
        var startOffset = bw.BaseStream.Position;

        var header = new Header();
        header.Format = (ushort)Format;
        header.ImageCount = (ushort)Images.Count;

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
        uint ColorsUsed;

        /// <summary>
        /// Minimum number of important color
        /// </summary>
        uint ColorsImportant;

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

    public List<IconImage> Images { get; } = [];
}
