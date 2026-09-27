# 微软拼音短语工具

Windows 10/11，.NET 8 WinForms。

## 功能

- TXT → 微软拼音 `.dat`
- 微软拼音 `.dat` → TXT
- 快速输入 `txt,1,txt`
- TXT 不存在自动创建
- TXT 存在自动追加
- 追加后立即重新生成 DAT
- UTF-16LE / `mschxudp` DAT 格式

TXT：

```text
api,1,api
api,2,API
exe,1,exe
exe,2,EXE
```

## 发布

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

输出目录：`publish\`

## 格式依据

微软拼音 DAT 使用 `mschxudp` 文件头、0x40 字节文件头、偏移表以及 UTF-16LE 词条结构。
