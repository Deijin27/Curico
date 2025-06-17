using Curico.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Reflection;
using SixLabors.ImageSharp.Processing;

namespace Curico.CLI;

internal class Program
{
    static void Main(string[] args)
    {
        var argCol = new ArgumentCollection(args);

        if (argCol.HasOption("--help") || argCol.HasOption("-h") || argCol.Count == 0)
        {
            DisplayHelp();
            return;
        }

        string outputFormat = argCol.GetRequiredParameter(0);
        string inputPath = argCol.GetRequiredParameter(1);
        string? outputPath = argCol.GetOption("--output");
        string? hotspotString = argCol.GetOption("--hotspots");
        string? generateSizes = argCol.GetOption("--sizes");

        if (outputFormat == "info")
        {
            using var file = new BinaryReader(File.OpenRead(inputPath));
            Console.WriteLine(Icon.DumpMetadata(file));
            return;
        }
        if (outputFormat == "export")
        {
            Export(inputPath, outputPath);
            return;
        }

        var icon = new Icon();

        if (outputFormat == "cur")
        {
            icon.Format = IconFormat.CUR;
        }
        else if (outputFormat == "ico")
        {
            icon.Format = IconFormat.ICO;
        }
        else
        {
            throw new Exception($"Unknown output format '{outputFormat}'. Should be 'cur' or 'ico'.");
        }

        string ext = icon.Format.GetExtension();

        outputPath ??= "output" + ext;

        Dictionary<int, Point>? hotspots = null;
        if (icon.Format == IconFormat.CUR && !string.IsNullOrEmpty(hotspotString))
        {
            hotspots = LoadHotspots(hotspotString);
        }

        Dictionary<int, IconImage> images;
        if (Directory.Exists(inputPath))
        {
            // Folder input: load sizes from files in folder
            images = [];
            foreach (var file in Directory.GetFiles(inputPath, "*.png"))
            {
                var image = LoadImage(file);
                images[image.Width] = new IconImage(image);
            }
        }
        else if (File.Exists(inputPath))
        {
            int[] sizes;
            if (hotspots != null)
            {
                sizes = hotspots.Keys.ToArray();
            }
            else if (generateSizes != null)
            {
                sizes = LoadSizes(generateSizes).ToArray();
            }
            else
            {
                sizes = [256, 128, 96, 64, 48, 32, 16];
            }

            // File input: generate different sizes
            var image = LoadImage(inputPath);
            images = GenerateImageSizes(image, sizes);
        }
        else
        {
            throw new FileNotFoundException(inputPath);
        }
        
        if (hotspots != null)
        {
            foreach (var kvp in hotspots)
            {
                if (images.TryGetValue(kvp.Key, out var iconImage))
                {
                    iconImage.Hotspot = kvp.Value;
                }
            }
        }

        icon.Images.AddRange(images.Values);
        icon.Save(outputPath);
    }

    private static void Export(string inputPath, string? outputPath)
    {
        outputPath ??= "output";

        Icon icon;
        using (var stream = new BinaryReader(File.OpenRead(inputPath)))
        {
            icon = new Icon(stream);
        }

        Directory.CreateDirectory(outputPath);

        foreach (var image in icon.Images)
        {
            // maybe necessary: new PngEncoder() { ColorType = PngColorType.RgbWithAlpha }
            image.Image.SaveAsPng(Path.Combine(outputPath, image.Image.Width.ToString() + ".png"));
        }

        if (icon.Format == IconFormat.CUR)
        {
            var hotspots = string.Join(';', icon.Images.Select(x => $"{x.Image.Width}:{x.Hotspot.X},{x.Hotspot.Y}"));
            File.WriteAllText(Path.Combine(outputPath, "hotspots.txt"), hotspots);
        }
    }

    private static Image<Rgba32> LoadImage(string filePath)
    {
        Image<Rgba32> image;
        try
        {
            image = Image.Load<Rgba32>(filePath);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error loading image '{filePath}'", ex);
        }
        if (image.Width != image.Height)
        {
            throw new Exception($"Image '{filePath}' is invalid size {image.Width}x{image.Height}. Images must have same width as height.");
        }
        return image;
        
    }

    private static List<int> LoadSizes(string sizeString)
    {
        var sizes = new List<int>();
        var sizesStrings = sizeString.Split(';');
        foreach (var s in sizesStrings)
        {
            if (!int.TryParse(s, out var intSize))
            {
                throw new Exception($"Invalid size '{s}' in sizes '{sizeString}'");
            }

            if (intSize < 16)
            {
                throw new Exception($"Invalid size '{intSize}' in sizes '{sizeString}'");
            }

            sizes.Add(intSize);
        }
        return sizes;
    }

    private static Dictionary<int, Point> LoadHotspots(string coordString)
    {
        var hotspots = new Dictionary<int, Point>();
        var hotspotPairs = coordString.Split(';');

        foreach (var pair in hotspotPairs)
        {
            var parts = pair.Split(':');

            // Ensure the format is correct: size:x,y
            if (parts.Length != 2)
            {
                throw new Exception($"Invalid hotspot format: '{pair}'. Expected format: 'size:x,y'.");
            }

            // Parse the size part
            if (!int.TryParse(parts[0], out int size))
            {
                throw new Exception($"Invalid size format in hotspot: '{pair}'. Size should be an integer.");
            }

            var coordValues = parts[1].Split(',');

            // Ensure the coordinate part is correct: x,y
            if (coordValues.Length != 2)
            {
                throw new Exception($"Invalid coordinate format in hotspot: '{pair}'. Expected format: 'x,y'.");
            }

            // Parse the x and y coordinates
            if (!int.TryParse(coordValues[0], out int x) || !int.TryParse(coordValues[1], out int y))
            {
                throw new Exception($"Invalid coordinate values in pair: '{pair}'. x and y should be integers.");
            }

            // Add the parsed size and coordinates to the dictionary
            hotspots[size] = new Point(x, y);
        }

        return hotspots;
    }

    private static Dictionary<int, IconImage> GenerateImageSizes(Image<Rgba32> image, int[] sizes)
    {
        if (image.Width != image.Height)
        {
            throw new Exception("Provided image was not square");
        }

        var result = new Dictionary<int, IconImage>();
        foreach (var size in sizes)
        {
            if (image.Width == size)
            {
                result[size] = new IconImage(image);
            }
            else
            {
                result[size] = new IconImage(image.Clone(g => g.Resize(size, size)));
            }
        }
        return result;
    }

    private static void DisplayHelp()
    {
        var version = Assembly.GetEntryAssembly()!.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
        Console.WriteLine($"""
            Curico v{version}

            A command-line tool to convert pngs to windows Icon or Cursor files.

            Usage:
              curico ico <inputPath> [--output=<path>] [--sizes=<size;size;...]
              curico cur <inputPath> [--output=<path>] [--hotspots=<size:x,y;size:x,y;...>]

            Parameters:
              format       Output format. cur or ico
              inputPath    Directory containing the images.

            Options:
              --output     (Optional) output file path. Default is 'output.ico'.
              --hotspots   (Optional) Only for cursors. Hotspots per image size. Omitted ones will be 0,0. If this option isn't passed, all will be 0,0.
                               Format: size1:x1,y1;size2:x2,y2;... e.g., 128:10,10;96:6,6
              --sizes      (Optional) If you provide a single file, icon sizes to generate. Defaults to 256;128;96;64;48;32;16. Unnecessary if you provide hotspots list.
                                      
            
            Examples:
              curico ico my_image.png --output=output.ico
              curico cur "/path/to/folder" --hotspots=128:10,10;96:6,6;64:4,4


            Other functions:

            View information about an icon:
                curico info <iconPath/cursorPath>

            Convert an icon to pngs:
                curico export <iconPath/cursorPath> [--output=<outputFolder>]

            """);
    }
}
