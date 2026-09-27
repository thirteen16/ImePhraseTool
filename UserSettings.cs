using System.Text.Json;

namespace ImePhraseTool;

public sealed record UserSettings
{
    public string OutputFolder { get; init; } = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
    public string TxtName { get; init; } = "IME_phrases.txt";
    public string DatName { get; init; } = "UserDefinedPhrase.dat";

    public float FontSize { get; init; } = 12F;

    public static string DefaultPath => Path.Combine(AppContext.BaseDirectory, "settings.json");

    public static UserSettings Load(string path, out string? warning)
    {
        warning = null;
        if (!File.Exists(path)) return new();
        try
        {
            var settings = JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(path))
                ?? throw new JsonException("����Ϊ�ա�");
            // Older or partially populated settings retain defaults for missing fields.
            return settings with
            {
                OutputFolder = settings.OutputFolder ?? new UserSettings().OutputFolder,
                TxtName = settings.TxtName ?? "IME_phrases.txt",
                DatName = settings.DatName ?? "UserDefinedPhrase.dat",
                FontSize = float.IsFinite(settings.FontSize) ? Math.Clamp(settings.FontSize, 8F, 24F) : 12F
            };
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            warning = "�޷���ȡ�ϴ����ã���ʹ��Ĭ�����á�";
            return new();
        }
    }

    public void Save(string path) => OutputFiles.Save(new[]
    {
        (path, JsonSerializer.SerializeToUtf8Bytes(this, new JsonSerializerOptions { WriteIndented = true }))
    });
}
