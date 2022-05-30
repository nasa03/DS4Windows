﻿using System.Runtime.InteropServices;
using DS4Windows.Shared.Common.Util;
using Ds4Windows.Shared.Devices.Interfaces.HID;
using JetBrains.Annotations;
using PInvoke;

namespace DS4Windows.Shared.Devices.HID
{
    /// <summary>
    ///     Describes a HID device's basic properties.
    /// </summary>
    public class HidDevice : IEquatable<HidDevice>, IDisposable, IHidDevice
    {
        private readonly IntPtr inputOverlapped;

        private readonly ManualResetEvent inputReportEvent;

        public HidDevice()
        {
            inputReportEvent = new ManualResetEvent(false);
            inputOverlapped = Marshal.AllocHGlobal(Marshal.SizeOf<NativeOverlapped>());
            Marshal.StructureToPtr(
                new NativeOverlapped { EventHandle = inputReportEvent.SafeWaitHandle.DangerousGetHandle() },
                inputOverlapped, false);
        }

        /// <inheritdoc />
        public bool IsVirtual { get; set; }

        /// <summary>
        ///     Native handle to device.
        /// </summary>
        protected Kernel32.SafeObjectHandle Handle { get; private set; }

        /// <inheritdoc />
        public string InstanceId { get; set; }

        /// <inheritdoc />
        public string Path { get; set; }

        /// <inheritdoc />
        public string Description { get; set; }

        /// <inheritdoc />
        [CanBeNull]
        public string DisplayName { get; set; }

        /// <inheritdoc />
        public string ParentInstance { get; set; }

        /// <inheritdoc />
        public Hid.HiddAttributes Attributes { get; set; }

        /// <inheritdoc />
        public Hid.HidpCaps Capabilities { get; set; }

        /// <inheritdoc />
        public string ManufacturerString { get; set; }

        /// <inheritdoc />
        public string ProductString { get; set; }

        /// <inheritdoc />
        [CanBeNull]
        public string SerialNumberString { get; set; }

        /// <inheritdoc />
        public bool IsOpen => Handle is not null && !Handle.IsClosed && !Handle.IsInvalid;

        public virtual void Dispose()
        {
            Handle?.Dispose();
        }

        public bool Equals(HidDevice other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(InstanceId, other.InstanceId, StringComparison.OrdinalIgnoreCase);
        }

        [DllImport("hid.dll")]
        private static extern bool HidD_SetOutputReport(IntPtr hidDeviceObject, byte[] lpReportBuffer,
            int reportBufferLength);

        [DllImport("hid.dll")]
        private static extern bool HidD_SetFeature(IntPtr hidDeviceObject, byte[] lpReportBuffer,
            int reportBufferLength);

        [DllImport("hid.dll")]
        private static extern bool HidD_GetFeature(IntPtr hidDeviceObject, byte[] lpReportBuffer,
            int reportBufferLength);

        /// <inheritdoc />
        public void OpenDevice()
        {
            if (IsOpen)
                Handle.Close();

            Handle = OpenAsyncHandle(Path);
        }

        /// <inheritdoc />
        public void CloseDevice()
        {
            if (!IsOpen) return;

            Handle?.Dispose();
        }

        protected virtual bool WriteFeatureReport(byte[] data)
        {
            return HidD_SetFeature(Handle.DangerousGetHandle(), data, data.Length);
        }

        protected virtual bool WriteOutputReportViaControl(byte[] outputBuffer)
        {
            return HidD_SetOutputReport(Handle.DangerousGetHandle(), outputBuffer, outputBuffer.Length);
        }

        protected virtual bool ReadFeatureData(byte[] inputBuffer)
        {
            return HidD_GetFeature(Handle.DangerousGetHandle(), inputBuffer, inputBuffer.Length);
        }

        protected bool WriteOutputReportViaInterrupt(byte[] outputBuffer, int timeout)
        {
            var unmanagedBuffer = Marshal.AllocHGlobal(outputBuffer.Length);

            Marshal.Copy(outputBuffer, 0, unmanagedBuffer, outputBuffer.Length);

            try
            {
                Handle.OverlappedWriteFile(unmanagedBuffer, outputBuffer.Length, out _);
            }
            finally
            {
                Marshal.FreeHGlobal(unmanagedBuffer);
            }

            return true;
        }

        protected virtual void ReadInputReport(IntPtr inputBuffer, int bufferSize, out int bytesReturned)
        {
            if (inputBuffer == IntPtr.Zero)
                throw new ArgumentNullException(nameof(inputBuffer), @"Passed uninitialized memory");

            int? bytesRead = 0;

            Kernel32.ReadFile(
                Handle,
                inputBuffer,
                bufferSize,
                ref bytesRead,
                inputOverlapped);

            if (!Kernel32.GetOverlappedResult(Handle, inputOverlapped, out bytesReturned, true))
                throw new Win32Exception(Kernel32.GetLastError(), "Reading input report failed.");
        }

        private Kernel32.SafeObjectHandle OpenAsyncHandle(string devicePathName, bool openExclusive = false,
            bool enumerateOnly = false)
        {
            return Kernel32.CreateFile(devicePathName,
                enumerateOnly
                    ? 0
                    : Kernel32.ACCESS_MASK.GenericRight.GENERIC_READ | Kernel32.ACCESS_MASK.GenericRight.GENERIC_WRITE,
                Kernel32.FileShare.FILE_SHARE_READ | Kernel32.FileShare.FILE_SHARE_WRITE,
                IntPtr.Zero, openExclusive ? 0 : Kernel32.CreationDisposition.OPEN_EXISTING,
                Kernel32.CreateFileFlags.FILE_ATTRIBUTE_NORMAL
                | Kernel32.CreateFileFlags.FILE_FLAG_NO_BUFFERING
                | Kernel32.CreateFileFlags.FILE_FLAG_WRITE_THROUGH
                | Kernel32.CreateFileFlags.FILE_FLAG_OVERLAPPED,
                Kernel32.SafeObjectHandle.Null
            );
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is HidDevice other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(InstanceId);
        }

        public override string ToString()
        {
            return $"{DisplayName ?? "<no name>"} ({InstanceId})";
        }
    }
}