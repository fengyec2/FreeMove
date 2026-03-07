# FreeMove

[简体中文](README.zh-CN.md) · [English](README.md)

[![license](https://img.shields.io/github/license/fengyec2/FreeMove.svg)](https://github.com/fengyec2/FreeMove/blob/master/LICENSE.txt)

Move directories freely without breaking installations or shortcuts

You can use this tool to move programs that install on C:\ by default to another drive to save space on your main drive

### How It works
1. The files are moved to the new location
2. A [symbolic link](https://www.wikiwand.com/en/NTFS_junction_point) is created from the old location redirecting to the new one. Any program trying to access a file in the old location will automatically be redirected to its new location

## Download
[![Github All Releases](https://img.shields.io/github/downloads/fengyec2/FreeMove/total.svg)](https://github.com/fengyec2/FreeMove/releases/latest)

#### From GitHub

[Download the latest release](https://github.com/fengyec2/FreeMove/releases/latest)

**Which version should I download?**

| Version | Description | Requirement |
| :--- | :--- | :--- |
| **.NET 10 (Standard)** | Recommended for most users. Smallest size. | Requires [.NET 10 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) |
| **.NET 10 (SelfContained)** | Best for portability. Works without installing .NET. | None |
| **.NET 4.8** | For legacy Windows versions (e.g., Windows 7/8). | Requires .NET Framework 4.8 (Built-in on Win10/11) |

*Note: Choose the architecture (`win-x64`, `win-x86`, or `win-arm64`) that matches your system.*

<!-- #### From Scoop

```
scoop install freemove
``` -->

### Usage

Run the executable and use the GUI

>[!NOTE]
>
> This program requires administrator privileges for its core functionality

## Recommendations
You should not move important system directories as they can break core functionalities like Windows Update and Windows Store Apps.

`C:\Users` - `C:\Documents and Settings` - `C:\Program Files` - `C:\Program Files (x86)` should not be moved. If you wish to do It anyway do it at your own risk. To move a directory back refer to the last part of the readme.

That said, moving directories contained in the previously mentioned directories should not cause any problem. So you are free to move `C:\Program Files\HugeProgramIDontWantOnMySSD` without any problem.

## Screenshots
![Screenshot](https://imgur.com/3J8gXpE.png)

## Uninstalling moved programs
Uninstall the program just as you would normally without deleting the junction. The uninstaller will work normally leaving an empty directory in the location you moved the program to, and the directory link in the original location, both of which you can then delete manually

## Moving back a program
Delete the junction in the old location (this won't delete the content) and move the directory back to its original position

## Contributing

The project is originally developed by [imDema](https://github.com/imDema). I am currently maintaining the project and am open to contributions. If you are interested in contributing, leave an issue or comment on an open issue and let me know!
