using System.Text.Json;
using System.Text.Json.Serialization;

namespace ImePhraseTool;

public sealed record UserSettings
{
    // ============================================================
    // 程序内置默认值
    //
    // 当 settings.json 中对应的 DefaultXXX 不存在或无效时，
    // 使用这里的值。
    // ============================================================

    [JsonIgnore]
    public static string BuiltInOutputFolder =>
        Environment.GetFolderPath(
            Environment.SpecialFolder.DesktopDirectory);

    [JsonIgnore]
    public const string BuiltInTxtName =
        "IME_phrases.txt";

    [JsonIgnore]
    public const string BuiltInDatName =
        "UserDefinedPhrase.dat";

    [JsonIgnore]
    public const float BuiltInQuickPhraseFontSize =
        13F;


    // ============================================================
    // 当前设置（4 项）
    //
    // 这 4 项会保存到 settings.json。
    // ============================================================

    public string? OutputFolder { get; init; }

    public string? TxtName { get; init; }

    public string? DatName { get; init; }

    public float? QuickPhraseFontSize { get; init; }


    // ============================================================
    // 默认设置（4 项）
    //
    // 这 4 项也会保存到 settings.json。
    //
    // 点击“恢复默认”时，当前设置恢复到这里。
    // ============================================================

    public string? DefaultOutputFolder { get; init; }

    public string? DefaultTxtName { get; init; }

    public string? DefaultDatName { get; init; }

    public float? DefaultQuickPhraseFontSize { get; init; }


    // ============================================================
    // 配置文件位置
    //
    // 不保存到 JSON。
    // ============================================================

    [JsonIgnore]
    public static string DefaultPath =>
        Path.Combine(
            AppContext.BaseDirectory,
            "settings.json");


    // ============================================================
    // 有效默认值
    //
    // DefaultXXX 有值：
    //     使用 DefaultXXX
    //
    // DefaultXXX 不存在或无效：
    //     使用程序内置默认值
    //
    // 这些属性只供程序内部使用，不保存到 JSON。
    // ============================================================

    [JsonIgnore]
    public string EffectiveDefaultOutputFolder =>
        string.IsNullOrWhiteSpace(DefaultOutputFolder)
            ? BuiltInOutputFolder
            : DefaultOutputFolder;

    [JsonIgnore]
    public string EffectiveDefaultTxtName =>
        string.IsNullOrWhiteSpace(DefaultTxtName)
            ? BuiltInTxtName
            : DefaultTxtName;

    [JsonIgnore]
    public string EffectiveDefaultDatName =>
        string.IsNullOrWhiteSpace(DefaultDatName)
            ? BuiltInDatName
            : DefaultDatName;

    [JsonIgnore]
    public float EffectiveDefaultQuickPhraseFontSize =>
        ValidFontSize(DefaultQuickPhraseFontSize)
            ? DefaultQuickPhraseFontSize!.Value
            : BuiltInQuickPhraseFontSize;


    // ============================================================
    // 当前有效值
    //
    // 当前值存在：
    //     使用当前值
    //
    // 当前值不存在：
    //     使用对应的 DefaultXXX
    //
    // DefaultXXX 也不存在：
    //     使用程序内置默认值
    //
    // 这些属性只供程序内部使用，不保存到 JSON。
    // ============================================================

    [JsonIgnore]
    public string EffectiveOutputFolder =>
        string.IsNullOrWhiteSpace(OutputFolder)
            ? EffectiveDefaultOutputFolder
            : OutputFolder;

    [JsonIgnore]
    public string EffectiveTxtName =>
        string.IsNullOrWhiteSpace(TxtName)
            ? EffectiveDefaultTxtName
            : TxtName;

    [JsonIgnore]
    public string EffectiveDatName =>
        string.IsNullOrWhiteSpace(DatName)
            ? EffectiveDefaultDatName
            : DatName;

    [JsonIgnore]
    public float EffectiveQuickPhraseFontSize =>
        ValidFontSize(QuickPhraseFontSize)
            ? QuickPhraseFontSize!.Value
            : EffectiveDefaultQuickPhraseFontSize;


    // ============================================================
    // 检查字号是否合法
    //
    // 允许范围：8 ～ 24 pt
    // ============================================================

    private static bool ValidFontSize(float? value)
    {
        return value.HasValue
            && float.IsFinite(value.Value)
            && value.Value >= 8F
            && value.Value <= 24F;
    }


    // ============================================================
    // 加载 settings.json
    //
    // 优先级：
    //
    // 当前值
    //     ↓ 没有 / 无效
    // DefaultXXX
    //     ↓ 没有 / 无效
    // 程序内置默认值
    //
    // 加载完成后，在内存中补全完整 8 项。
    // ============================================================

    public static UserSettings Load(
        string path,
        out string? warning)
    {
        warning = null;

        UserSettings settings;

        // --------------------------------------------------------
        // settings.json 不存在
        // --------------------------------------------------------

        if (!File.Exists(path))
        {
            settings = new UserSettings();
        }
        else
        {
            try
            {
                settings =
                    JsonSerializer.Deserialize<UserSettings>(
                        File.ReadAllText(path))
                    ?? new UserSettings();
            }
            catch (Exception ex)
                when (ex is IOException
                    or UnauthorizedAccessException
                    or JsonException)
            {
                warning =
                    "无法读取上次配置，已使用默认设置。";

                settings =
                    new UserSettings();
            }
        }


        // ========================================================
        // 第一步：确定 4 个默认值
        // ========================================================

        string defaultOutputFolder =
            string.IsNullOrWhiteSpace(
                settings.DefaultOutputFolder)
                ? BuiltInOutputFolder
                : settings.DefaultOutputFolder;

        string defaultTxtName =
            string.IsNullOrWhiteSpace(
                settings.DefaultTxtName)
                ? BuiltInTxtName
                : settings.DefaultTxtName;

        string defaultDatName =
            string.IsNullOrWhiteSpace(
                settings.DefaultDatName)
                ? BuiltInDatName
                : settings.DefaultDatName;

        float defaultQuickPhraseFontSize =
            ValidFontSize(
                settings.DefaultQuickPhraseFontSize)
                ? settings.DefaultQuickPhraseFontSize!.Value
                : BuiltInQuickPhraseFontSize;


        // ========================================================
        // 第二步：确定 4 个当前值
        // ========================================================

        string outputFolder =
            string.IsNullOrWhiteSpace(
                settings.OutputFolder)
                ? defaultOutputFolder
                : settings.OutputFolder;

        string txtName =
            string.IsNullOrWhiteSpace(
                settings.TxtName)
                ? defaultTxtName
                : settings.TxtName;

        string datName =
            string.IsNullOrWhiteSpace(
                settings.DatName)
                ? defaultDatName
                : settings.DatName;

        float quickPhraseFontSize =
            ValidFontSize(
                settings.QuickPhraseFontSize)
                ? settings.QuickPhraseFontSize!.Value
                : defaultQuickPhraseFontSize;


        // ========================================================
        // 返回完整 8 项配置
        // ========================================================

        return settings with
        {
            // 当前值
            OutputFolder =
                outputFolder,

            TxtName =
                txtName,

            DatName =
                datName,

            QuickPhraseFontSize =
                quickPhraseFontSize,


            // 默认值
            DefaultOutputFolder =
                defaultOutputFolder,

            DefaultTxtName =
                defaultTxtName,

            DefaultDatName =
                defaultDatName,

            DefaultQuickPhraseFontSize =
                defaultQuickPhraseFontSize
        };
    }


    // ============================================================
    // 保存 settings.json
    //
    // JSON 中只会保存：
    //
    // 1. OutputFolder
    // 2. TxtName
    // 3. DatName
    // 4. QuickPhraseFontSize
    // 5. DefaultOutputFolder
    // 6. DefaultTxtName
    // 7. DefaultDatName
    // 8. DefaultQuickPhraseFontSize
    //
    // EffectiveXXX 全部有 [JsonIgnore]，
    // 所以不会出现在 JSON 中。
    // ============================================================

    public void Save(string path)
    {
        string? directory =
            Path.GetDirectoryName(path);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json =
            JsonSerializer.Serialize(
                this,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

        File.WriteAllText(
            path,
            json);
    }
}