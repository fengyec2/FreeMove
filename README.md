# FreeMove

[简体中文](README.zh-CN.md) · [English](README.md)

<img src="https://img.shields.io/github/license/fengyec2/FreeMove" alt="License"/> <img src="https://img.shields.io/github/v/release/fengyec2/FreeMove" alt="Version"/> <img src="https://img.shields.io/github/actions/workflow/status/fengyec2/FreeMove/build-release.yml" alt="Build"/> <img src="https://img.shields.io/github/downloads/fengyec2/FreeMove/total" alt="Downloads"/>

Move directories or files freely without breaking installations or shortcuts

You can use this tool to move programs that install on `C:\` by default to another drive to save space on your main drive

## How It works
1. The files are moved to the new location
2. A [symbolic link](https://www.wikiwand.com/en/NTFS_junction_point) is created from the old location redirecting to the new one. Any program trying to access a file in the old location will automatically be redirected to its new location

## Core Features
- [x] In-app directory browsing/operations
- [x] Multi-language support
- [x] Move directories or files
- [x] Automatically create target directories
- [x] Create symbolic links
- [x] Quickly restore symbolic links
- [x] Everything search integration
- [x] Context menu integration
- [x] Individual file symbolic links

## Download

### From GitHub

[![GitHub Release](https://img.shields.io/github/v/release/fengyec2/FreeMove?style=for-the-badge&logo=github)](https://github.com/fengyec2/FreeMove/releases)

**Which version should I download?**

| Version | Description | Requirement |
| :--- | :--- | :--- |
| **.NET 8 (Standard)** | Suitable for users with .NET 8 installed. Smallest size. | Requires [.NET 8 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) |
| **.NET 8 (SelfContained)** | Best for portability. Works without installing .NET. | None |
| **.NET 4.8** | Suitable for users who do not want to install .NET 8 or are using Windows 7/8 | Requires .NET Framework 4.8 (Built-in on Win10/11) |

*Note: Choose the architecture (`win-x64`, `win-x86`, or `win-arm64`) that matches your system.*

<!-- ### From Scoop

```
scoop install freemove
``` -->

## Usage

>[!NOTE]
>
> This program requires administrator privileges for its core functionality

Run the executable and use the GUI

### Command Line Arguments

| Argument | Description |
| :--- | :--- |
| `--unsafe-skip-checks` | Force disable all safety checks (not recommended) |
| `--source <source>` | Set the source directory to move from |
| `--destination <destination>` | Set the destination directory to move to |

## Recommendations

>[!WARNING]
>
> You should not move important system directories as they can break core functionalities like Windows Update and Windows Store Apps.

Important system directories such as:

- `C:\Users` 
- `C:\Documents and Settings`
- `C:\Program Files`
- `C:\Program Files (x86)` 

and so on should not be moved. If you wish to do It anyway do it at your own risk. To move a directory back refer to the FAQ part of the readme.

That said, moving directories **contained in** the previously mentioned directories should not cause any problem. So you are free to move `C:\Program Files\HugeProgramIDontWantOnMySSD` without any problem.

<details>
<summary>Safety Checks Range</summary>

#### System directories: cannot be moved

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

#### Important directories: cannot be moved in safe mode

- `C:\Program Files`
- `C:\Program Files (x86)`
- `C:\Program Files\Common Files`
- `C:\Program Files (x86)\Common Files`
- `%USERPROFILE%`
- `%APPDATA%`
- `%LOCALAPPDATA%`

</details>

## Screenshots

<img src="https://i.imgur.com/xvkVdc6.png" width="400" alt="Screenshot" />

## FAQ

### Q: How to uninstall moved programs?

A: Uninstall the program just as you would normally without deleting the junction. The uninstaller will work normally leaving an empty directory in the location you moved the program to, and the directory link in the original location, both of which you can then delete manually.

### Q: How to move a program back?


#### Using the GUI

Select a symbolic link and right click "Restore symbolic link".

#### Manual move

Delete the junction in the old location (this won't delete the content) and move the directory back to its original position.

### Q: How to create multiple symbolic links for the same file or folder?

A: Right-click the existing symbolic link in File Explorer and then select "Copy". Then paste paste it where you want.

### Q: Everything not found or DLL missing

A: The Everything search integration requires [Everything](https://www.voidtools.com/downloads/) (Install or Portable) to be installed and `Everything64.dll` or `Everything32.dll` (depending on your system architecture) to be either in the same directory as the FreeMove executable or in the system environment variables.

### Q: Everything not running

A: The Everything search integration requires the Everything program to be running. Please make sure you have.

## Contributing

The project is originally developed by [imDema](https://github.com/imDema). I am currently maintaining the project and am open to contributions. If you are interested in contributing, leave an issue or comment on an open issue and let me know!
