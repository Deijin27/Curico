using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Diagnostics;

namespace Curico.Core;

[DebuggerDisplay("IconImage: {Image.Width}x{Image.Height}")]
public class IconImage
{
    public IconImage(Image<Rgba32> image, Point hotspot = default)
    {
        Image = image;
        Hotspot = hotspot;
    }

    public Image<Rgba32> Image { get; set; }
    public Point Hotspot { get; set; }
}
