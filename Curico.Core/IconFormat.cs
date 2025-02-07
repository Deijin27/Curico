namespace Curico.Core;

public enum IconFormat : ushort
{
    ICO = 1,
    CUR = 2,
}

public static class IconFormatExtensions
{
    public static string GetExtension(this IconFormat format)
    {
        return format switch
        {
            IconFormat.ICO => ".ico",
            IconFormat.CUR => ".cur",
            _ => throw new Exception($"Unknown format '{format}'")
         };
    }
}