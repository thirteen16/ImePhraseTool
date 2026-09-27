using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace ImePhraseTool;

public class MainForm : Form
{
    private readonly TextBox txtToDatInput = new();
    private readonly TextBox txtToDatOutput = new();

    private readonly TextBox datToTxtInput = new();
    private readonly TextBox datToTxtOutput = new();

    private readonly TextBox quickTxtFile = new();
    private readonly TextBox quickDatFile = new();
    private readonly TextBox quickPhrase = new();

    private const string DefaultDatName = "UserDefinedPhrase.dat";
    private const string DefaultTxtName = "IME_phrases.txt";

    public MainForm()
    {
        Text = "微软拼音短语工具";
        StartPosition = FormStartPosition.CenterScreen;

        ClientSize = new Size(1100, 850);
        MinimumSize = new Size(900, 760);

        AutoScaleMode = AutoScaleMode.Dpi;

        BuildUi();
    }

    private void BuildUi()
    {
        var main = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 1,
            RowCount = 4
        };

        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 215));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 215));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 215));

        Controls.Add(main);

        var title = new Label
        {
            Text = "微软拼音自定义短语工具",
            Dock = DockStyle.Fill,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font(
                "Microsoft YaHei UI",
                17F,
                FontStyle.Bold),
            AutoEllipsis = false
        };

        main.Controls.Add(title, 0, 0);

        main.Controls.Add(
            CreateTxtToDatGroup(),
            0,
            1);

        main.Controls.Add(
            CreateDatToTxtGroup(),
            0,
            2);

        main.Controls.Add(
            CreateQuickAddGroup(),
            0,
            3);
    }

    // ============================================================
    // TXT → DAT
    // ============================================================

    private GroupBox CreateTxtToDatGroup()
    {
        var group = CreateGroupBox("TXT → DAT");
        var table = CreateConversionTable();

        // TXT 文件
        AddControl(
            table,
            CreateLabel("TXT 文件："),
            0, 0);

        ConfigureTextBox(txtToDatInput);

        AddControl(
            table,
            txtToDatInput,
            1, 0);

        AddControl(
            table,
            CreateButton("选择文件", SelectTxtToDatInput),
            2, 0);

        AddControl(
            table,
            CreateButton("开始转换", ConvertTxtToDat),
            3, 0);

        // 输出位置
        AddControl(
            table,
            CreateLabel("输出位置："),
            0, 1);

        ConfigureTextBox(txtToDatOutput);

        AddControl(
            table,
            txtToDatOutput,
            1, 1);

        AddControl(
            table,
            CreateButton("选择文件", SelectTxtToDatOutputFile),
            2, 1);

        AddControl(
            table,
            CreateButton("选择目录", SelectTxtToDatOutputFolder),
            3, 1);

        group.Controls.Add(table);

        return group;
    }

    private void SelectTxtToDatInput()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "选择 TXT 文件",
            Filter = "TXT 文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            txtToDatInput.Text = dialog.FileName;
        }
    }

    private void SelectTxtToDatOutputFile()
    {
        using var dialog = new SaveFileDialog
        {
            Title = "选择 DAT 输出文件",
            Filter = "DAT 文件 (*.dat)|*.dat|所有文件 (*.*)|*.*",
            DefaultExt = "dat",
            AddExtension = true,
            FileName = DefaultDatName,
            OverwritePrompt = false
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            txtToDatOutput.Text = dialog.FileName;
        }
    }

    private void SelectTxtToDatOutputFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "选择 DAT 输出目录",
            ShowNewFolderButton = true
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            txtToDatOutput.Text =
                Path.Combine(
                    dialog.SelectedPath,
                    DefaultDatName);
        }
    }

    private void ConvertTxtToDat()
    {
        try
        {
            if (!File.Exists(txtToDatInput.Text))
            {
                ShowInfo("请先选择有效的 TXT 文件。");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtToDatOutput.Text))
            {
                ShowInfo("请选择 DAT 输出文件或输出目录。");
                return;
            }

            var entries = ReadTxt(txtToDatInput.Text);

            entries.Sort(CompareEntries);

            WriteTxt(
                txtToDatInput.Text,
                entries);

            WriteDat(
                txtToDatOutput.Text,
                entries);

            MessageBox.Show(
                this,
                $"转换完成。\n\n共 {entries.Count} 条短语。",
                "完成",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    // ============================================================
    // DAT → TXT
    // ============================================================

    private GroupBox CreateDatToTxtGroup()
    {
        var group = CreateGroupBox("DAT → TXT");
        var table = CreateConversionTable();

        AddControl(
            table,
            CreateLabel("DAT 文件："),
            0, 0);

        ConfigureTextBox(datToTxtInput);

        AddControl(
            table,
            datToTxtInput,
            1, 0);

        AddControl(
            table,
            CreateButton("选择文件", SelectDatToTxtInput),
            2, 0);

        AddControl(
            table,
            CreateButton("开始转换", ConvertDatToTxt),
            3, 0);

        AddControl(
            table,
            CreateLabel("输出位置："),
            0, 1);

        ConfigureTextBox(datToTxtOutput);

        AddControl(
            table,
            datToTxtOutput,
            1, 1);

        AddControl(
            table,
            CreateButton("选择文件", SelectDatToTxtOutputFile),
            2, 1);

        AddControl(
            table,
            CreateButton("选择目录", SelectDatToTxtOutputFolder),
            3, 1);

        group.Controls.Add(table);

        return group;
    }

    private void SelectDatToTxtInput()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "选择 DAT 文件",
            Filter = "DAT 文件 (*.dat)|*.dat|所有文件 (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            datToTxtInput.Text = dialog.FileName;
        }
    }

    private void SelectDatToTxtOutputFile()
    {
        using var dialog = new SaveFileDialog
        {
            Title = "选择 TXT 输出文件",
            Filter = "TXT 文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
            DefaultExt = "txt",
            AddExtension = true,
            FileName = DefaultTxtName,
            OverwritePrompt = false
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            datToTxtOutput.Text = dialog.FileName;
        }
    }

    private void SelectDatToTxtOutputFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "选择 TXT 输出目录",
            ShowNewFolderButton = true
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            datToTxtOutput.Text =
                Path.Combine(
                    dialog.SelectedPath,
                    DefaultTxtName);
        }
    }

    private void ConvertDatToTxt()
    {
        try
        {
            if (!File.Exists(datToTxtInput.Text))
            {
                ShowInfo("请先选择有效的 DAT 文件。");
                return;
            }

            if (string.IsNullOrWhiteSpace(datToTxtOutput.Text))
            {
                ShowInfo("请选择 TXT 输出文件或输出目录。");
                return;
            }

            var entries = ReadDat(datToTxtInput.Text);

            entries.Sort(CompareEntries);

            WriteTxt(
                datToTxtOutput.Text,
                entries);

            MessageBox.Show(
                this,
                $"转换完成。\n\n共 {entries.Count} 条短语。",
                "完成",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    // ============================================================
    // 快速添加
    // ============================================================

    private GroupBox CreateQuickAddGroup()
    {
        var group = CreateGroupBox("快速添加");
        var table = CreateQuickTable();

        AddControl(
            table,
            CreateLabel("TXT 文件："),
            0, 0);

        ConfigureTextBox(quickTxtFile);

        AddControl(
            table,
            quickTxtFile,
            1, 0);

        AddControl(
            table,
            CreateButton("选择文件", SelectQuickTxtFile),
            2, 0);

        AddControl(
            table,
            CreateLabel("DAT 文件："),
            0, 1);

        ConfigureTextBox(quickDatFile);

        AddControl(
            table,
            quickDatFile,
            1, 1);

        AddControl(
            table,
            CreateButton("选择文件", SelectQuickDatFile),
            2, 1);

        AddControl(
            table,
            CreateLabel("短语："),
            0, 2);

        ConfigureTextBox(quickPhrase);

        quickPhrase.PlaceholderText =
            "拼音，位置（词频），输出文本";

        AddControl(
            table,
            quickPhrase,
            1, 2);

        AddControl(
            table,
            CreateButton("添加", QuickAdd),
            2, 2);

        group.Controls.Add(table);

        return group;
    }

    private void SelectQuickTxtFile()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "选择 TXT 文件",
            Filter = "TXT 文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            quickTxtFile.Text = dialog.FileName;
        }
    }

    private void SelectQuickDatFile()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "选择 DAT 文件",
            Filter = "DAT 文件 (*.dat)|*.dat|所有文件 (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            quickDatFile.Text = dialog.FileName;
        }
    }

    private void QuickAdd()
    {
        try
        {
            if (!File.Exists(quickTxtFile.Text))
            {
                ShowInfo("请先选择有效的 TXT 文件。");
                return;
            }

            if (!File.Exists(quickDatFile.Text))
            {
                ShowInfo("请先选择有效的 DAT 文件。");
                return;
            }

            if (string.IsNullOrWhiteSpace(quickPhrase.Text))
            {
                ShowInfo(
                    "请输入短语，例如：\n\napi,1,api");
                return;
            }

            var newEntry =
                ParseEntry(quickPhrase.Text);

            var entries =
                ReadTxt(quickTxtFile.Text);

            entries.Add(newEntry);

            entries.Sort(CompareEntries);

            WriteTxt(
                quickTxtFile.Text,
                entries);

            WriteDat(
                quickDatFile.Text,
                entries);

            quickPhrase.Clear();

            MessageBox.Show(
                this,
                $"添加完成。\n\n当前共有 {entries.Count} 条短语。",
                "完成",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    // ============================================================
    // UI 布局
    // ============================================================

    private static GroupBox CreateGroupBox(string text)
    {
        return new GroupBox
        {
            Text = text,
            Dock = DockStyle.Fill,
            Padding = new Padding(14),
            Font = new Font(
                "Microsoft YaHei UI",
                9F,
                FontStyle.Regular)
        };
    }

    private static Label CreateLabel(string text)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = false,
            UseCompatibleTextRendering = false,
            Margin = new Padding(0, 4, 8, 4)
        };
    }

    private static Button CreateButton(
        string text,
        Action action)
    {
        var button = new Button
        {
            Text = text,

            // 关键：
            // 不允许按钮根据文字重新计算尺寸
            AutoSize = false,

            // 不允许按钮文字因为尺寸不足而产生奇怪布局
            UseCompatibleTextRendering = false,

            Dock = DockStyle.Fill,

            Margin = new Padding(5, 4, 5, 4),

            MinimumSize = new Size(110, 36),

            TextAlign = ContentAlignment.MiddleCenter,

            Padding = new Padding(0),

            TabStop = true
        };

        button.Click += (_, _) => action();

        return button;
    }

    private static void ConfigureTextBox(TextBox textBox)
    {
        textBox.Dock = DockStyle.Fill;
        textBox.AutoSize = false;

        textBox.Margin =
            new Padding(5, 5, 5, 5);

        textBox.Font =
            new Font(
                "Microsoft YaHei UI",
                10F);

        textBox.Height = 36;
    }

    private static TableLayoutPanel CreateConversionTable()
    {
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,

            ColumnCount = 4,
            RowCount = 2,

            Margin = new Padding(0),
            Padding = new Padding(0),

            GrowStyle = TableLayoutPanelGrowStyle.FixedSize
        };

        // 标签
        table.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Absolute,
                140));

        // 输入框
        table.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                100));

        // 选择文件
        table.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Absolute,
                120));

        // 选择目录 / 开始转换
        table.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Absolute,
                120));

        table.RowStyles.Add(
            new RowStyle(
                SizeType.Absolute,
                65));

        table.RowStyles.Add(
            new RowStyle(
                SizeType.Absolute,
                65));

        return table;
    }

    private static TableLayoutPanel CreateQuickTable()
    {
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,

            ColumnCount = 3,
            RowCount = 3,

            Margin = new Padding(0),
            Padding = new Padding(0),

            GrowStyle = TableLayoutPanelGrowStyle.FixedSize
        };

        table.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Absolute,
                140));

        table.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                100));

        table.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Absolute,
                120));

        table.RowStyles.Add(
            new RowStyle(
                SizeType.Absolute,
                55));

        table.RowStyles.Add(
            new RowStyle(
                SizeType.Absolute,
                55));

        table.RowStyles.Add(
            new RowStyle(
                SizeType.Absolute,
                55));

        return table;
    }

    private static void AddControl(
        TableLayoutPanel table,
        Control control,
        int column,
        int row)
    {
        table.Controls.Add(
            control,
            column,
            row);
    }

    private void ShowInfo(string message)
    {
        MessageBox.Show(
            this,
            message,
            "提示",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void ShowError(Exception ex)
    {
        MessageBox.Show(
            this,
            $"操作失败：\n\n{ex.Message}",
            "错误",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    // ============================================================
    // TXT
    // ============================================================

    private static List<PhraseEntry> ReadTxt(string file)
    {
        var result =
            new List<PhraseEntry>();

        foreach (var rawLine in File.ReadAllLines(
                     file,
                     new UTF8Encoding(false)))
        {
            var line = rawLine.Trim();

            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (line.StartsWith("#"))
                continue;

            result.Add(ParseEntry(line));
        }

        return result;
    }

    private static PhraseEntry ParseEntry(string line)
    {
        var parts =
            line.Split(',', 3);

        if (parts.Length != 3)
        {
            throw new InvalidDataException(
                $"TXT 行格式错误：\n\n{line}\n\n" +
                "正确格式：拼音,位置（词频）,输出文本");
        }

        var pinyin =
            parts[0].Trim();

        var positionText =
            parts[1].Trim();

        var phrase =
            parts[2];

        if (string.IsNullOrWhiteSpace(pinyin))
        {
            throw new InvalidDataException(
                $"拼音不能为空：\n\n{line}");
        }

        if (!byte.TryParse(
                positionText,
                out var position))
        {
            throw new InvalidDataException(
                $"位置（词频）必须是 0～255 的数字：\n\n{line}");
        }

        return new PhraseEntry(
            pinyin,
            position,
            phrase);
    }

    private static void WriteTxt(
        string file,
        IEnumerable<PhraseEntry> entries)
    {
        var directory =
            Path.GetDirectoryName(file);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var lines =
            entries.Select(
                x =>
                    $"{x.Pinyin},{x.Position},{x.Phrase}");

        File.WriteAllLines(
            file,
            lines,
            new UTF8Encoding(false));
    }

    private static int CompareEntries(
        PhraseEntry? x,
        PhraseEntry? y)
    {
        if (ReferenceEquals(x, y))
            return 0;

        if (x is null)
            return -1;

        if (y is null)
            return 1;

        var result =
            string.Compare(
                x.Pinyin,
                y.Pinyin,
                StringComparison.Ordinal);

        if (result != 0)
            return result;

        result =
            x.Position.CompareTo(
                y.Position);

        if (result != 0)
            return result;

        return string.Compare(
            x.Phrase,
            y.Phrase,
            StringComparison.Ordinal);
    }

    // ============================================================
    // DAT
    // ============================================================

    private static List<PhraseEntry> ReadDat(string file)
    {
        var data =
            File.ReadAllBytes(file);

        if (data.Length < 0x40)
        {
            throw new InvalidDataException(
                "DAT 文件太小，无法识别。");
        }

        var magic =
            Encoding.ASCII.GetString(
                data,
                0,
                8);

        if (magic != "mschxudp")
        {
            throw new InvalidDataException(
                "不是有效的微软拼音 UserDefinedPhrase.dat 文件。");
        }

        var offsetTableStart =
            ReadInt32(data, 0x10);

        var phraseStart =
            ReadInt32(data, 0x14);

        var count =
            ReadInt32(data, 0x1C);

        if (offsetTableStart < 0 ||
            phraseStart < 0 ||
            count < 0)
        {
            throw new InvalidDataException(
                "DAT 文件头中的数据无效。");
        }

        if (offsetTableStart >= data.Length ||
            phraseStart >= data.Length)
        {
            throw new InvalidDataException(
                "DAT 文件结构损坏。");
        }

        var result =
            new List<PhraseEntry>(count);

        for (var i = 0; i < count; i++)
        {
            var offsetPosition =
                offsetTableStart + i * 4;

            if (offsetPosition + 4 >
                data.Length)
            {
                throw new InvalidDataException(
                    "DAT 文件的偏移表超出文件范围。");
            }

            var relativeOffset =
                ReadInt32(
                    data,
                    offsetPosition);

            var entryPosition =
                phraseStart + relativeOffset;

            if (entryPosition < phraseStart ||
                entryPosition + 0x10 >
                data.Length)
            {
                throw new InvalidDataException(
                    "DAT 文件中的条目位置无效。");
            }

            var entryMagic =
                ReadUInt32(
                    data,
                    entryPosition);

            if (entryMagic != 0x00100010)
            {
                throw new InvalidDataException(
                    "DAT 文件中的条目格式无效。");
            }

            var phraseOffset =
                ReadUInt16(
                    data,
                    entryPosition + 4);

            var position =
                data[entryPosition + 6];

            var pinyinStart =
                entryPosition + 0x10;

            var pinyinEnd =
                FindUtf16Null(
                    data,
                    pinyinStart);

            if (pinyinEnd < 0)
            {
                throw new InvalidDataException(
                    "DAT 文件中的拼音字符串无效。");
            }

            var pinyin =
                Encoding.Unicode.GetString(
                    data,
                    pinyinStart,
                    pinyinEnd - pinyinStart);

            var phraseStartPosition =
                entryPosition +
                4 +
                phraseOffset;

            if (phraseStartPosition < 0 ||
                phraseStartPosition >= data.Length)
            {
                throw new InvalidDataException(
                    "DAT 文件中的文本偏移无效。");
            }

            var phraseEnd =
                FindUtf16Null(
                    data,
                    phraseStartPosition);

            if (phraseEnd < 0)
            {
                throw new InvalidDataException(
                    "DAT 文件中的文本字符串无效。");
            }

            var phrase =
                Encoding.Unicode.GetString(
                    data,
                    phraseStartPosition,
                    phraseEnd - phraseStartPosition);

            result.Add(
                new PhraseEntry(
                    pinyin,
                    position,
                    phrase));
        }

        return result;
    }

    private static void WriteDat(
        string file,
        List<PhraseEntry> entries)
    {
        var directory =
            Path.GetDirectoryName(file);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var stream =
            new MemoryStream();

        using var writer =
            new BinaryWriter(stream);

        writer.Write(
            Encoding.ASCII.GetBytes(
                "mschxudp"));

        writer.Write(0);
        writer.Write(0x00600002);
        writer.Write(1);
        writer.Write(0x40);

        var phraseStart =
            0x40 +
            entries.Count * 4;

        writer.Write(phraseStart);

        var fileSizePosition =
            stream.Position;

        writer.Write(0);

        writer.Write(entries.Count);

        writer.Write(
            DateTimeOffset.UtcNow
                .ToUnixTimeSeconds());

        while (stream.Position < 0x40)
        {
            writer.Write((byte)0);
        }

        var offsets =
            new List<int>(
                entries.Count);

        using var entryStream =
            new MemoryStream();

        using var entryWriter =
            new BinaryWriter(entryStream);

        foreach (var entry in entries)
        {
            offsets.Add(
                checked(
                    (int)entryStream.Position));

            var pinyinBytes =
                Encoding.Unicode.GetBytes(
                    entry.Pinyin);

            var phraseBytes =
                Encoding.Unicode.GetBytes(
                    entry.Phrase);

            entryWriter.Write(
                0x00100010u);

            var phraseOffset =
                checked(
                    (ushort)(
                        0x0E +
                        pinyinBytes.Length));

            entryWriter.Write(
                phraseOffset);

            entryWriter.Write(
                entry.Position);

            entryWriter.Write(
                (byte)0x06);

            entryWriter.Write(0u);

            entryWriter.Write(
                0x323E4BD0u);

            entryWriter.Write(
                pinyinBytes);

            entryWriter.Write(
                (ushort)0);

            entryWriter.Write(
                phraseBytes);

            entryWriter.Write(
                (ushort)0);
        }

        foreach (var offset in offsets)
        {
            writer.Write(offset);
        }

        writer.Write(
            entryStream.ToArray());

        var fileSize =
            checked(
                (int)stream.Length);

        stream.Position =
            fileSizePosition;

        writer.Write(fileSize);

        writer.Flush();

        File.WriteAllBytes(
            file,
            stream.ToArray());
    }

    private static int ReadInt32(
        byte[] data,
        int offset)
    {
        return BitConverter.ToInt32(
            data,
            offset);
    }

    private static uint ReadUInt32(
        byte[] data,
        int offset)
    {
        return BitConverter.ToUInt32(
            data,
            offset);
    }

    private static ushort ReadUInt16(
        byte[] data,
        int offset)
    {
        return BitConverter.ToUInt16(
            data,
            offset);
    }

    private static int FindUtf16Null(
        byte[] data,
        int start)
    {
        for (var i = start;
             i + 1 < data.Length;
             i += 2)
        {
            if (data[i] == 0 &&
                data[i + 1] == 0)
            {
                return i;
            }
        }

        return -1;
    }

    // ============================================================
    // Model
    // ============================================================

    private sealed class PhraseEntry
    {
        public string Pinyin { get; }

        public byte Position { get; }

        public string Phrase { get; }

        public PhraseEntry(
            string pinyin,
            byte position,
            string phrase)
        {
            Pinyin = pinyin;
            Position = position;
            Phrase = phrase;
        }
    }
}