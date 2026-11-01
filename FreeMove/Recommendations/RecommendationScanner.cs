// FreeMove -- Move directories without breaking shortcuts or installations
//    Copyright(C) 2020  Luca De Martini
//
//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FreeMove.Recommendations
{
    public enum RecommendationKind
    {
        Program,
        AppData,
        UserFolder,
        Other
    }

    /// <summary>
    /// A folder on the system drive that is large enough and safe enough to suggest moving.
    /// </summary>
    public class MoveRecommendation
    {
        public string SourcePath { get; set; }
        public long SizeBytes { get; set; }
        public RecommendationKind Kind { get; set; }

        /// <summary>
        /// Where the folder will go: the destination root plus the source path without its drive letter,
        /// e.g. C:\Program Files\Foo -> D:\FreeMove\Program Files\Foo. Mirroring the path keeps
        /// same-named folders (AppData\Local\Foo vs AppData\Roaming\Foo) from colliding.
        /// </summary>
        public string GetDestination(string destinationRoot)
        {
            string root = Path.GetPathRoot(SourcePath);
            string relative = SourcePath.Substring(root.Length).TrimStart('\\');
            return Path.Combine(destinationRoot, relative);
        }
    }

    /// <summary>
    /// Finds the largest folders on the Windows drive that FreeMove can move and link back.
    /// Only well-known locations are scanned and system, driver and security folders are skipped,
    /// so every suggestion is something that normally keeps working through a symbolic link.
    /// </summary>
    public static class RecommendationScanner
    {
        // Children of Program Files that belong to Windows, drivers or security software.
        static readonly HashSet<string> ProgramFilesSkip = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Common Files", "WindowsApps", "ModifiableWindowsApps", "Windows Defender",
            "Windows Defender Advanced Threat Protection", "Windows Mail", "Windows Media Player",
            "Windows Multimedia Platform", "Windows NT", "Windows Photo Viewer", "Windows Portable Devices",
            "Windows Security", "Windows Sidebar", "WindowsPowerShell", "Internet Explorer",
            "Microsoft Update Health Tools", "Reference Assemblies", "MSBuild", "dotnet",
            "Uninstall Information", "Microsoft.NET", "PackageManagement", "Microsoft Office",
            "Microsoft OneDrive", "NVIDIA Corporation", "Intel", "AMD", "Realtek", "Synaptics",
            "Microsoft Analysis Services", "Microsoft SQL Server", "IIS", "IIS Express"
        };

        // Children of AppData\Local and AppData\Roaming that Windows itself depends on.
        static readonly HashSet<string> AppDataSkip = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Microsoft", "Packages", "Temp", "ConnectedDevicesPlatform", "D3DSCache", "CrashDumps",
            "Comms", "History", "Programs", "PlaceholderTileLogoFolder", "Publishers", "VirtualStore",
            "Application Data", "Temporary Internet Files", "Microsoft_Corporation"
        };

        // Top-level folders of the system drive that must stay where they are.
        static readonly HashSet<string> RootSkip = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Windows", "Program Files", "Program Files (x86)", "ProgramData", "Users", "$Recycle.Bin",
            "System Volume Information", "Recovery", "PerfLogs", "boot", "$WinREAgent", "$Windows.~BT",
            "$Windows.~WS", "$SysReset", "$GetCurrent", "Config.Msi", "Documents and Settings",
            "OneDriveTemp", "XboxGames", "Intel", "AMD", "NVIDIA", "Drivers", "MSOCache", "inetpub"
        };

        /// <summary>
        /// Scans the system drive and returns folders of at least <paramref name="minimumBytes"/>,
        /// largest first.
        /// </summary>
        public static Task<List<MoveRecommendation>> ScanAsync(long minimumBytes, IProgress<string> progress, CancellationToken token)
        {
            return Task.Run(() =>
            {
                List<MoveRecommendation> candidates = GetCandidates();
                var results = new ConcurrentBag<MoveRecommendation>();
                var options = new ParallelOptions { CancellationToken = token, MaxDegreeOfParallelism = 4 };

                Parallel.ForEach(candidates, options, candidate =>
                {
                    progress?.Report(candidate.SourcePath);
                    candidate.SizeBytes = GetDirectorySize(candidate.SourcePath, token);
                    if (candidate.SizeBytes >= minimumBytes)
                        results.Add(candidate);
                });

                token.ThrowIfCancellationRequested();
                return results.OrderByDescending(r => r.SizeBytes).ToList();
            }, token);
        }

        /// <summary>
        /// The drive Windows is installed on, e.g. "C:\".
        /// </summary>
        public static string SystemDrive
        {
            get
            {
                string windowsPath = Environment.GetEnvironmentVariable("WINDIR") ?? Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                return Path.GetPathRoot(windowsPath);
            }
        }

        /// <summary>
        /// Suggests a destination root: D:\FreeMove when D: is a fixed drive, otherwise the fixed
        /// non-system drive with the most free space. Returns null when there is none.
        /// </summary>
        public static string SuggestDestinationRoot()
        {
            try
            {
                List<DriveInfo> drives = DriveInfo.GetDrives()
                    .Where(d => d.IsReady && d.DriveType == DriveType.Fixed &&
                                !string.Equals(d.Name, SystemDrive, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                DriveInfo pick = drives.FirstOrDefault(d => d.Name.StartsWith("D", StringComparison.OrdinalIgnoreCase))
                                 ?? drives.OrderByDescending(d => d.AvailableFreeSpace).FirstOrDefault();
                return pick == null ? null : Path.Combine(pick.Name, "FreeMove");
            }
            catch
            {
                return null;
            }
        }

        static List<MoveRecommendation> GetCandidates()
        {
            var list = new List<MoveRecommendation>();
            string systemDrive = SystemDrive;

            void AddChildren(string parent, RecommendationKind kind, HashSet<string> skip)
            {
                if (string.IsNullOrEmpty(parent) || !Directory.Exists(parent)) return;
                if (!IsOnDrive(parent, systemDrive)) return;
                string[] children;
                try { children = Directory.GetDirectories(parent); }
                catch { return; }

                foreach (string child in children)
                {
                    string name = Path.GetFileName(child);
                    if (skip != null && skip.Contains(name)) continue;
                    if (name.StartsWith("Windows ", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!IsMovable(child)) continue;
                    list.Add(new MoveRecommendation { SourcePath = child, Kind = kind });
                }
            }

            void AddSingle(string path, RecommendationKind kind)
            {
                if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return;
                if (!IsOnDrive(path, systemDrive) || !IsMovable(path)) return;
                list.Add(new MoveRecommendation { SourcePath = path, Kind = kind });
            }

            // Installed programs
            AddChildren(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), RecommendationKind.Program, ProgramFilesSkip);
            AddChildren(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), RecommendationKind.Program, ProgramFilesSkip);
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            AddChildren(Path.Combine(localAppData, "Programs"), RecommendationKind.Program, null);

            // Per-user app data (caches, game data, SDKs, ...)
            AddChildren(localAppData, RecommendationKind.AppData, AppDataSkip);
            AddChildren(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), RecommendationKind.AppData, AppDataSkip);

            // Large personal folders. Documents and Desktop are left out because OneDrive
            // backup often manages them; those are better moved through their Location tab.
            string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            AddSingle(Path.Combine(profile, "Downloads"), RecommendationKind.UserFolder);
            AddSingle(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), RecommendationKind.UserFolder);
            AddSingle(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), RecommendationKind.UserFolder);
            AddSingle(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), RecommendationKind.UserFolder);

            // Folders people created directly on the system drive, e.g. C:\Games
            AddChildren(systemDrive, RecommendationKind.Other, RootSkip);

            // Drop duplicates (e.g. a known folder redirected inside another candidate)
            return list
                .GroupBy(c => c.SourcePath.TrimEnd('\\'), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();
        }

        static bool IsOnDrive(string path, string drive)
        {
            return string.Equals(Path.GetPathRoot(Path.GetFullPath(path)), drive, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Rejects folders that are already links (already moved), hidden system folders,
        /// and anything inside a OneDrive folder.
        /// </summary>
        static bool IsMovable(string path)
        {
            try
            {
                FileAttributes attributes = File.GetAttributes(path);
                if ((attributes & FileAttributes.ReparsePoint) != 0) return false;
                if ((attributes & FileAttributes.System) != 0 && (attributes & FileAttributes.Hidden) != 0) return false;
                if (path.IndexOf("\\OneDrive", StringComparison.OrdinalIgnoreCase) >= 0) return false;
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Total size of all files under <paramref name="path"/>. Linked subfolders are not
        /// followed (their data lives elsewhere) and unreadable folders are skipped.
        /// </summary>
        public static long GetDirectorySize(string path, CancellationToken token)
        {
            long total = 0;
            var pending = new Stack<string>();
            pending.Push(path);

            while (pending.Count > 0)
            {
                token.ThrowIfCancellationRequested();
                string current = pending.Pop();
                DirectoryInfo dir = new DirectoryInfo(current);

                try
                {
                    foreach (FileInfo file in dir.EnumerateFiles())
                    {
                        try { total += file.Length; } catch { }
                    }
                    foreach (DirectoryInfo sub in dir.EnumerateDirectories())
                    {
                        try
                        {
                            if ((sub.Attributes & FileAttributes.ReparsePoint) == 0)
                                pending.Push(sub.FullName);
                        }
                        catch { }
                    }
                }
                catch (UnauthorizedAccessException) { }
                catch (IOException) { }
                catch (System.Security.SecurityException) { }
            }

            return total;
        }

        public static string FormatSize(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double value = bytes;
            int unit = 0;
            while (value >= 1024 && unit < units.Length - 1)
            {
                value /= 1024;
                unit++;
            }
            return unit == 0 ? $"{bytes} B" : $"{value:0.0} {units[unit]}";
        }
    }
}
