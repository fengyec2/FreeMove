# FreeMove

[English](README.md) · [简体中文](README.zh-CN.md)


<img src="https://img.shields.io/github/license/fengyec2/FreeMove" alt="License"/> <img src="https://img.shields.io/github/v/release/fengyec2/FreeMove" alt="Version"/> <img src="https://img.shields.io/github/actions/workflow/status/fengyec2/FreeMove/build-release.yml" alt="Build"/> <img src="https://img.shields.io/github/downloads/fengyec2/FreeMove/total" alt="Downloads"/>

自由移动目录而不破坏安装或快捷方式

你可以使用此工具将默认安装在 `C:\` 的程序移动到另一个驱动器，以节省主盘空间

## 工作原理
1. 文件被移动到新位置
2. 从旧位置创建一个指向新位置的[符号链接](https://www.wikiwand.com/en/NTFS_junction_point)。任何尝试访问旧位置中文件的程序都会自动重定向到新的位置

## 核心功能

- [x] 移动目录或文件
- [x] 创建符号链接
- [x] 快速恢复符号链接
- [x] Everything 集成

## 下载

### 从 GitHub

[下载最新版本](https://github.com/fengyec2/FreeMove/releases/latest)

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

运行可执行文件并使用图形界面

>[!NOTE]
>
> 此程序的核心功能需要管理员权限

## 建议

>[!WARNING]
>
> 你不应该移动重要的系统目录，因为它们可能会破坏诸如 Windows Update 和 Windows Store 应用之类的核心功能。

`C:\Users` - `C:\Documents and Settings` - `C:\Program Files` - `C:\Program Files (x86)` 不应被移动。如果你仍然想移动，请自行承担风险。要将目录移回，请参阅 readme 的最后部分。

也就是说，移动前面提到的目录中所包含的子目录通常不会导致问题。所以你可以安全地移动 `C:\Program Files\HugeProgramIDontWantOnMySSD`。

## 屏幕截图
![Screenshot](https://i.imgur.com/vD1jCux.png)

## 卸载已移动的程序
像平常一样卸载程序，不要删除联接点。卸载程序会正常工作，并留下一个空目录在你移动程序后的新位置，以及原位置的目录链接。你可以手动删除这些内容。

## 将程序移回原位

### 使用 GUI

选择一个符号链接，右键点击“恢复符号链接”

### 手动移动
删除旧位置的联接点（这不会删除其下的文件或目录），然后将目录移动回其原始位置。

## 贡献

该项目最初由 [imDema](https://github.com/imDema) 开发。我目前维护该项目并欢迎贡献。如果你有兴趣贡献，请在 issue 中留言或在已有 issue 下评论。
