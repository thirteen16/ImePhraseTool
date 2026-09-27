# 微软拼音短语工具

用于 Windows 10 / 11 的微软拼音自定义短语管理工具，支持 TXT 与 DAT 双向转换、排序和批量添加 / 更新。采用 .NET 8 WinForms，支持窗口比例布局和高 DPI 缩放。

## **从源码运行**

需要 Windows 电脑和 **.NET 8 或更高版本的 SDK**（仅安装运行时不足以编译）。首次构建需要联网还原依赖。

```powershell
git clone https://github.com/thirteen16/ImePhraseTool.git
cd ImePhraseTool
dotnet run --project ImePhraseTool.csproj
```

也可以使用 Visual Studio 2022（安装“.NET 桌面开发”工作负载）打开 `ImePhraseTool.sln`。

## **生成独立 exe**

在项目根目录执行：

```powershell
powershell -ExecutionPolicy Bypass -File .\publish.ps1
```

输出文件：

```text
publish/
└── 微软拼音短语工具.exe
```

默认生成 **Windows x64** 单文件程序，包含 .NET 运行库，使用者无需另外安装 .NET。直接双击 exe 即可。

ARM64 或 x86 电脑可指定对应架构；每次发布都会更新同一个 `publish` 目录：

```powershell
powershell -ExecutionPolicy Bypass -File .\publish.ps1 -Runtime win-arm64
powershell -ExecutionPolicy Bypass -File .\publish.ps1 -Runtime win-x86
```

`publish/`、编译缓存和个人配置均不提交到 Git。clone 后请按上述步骤运行或构建；若要分享免安装程序，可将 exe 上传至 GitHub Releases，不要提交到源码仓库。

## **使用方法**

顶部设置统一用于输入和输出：

- 目录默认是当前用户桌面。
- TXT 文件名默认是 `IME_phrases.txt`。
- DAT 文件名默认是 `UserDefinedPhrase.dat`。
- 输入文件需放在指定目录下，文件名应与界面中的设置一致。

| 操作         | 行为                                                         |
| ------------ | ------------------------------------------------------------ |
| TXT → DAT    | 校验 TXT，生成 DAT，同时将排序后的内容写回原 TXT；执行前提示确认。 |
| DAT → TXT    | 校验 DAT，输出升序排列的 TXT；已有 TXT 会提示覆盖。          |
| 仅排序 TXT   | 只对 TXT 排序并写回，不修改 DAT；执行前提示确认。            |
| 导入微软拼音 | 复制顶部指定的目录路径，并打开 Windows 的“用户定义的短语”页面。 |
| 添加         | 每行输入一条短语，合并后排序保存 TXT 和 DAT，显示新增、替换和未变化的明细。 |
| 定位文件     | 在资源管理器中选中文件；文件不存在时打开对应目录，目录不存在则创建。 |
| 恢复默认     | 将目录、TXT 文件名、DAT 文件名和短语输入框字号恢复为 `settings.json` 中对应的默认值；不删除词库文件。 |

### **设置与字号**

程序普通界面字号固定为 **12 磅**。

“短语”输入框的文字字号默认为 **13 磅**。当鼠标位于“短语”输入框内时，可以按住 `Ctrl` 滚动鼠标滚轮调整输入框文字字号：

- 每格调整 1 磅。
- 最小 8 磅。
- 最大 24 磅。
- 只改变“短语”输入框内的文字。
- 不改变其他界面文字字号。
- 不改变输入框高度、按钮大小或窗口大小。

调整后的字号会保存到 `settings.json` 的：

```text
QuickPhraseFontSize
```

下次启动时自动恢复。

### **settings.json**

程序配置保存在 exe 同级目录的：

```text
settings.json
```

如果 `settings.json` 不存在，程序仍可正常启动，并使用程序内置默认值。

配置包含 **4 个当前值 + 4 个默认值，共 8 项**：

```json
{
  "OutputFolder": "D:\\win11\\LittleTools\\ImePhraseTool",
  "TxtName": "IME_phrases.txt",
  "DatName": "UserDefinedPhrase.dat",
  "QuickPhraseFontSize": 13,
  "DefaultOutputFolder": "C:\\Users\\用户名\\Desktop",
  "DefaultTxtName": "IME_phrases.txt",
  "DefaultDatName": "UserDefinedPhrase.dat",
  "DefaultQuickPhraseFontSize": 13
}
```

其中：

| 当前值                | 对应默认值                   | 程序内置默认值          |
| --------------------- | ---------------------------- | ----------------------- |
| `OutputFolder`        | `DefaultOutputFolder`        | 当前用户桌面            |
| `TxtName`             | `DefaultTxtName`             | `IME_phrases.txt`       |
| `DatName`             | `DefaultDatName`             | `UserDefinedPhrase.dat` |
| `QuickPhraseFontSize` | `DefaultQuickPhraseFontSize` | `13`                    |

配置读取优先级为：

```text
当前值
  ↓ 不存在或无效
对应的 Default 值
  ↓ 不存在或无效
程序内置默认值
```

例如：

```json
{
  "DefaultQuickPhraseFontSize": 15
}
```

如果没有设置 `QuickPhraseFontSize`，短语输入框会使用 `DefaultQuickPhraseFontSize`，即 **15 磅**。

点击“恢复默认”时，四个当前值会恢复为对应的：

```text
DefaultOutputFolder
DefaultTxtName
DefaultDatName
DefaultQuickPhraseFontSize
```

如果对应的默认值不存在或无效，则使用程序内置默认值。

因此可以直接编辑 `settings.json` 来修改程序的默认设置。

程序保存配置后会补全并保存完整的 8 个配置项。

移动 exe 时，如果希望保留当前设置和自定义默认值，可以将 `settings.json` 一并移动。

### **添加与更新规则**

- “短语”输入框支持多行输入和粘贴。
- 输入框文字默认字号为 13 磅，可通过 `Ctrl + 滚轮` 单独调整。
- **拼音＋位置**是匹配键：键不存在则新增；键相同、文本不同则替换；三项都相同则跳过。
- 同一批输入中，相同键以最后一条为准。
- 合并时 TXT 的文本优先于 DAT 中的旧文本，DAT 独有的键也会保留。
- 合并结果写入两个文件，不弹出覆盖确认；完成后显示处理明细。
- 全部未变化时不修改文件；任意输入行格式错误时整批停止并提示行号。

## **TXT 格式**

每行使用英文逗号分隔：

```text
拼音,位置,输出文本
```

例如：

```text
api,1,api
api,2,API
nihao,1,你好
```

- **拼音**：1～32 个小写英文字母，支持简拼。
- **位置**：1～9 的整数，表示候选位置，不是任意词频数值。
- **输出文本**：1～64 个 UTF-16 代码单元，不能全为空白或包含控制字符；表情可能占两个代码单元。文本中的后续逗号及首尾空格会保留。
- **编码**：读取 UTF-8（有无 BOM 均可）和带 BOM 的 UTF-16 LE/BE；输出为无 BOM 的 UTF-8、Windows 换行。
- **排序**：拼音 → 位置数字 → 输出文本，均为升序，文本使用 Ordinal 比较。

**TXT 排序或转换写回时，会移除空行和 `#` 注释行并统一编码。**

普通转换和排序保留重复词条；添加 / 更新按前述匹配键处理。

## **DAT 支持范围**

支持 `mschxudp` 文件头、版本 1、`0x00100010` 普通短语结构。

其他输入法词库、动态格式化短语、空词库、损坏或不支持的版本会报错。

生成的 DAT 可在微软拼音设置中的“用户定义的短语”页面手动导入；本软件提供直达该页面的按钮。

### **通过 Windows 导入微软拼音**

1. 先生成需要导入的 DAT 文件。
2. 点击“导入微软拼音”，程序将顶部的**目录路径**复制到剪贴板，然后打开 Windows 的“用户定义的短语”页面。目录留空时使用桌面目录。
3. 在系统页面点击“导入”，在文件选择窗口的地址栏粘贴目录路径，选择 DAT 文件并完成导入。

按钮只复制目录和打开页面，不读取或校验 DAT，也不自动操作系统窗口或修改系统词库。导入结果以 Windows 提示为准。

直达地址：

```text
ms-settings:regionlanguage-chsime-pinyin-udp
```

## 项目结构

```text
ImePhraseTool.csproj / .sln  项目与解决方案
Program.cs                  程序入口
MainForm.cs                 桌面界面
PhraseConverter.cs          TXT / DAT 校验与转换
BatchPhraseAdder.cs         批量添加与替换
OutputFiles.cs              暂存、保存与失败回滚
UserSettings.cs             配置读取与保存
assets/                     软件图标及生成脚本
publish.ps1                 独立 exe 发布脚本
```

图标已包含在源码中，正常构建无需重新生成。需要调整图标时，可使用 PowerShell 7 运行：

```powershell
assets/CreateIcon.ps1
```

## 参考与许可证

格式实现结合项目原有 DAT 样本核对，参考：

- UserDefinedPhraser
- MsPinyinDat

本项目使用 [GNU AGPL-3.0](LICENSE.txt) 许可证。
