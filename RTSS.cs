// This code was translated from python using AI

using System;
using System.Runtime.InteropServices;

namespace TempAndFanServer
{
    public class RTSS : IDisposable
    {
        private const uint FILE_MAP_READ = 0x0004;
        private const string MappingName = "RTSSSharedMemoryV2";

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct RTSS_SHARED_MEMORY
        {
            public uint dwSignature;
            public uint dwVersion;
            public uint dwAppEntrySize;
            public uint dwAppArrOffset;
            public uint dwAppArrSize;
            public uint dwOSDEntrySize;
            public uint dwOSDArrOffset;
            public uint dwOSDArrSize;
            public uint dwOSDFrame;
            public int dwBusy;
            public uint dwDesktopVideoCaptureFlags;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
            public uint[] dwDesktopVideoCaptureStat;

            public uint dwLastForegroundApp;
            public uint dwLastForegroundAppProcessID;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        private struct RTSS_APP_ENTRY
        {
            public uint dwProcessID;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szName;

            public uint dwFlags;
            public uint dwTime0;
            public uint dwTime1;
            public uint dwFrames;
            public uint dwFrameTime;
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr OpenFileMapping(
            uint dwDesiredAccess,
            bool bInheritHandle,
            string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr MapViewOfFile(
            IntPtr hFileMappingObject,
            uint dwDesiredAccess,
            uint dwFileOffsetHigh,
            uint dwFileOffsetLow,
            UIntPtr dwNumberOfBytesToMap);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        private IntPtr _hMap = IntPtr.Zero;
        private IntPtr _ptr = IntPtr.Zero;
        private RTSS_SHARED_MEMORY _shared;
        private uint _lastFrames;
        private int _staleCount;

        private RTSS() { }

        public static bool Initialize(out RTSS? rtss)
        {
            rtss = null;

            try
            {
                var instance = new RTSS();

                if (!instance.Open())
                    return false;

                rtss = instance;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool Open()
        {
            _hMap = OpenFileMapping(FILE_MAP_READ, false, MappingName);

            if (_hMap == IntPtr.Zero)
                return false;

            _ptr = MapViewOfFile(
                _hMap,
                FILE_MAP_READ,
                0,
                0,
                UIntPtr.Zero);

            if (_ptr == IntPtr.Zero)
            {
                CloseHandle(_hMap);
                _hMap = IntPtr.Zero;
                return false;
            }

            _shared = Marshal.PtrToStructure<RTSS_SHARED_MEMORY>(_ptr);

            if (_shared.dwSignature != 0x52545353) // "SSTR"
            {
                Dispose();
                return false;
            }

            return true;
        }

        public float GetFPS()
        {
            if (_ptr == IntPtr.Zero)
                return 0.0f;

            try
            {
                _shared = Marshal.PtrToStructure<RTSS_SHARED_MEMORY>(_ptr);

                if (_shared.dwBusy != 0)
                    return 0.0f;

                uint index = _shared.dwLastForegroundApp;

                if (index >= _shared.dwAppArrSize)
                    return 0.0f;

                IntPtr entryPtr = IntPtr.Add(
                    _ptr,
                    checked((int)(_shared.dwAppArrOffset +
                                  index * _shared.dwAppEntrySize)));

                var app = Marshal.PtrToStructure<RTSS_APP_ENTRY>(entryPtr);

                if (app.dwProcessID == 0)
                    return 0.0f;

                if (app.dwFrames == _lastFrames)
                {
                    _staleCount++;

                    if (_staleCount == 10)
                        return 0.0f;
                }
                else
                {
                    _staleCount = 0;
                    _lastFrames = app.dwFrames;
                }

                if (app.dwFrameTime == 0)
                    return 0.0f;

                uint elapsed = app.dwTime1 - app.dwTime0;

                if (elapsed == 0)
                    return 0.0f;

                return 1000.0f * app.dwFrames / elapsed;
            }
            catch
            {
                return 0.0f;
            }
        }

        public void Dispose()
        {
            if (_ptr != IntPtr.Zero)
            {
                UnmapViewOfFile(_ptr);
                _ptr = IntPtr.Zero;
            }

            if (_hMap != IntPtr.Zero)
            {
                CloseHandle(_hMap);
                _hMap = IntPtr.Zero;
            }
        }
    }
}