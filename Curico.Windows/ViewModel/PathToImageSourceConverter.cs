using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;

namespace Curico.Windows.ViewModel;

public class PathToImageSourceConverter
{
    public static ImageSource? TryConvert(string file)
    {
        if (!File.Exists(file))
        {
            return null;
        }
        using var fs = new FileStream(file, FileMode.Open, FileAccess.Read);
        if (fs.Length == 0)
        {
            return null;
        }
        try
        {
            return BitmapFrame.Create(fs, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        }
        catch
        {
            return null;
        }
    }

    public static ImageSource? TryConvert(Image<Rgba32> image)
    {
        var file = Path.GetTempFileName() + ".png";
        try
        {
            image.SaveAsPng(file);
            return TryConvert(file);
        }
        finally
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
    }
}