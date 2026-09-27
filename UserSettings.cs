using System.Text.Json;

namespace ImePhraseTool;

public sealed record UserSettings
{
    public string OutputFolder { get; init; } =
        Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

    public string TxtName { get; init; } = "IME_phrases.txt";
    public string DatName { get; init; } = "UserDefinedPhrase.dat";

    public float QuickPhraseFontSize { get; init; } = 13F;

    public static string DefaultPath =>
        Path.Combine(AppContext.BaseDirectory, "settings.json");

    public static UserSettings Load(string path, out string? warning)
    {
        warning = null;

        if (!File.Exists(path))
            return new();

        try
        {
            var settings =
                JsonSerializer.Deserialize<UserSettings>(
                    File.ReadAllText(path))
                ?? throw new JsonException("配置为空。");

            return settings with
            {
                OutputFolder =
                    settings.OutputFolder ??
                    new UserSettings().OutputFolder,

                TxtName =
                    settings.TxtName ??
                    "IME_phrases.txt",

                DatName =
                    settings.DatName ??
                    "UserDefinedPhrase.dat",

                QuickPhraseFontSize =
                    float.IsFinite(settings.QuickPhraseFontSize)
                        ? Math.Clamp(
                            settings.QuickPhraseFontSize,
                            8F,
                            24F)
                        : 13F
            };
        }
        catch (Exception ex)
            when (ex is IOException
                or UnauthorizedAccessException
                or JsonException)
        {
            warning =
                "无法读取上次配置，已使用默认设置。";

            return new();
        }
    }

    public void Save(string path) =>
        OutputFiles.Save(
            new[]
            {
                (
                    path,
                    JsonSerializer.SerializeToUtf8Bytes(
                        this,
                        new JsonSerializerOptions
                        {
                            WriteIndented = true
                        })
                )
            });
}