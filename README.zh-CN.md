# FreeMove

[English](README.md) · [简体中文](README.zh-CN.md)

[![license](https://img.shields.io/github/license/ImDema/FreeMove.svg)](https://github.com/imDema/FreeMove/blob/master/LICENSE.txt)

自由移动目录而不破坏安装或快捷方式

你可以使用此工具将默认安装在 `C:\` 的程序移动到另一个驱动器，以节省主盘空间

### 工作原理
1. 文件被移动到新位置
2. 从旧位置创建一个指向新位置的[符号链接](https://www.wikiwand.com/en/NTFS_junction_point)。任何尝试访问旧位置中文件的程序都会自动重定向到新的位置

## 下载
[![Github All Releases](https://img.shields.io/github/downloads/imDema/FreeMove/total.svg)](https://github.com/imDema/FreeMove/releases/latest)

#### 从 GitHub

[下载最新版本](https://github.com/imDema/FreeMove/releases/latest)

#### 从 Scoop

```
scoop install freemove
```

### 使用方法

运行可执行文件并使用图形界面

>[!NOTE]
>
> 此程序的核心功能需要管理员权限

## 建议
你不应该移动重要的系统目录，因为它们可能会破坏诸如 Windows Update 和 Windows Store 应用之类的核心功能。

`C:\Users` - `C:\Documents and Settings` - `C:\Program Files` - `C:\Program Files (x86)` 不应被移动。如果你仍然想移动，请自行承担风险。要将目录移回，请参阅 readme 的最后部分。

也就是说，移动前面提到的目录中所包含的子目录通常不会导致问题。所以你可以安全地移动 `C:\Program Files\HugeProgramIDontWantOnMySSD`。

## 屏幕截图
![Screenshot](https://imgur.com/CBEoA5J.png)

## 卸载已移动的程序
像平常一样卸载程序，不要删除联接点。卸载程序会正常工作，并留下一个空目录在你移动程序后的新位置，以及原位置的目录链接。你可以手动删除这些内容。

## 将程序移回原位
删除旧位置的联接点（这不会删除其下的文件或目录），然后将目录移动回其原始位置。

## 贡献

该项目目前仅维护修复，暂时没有新的功能在开发或计划中。

我独自编写了该工具，目前我是唯一的开发者，目前我在攻读博士学位，因其他项目繁忙无法开发新功能。

我会继续关注该项目并管理可能的贡献，如果你有兴趣贡献，请在 issue 中留言或在已有 issue 下评论。