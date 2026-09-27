namespace ImePhraseTool;

public sealed record BatchAddResult(List<PhraseEntry> Added,
    List<(int Line, List<PhraseEntry> Before, PhraseEntry Entry)> Replaced,
    List<(int Line, PhraseEntry Entry)> Duplicates, int Total);

public static class BatchPhraseAdder
{
    public static BatchAddResult Add(string txtPath, string datPath, string text)
    {
        // Validate the entire batch before writing either file.
        var input = new List<(int Line, PhraseEntry Entry)>();
        using var reader = new StringReader(text);
        int lineNumber = 0;
        while (reader.ReadLine() is { } line)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line)) continue;
            try { input.Add((lineNumber, PhraseConverter.ParseEntry(line))); }
            catch (InvalidDataException ex) { throw new InvalidDataException($"新增短语第 {lineNumber} 行：{ex.Message}"); }
        }
        if (input.Count == 0) throw new InvalidDataException("请输入至少一行短语，格式：拼音,位置,输出文本。");

        var entries = File.Exists(txtPath) ? PhraseConverter.ReadTxt(txtPath, allowEmpty: true) : new List<PhraseEntry>();
        var known = entries.Select(x => (x.Pinyin, x.Position)).ToHashSet();
        // TXT takes precedence over stale DAT values for the same key.
        if (File.Exists(datPath))
            foreach (var entry in PhraseConverter.ReadDat(datPath))
                if (known.Add((entry.Pinyin, entry.Position))) entries.Add(entry);
        var addedKeys = new List<(string Pinyin, byte Position)>();
        var replaced = new List<(int Line, List<PhraseEntry> Before, PhraseEntry Entry)>();
        var duplicates = new List<(int Line, PhraseEntry Entry)>();
        foreach (var item in input)
        {
            var key = (item.Entry.Pinyin, item.Entry.Position);
            var matches = entries.Where(x => (x.Pinyin, x.Position) == key).ToList();
            if (matches.Count == 0)
            {
                entries.Add(item.Entry);
                addedKeys.Add(key);
            }
            else if (matches.Count == 1 && matches[0] == item.Entry) duplicates.Add(item);
            else
            {
                entries.RemoveAll(x => (x.Pinyin, x.Position) == key);
                entries.Add(item.Entry);
                replaced.Add((item.Line, matches, item.Entry));
            }
        }
        var added = addedKeys.Select(key => entries.Single(x => (x.Pinyin, x.Position) == key)).ToList();
        if (added.Count > 0 || replaced.Count > 0)
            OutputFiles.Save(new[] {
                (txtPath, PhraseConverter.EncodeTxt(entries)),
                (datPath, PhraseConverter.EncodeDat(entries)) });
        return new(added, replaced, duplicates, entries.Count);
    }
}
