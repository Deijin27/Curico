namespace Curico.CLI;

internal class ArgumentCollection
{
    public int Count => _argDictionary.Count + _parameterList.Count;
    public int ParameterCount => _parameterList.Count;

    private readonly Dictionary<string, string> _argDictionary = [];
    private readonly List<string> _parameterList = [];
    public ArgumentCollection(string[] args)
    {
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
            else
            {
                _parameterList.Add(arg);
            }
        }
    }

    public bool HasOption(string arg)
    {
        return _argDictionary.ContainsKey(arg);
    }

    public string? GetOption(string key)
    {
        if (_argDictionary.TryGetValue(key, out var value))
        {
            return value;
        }
        return null;
    }

    public string GetRequiredOption(string key)
    {
        var value = GetOption(key);
        if (value == null)
        {
            throw new Exception($"Missing required arg '{key}'");
        }
        return value;
    }

    public string? GetParameter(int index)
    {
        if (_parameterList.Count > index)
        {
            return _parameterList[index];
        }
        return null;
    }

    public string GetRequiredParameter(int index)
    {
        var value = GetParameter(index);
        if (value == null)
        {
            throw new Exception($"Missing required parameter {index}");
        }
        return value;
    }
}