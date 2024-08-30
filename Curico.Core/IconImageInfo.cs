namespace Curico.Core;

internal struct IconImageInfo
{
    public const int Length = 16;

    public byte Width;
    public byte Height;
    public byte Palette;
    public byte Reserved;
    public ushort Var1;
    public ushort Var2;
    public int SizeImgAndMaskData;
    public int OffsetToData;

    public IconImageInfo(BinaryReader br)
    {
        Width = br.ReadByte();
        Height = br.ReadByte();
        Palette = br.ReadByte();
        Reserved = br.ReadByte();
        Var1 = br.ReadUInt16();
        Var2 = br.ReadUInt16();
        SizeImgAndMaskData = br.ReadInt32();
        OffsetToData = br.ReadInt32();
    }

    public void WriteTo(BinaryWriter bw)
    {
        bw.Write(Width);
        bw.Write(Height);
        bw.Write(Palette);
        bw.Write(Reserved);
        bw.Write(Var1);
        bw.Write(Var2);
        bw.Write(SizeImgAndMaskData);
        bw.Write(OffsetToData);
    }
}
