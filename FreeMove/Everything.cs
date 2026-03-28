using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace FreeMove
{
    public static class Everything
    {
        private const string DLL_NAME_64 = "Everything64.dll";
        private const string DLL_NAME_32 = "Everything.dll";

        private static bool Is64Bit => IntPtr.Size == 8;
        private static string DllName => Is64Bit ? DLL_NAME_64 : DLL_NAME_32;

        [DllImport(DLL_NAME_64, CharSet = CharSet.Unicode, EntryPoint = "Everything_SetSearchW")]
        private static extern uint Everything_SetSearchW_64(string lpSearchString);
        [DllImport(DLL_NAME_32, CharSet = CharSet.Unicode, EntryPoint = "Everything_SetSearchW")]
        private static extern uint Everything_SetSearchW_32(string lpSearchString);

        [DllImport(DLL_NAME_64, EntryPoint = "Everything_SetRequestFlags")]
        private static extern void Everything_SetRequestFlags_64(uint dwRequestFlags);
        [DllImport(DLL_NAME_32, EntryPoint = "Everything_SetRequestFlags")]
        private static extern void Everything_SetRequestFlags_32(uint dwRequestFlags);

        [DllImport(DLL_NAME_64, EntryPoint = "Everything_QueryW")]
        private static extern bool Everything_QueryW_64(bool bWait);
        [DllImport(DLL_NAME_32, EntryPoint = "Everything_QueryW")]
        private static extern bool Everything_QueryW_32(bool bWait);

        [DllImport(DLL_NAME_64, EntryPoint = "Everything_GetNumResults")]
        private static extern uint Everything_GetNumResults_64();
        [DllImport(DLL_NAME_32, EntryPoint = "Everything_GetNumResults")]
        private static extern uint Everything_GetNumResults_32();

        [DllImport(DLL_NAME_64, CharSet = CharSet.Unicode, EntryPoint = "Everything_GetResultFullPathNameW")]
        private static extern void Everything_GetResultFullPathNameW_64(uint nIndex, StringBuilder lpString, uint nMaxCount);
        [DllImport(DLL_NAME_32, CharSet = CharSet.Unicode, EntryPoint = "Everything_GetResultFullPathNameW")]
        private static extern void Everything_GetResultFullPathNameW_32(uint nIndex, StringBuilder lpString, uint nMaxCount);

        [DllImport(DLL_NAME_64, EntryPoint = "Everything_GetLastError")]
        private static extern uint Everything_GetLastError_64();
        [DllImport(DLL_NAME_32, EntryPoint = "Everything_GetLastError")]
        private static extern uint Everything_GetLastError_32();

        [DllImport(DLL_NAME_64, EntryPoint = "Everything_IsDBLoaded")]
        private static extern bool Everything_IsDBLoaded_64();
        [DllImport(DLL_NAME_32, EntryPoint = "Everything_IsDBLoaded")]
        private static extern bool Everything_IsDBLoaded_32();

        private const uint EVERYTHING_OK = 0;
        private const uint EVERYTHING_ERROR_MEMORY = 1;
        private const uint EVERYTHING_ERROR_IPC = 2;
        private const uint EVERYTHING_ERROR_REGISTERCLASSEX = 3;
        private const uint EVERYTHING_ERROR_CREATEWINDOW = 4;
        private const uint EVERYTHING_ERROR_CREATETHREAD = 5;
        private const uint EVERYTHING_ERROR_INVALIDINDEX = 6;
        private const uint EVERYTHING_ERROR_INVALIDCALL = 7;

        public enum EverythingFailureKind
        {
            None,
            DllNotFound,
            NotRunning,
            Other
        }

        public static bool IsAvailable()
        {
            try
            {
                if (Is64Bit)
                {
                    return Everything_IsDBLoaded_64();
                }
                else
                {
                    return Everything_IsDBLoaded_32();
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 区分 Everything 不可用的原因（未加载 DLL、进程未运行等），用于本地化提示。
        /// </summary>
        public static EverythingFailureKind GetAvailabilityFailureKind()
        {
            try
            {
                bool loaded = Is64Bit ? Everything_IsDBLoaded_64() : Everything_IsDBLoaded_32();
                if (loaded)
                    return EverythingFailureKind.None;
                uint err = Is64Bit ? Everything_GetLastError_64() : Everything_GetLastError_32();
                if (err == EVERYTHING_ERROR_IPC)
                    return EverythingFailureKind.NotRunning;
                return EverythingFailureKind.Other;
            }
            catch (DllNotFoundException)
            {
                return EverythingFailureKind.DllNotFound;
            }
            catch
            {
                return EverythingFailureKind.Other;
            }
        }

        /// <summary>
        /// 将 Everything 返回的错误码转为当前 UI 语言的说明（供搜索失败等提示使用）。
        /// </summary>
        public static string GetLocalizedLastErrorMessage()
        {
            try
            {
                uint error = Is64Bit ? Everything_GetLastError_64() : Everything_GetLastError_32();
                return MapErrorCodeToLocalizedString(error);
            }
            catch (DllNotFoundException)
            {
                return Properties.Resources.ResourceManager.GetString("Everything_NotInstalled");
            }
            catch (Exception ex)
            {
                return string.Format(CultureInfo.CurrentUICulture, Properties.Resources.ResourceManager.GetString("Everything_Err_Exception"), ex.Message);
            }
        }

        private static string MapErrorCodeToLocalizedString(uint error)
        {
            var rm = Properties.Resources.ResourceManager;
            switch (error)
            {
                case EVERYTHING_OK: return "OK";
                case EVERYTHING_ERROR_MEMORY: return rm.GetString("Everything_Err_Memory");
                case EVERYTHING_ERROR_IPC: return rm.GetString("Everything_NotRunning");
                case EVERYTHING_ERROR_REGISTERCLASSEX: return rm.GetString("Everything_Err_RegisterClass");
                case EVERYTHING_ERROR_CREATEWINDOW: return rm.GetString("Everything_Err_CreateWindow");
                case EVERYTHING_ERROR_CREATETHREAD: return rm.GetString("Everything_Err_CreateThread");
                case EVERYTHING_ERROR_INVALIDINDEX: return rm.GetString("Everything_Err_InvalidIndex");
                case EVERYTHING_ERROR_INVALIDCALL: return rm.GetString("Everything_Err_InvalidCall");
                default:
                    return string.Format(CultureInfo.CurrentUICulture, rm.GetString("Everything_Err_Unknown"), error);
            }
        }

        public static string GetLastErrorMessage()
        {
            return GetLocalizedLastErrorMessage();
        }

        private const uint EVERYTHING_REQUEST_FILE_NAME = 0x00000001;
        private const uint EVERYTHING_REQUEST_PATH = 0x00000002;
        private const uint EVERYTHING_REQUEST_FULL_PATH_AND_FILE_NAME = 0x00000004;

        public static IEnumerable<string> Search(string query, int maxResults = 100)
        {
            List<string> results = new List<string>();
            try
            {
                if (Is64Bit)
                {
                    Everything_SetSearchW_64(query);
                    Everything_SetRequestFlags_64(EVERYTHING_REQUEST_FULL_PATH_AND_FILE_NAME);
                    if (Everything_QueryW_64(true))
                    {
                        uint numResults = Everything_GetNumResults_64();
                        uint count = Math.Min(numResults, (uint)maxResults);
                        for (uint i = 0; i < count; i++)
                        {
                            StringBuilder sb = new StringBuilder(260);
                            Everything_GetResultFullPathNameW_64(i, sb, 260);
                            results.Add(sb.ToString());
                        }
                    }
                }
                else
                {
                    Everything_SetSearchW_32(query);
                    Everything_SetRequestFlags_32(EVERYTHING_REQUEST_FULL_PATH_AND_FILE_NAME);
                    if (Everything_QueryW_32(true))
                    {
                        uint numResults = Everything_GetNumResults_32();
                        uint count = Math.Min(numResults, (uint)maxResults);
                        for (uint i = 0; i < count; i++)
                        {
                            StringBuilder sb = new StringBuilder(260);
                            Everything_GetResultFullPathNameW_32(i, sb, 260);
                            results.Add(sb.ToString());
                        }
                    }
                }
            }
            catch (DllNotFoundException)
            {
                // Everything not installed or DLL not found
            }
            return results;
        }
    }
}
