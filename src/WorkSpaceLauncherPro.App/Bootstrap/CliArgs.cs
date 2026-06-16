namespace WorkSpaceLauncherPro.App.Bootstrap;

/// <summary>Parses simple CLI flags. Supports --key and --key=value.</summary>
public sealed class CliArgs
{
    private readonly Dictionary<string, string?> _flags = new(StringComparer.OrdinalIgnoreCase);

    public static CliArgs Parse(string[] args)
    {
        var result = new CliArgs();
        foreach (var a in args)
        {
            if (!a.StartsWith("--")) continue;
            var trimmed = a[2..];
            var eq = trimmed.IndexOf('=');
            if (eq < 0)
                result._flags[trimmed] = null;
            else
                result._flags[trimmed[..eq]] = trimmed[(eq + 1)..];
        }
        return result;
    }

    public bool Has(string key) => _flags.ContainsKey(key);
    public string? Get(string key) => _flags.GetValueOrDefault(key);
}
