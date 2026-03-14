using System;
using System.Collections.Generic;
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

        public static string GetLastErrorMessage()
        {
            try
            {
                uint error = Is64Bit ? Everything_GetLastError_64() : Everything_GetLastError_32();
                switch (error)
                {
                    case EVERYTHING_OK: return "OK";
                    case EVERYTHING_ERROR_MEMORY: return "Memory error";
                    case EVERYTHING_ERROR_IPC: return "Everything is not running";
                    case EVERYTHING_ERROR_REGISTERCLASSEX: return "Register window class error";
                    case EVERYTHING_ERROR_CREATEWINDOW: return "Create window error";
                    case EVERYTHING_ERROR_CREATETHREAD: return "Create thread error";
                    case EVERYTHING_ERROR_INVALIDINDEX: return "Invalid index";
                    case EVERYTHING_ERROR_INVALIDCALL: return "Invalid call";
                    default: return "Unknown error: " + error;
                }
            }
            catch (DllNotFoundException)
            {
                return "DLL not found";
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
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
