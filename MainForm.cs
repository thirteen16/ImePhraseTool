using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;

namespace ImePhraseTool;

public class MainForm : Form, IMessageFilter
{
    private readonly string settingsPath;
    private readonly System.Windows.Forms.Timer settingsTimer = new() { Interval = 600 };
    private bool applyingSettings;
    private float quickPhraseFontSize = 13F;
    private int wheelDelta;

    private readonly TextBox outputFolder = new();
    private readonly TextBox txtName = new() { Text = "IME_phrases.txt" };
    private readonly TextBox datName = new() { Text = "UserDefinedPhrase.dat" };

    private readonly TextBox quickPhrase = new()
    {
        PlaceholderText = "每行一条，例如：nihao,1,你好",
        Multiline = true,
        AcceptsReturn = true,
        ScrollBars = ScrollBars.Vertical,
        WordWrap = false
    };

    private readonly Label status = new()
    {
        Text = "",
        AutoSize = true,
        Dock = DockStyle.Fill,
        ForeColor = Color.FromArgb(100, 116, 139),
        Visible = false
    };

    public MainForm() : this(UserSettings.DefaultPath) { }

    public MainForm(string settingsPath)
    {
        this.settingsPath = settingsPath;

        SuspendLayout();

        Text = "微软拼音短语工具";

        Font = new Font(
            (SystemFonts.MessageBoxFont ?? SystemFonts.DefaultFont).FontFamily,
            12F,
            FontStyle.Regular,
            GraphicsUnit.Point);

        BackColor = Color.FromArgb(245, 247, 251);
        ForeColor = Color.FromArgb(30, 41, 59);

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        StartPosition = FormStartPosition.CenterScreen;

        using (var iconStream =
               typeof(MainForm).Assembly.GetManifestResourceStream("ImePhraseTool.AppIcon"))
        {
            if (iconStream is not null)
                Icon = new Icon(iconStream);
        }

        Load += (_, _) =>
        {
            var area = Screen.FromControl(this).WorkingArea;

            MinimumSize = new Size(
                Math.Min((int)(area.Width * .9), Font.Height * 38),
                Math.Min((int)(area.Height * .9), Font.Height * 22));

            Size = new Size(
                Math.Max(MinimumSize.Width, (int)(area.Width * .55)),
                Math.Max(MinimumSize.Height, (int)(area.Height * .52)));
        };

        outputFolder.Text =
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

        var main = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(12)
        };

        main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        foreach (int share in new[] { 46, 16, 38 })
            main.RowStyles.Add(new RowStyle(SizeType.Percent, share));

        main.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        Controls.Add(main);

        // =========================
        // 顶部设置区域
        // =========================

        var settings = Table(3, true);

        AddRow(
            settings,
            0,
            "目录",
            outputFolder,
            Button("选择目录", SelectFolder));

        settings.Controls.Add(
            Button("恢复默认", ResetSettings),
            3,
            0);

        AddRow(
            settings,
            1,
            "TXT",
            txtName,
            Button("定位文件", () => LocateFile(() => OutputPath(true))));

        AddRow(
            settings,
            2,
            "DAT",
            datName,
            Button("定位文件", () => LocateFile(() => OutputPath(false))));

        settings.SetColumnSpan(
            settings.GetControlFromPosition(2, 1)!,
            2);

        settings.SetColumnSpan(
            settings.GetControlFromPosition(2, 2)!,
            2);

        main.Controls.Add(Card(settings), 0, 0);

        // =========================
        // 转换按钮
        // =========================

        var conversions = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            Margin = new Padding(0, 6, 0, 6)
        };

        for (int i = 0; i < 4; i++)
            conversions.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 25));

        conversions.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        conversions.Controls.Add(
            Button("TXT → DAT", () => ConvertFile(true)),
            0,
            0);

        conversions.Controls.Add(
            Button("DAT → TXT", () => ConvertFile(false)),
            1,
            0);

        conversions.Controls.Add(
            Button("仅排序 TXT", SortTxt),
            2,
            0);

        conversions.Controls.Add(
            Button("导入微软拼音", ImportPinyin, true),
            3,
            0);

        main.Controls.Add(conversions, 0, 1);

        // =========================
        // 快速添加区域
        // =========================

        var quick = Table(1);
        var addButton = Button("添加", QuickAdd, true);

        AddRow(
            quick,
            0,
            "短语",
            quickPhrase,
            addButton);

        addButton.Dock = DockStyle.None;
        addButton.Anchor =
            AnchorStyles.Left | AnchorStyles.Right;

        quickPhrase.Dock = DockStyle.None;
        quickPhrase.Anchor =
            AnchorStyles.Left | AnchorStyles.Right;

        // 窗口真正显示以后：
        // 1. 设置输入框高度
        // 2. 按最终窗口尺寸重新居中
        Shown += (_, _) =>
        {
            addButton.Height =
                quickPhrase.Height =
                    Font.Height * 5 + 8;

            CenterToScreen();
        };

        // DPI 改变时调整控件高度。
        // 使用主窗口字体高度，不使用 quickPhrase 字体高度，
        // 避免 Ctrl+滚轮改变文字字号时输入框跟着变高。
        DpiChanged += (_, _) =>
        {
            addButton.Height =
                quickPhrase.Height =
                    Font.Height * 5 + 8;
        };

        main.Controls.Add(Card(quick), 0, 2);

        // =========================
        // 状态栏
        // =========================

        main.Controls.Add(status, 0, 3);

        status.TextChanged += (_, _) =>
            status.Visible = status.Text.Length > 0;

        // =========================
        // 加载配置
        // =========================

        ApplySettings(
            UserSettings.Load(settingsPath, out var warning));

        if (warning is not null)
            status.Text = warning;

        settingsTimer.Tick += (_, _) =>
            SaveSettings();

        foreach (var field in new[]
                 {
                     outputFolder,
                     txtName,
                     datName
                 })
        {
            field.TextChanged += (_, _) =>
            {
                if (applyingSettings)
                    return;

                settingsTimer.Stop();
                settingsTimer.Start();
            };
        }

        FormClosing += (_, _) =>
            SaveSettings();

        if (!File.Exists(settingsPath))
            SaveSettings();

        ResumeLayout(true);

        Application.AddMessageFilter(this);
    }

    // ============================================================
    // Ctrl + 滚轮
    // 仅当鼠标位于“短语”输入框上时修改该输入框文字字号
    // ============================================================

    public bool PreFilterMessage(ref Message message)
    {
        const int MouseWheel = 0x020A;

        if (message.Msg != MouseWheel)
            return false;

        if ((ModifierKeys & Keys.Control) == 0)
        {
            wheelDelta = 0;
            return false;
        }

        // 根据鼠标实际位置判断，而不是焦点。
        var mousePosition =
            quickPhrase.PointToClient(Cursor.Position);

        if (!quickPhrase.ClientRectangle.Contains(mousePosition))
        {
            wheelDelta = 0;
            return false;
        }

        wheelDelta += unchecked(
            (short)((message.WParam.ToInt64() >> 16) & 0xffff));

        int steps = wheelDelta / 120;
        wheelDelta %= 120;

        if (steps != 0)
        {
            SetQuickPhraseFontSize(
                quickPhraseFontSize + steps);

            // 延迟保存配置，避免滚轮连续滚动时反复写磁盘。
            settingsTimer.Stop();
            settingsTimer.Start();
        }

        // 阻止输入框自己处理 Ctrl+滚轮。
        return true;
    }

    private void SetQuickPhraseFontSize(float size)
    {
        quickPhraseFontSize =
            float.IsFinite(size)
                ? Math.Clamp(size, 8F, 24F)
                : 13F;

        var oldFont = quickPhrase.Font;

        quickPhrase.Font = new Font(
            oldFont.FontFamily,
            quickPhraseFontSize,
            oldFont.Style,
            GraphicsUnit.Point);
    }

    // ============================================================
    // 设置
    // ============================================================

    private void ApplySettings(UserSettings settings)
    {
        applyingSettings = true;

        try
        {
            outputFolder.Text = settings.OutputFolder;
            txtName.Text = settings.TxtName;
            datName.Text = settings.DatName;

            SetQuickPhraseFontSize(
                settings.QuickPhraseFontSize);
        }
        finally
        {
            applyingSettings = false;
        }
    }

    private bool SaveSettings()
    {
        settingsTimer.Stop();

        try
        {
            new UserSettings
            {
                OutputFolder = outputFolder.Text,
                TxtName = txtName.Text,
                DatName = datName.Text,
                QuickPhraseFontSize = quickPhraseFontSize
            }.Save(settingsPath);

            return true;
        }
        catch (Exception ex)
            when (ex is IOException
                or UnauthorizedAccessException)
        {
            status.Text =
                "配置保存失败：" + ex.Message;

            return false;
        }
    }

    private void ResetSettings()
    {
        ApplySettings(new UserSettings());

        if (SaveSettings())
            status.Text = "已恢复默认";
    }

    // ============================================================
    // 文件定位
    // ============================================================

    private void LocateFile(Func<string> resolvePath)
    {
        try
        {
            var path = resolvePath();

            if (File.Exists(path))
            {
                Process.Start(
                    new ProcessStartInfo(
                        "explorer.exe",
                        $"/select,\"{path}\"")
                    {
                        UseShellExecute = true
                    });
            }
            else
            {
                var directory =
                    Path.GetDirectoryName(path)!;

                Directory.CreateDirectory(directory);

                Process.Start(
                    new ProcessStartInfo(directory)
                    {
                        UseShellExecute = true
                    });

                status.Text =
                    "文件尚不存在，已打开对应目录：" +
                    directory;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "无法定位文件",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    // ============================================================
    // 释放资源
    // ============================================================

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Application.RemoveMessageFilter(this);
            settingsTimer.Dispose();
        }

        base.Dispose(disposing);
    }

    // ============================================================
    // UI 辅助
    // ============================================================

    private static TableLayoutPanel Table(
        int rows,
        bool extraAction = false)
    {
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = extraAction ? 4 : 3,
            RowCount = rows
        };

        table.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 9));

        table.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                extraAction ? 59 : 70));

        table.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                extraAction ? 16 : 21));

        if (extraAction)
        {
            table.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 16));
        }

        for (int i = 0; i < rows; i++)
        {
            table.RowStyles.Add(
                new RowStyle(
                    SizeType.Percent,
                    100F / rows));
        }

        return table;
    }

    private Control Card(Control content)
    {
        var group = new Panel
        {
            BackColor = Color.White,
            Dock = DockStyle.Fill,
            Padding = new Padding(8),
            Margin = new Padding(0, 6, 0, 6)
        };

        group.Controls.Add(content);

        return group;
    }

    private static Button Button(
        string text,
        Action action,
        bool primary = false)
    {
        var button = new Button
        {
            Text = text,
            Dock = DockStyle.Fill,
            Margin = new Padding(4),
            AutoSize = false,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            BackColor = primary
                ? Color.FromArgb(37, 99, 235)
                : Color.White,
            ForeColor = primary
                ? Color.White
                : Color.FromArgb(51, 65, 85)
        };

        button.FlatAppearance.BorderColor =
            Color.FromArgb(218, 225, 235);

        button.FlatAppearance.BorderSize =
            primary ? 0 : 1;

        button.FlatAppearance.MouseOverBackColor =
            primary
                ? Color.FromArgb(29, 78, 216)
                : Color.FromArgb(239, 246, 255);

        button.Click += (_, _) =>
            action();

        return button;
    }

    private static void AddRow(
        TableLayoutPanel table,
        int row,
        string label,
        TextBox field,
        Button? button = null)
    {
        table.Controls.Add(
            new Label
            {
                Text = label,
                Dock = DockStyle.Fill,
                TextAlign =
                    ContentAlignment.MiddleLeft
            },
            0,
            row);

        field.Dock = DockStyle.None;
        field.Anchor =
            AnchorStyles.Left |
            AnchorStyles.Right;

        field.BackColor = Color.White;
        field.ForeColor =
            Color.FromArgb(30, 41, 59);

        field.Margin =
            new Padding(4, 8, 4, 4);

        table.Controls.Add(
            field,
            1,
            row);

        if (button is not null)
        {
            table.Controls.Add(
                button,
                2,
                row);
        }
    }

    // ============================================================
    // 选择目录
    // ============================================================

    private void SelectFolder()
    {
        using var dialog =
            new FolderBrowserDialog
            {
                SelectedPath = outputFolder.Text,
                Description = "选择生成目录",
                UseDescriptionForTitle = true
            };

        if (dialog.ShowDialog(this) ==
            DialogResult.OK)
        {
            outputFolder.Text =
                dialog.SelectedPath;
        }
    }

    // ============================================================
    // 输出路径
    // ============================================================

    private string OutputPath(bool txt)
    {
        string folder =
            outputFolder.Text.Trim();

        if (folder.Length == 0)
        {
            folder =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.DesktopDirectory);
        }

        if (!Path.IsPathFullyQualified(folder))
        {
            throw new InvalidDataException(
                "生成目录必须是完整路径，请使用“选择目录”。");
        }

        var name =
            (txt ? txtName.Text : datName.Text)
            .Trim();

        if (name.Length == 0)
        {
            name = txt
                ? "IME_phrases.txt"
                : "UserDefinedPhrase.dat";
        }

        string extension =
            txt ? ".txt" : ".dat";

        if (name.IndexOfAny(
                Path.GetInvalidFileNameChars()) >= 0 ||
            name.EndsWith('.') ||
            !string.Equals(
                Path.GetExtension(name),
                extension,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                $"文件名须为有效的 {extension} 文件名，不能包含目录。");
        }

        return Path.GetFullPath(
            Path.Combine(folder, name));
    }

    // ============================================================
    // 打开微软拼音设置
    // ============================================================

    private void ImportPinyin()
    {
        try
        {
            var folder =
                outputFolder.Text.Trim();

            if (folder.Length == 0)
            {
                folder =
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.DesktopDirectory);
            }

            if (!Path.IsPathFullyQualified(folder))
            {
                throw new InvalidDataException(
                    "目录必须是完整路径，请使用“选择目录”。");
            }

            Clipboard.SetText(
                Path.GetFullPath(folder));

            status.Text =
                "目录路径已复制。";

            Process.Start(
                new ProcessStartInfo(
                    "ms-settings:regionlanguage-chsime-pinyin-udp")
                {
                    UseShellExecute = true
                });

            status.Text =
                "目录路径已复制，请在系统页面点击“导入”并选择 DAT 文件。";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "无法复制目录或打开设置",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    // ============================================================
    // TXT / DAT 转换
    // ============================================================

    private async void ConvertFile(bool fromTxt)
    {
        await RunConversion(
            () =>
            {
                var input =
                    OutputPath(fromTxt);

                if (!File.Exists(input))
                {
                    throw new InvalidDataException(
                        $"输入文件不存在：\n{input}\n\n" +
                        "请检查上方目录和文件名，或将源文件放入该目录。");
                }

                var entries =
                    fromTxt
                        ? PhraseConverter.ReadTxt(input)
                        : PhraseConverter.ReadDat(input);

                var files =
                    new List<(string Path, byte[] Data)>
                    {
                        (
                            OutputPath(true),
                            PhraseConverter.EncodeTxt(entries)
                        )
                    };

                if (fromTxt)
                {
                    files.Add(
                        (
                            OutputPath(false),
                            PhraseConverter.EncodeDat(entries)
                        ));
                }

                if (files.Any(
                        x =>
                            string.Equals(
                                x.Path,
                                Path.GetFullPath(input),
                                StringComparison.OrdinalIgnoreCase) &&
                            !(fromTxt &&
                              x.Path == files[0].Path)))
                {
                    throw new InvalidDataException(
                        "输出文件不能覆盖输入文件。");
                }

                return (entries.Count, files);
            },
            rewritesSourceTxt: fromTxt);
    }

    // ============================================================
    // TXT 排序
    // ============================================================

    private async void SortTxt()
    {
        await RunConversion(
            () =>
            {
                var path =
                    OutputPath(true);

                if (!File.Exists(path))
                {
                    throw new InvalidDataException(
                        $"TXT 文件不存在：\n{path}\n\n" +
                        "请检查上方目录和 TXT 文件名。");
                }

                var entries =
                    PhraseConverter.ReadTxt(path);

                return (
                    entries.Count,
                    new List<(string Path, byte[] Data)>
                    {
                        (
                            path,
                            PhraseConverter.EncodeTxt(entries)
                        )
                    });
            },
            sortOnly: true);
    }

    // ============================================================
    // 快速添加
    // ============================================================

    private async void QuickAdd()
    {
        try
        {
            var txtPath =
                OutputPath(true);

            var datPath =
                OutputPath(false);

            var input =
                quickPhrase.Text;

            Enabled = false;
            UseWaitCursor = true;

            var result =
                await Task.Run(
                    () => BatchPhraseAdder.Add(
                        txtPath,
                        datPath,
                        input));

            status.Text =
                $"处理完成：新增 {result.Added.Count} 条，" +
                $"替换 {result.Replaced.Count} 次，" +
                $"未变化 {result.Duplicates.Count} 条。";

            string Format(PhraseEntry entry) =>
                $"{entry.Pinyin},{entry.Position},{entry.Phrase}";

            var details =
                status.Text +
                "\r\n" +
                (
                    result.Added.Count == 0 &&
                    result.Replaced.Count == 0
                        ? "内容均未变化，文件未修改。"
                        : $"合并后共 {result.Total} 条短语，TXT 和 DAT 已按升序保存。"
                ) +
                "\r\n\r\n成功新增（最终保存内容）：\r\n" +
                (
                    result.Added.Count == 0
                        ? "（无）"
                        : string.Join(
                            "\r\n",
                            result.Added.Select(Format))
                ) +
                "\r\n\r\n替换记录（按输入顺序，同一拼音＋位置以最后一条为准）：\r\n" +
                (
                    result.Replaced.Count == 0
                        ? "（无）"
                        : string.Join(
                            "\r\n",
                            result.Replaced.Select(
                                x =>
                                    $"第 {x.Line} 行：" +
                                    $"{string.Join(" / ", x.Before.Select(Format))} " +
                                    $"→ {Format(x.Entry)}"))
                ) +
                "\r\n\r\n未变化（拼音、位置及文本完全相同，跳过）：\r\n" +
                (
                    result.Duplicates.Count == 0
                        ? "（无）"
                        : string.Join(
                            "\r\n",
                            result.Duplicates.Select(
                                x =>
                                    $"第 {x.Line} 行：" +
                                    Format(x.Entry)))
                );

            Enabled = true;
            UseWaitCursor = false;

            using var dialog =
                new Form
                {
                    Text = "添加完成",
                    Font = Font,
                    StartPosition =
                        FormStartPosition.CenterParent,
                    Size = new Size(720, 500),
                    MinimumSize =
                        new Size(500, 320),
                    MinimizeBox = false,
                    MaximizeBox = false
                };

            var report =
                new TextBox
                {
                    Multiline = true,
                    ReadOnly = true,
                    ScrollBars =
                        ScrollBars.Both,
                    WordWrap = false,
                    Dock = DockStyle.Fill,
                    Text = details
                };

            var close =
                new Button
                {
                    Text = "确定",
                    Dock = DockStyle.Bottom,
                    Height = 42,
                    DialogResult =
                        DialogResult.OK
                };

            dialog.Controls.Add(report);
            dialog.Controls.Add(close);

            dialog.AcceptButton = close;
            dialog.CancelButton = close;

            report.Select(0, 0);

            dialog.ShowDialog(this);
        }
        catch (Exception ex)
        {
            status.Text =
                "添加失败，未完成保存。";

            MessageBox.Show(
                this,
                ex.Message,
                "添加失败",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            Enabled = true;
            UseWaitCursor = false;
        }
    }

    // ============================================================
    // 保存转换结果
    // ============================================================

    private async Task RunConversion(
        Func<(int Count,
            List<(string Path, byte[] Data)> Files)> prepare,
        bool rewritesSourceTxt = false,
        bool sortOnly = false)
    {
        try
        {
            var result =
                prepare();

            var existing =
                result.Files
                    .Where(x => File.Exists(x.Path))
                    .Select(x => x.Path)
                    .ToArray();

            var confirmation =
                sortOnly
                    ? "将按拼音 → 位置 → 输出文本升序排序，并写回原 TXT：\n\n" +
                      result.Files[0].Path +
                      "\n\n空行和注释将被移除，保存为 UTF-8。" +
                      "\n不会生成或修改 DAT。" +
                      "\n\n是否继续？选择“否”不会修改任何文件。"

                    : rewritesSourceTxt
                        ? "本次 TXT → DAT 将执行以下操作：\n\n" +
                          "1. 原 TXT 按升序排序后写回（不是只读取）：\n" +
                          result.Files[0].Path +
                          "\n空行和注释将被移除，保存为 UTF-8。\n\n" +
                          (
                              File.Exists(result.Files[1].Path)
                                  ? "2. 覆盖已有 DAT：\n"
                                  : "2. 生成新 DAT：\n"
                          ) +
                          result.Files[1].Path +
                          "\n\n是否继续？选择“否”不会修改任何文件。"

                        : "以下文件已存在，是否覆盖？\n\n" +
                          string.Join("\n", existing);

            if ((sortOnly ||
                 rewritesSourceTxt ||
                 existing.Length > 0) &&
                MessageBox.Show(
                    this,
                    confirmation,
                    sortOnly
                        ? "确认 TXT 排序"
                        : rewritesSourceTxt
                            ? "确认排序写回与转换"
                            : "确认覆盖",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2)
                != DialogResult.Yes)
            {
                return;
            }

            Enabled = false;
            UseWaitCursor = true;

            status.Text =
                "正在保存…";

            await Task.Run(
                () => OutputFiles.Save(result.Files));

            status.Text =
                $"已完成：{result.Count} 条短语，已按升序保存。" +
                (sortOnly ? "DAT 未修改。" : "");

            MessageBox.Show(
                this,
                status.Text +
                "\n\n" +
                string.Join(
                    "\n",
                    result.Files.Select(x => x.Path)),
                sortOnly
                    ? "排序完成"
                    : "转换完成",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            status.Text =
                sortOnly
                    ? "排序失败，请检查 TXT 文件。"
                    : "转换失败，请检查文件或生成位置。";

            MessageBox.Show(
                this,
                ex.Message,
                sortOnly
                    ? "排序失败"
                    : "转换失败",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            Enabled = true;
            UseWaitCursor = false;
        }
    }
}