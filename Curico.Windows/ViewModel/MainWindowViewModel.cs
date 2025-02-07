using Curico.Core;
using Microsoft.Win32;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;

namespace Curico.Windows.ViewModel;
public class MainWindowViewModel : ViewModelBase
{
    public ObservableCollection<ImageViewModel> Images { get; } = [];

    public IconFormat Format { get; }

    public MainWindowViewModel()
    {
        foreach (var image in Directory.GetFiles(@"C:\Users\Mia\Desktop\large\output", "*.png"))
        {
            Images.Add(new(image));
        }
        GenerateComamnd = new RelayCommand(Generate);
    }

    private Cursor _cursor = Cursors.Hand;
    public Cursor Cursor
    {
        get => _cursor;
        set => SetProperty(ref _cursor, value);
    }

    public void Test()
    {
        var folder = @"C:\Users\Mia\Desktop\aero_arrow-0";

        var icon = new Icon() { Format = IconFormat.CUR };
        foreach (var file in Directory.GetFiles(folder, "*.png"))
        {
            icon.Images.Add(new IconImage(Image.Load<Rgba32>(file), new Point(0, 0)));
        }
        var saveFile = @"C:\Users\Mia\Desktop\aero_arrow-0\aero_arrow_test.cur";
        icon.Save(saveFile);

        foreach (var image in icon.Images)
        {
            // have to specify the format else it will use the format the png we loaded was, aka grayscale with transparency.
            image.Image.SaveAsPng(
                $@"C:\Users\Mia\Desktop\aero_arrow-0\test\{image.Image.Width}x{image.Image.Height}.png",
               new PngEncoder() { ColorType = PngColorType.RgbWithAlpha });
        }
        //var realCursor = @"C:\Users\Mia\Desktop\aero_arrow-0\aero_arrow.cur";
        
        var cur = new Cursor(saveFile, true);
        Cursor = cur;
    }

    public ICommand GenerateComamnd { get; }

    private void Generate()
    {
        var filePicker = new SaveFileDialog();
        filePicker.DefaultExt = Format.GetExtension();

        if (filePicker.ShowDialog() != true)
        {
            return;
        }

        var path = filePicker.FileName;
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        var icon = new Icon { Format = Format };
        foreach (var image in Images)
        {
            icon.Images.Add(new IconImage(image.ImageShp, new Point(image.HotspotX, image.HotspotY)));
        }
        icon.Save(path);
    }
}

public class ImageViewModel : ViewModelBase
{
    private int _hotspotX = 10;
    private int _hotspotY = 10;
    private Image<Rgba32> _image;

    public ImageViewModel(string file)
    {
        _image = Image.Load<Rgba32>(file);
        Size = _image.Width;
        UpdateImage();
        

    }

    

    public int Size { get; }
    public int HotspotX
    {
        get => _hotspotX;
        set
        {
            if (SetProperty(ref _hotspotX, value))
            {
                UpdateImage();
            }
        }
    }
    public int HotspotY
    {
        get => _hotspotY;
        set
        {
            if (SetProperty(ref _hotspotY, value))
            {
                UpdateImage();
            }
        }
    }

    private void UpdateImage()
    {
        using var cloned = _image.Clone();
        cloned[HotspotX, HotspotY] = Color.Red;
        ImageSrc = PathToImageSourceConverter.TryConvert(cloned);
    }

    private System.Windows.Media.ImageSource? _imageSrc;
    public System.Windows.Media.ImageSource? ImageSrc
    {
        get => _imageSrc;
        set => SetProperty(ref _imageSrc, value);
    }
    public Image<Rgba32> ImageShp { get => _image; private set => _image = value; }
}
