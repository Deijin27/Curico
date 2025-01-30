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

        var icon = new Icon();
        
        string outputFormat = argCol.GetRequiredParameter(0);
        string inputPath = argCol.GetRequiredParameter(1);
        string? outputPath = argCol.GetOption("--output");
        string? hotspotString = argCol.GetOption("--hotspots");

        string ext;
        if (outputFormat == "cur")
        {
            icon.Format = IconFormat.CUR;
            ext = ".cur";
        }
        else if (outputFormat == "ico")
        {
            icon.Format = IconFormat.ICO;
            ext = ".ico";
        }
        else
        {
            throw new Exception($"Unknown output format '{outputFormat}'. Should be 'cur' or 'ico'.");
        }

        outputPath ??= "output" + ext;

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
            // File input: generate different sizes
            var image = LoadImage(inputPath);
            images = GenerateImageSizes(image, [16, 24, 32, 48, 64, 96, 128, 256]);
        }
        else
        {
            throw new FileNotFoundException(inputPath);
        }
        
        if (icon.Format == IconFormat.CUR && !string.IsNullOrEmpty(hotspotString))
        {
            foreach (var kvp in LoadHotspots(hotspotString))
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
              curico ico <inputPath> [--output=<path>]
              curico cur <inputPath> [--output=<path>] [--hotspots=<size:x,y;size:x,y;...>]

            Parameters:
              format       Output format. cur or ico
              inputPath    Directory containing the images.

            Options:
              --output     (Optional) output file path. Default is 'output.ico'.
              --hotspots   (Optional) Only for cursors. Hotspots per image size. Omitted ones will be 0,0. If this option isn't passed, all will be 0,0.
                               Format: size1:x1,y1;size2:x2,y2;... e.g., 128:10,10;96:6,6
            
            Examples:
              curico ico my_image.png --output=output.ico
              curico cur "/path/to/folder" --hotspots=128:10,10;96:6,6;64:4,4
            """);
    }
}
