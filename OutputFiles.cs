namespace ImePhraseTool;

public static class OutputFiles
{
    public static void Save(IReadOnlyList<(string Path, byte[] Data)> files)
    {
        var staged = new List<(string Target, string Temp)>();
        var committed = new List<(string Target, string? Backup)>();
        try
        {
            foreach (var file in files)
            {
                var target = Path.GetFullPath(file.Path);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                var temp = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
                staged.Add((target, temp));
                File.WriteAllBytes(temp, file.Data);
            }
            foreach (var item in staged)
            {
                string? backup = null;
                if (File.Exists(item.Target))
                {
                    backup = item.Target + "." + Guid.NewGuid().ToString("N") + ".bak";
                    File.Replace(item.Temp, item.Target, backup);
                }
                else File.Move(item.Temp, item.Target);
                committed.Add((item.Target, backup));
            }
        }
        catch
        {
            foreach (var item in committed.AsEnumerable().Reverse())
            {
                if (item.Backup is not null) File.Move(item.Backup, item.Target, true);
                else File.Delete(item.Target);
            }
            throw;
        }
        finally { foreach (var item in staged) TryDelete(item.Temp); }
        foreach (var item in committed) if (item.Backup is not null) TryDelete(item.Backup);
    }
    private static void TryDelete(string path)
    {
        try { File.Delete(path); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
