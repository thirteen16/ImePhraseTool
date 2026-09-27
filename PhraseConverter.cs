using System.Globalization;
using System.Text;

namespace ImePhraseTool;
public sealed record PhraseEntry(string Pinyin, byte Position, string Phrase);

public static class PhraseConverter
{
    private static readonly Encoding Utf8 = new UTF8Encoding(false, true);
    private static readonly Encoding Utf16 = new UnicodeEncoding(false, false, true);
    public static List<PhraseEntry> Sort(IEnumerable<PhraseEntry> entries) => entries
        .OrderBy(x => x.Pinyin, StringComparer.Ordinal).ThenBy(x => x.Position)
        .ThenBy(x => x.Phrase, StringComparer.Ordinal).ToList();

    public static PhraseEntry ParseEntry(string line)
    {
        var parts = line.Split(',', 3);
        if (parts.Length != 3) throw new InvalidDataException("格式应为：拼音,位置,输出文本（使用英文逗号）。");
        if (!byte.TryParse(parts[1].Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var position))
            throw new InvalidDataException("位置必须为 1～9 的整数。");
        var entry = new PhraseEntry(parts[0].Trim(), position, parts[2]);
        Validate(entry);
        return entry;
    }
    private static void Validate(PhraseEntry entry)
    {
        if (entry.Pinyin.Length is < 1 or > 32 || entry.Pinyin.Any(c => c < 'a' || c > 'z'))
            throw new InvalidDataException("拼音须为 1～32 个小写英文字母（可使用简拼）。");
        if (entry.Position is < 1 or > 9) throw new InvalidDataException("位置必须为 1～9 的候选位置，不是任意词频数值。");
        if (string.IsNullOrWhiteSpace(entry.Phrase) || entry.Phrase.Length > 64 || entry.Phrase.Any(char.IsControl))
            throw new InvalidDataException("输出文本须为 1～64 个 UTF-16 字符，不能全为空白或包含控制字符。");
        try { Utf16.GetByteCount(entry.Phrase); }
        catch (EncoderFallbackException) { throw new InvalidDataException("输出文本包含无效的 Unicode 字符。"); }
    }
    public static List<PhraseEntry> ReadTxt(string path, bool allowEmpty = false)
    {
        var bytes = File.ReadAllBytes(path);
        string text;
        try
        {
            if (bytes.AsSpan().StartsWith(new byte[] { 0xff, 0xfe })) text = Utf16.GetString(bytes, 2, bytes.Length - 2);
            else if (bytes.AsSpan().StartsWith(new byte[] { 0xfe, 0xff })) text = new UnicodeEncoding(true, false, true).GetString(bytes, 2, bytes.Length - 2);
            else
            {
                var start = bytes.AsSpan().StartsWith(new byte[] { 0xef, 0xbb, 0xbf }) ? 3 : 0;
                text = Utf8.GetString(bytes, start, bytes.Length - start);
            }
        }
        catch (DecoderFallbackException) { throw new InvalidDataException("TXT 编码无效，请另存为 UTF-8 或带 BOM 的 UTF-16 文本。"); }
        var entries = new List<PhraseEntry>();
        using var reader = new StringReader(text);
        int lineNumber = 0;
        while (reader.ReadLine() is { } line)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#')) continue;
            try { entries.Add(ParseEntry(line)); }
            catch (InvalidDataException ex) { throw new InvalidDataException($"TXT 第 {lineNumber} 行：{ex.Message}"); }
        }
        if (entries.Count == 0 && !allowEmpty) throw new InvalidDataException("TXT 中没有有效短语。");
        return Sort(entries);
    }
    public static byte[] EncodeTxt(IEnumerable<PhraseEntry> entries)
    {
        var sorted = Sort(entries);
        foreach (var entry in sorted) Validate(entry);
        return Utf8.GetBytes(string.Concat(sorted.Select(x => $"{x.Pinyin},{x.Position},{x.Phrase}\r\n")));
    }
    public static List<PhraseEntry> ReadDat(string path) => DecodeDat(File.ReadAllBytes(path));
    public static List<PhraseEntry> DecodeDat(byte[] data, bool allowEmpty = false)
    {
        void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidDataException("DAT 文件无效：" + message);
        }
        Require(data.Length >= 0x40, "文件头不完整。");
        Require(Encoding.ASCII.GetString(data, 0, 8) == "mschxudp", "不是微软拼音自定义短语文件。");
        Require(BitConverter.ToUInt32(data, 8) == 0x00600002 && BitConverter.ToUInt32(data, 12) == 1, "不支持的格式版本。");
        int table = BitConverter.ToInt32(data, 0x10), start = BitConverter.ToInt32(data, 0x14);
        int size = BitConverter.ToInt32(data, 0x18), count = BitConverter.ToInt32(data, 0x1c);
        Require(table == 0x40 && count >= 0 && size == data.Length && start <= size &&
            (long)table + (long)count * 4 == start, "文件大小、词条数或偏移表损坏。");
        if (count == 0 && allowEmpty)
        {
            Require(start == size, "空词库包含多余数据。");
            return new List<PhraseEntry>();
        }
        Require(count > 0, "文件中没有短语。");
        Require(count <= (size - start) / 24, "词条数量与数据长度不符。");
        var entries = new List<PhraseEntry>(count);
        int previousEnd = start;
        for (int i = 0; i < count; i++)
        {
            long position = (long)start + BitConverter.ToInt32(data, table + i * 4);
            long next = i + 1 == count ? size : (long)start + BitConverter.ToInt32(data, table + (i + 1) * 4);
            Require(position == previousEnd && next > position && next <= size && next - position >= 24 && (next - position) % 2 == 0,
                $"第 {i + 1} 条的边界无效。");
            int p = (int)position, end = (int)next;
            Require(BitConverter.ToUInt32(data, p) == 0x00100010 && data[p + 7] == 6 && BitConverter.ToUInt32(data, p + 8) == 0,
                $"第 {i + 1} 条不是支持的普通短语。");
            int offset = BitConverter.ToUInt16(data, p + 4);
            Require(offset >= 20 && offset % 2 == 0 && offset <= end - p - 4, $"第 {i + 1} 条文本偏移无效。");
            Require(data[p + offset - 2] == 0 && data[p + offset - 1] == 0 && data[end - 2] == 0 && data[end - 1] == 0,
                $"第 {i + 1} 条缺少字符串结束标记。");
            try
            {
                var entry = new PhraseEntry(Utf16.GetString(data, p + 16, offset - 18), data[p + 6],
                    Utf16.GetString(data, p + offset, end - p - offset - 2));
                Validate(entry);
                entries.Add(entry);
            }
            catch (Exception ex) when (ex is InvalidDataException or DecoderFallbackException)
            { throw new InvalidDataException($"DAT 第 {i + 1} 条：{ex.Message}"); }
            previousEnd = end;
        }
        return Sort(entries);
    }
    public static byte[] EncodeDat(IEnumerable<PhraseEntry> source)
    {
        var entries = Sort(source);
        if (entries.Count == 0) throw new InvalidDataException("至少需要一条短语。");
        foreach (var entry in entries) Validate(entry);
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        var start = checked(0x40 + entries.Count * 4);
        var timestamp = checked((uint)(DateTimeOffset.UtcNow - new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero)).TotalSeconds);
        writer.Write(Encoding.ASCII.GetBytes("mschxudp"));
        writer.Write(0x00600002);
        writer.Write(1);
        writer.Write(0x40);
        writer.Write(start);
        writer.Write(0); // File size, filled after encoding.
        writer.Write(entries.Count);
        writer.Write(timestamp);
        writer.Write(new byte[start - (int)stream.Position]);
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var entryStart = stream.Position;
            stream.Position = 0x40 + i * 4;
            writer.Write(checked((int)entryStart - start));
            stream.Position = entryStart;
            writer.Write(0x00100010u);
            writer.Write(checked((ushort)(18 + entry.Pinyin.Length * 2)));
            writer.Write(entry.Position);
            writer.Write((byte)6);
            writer.Write(0u);
            writer.Write(timestamp);
            writer.Write(Utf16.GetBytes(entry.Pinyin));
            writer.Write((ushort)0);
            writer.Write(Utf16.GetBytes(entry.Phrase));
            writer.Write((ushort)0);
        }
        stream.Position = 0x18;
        writer.Write(checked((int)stream.Length));
        return stream.ToArray();
    }
}
