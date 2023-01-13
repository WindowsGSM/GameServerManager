using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;

namespace GameServerManager.GameServers.Components
{
    /// <summary>
    /// Provides a output redirection solution on srcds.exe
    /// </summary>
    public class SrcdsControl : IDisposable
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct SECURITY_ATTRIBUTES
        {
            public int nLength;
            public IntPtr lpSecurityDescriptor;
            public int bInheritHandle;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateEvent(SECURITY_ATTRIBUTES lpEventAttributes, bool bManualReset, bool bInitialState, string? lpName);

        private readonly MemoryMappedFile _mappedFile;
        private readonly ManualResetEvent _parent, _child;

        /// <summary>
        /// Console line width (Default: 80)
        /// </summary>
        public int ConsoleLineWidth { get; init; } = 80;

        /// <summary>
        /// Read buffer size (Default: 65536)
        /// </summary>
        public int BufferSize { get; init; } = 65536;

        /// <summary>
        /// Process of srcds.exe
        /// </summary>
        public Process Process { get; private set; }

        /// <summary>
        /// Provides a output redirection solution on srcds.exe
        /// </summary>
        public SrcdsControl(ProcessStartInfo processStartInfo)
        {
            _mappedFile = MemoryMappedFile.CreateNew(null, BufferSize, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.Inheritable);
            _parent = CreateManualResetEvent();
            _child = CreateManualResetEvent();

            IntPtr hFile = _mappedFile.SafeMemoryMappedFileHandle.DangerousGetHandle();
            IntPtr hParent = _parent.SafeWaitHandle.DangerousGetHandle();
            IntPtr hChild = _child.SafeWaitHandle.DangerousGetHandle();

            Process = new()
            {
                EnableRaisingEvents = true,
                StartInfo = new()
                {
                    FileName = processStartInfo.FileName,
                    Arguments = $"-HFILE {hFile} -HPARENT {hParent} -HCHILD {hChild} {processStartInfo.Arguments}"
                }
            };

            Process.Exited += (sender, e) => _child.Set();
        }

        /// <summary>
        /// Send command to srcds.exe
        /// </summary>
        /// <param name="data"></param>
        /// <returns><see langword="true"/> if success, else <see langword="false"/></returns>
        public bool Write(string data)
        {
            using MemoryMappedViewStream stream = _mappedFile.CreateViewStream();
            using BinaryWriter binaryWriter = new(stream);
            binaryWriter.Write(0x2);
            binaryWriter.Write(data);

            WaitHandle.SignalAndWait(_parent, _child);

            using MemoryMappedViewStream stream2 = _mappedFile.CreateViewStream();
            using BinaryReader binaryReader = new(stream2);

            return binaryReader.ReadBoolean();
        }

        /// <summary>
        /// Get srcds.exe screen buffer
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns>Screen buffer</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public string GetScreenBuffer(int start, int end)
        {
            SignalAndWait(new int[] { 0x3, start, end });

            using MemoryMappedViewStream stream = _mappedFile.CreateViewStream();
            using BinaryReader binaryReader = new(stream);

            if (!binaryReader.ReadBoolean())
            {
                throw new InvalidOperationException("Fail to GetScreenBuffer");
            }

            byte[] bytes = binaryReader.ReadBytes(BufferSize);

            return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Get srcds.exe screen buffer size
        /// </summary>
        /// <returns>Number of lines</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public int GetScreenBufferSize()
        {
            SignalAndWait(new int[] { 0x4 });

            int[] data = new int[2];
            using MemoryMappedViewStream stream = _mappedFile.CreateViewStream();
            Marshal.Copy(stream.SafeMemoryMappedViewHandle.DangerousGetHandle(), data, 0, data.Length);

            if (data[0] != 1)
            {
                throw new InvalidOperationException("Fail to GetScreenBufferSize");
            }

            return data[1];
        }

        /// <summary>
        /// Set srcds.exe screen buffer size
        /// </summary>
        /// <param name="size"></param>
        /// <returns><see langword="true"/> if success, else <see langword="false"/></returns>
        public bool SetScreenBufferSize(int size)
        {
            SignalAndWait(new int[] { 0x5, size });

            int[] data = new int[1];
            using MemoryMappedViewStream stream = _mappedFile.CreateViewStream();
            Marshal.Copy(stream.SafeMemoryMappedViewHandle.DangerousGetHandle(), data, 0, data.Length);

            return data[0] == 1;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _mappedFile?.Dispose();
            _parent?.Dispose();
            _child?.Dispose();

            if (Process != null && !Process.HasExited)
            {
                Process.Kill();
            }

            GC.SuppressFinalize(this);
        }

        private static ManualResetEvent CreateManualResetEvent()
        {
            SECURITY_ATTRIBUTES lpSecurityAttributes = new();
            lpSecurityAttributes.nLength = Marshal.SizeOf(lpSecurityAttributes);
            lpSecurityAttributes.lpSecurityDescriptor = IntPtr.Zero;
            lpSecurityAttributes.bInheritHandle = 1;

            ManualResetEvent manualResetEvent = new(false);
            manualResetEvent.SetSafeWaitHandle(new SafeWaitHandle(CreateEvent(lpSecurityAttributes, false, false, null), true));

            return manualResetEvent;
        }

        private void SignalAndWait(int[] parameters)
        {
            using MemoryMappedViewStream stream = _mappedFile.CreateViewStream();
            Marshal.Copy(parameters, 0, stream.SafeMemoryMappedViewHandle.DangerousGetHandle(), parameters.Length);

            WaitHandle.SignalAndWait(_parent, _child);
        }
    }
}
