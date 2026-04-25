# FreeMove

[English](README.md) · [简体中文](README.zh-CN.md)


<img src="https://img.shields.io/github/license/fengyec2/FreeMove" alt="License"/> <img src="https://img.shields.io/github/v/release/fengyec2/FreeMove" alt="Version"/> <img src="https://img.shields.io/github/actions/workflow/status/fengyec2/FreeMove/build-release.yml" alt="Build"/> <img src="https://img.shields.io/github/downloads/fengyec2/FreeMove/total" alt="Downloads"/>

自由移动目录或文件而不破坏安装或快捷方式

你可以使用此工具将默认安装在 `C:\` 的程序移动到另一个驱动器，以节省主盘空间

## 工作原理
1. 文件被移动到新位置
2. 从旧位置创建一个指向新位置的[符号链接](https://www.wikiwand.com/en/NTFS_junction_point)。任何尝试访问旧位置中文件的程序都会自动重定向到新的位置

## 核心功能

- [x] 应用内目录浏览/操作
- [x] 多语言支持
- [x] 移动目录或文件
- [x] 自动创建目标目录
- [x] 创建符号链接
- [x] 快速恢复符号链接
- [x] Everything 搜索集成
- [x] 上下文菜单集成
- [x] 单个文件符号链接

## 下载

### 从 GitHub

[![GitHub Release](https://img.shields.io/github/v/release/fengyec2/FreeMove?style=for-the-badge&logo=github)](https://github.com/fengyec2/FreeMove/releases)

**我该下载哪个版本？**

| 版本 | 说明 | 运行要求 |
| :--- | :--- | :--- |
| **.NET 8 (标准版)** | 适合已安装 .NET 8 的用户，体积最小 | 需要安装 [.NET 8 运行时](https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0) |
| **.NET 8 (自包含版)** | 适合便携使用，无需安装 .NET 环境即可运行 | 无 |
| **.NET 4.8** | 适用于不希望安装 .NET 8 或使用 Windows 7/8 系统的用户 | 需要 .NET Framework 4.8（Win10/11 已内置） |

*注：请根据你的系统架构（`win-x64`、`win-x86` 或 `win-arm64`）选择对应的下载包。*

<!-- ### 从 Scoop

```
scoop install freemove
``` -->

## 使用方法

>[!NOTE]
>
> 此程序的核心功能需要管理员权限

运行可执行文件并使用图形界面

### 命令行参数

| 参数 | 说明 |
| :--- | :--- |
| `--unsafe-skip-checks` | 强制禁用所有安全检查（不推荐） |
| `--source <source>` | 设置要移动的源目录 |
| `--destination <destination>` | 设置要移动到的目标目录 |

## 建议

>[!WARNING]
>
> 你不应该移动重要的系统目录，因为它们可能会破坏诸如 Windows Update 和 Windows Store 应用之类的核心功能。

重要的系统目录，例如：

- `C:\Users` 
- `C:\Documents and Settings`
- `C:\Program Files`
- `C:\Program Files (x86)` 

等不应被移动。如果你仍然想移动，请自行承担风险。要将目录移回，请参阅 readme 的 FAQ 部分。

也就是说，移动前面提到的目录中所包含的**子目录**通常不会导致问题。所以你可以安全地移动 `C:\Program Files\HugeProgramIDontWantOnMySSD`。

<details>
<summary>安全模式检查范围</summary>

#### 系统目录：无法进行移动

- `C:\Windows`
- `C:\Windows\System32`
- `C:\Windows\SysWOW64`
- `C:\Windows\Config`
- `C:\ProgramData`
- `C:\`
- `%TEMP%`
- `C:\Users`
- `C:\boot`
- `C:\System Volume Information`
- `C:\$Recycle.Bin`

#### 重要目录：无法在安全模式下进行移动

- `C:\Program Files`
- `C:\Program Files (x86)`
- `C:\Program Files\Common Files`
- `C:\Program Files (x86)\Common Files`
- `%USERPROFILE%`
- `%APPDATA%`
- `%LOCALAPPDATA%`

</details>


## 屏幕截图
<img src="https://i.imgur.com/vD1jCux.png" width="400" alt="Screenshot" />

## 常见问题

### Q：如何卸载已移动的程序

A：像平常一样卸载程序，不要删除联接点。卸载程序会正常工作，并留下一个空目录在你移动程序后的新位置，以及原位置的目录链接。你可以手动删除这些内容。

### Q：如何将程序移回原位

#### 使用 GUI

选择一个符号链接，右键点击“恢复符号链接”。

#### 手动移动

删除旧位置的联接点（这不会删除其下的文件或目录），然后将目录移动回其原始位置。

### Q：如何为同一个文件（夹）创建多个符号链接

A：直接在文件资源管理器中右键复制已经创建的符号链接，即可创建多个符号链接。

### Q：未找到 Everything 或 DLL 丢失

A：Everything 搜索集成需要安装 [Everything](https://www.voidtools.com/downloads/)（安装版或便携版），并且 `Everything64.dll` 或 `Everything32.dll`（取决于你的系统架构）必须位于 FreeMove 可执行文件所在的目录或系统环境变量中。

### Q：Everything 未运行

A：Everything 搜索集成功能需要 Everything 程序正在运行。请确保你已经启动了 Everything。

## 贡献

该项目最初由 [imDema](https://github.com/imDema) 开发。我目前维护该项目并欢迎贡献。如果你有兴趣贡献，请在 issue 中留言或在已有 issue 下评论。
