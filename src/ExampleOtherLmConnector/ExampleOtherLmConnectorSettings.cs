using System.Text.Json;

namespace ExampleOtherLmConnector;

internal sealed class ExampleOtherLmConnectorSettings
{
    public string Handedness { get; set; } = "RH";
    public string Mode { get; set; } = "NORMAL";

    public static ExampleOtherLmConnectorSettings Load()
    {
        var path = GetPath();
        if (!File.Exists(path))
            return new ExampleOtherLmConnectorSettings();

        var json = File.ReadAllText(path);
        var settings = JsonSerializer.Deserialize<ExampleOtherLmConnectorSettings>(json)
            ?? new ExampleOtherLmConnectorSettings();
        settings.Normalize();
        return settings;
    }

    public void Save()
    {
        Normalize();
        var path = GetPath();
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(
            path,
            JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }

    public ExampleOtherLmConnectorSettings Copy()
        => (ExampleOtherLmConnectorSettings)MemberwiseClone();

    private void Normalize()
    {
        Handedness = string.Equals(Handedness, "LH", StringComparison.OrdinalIgnoreCase) ? "LH" : "RH";
        Mode = Mode?.Trim().ToUpperInvariant() switch
        {
            "PUTTING" => "PUTTING",
            "CHIPPING" => "CHIPPING",
            _ => "NORMAL"
        };
    }

    private static string GetPath()
        => Path.Combine(
            AppContext.BaseDirectory,
            "Settings",
            "Other",
            "example-other-connector.json");
}
