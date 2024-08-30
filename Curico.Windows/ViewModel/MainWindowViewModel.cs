using Curico.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Curico.Windows.ViewModel;
public class MainWindowViewModel : ViewModelBase
{


    public ObservableCollection<ImageViewModel> Images { get; } = [];

    public MainWindowViewModel()
    {
        Images.Add(new ImageViewModel { ImagePath = @"C:\Users\Mia\Desktop\32x32.png" });
        TestCommand = new RelayCommand(Test);
        Test();
    }

    public ICommand TestCommand { get; }

    private Cursor _cursor = Cursors.Arrow;
    public Cursor Cursor
    {
        get => _cursor;
        set => SetProperty(ref _cursor, value);
    }

    public void Test()
    {
        var folder = @"C:\Users\Mia\Desktop\aero_arrow-0";

        var icon = new Icon();
        icon.Format = IconFormat.CUR;
        foreach (var file in Directory.GetFiles(folder, "*.png"))
        {
            using var image = Image.Load<Rgba32>(file);
            var newImage = new Image<Rgba32>(image.Width, image.Height);
            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    var pixel = image[x, y];
                    if (pixel.A == 0 || false)
                    {
                        newImage[x, y] = Color.Transparent;// new Rgba32(0, 0, 0, 0);
                    }
                    else
                    {
                        newImage[x, y] = new Rgba32(pixel.R, pixel.G, pixel.B, pixel.A);
                    }
                }
            }
            newImage.SaveAsPng($@"C:\Users\Mia\Desktop\aero_arrow-0\test\{image.Width}x{image.Height}.png");
            icon.Images.Add(new IconImage(newImage, new Point(0, 0)));
            icon.Images.Sort((a, b) => b.Image.Width.CompareTo(a.Image.Width));
        }
        var saveFile = @"C:\Users\Mia\Desktop\aero_arrow-0\aero_arrow_test.cur";
        icon.Save(saveFile);

        //var realCursor = @"C:\Users\Mia\Desktop\aero_arrow-0\aero_arrow.cur";
        
        var cur = new Cursor(saveFile, true);
        Cursor = cur;
    }

    private static byte[] CreateMask(BitmapSource image)
    {
        var pixels = new uint[image.PixelWidth * image.PixelHeight];
        var dim = image.PixelWidth;

        int maskStride = (((dim + 7) / 8) + 3) & ~3;
        var mask = new byte[maskStride * dim];

        image.CopyPixels(pixels, dim * 4, 0);

        for (int y = 0; y < dim; y++)
        {
            for (int x = 0; x < dim; x += 8)
            {
                byte pix = 0;
                for (int i = 0; i < 8; i++)
                {
                    var p = pixels[i + x + y * dim] >> 24;
                    if (p > 127)
                    {
                        pix |= (byte)(1 << i);
                    }
                }
                mask[x / 8 + y * maskStride] = pix;
            }
        }

        return mask;
    }
}

public class ImageViewModel
{
    public string? ImagePath { get; set; }
}
