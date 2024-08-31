namespace Curico.CLI;

internal class ArgumentCollection
{
    public int Count => _argDictionary.Count;

    private readonly Dictionary<string, string> _argDictionary;
    public ArgumentCollection(string[] args)
    {
        _argDictionary = [];

        foreach (var arg in args)
        {
            if (arg.StartsWith("--") || arg.StartsWith('-'))
            {
                var splitIndex = arg.IndexOf('=');
                if (splitIndex > 0)
                {
                    var key = arg.Substring(0, splitIndex);
                    var value = arg.Substring(splitIndex + 1);
                    _argDictionary[key] = value;
                }
                else
                {
                    _argDictionary[arg] = string.Empty; // for flags like --help
                }
            }
        }
    }

    public bool HasArg(string arg)
    {
        return _argDictionary.ContainsKey(arg);
    }

    public string? GetOptional(string key)
    {
        if (_argDictionary.TryGetValue(key, out var value))
        {
            return value;
        }
        return null;
    }

    public string GetRequired(string key)
    {
        var value = GetOptional(key);
        if (value == null)
        {
            throw new Exception($"Missing required arg '{key}'");
        }
        return value;
    }
}