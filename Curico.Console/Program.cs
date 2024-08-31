using Curico.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Reflection;

namespace Curico.CLI;

internal class Program
{
    static void Main(string[] args)
    {
        var argDictionary = new ArgumentCollection(args);

        if (argDictionary.HasArg("--help") || argDictionary.HasArg("-h") || argDictionary.Count == 0)
        {
            DisplayHelp();
            return;
        }

        var icon = new Icon();
        
        string outputFormat = argDictionary.GetRequired("--format");
        string imageDir = argDictionary.GetRequired("--input");
        string? outputPath = argDictionary.GetOptional("--output");
        string? hotspotString = argDictionary.GetOptional("--hotspots");

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

        var images = new Dictionary<int, IconImage>();
        foreach (var file in Directory.GetFiles(imageDir, "*.png"))
        {
            try
            {
                var image = Image.Load<Rgba32>(file);
                images[image.Width] = new IconImage(image);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading image {file}", ex);
            }
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

    private static void DisplayHelp()
    {
        var version = Assembly.GetEntryAssembly()!.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
        Console.WriteLine($"""
            Curico v{version}"

            A command-line tool to convert pngs to windows Icon or Cursor files.

            Usage:
              curico --format=<ico/cur> --input=<path> [--hotspots=<size:x,y;size:x,y;...>] [--output=<path>]
            
            Options:
              --format     Specify the output format. cur or ico
              --input      Specify the directory containing the images.
              --hotspots   (Optional) Specify hotspots for cursors per image size. Unspecified ones will be 0,0
                               Format: size1:x1,y1;size2:x2,y2;... e.g., 128:10,10;96:6,6
              --output     (Optional) Specify the output file path. Default is 'output.ico'.
              --help       Display this help text.
            
            Examples:
              curico --format=ico --input=/path/to/images --output=/path/to/output.ico
              curico --format=cur --input=/path/to/images --hotspots=128:10,10;96:6,6;64:4,4
            """);
    }
}
