using Curico.Core;
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
}

public class ImageViewModel
{
    public string? ImagePath { get; set; }
}
