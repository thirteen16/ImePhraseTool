# 微软拼音短语工具

用于 Windows 10 / 11 的微软拼音自定义短语管理工具，支持 TXT 与 DAT 双向转换、TXT 排序以及批量添加 / 更新短语。

使用 .NET 8 WinForms 开发。

## 使用方法

程序顶部设置三个项目：

- **目录**：TXT 和 DAT 文件所在目录，默认使用当前用户桌面。
- **TXT**：TXT 文件名，默认为 `IME_phrases.txt`。
- **DAT**：微软拼音短语文件名，默认为 `UserDefinedPhrase.dat`。

输入文件需要位于指定目录中，并与界面设置的文件名一致。

### 功能

| 操作 | 功能 |
| --- | --- |
| TXT → DAT | 读取 TXT 并生成 DAT，同时将排序后的内容写回 TXT。 |
| DAT → TXT | 读取 DAT 并生成排序后的 TXT。 |
| 仅排序 TXT | 对 TXT 内容排序并写回，不修改 DAT。 |
| 导入微软拼音 | 复制当前目录并打开 Windows 微软拼音“用户定义的短语”页面。 |
| 添加 | 批量新增或更新短语，并同步保存 TXT 和 DAT。 |
| 定位文件 | 复制对应文件名输入框的内容，并在资源管理器中定位文件；文件不存在时打开所在目录。 |
| 恢复默认 | 恢复目录、TXT 文件名、DAT 文件名和短语输入框字号的默认设置。 |

## 添加短语

在“短语”输入框中每行输入一条：

```text
拼音,位置,输出文本
```

例如：

```text
nihao,1,你好
zaijian,1,再见
api,1,api
api,2,API
```

其中：

- **拼音**：1～32 个小写英文字母，支持简拼。
- **位置**：1～9，表示候选位置。
- **输出文本**：1～64 个 UTF-16 代码单元。

程序以 **拼音 + 位置** 作为匹配键：

- 不存在：新增。
- 拼音和位置相同、文本不同：替换。
- 三项完全相同：跳过。
- 同一批输入中存在相同键时，以最后一条为准。

添加完成后会显示新增、替换和未变化的处理结果。

## TXT 格式

TXT 每行格式：

```text
拼音,位置,输出文本
```

例如：

```text
api,1,api
api,2,API
nihao,1,你好
```

支持读取：

```text
UTF-8
UTF-8 BOM
UTF-16 LE BOM
UTF-16 BE BOM
```

写回 TXT 时统一使用：

```text
UTF-8（无 BOM）
```

排序顺序为：

```text
拼音 → 位置 → 输出文本
```

排序或转换写回时会移除空行和 `#` 注释行。

## DAT

程序支持微软拼音 `mschxudp` 版本 1 的普通短语 DAT 格式。

生成 DAT 后，可以点击“导入微软拼音”打开 Windows 的“用户定义的短语”页面，然后点击系统的“导入”按钮选择 DAT 文件。

不支持其他输入法词库、损坏文件或不支持的 DAT 格式。

## 短语输入框字号

程序界面字号为 12 pt，“短语”输入框默认字号为 13 pt。

当鼠标位于“短语”输入框内时，可以使用：

```text
Ctrl + 鼠标滚轮
```

调整输入框文字字号，范围为：

```text
8 ～ 24 pt
```

该操作只改变“短语”输入框中的文字，不改变其他界面元素。

字号和其他设置会自动保存到 EXE 同目录的 `settings.json`。

## 配置

`settings.json` 保存当前设置以及“恢复默认”使用的默认设置。

示例：

```json
{
  "OutputFolder": "C:\\Users\\用户名\\Desktop",
  "TxtName": "IME_phrases.txt",
  "DatName": "UserDefinedPhrase.dat",
  "QuickPhraseFontSize": 13,
  "DefaultOutputFolder": "C:\\Users\\用户名\\Desktop",
  "DefaultTxtName": "IME_phrases.txt",
  "DefaultDatName": "UserDefinedPhrase.dat",
  "DefaultQuickPhraseFontSize": 13
}
```

如果 `settings.json` 不存在，程序会使用内置默认值：

- 目录：当前用户桌面
- TXT：`IME_phrases.txt`
- DAT：`UserDefinedPhrase.dat`
- 短语输入框字号：13 pt

---

# 开发

## 环境

需要：

- Windows 10 / 11
- .NET 8 或更高版本的 SDK

也可以使用 Visual Studio 2022，并安装 **.NET 桌面开发**工作负载。

## 从源码运行

克隆项目：

```powershell
git clone https://github.com/thirteen16/ImePhraseTool.git
cd ImePhraseTool
```

运行：

```powershell
dotnet run --project ImePhraseTool.csproj
```

也可以直接使用 Visual Studio 打开：

```text
ImePhraseTool.sln
```

## 发布

在项目根目录执行：

```powershell
powershell -ExecutionPolicy Bypass -File .\publish.ps1
```

默认发布 Windows x64 版本：

```text
publish/
└── 微软拼音短语工具.exe
```

发布方式为**框架依赖单文件**，因此目标电脑需要安装对应架构的 **.NET 8 Desktop Runtime**。

发布 ARM64：

```powershell
powershell -ExecutionPolicy Bypass -File .\publish.ps1 -Runtime win-arm64
```

发布 x86：

```powershell
powershell -ExecutionPolicy Bypass -File .\publish.ps1 -Runtime win-x86
```

## 项目结构

```text
ImePhraseTool/
│
├── ImePhraseTool.csproj
├── ImePhraseTool.sln
│
├── Program.cs
├── MainForm.cs
├── PhraseConverter.cs
├── BatchPhraseAdder.cs
├── OutputFiles.cs
├── UserSettings.cs
│
├── publish.ps1
├── README.md
├── LICENSE.txt
│
└── assets/
```

| 文件 | 作用 |
| --- | --- |
| `ImePhraseTool.csproj` | .NET 项目配置 |
| `ImePhraseTool.sln` | Visual Studio 解决方案 |
| `Program.cs` | 程序入口 |
| `MainForm.cs` | WinForms 主界面及界面操作 |
| `PhraseConverter.cs` | TXT / DAT 读取、校验和转换 |
| `BatchPhraseAdder.cs` | 批量新增和替换短语 |
| `OutputFiles.cs` | TXT / DAT 文件保存及失败回滚 |
| `UserSettings.cs` | `settings.json` 配置读取和保存 |
| `publish.ps1` | 单文件 EXE 发布脚本 |
| `assets/` | 程序图标及相关资源 |

正常编译不需要重新生成图标。

## 许可证

本项目使用 GNU AGPL-3.0 许可证，详见 `LICENSE.txt`。