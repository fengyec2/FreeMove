// FreeMove -- Move directories without breaking shortcuts or installations 
//    Copyright(C) 2020  Luca De Martini

//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.

//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//    GNU General Public License for more details.

//    You should have received a copy of the GNU General Public License
//    along with this program.If not, see<http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FreeMove
{
    class IOHelper
    {
        public class ReadOnlyPrecheckException : Exception
        {
            public string FilePath { get; }

            public ReadOnlyPrecheckException(string filePath, Exception innerException)
                : base(string.Format(CultureInfo.CurrentUICulture, Properties.Resources.ResourceManager.GetString("ReadOnlyPrecheck_FileMessage"), filePath), innerException)
            {
                FilePath = filePath;
            }
        }

        #region SymLink
        //External dll functions
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, SymbolicLink dwFlags);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern IntPtr CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern uint GetFinalPathNameByHandle(IntPtr hFile, [Out] StringBuilder lpszFilePath, uint cchFilePath, uint dwFlags);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);

        const uint GENERIC_READ = 0x80000000;
        const uint FILE_SHARE_READ = 0x00000001;
        const uint OPEN_EXISTING = 3;
        const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;

        const uint VOLUME_NAME_DOS = 0x0;
        const uint FILE_NAME_NORMALIZED = 0x0;

        enum SymbolicLink
        {
            File = 0,
            Directory = 1
        }

        public static bool MakeLink(string directory, string symlink)
        {
            return CreateSymbolicLink(symlink, directory, SymbolicLink.Directory);
        }

        /// <summary>
        /// 判断路径是否为重解析点（符号链接或挂载点）
        /// </summary>
        public static bool IsReparsePoint(string path)
        {
            try
            {
                FileAttributes attributes = File.GetAttributes(path);
                return (attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取符号链接或挂载点的目标路径
        /// </summary>
        public static string GetSymbolicLinkTarget(string path)
        {
            // Use FILE_FLAG_BACKUP_SEMANTICS to open directory
            IntPtr handle = CreateFile(path, GENERIC_READ, FILE_SHARE_READ, IntPtr.Zero, OPEN_EXISTING, FILE_FLAG_BACKUP_SEMANTICS, IntPtr.Zero);
            if (handle == IntPtr.Zero || handle == new IntPtr(-1)) return null;

            try
            {
                StringBuilder sb = new StringBuilder(1024);
                uint res = GetFinalPathNameByHandle(handle, sb, (uint)sb.Capacity, VOLUME_NAME_DOS | FILE_NAME_NORMALIZED);
                if (res == 0) return null;

                string target = sb.ToString();

                // Normalize path: strip NT prefixes
                if (target.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase))
                    target = @"\\" + target.Substring(8);
                else if (target.StartsWith(@"\\?\"))
                    target = target.Substring(4);

                return target;
            }
            finally
            {
                CloseHandle(handle);
            }
        }
        #endregion

        public static IO.MoveOperation MoveDir(string source, string destination, bool createDestination)
        {
            return new IO.MoveOperation(source, destination, createDestination);
        }
        /// <summary>
        /// 根据当前界面状态和指定的权限检查级别执行移动前预检查。
        /// </summary>
        public static void CheckDirectories(string source, string destination, bool safeMode, bool createDestination, Settings.PermissionCheckLevel? permissionCheckLevelOverride = null)
        {
            List<Exception> exceptions = new List<Exception>();
            Settings.PermissionCheckLevel permissionCheckLevel = permissionCheckLevelOverride ?? Settings.PermCheck;
            //Check for correct file path format
            try
            {
                Path.GetFullPath(source);
                Path.GetFullPath(destination);
            }
            catch (Exception e)
            {
                exceptions.Add(new Exception(Properties.Resources.ResourceManager.GetString("Error_InvalidPath"), e));
            }
            string pattern = @"^[A-Za-z]:\\{1,2}";
            if (!Regex.IsMatch(source, pattern) || !Regex.IsMatch(destination, pattern))
            {
                exceptions.Add(new Exception(Properties.Resources.ResourceManager.GetString("Error_InvalidPathFormat")));
            }

            //Check if the chosen directory is blacklisted
            string windowsPath = Environment.GetEnvironmentVariable("WINDIR") ?? Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string system32Path = Environment.GetFolderPath(Environment.SpecialFolder.System);
            string sysWOW64Path = Path.Combine(windowsPath, "SysWOW64");
            string configPath = Path.Combine(windowsPath, "Config");
            string programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string drivePath = Path.GetPathRoot(windowsPath);
            string tempPath = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);
            string usersPath = Path.Combine(drivePath, "Users");
            string bootPath = Path.Combine(drivePath, "boot");
            string systemVolumeInfoPath = Path.Combine(drivePath, "System Volume Information");
            string recycleBinPath = Path.Combine(drivePath, "$Recycle.Bin");

            string normalizedSource = Path.GetFullPath(source).TrimEnd(Path.DirectorySeparatorChar).ToUpperInvariant();
            string[] Blacklist = { windowsPath, system32Path, sysWOW64Path, configPath, programDataPath, drivePath, tempPath, usersPath, bootPath, systemVolumeInfoPath, recycleBinPath };
            foreach (string item in Blacklist)
            {
                string normalizedItem = Path.GetFullPath(item).TrimEnd(Path.DirectorySeparatorChar).ToUpperInvariant();
                if (normalizedSource == normalizedItem)
                {
                    exceptions.Add(new Exception(string.Format(CultureInfo.CurrentUICulture, Properties.Resources.ResourceManager.GetString("Error_DirectoryCannotMove"), source)));
                }
            }

            //Check if folder is critical
            if (safeMode)
            {
                string programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                string programFilesX86Path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                string commonProgramFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles);
                string commonProgramFilesX86Path = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86);
                string userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

                string normalizedSourceForSafeMode = Path.GetFullPath(source).TrimEnd(Path.DirectorySeparatorChar).ToUpperInvariant();
                string[] safePaths = { programFilesPath, programFilesX86Path, commonProgramFilesPath, commonProgramFilesX86Path, userProfilePath, appDataPath, localAppDataPath };
                foreach (string item in safePaths)
                {
                    string normalizedSafePath = Path.GetFullPath(item).TrimEnd(Path.DirectorySeparatorChar).ToUpperInvariant();
                    if (normalizedSourceForSafeMode == normalizedSafePath)
                    {
                        exceptions.Add(new Exception(string.Format(CultureInfo.CurrentUICulture, Properties.Resources.ResourceManager.GetString("Error_SafeModeProgramFiles"), source)));
                        break;
                    }
                }
            }

            //Check for existence of directories
            if (!Directory.Exists(source))
                exceptions.Add(new Exception(Properties.Resources.ResourceManager.GetString("Error_SourceNotExist")));

            if (Directory.Exists(destination))
                exceptions.Add(new Exception(Properties.Resources.ResourceManager.GetString("Error_DestNameExists")));

            if (!createDestination && !Directory.Exists(Directory.GetParent(destination).FullName))
                exceptions.Add(new Exception(Properties.Resources.ResourceManager.GetString("Error_DestParentNotExist")));

            // Next checks rely on the previous so if there was any exception return
            if (exceptions.Count > 0)
                throw new AggregateException(exceptions);

            //Check admin privileges
            string TestFile = Path.Combine(Path.GetDirectoryName(source), "deleteme");
            int ti;
            for (ti = 0; File.Exists(TestFile + ti.ToString()); ti++) ; // Change name if a file with the same name already exists
            TestFile += ti.ToString();

            try
            {
                // DEPRECATED // System.Security.AccessControl.DirectorySecurity ds = Directory.GetAccessControl(source);
                //Try creating a file to check permissions
                File.Create(TestFile).Close();
            }
            catch (UnauthorizedAccessException e)
            {
                exceptions.Add(new Exception(Properties.Resources.ResourceManager.GetString("Error_NoPrivileges"), e));
            }
            finally
            {
                if (File.Exists(TestFile))
                    File.Delete(TestFile);
            }

            //Try creating a symbolic link to check permissions
            try
            {
                if (!CreateSymbolicLink(TestFile, Path.GetDirectoryName(destination), SymbolicLink.Directory))
                    exceptions.Add(new Exception(Properties.Resources.ResourceManager.GetString("Error_CannotCreateSymlinkTest")));
            }
            finally
            {
                if (Directory.Exists(TestFile))
                    Directory.Delete(TestFile);
            }

            // Next checks rely on the previous so if there was any exception return
            if (exceptions.Count > 0)
                throw new AggregateException(exceptions);

            long size = 0;
            DirectoryInfo dirInf = new DirectoryInfo(source);
            foreach (FileInfo file in dirInf.GetFiles("*", SearchOption.AllDirectories))
            {
                size += file.Length;
            }
            try
            {
                DriveInfo dstDrive = new(Path.GetPathRoot(destination));
                if (dstDrive.AvailableFreeSpace < size)
                    exceptions.Add(new Exception(string.Format(CultureInfo.CurrentUICulture, Properties.Resources.ResourceManager.GetString("Error_InsufficientDiskSpace"), dstDrive.Name, size / 1000000, dstDrive.AvailableFreeSpace / 1000000)));
            }
            catch (Exception e)
            {
                exceptions.Add(e);
            }

            if (exceptions.Count > 0)
                throw new AggregateException(exceptions);

            //If set to do full check try to open for write all files
            if (permissionCheckLevel != Settings.PermissionCheckLevel.None)
            {
                var exceptionBag = new ConcurrentBag<Exception>();
                Action<string> CheckFile = (file) =>
                {
                    Exception failure = TryCreateAccessCheckException(file);
                    if (failure != null)
                    {
                        exceptionBag.Add(failure);
                    }
                };
                if (permissionCheckLevel == Settings.PermissionCheckLevel.Fast)
                {
                    Parallel.ForEach(Directory.GetFiles(source, "*.exe", SearchOption.AllDirectories), CheckFile);
                    Parallel.ForEach(Directory.GetFiles(source, "*.dll", SearchOption.AllDirectories), CheckFile);
                }
                else
                {
                    Parallel.ForEach(Directory.GetFiles(source, "*", SearchOption.AllDirectories), CheckFile);
                }

                exceptions.AddRange(exceptionBag);
            }
            if (exceptions.Count > 0)
                throw new AggregateException(exceptions);
        }

        /// <summary>
        /// 判断本次预检查失败是否全部由只读属性引起。
        /// </summary>
        public static bool TryGetReadOnlyPrecheckFailures(AggregateException aggregateException, out List<ReadOnlyPrecheckException> readOnlyFailures)
        {
            readOnlyFailures = new List<ReadOnlyPrecheckException>();
            foreach (Exception exception in aggregateException.InnerExceptions)
            {
                if (exception is ReadOnlyPrecheckException readOnlyException)
                {
                    readOnlyFailures.Add(readOnlyException);
                    continue;
                }

                readOnlyFailures.Clear();
                return false;
            }

            return readOnlyFailures.Count > 0;
        }

        /// <summary>
        /// 递归清除目录树上的只读属性，并跳过重解析点。
        /// </summary>
        public static void ClearReadOnlyAttributes(string rootPath)
        {
            ClearReadOnlyAttribute(rootPath);

            foreach (string file in Directory.GetFiles(rootPath))
            {
                ClearReadOnlyAttribute(file);
            }

            foreach (string directory in Directory.GetDirectories(rootPath))
            {
                ClearReadOnlyAttribute(directory);
                if (IsReparsePoint(directory))
                {
                    continue;
                }

                ClearReadOnlyAttributes(directory);
            }
        }

        /// <summary>
        /// 清除单个路径上的只读属性，保留其他属性不变。
        /// </summary>
        private static void ClearReadOnlyAttribute(string path)
        {
            FileAttributes attributes = File.GetAttributes(path);
            if ((attributes & FileAttributes.ReadOnly) == 0)
            {
                return;
            }

            File.SetAttributes(path, attributes & ~FileAttributes.ReadOnly);
        }

        /// <summary>
        /// 将访问失败区分为只读失败和其他失败，供界面决定是否允许自动修复。
        /// </summary>
        private static Exception TryCreateAccessCheckException(string file)
        {
            FileInfo fileInfo = new FileInfo(file);
            FileStream fileStream = null;
            try
            {
                fileStream = fileInfo.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                return null;
            }
            catch (Exception ex)
            {
                if (IsReadOnlyOnlyFailure(fileInfo))
                {
                    return new ReadOnlyPrecheckException(file, ex);
                }

                return ex;
            }
            finally
            {
                if (fileStream != null)
                {
                    fileStream.Dispose();
                }
            }
        }

        /// <summary>
        /// 仅当独占只读打开成功、但读写打开失败时，才视为纯只读导致的失败。
        /// </summary>
        private static bool IsReadOnlyOnlyFailure(FileInfo fileInfo)
        {
            try
            {
                if ((fileInfo.Attributes & FileAttributes.ReadOnly) == 0)
                {
                    return false;
                }

                using FileStream fileStream = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.None);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
