﻿using System.Net.NetworkInformation;

using Windows.Win32.Foundation;

using Microsoft.Extensions.Logging;

using Nefarius.Utilities.Bluetooth;
using Nefarius.Utilities.Bluetooth.SDP;
using Nefarius.Utilities.DeviceManagement.PnP;

using Vapour.Shared.Common.Util;
using Vapour.Shared.Devices.HID;

namespace Vapour.Shared.Devices.Services.Configuration;

public partial class FilterService
{
    /// <summary>
    ///     The DEVPKEY_Bluetooth_DeviceAddress which stores the remote device address as hex string.
    /// </summary>
    private static DevicePropertyKey BluetoothDeviceAddressProperty => CustomDeviceProperty.CreateCustomDeviceProperty(
        Guid.Parse("{0x2bd67d8b, 0x8beb, 0x48d5, {0x87, 0xe0, 0x6c, 0xda, 0x34, 0x28, 0x04, 0x0a}}"), 1,
        typeof(string));

    public bool IsBtFiltered(string instanceId)
    {
        BthPortDevice bthDevice = GetBthDevice(instanceId);

        _logger.LogInformation("Check for bth device with {Address} patched is {isPatched}",
            bthDevice.RemoteAddress.ToFriendlyName(), bthDevice.IsCachedServicesPatched);

        return bthDevice.IsCachedServicesPatched;
    }

    private void FilterBtController(ICompatibleHidDevice device, CancellationToken ct = default)
    {
        device.Close();
        BthPortDevice bthDevice = GetBthDevice(device.SourceDevice.InstanceId, out PnPDevice parentDevice);

        if (bthDevice.IsCachedServicesPatched)
        {
            _logger.LogWarning("Device {Address} is already patched, nothing to do",
                bthDevice.RemoteAddress.ToFriendlyName());
            return;
        }

        _logger.LogInformation("Performing filter of bluetooth device {Address}",
            bthDevice.RemoteAddress.ToFriendlyName());

        if (!SdpPatcher.AlterHidDeviceToVenderDefined(bthDevice.CachedServices, out byte[] patched))
        {
            throw new FilterServiceException(
                $"Failed to patch device with address {bthDevice.RemoteAddress.ToFriendlyName()}.");
        }

        // overwrite patched record
        bthDevice.CachedServices = patched;

        ResetDevice(bthDevice, parentDevice, ct);
    }

    /// <summary>
    ///     Reverts the patched SDP records of a given wireless device to original state and optionally restarts the Bluetooth
    ///     host radio.
    /// </summary>
    /// <param name="device">The Compatible HID device connected via Bluetooth.</param>
    /// <param name="ct">Optional cancellation token.</param>
    private void UnfilterBtController(ICompatibleHidDevice device, CancellationToken ct = default)
    {
        device.Close();
        BthPortDevice bthDevice = GetBthDevice(device.SourceDevice.InstanceId, out PnPDevice parentDevice);

        if (!bthDevice.IsCachedServicesPatched)
        {
            _logger.LogWarning("Device {Address} is not patched, nothing to do",
                bthDevice.RemoteAddress.ToFriendlyName());
            return;
        }

        _logger.LogInformation("Performing unfilter of bluetooth device {Address}",
            bthDevice.RemoteAddress.ToFriendlyName());

        bthDevice.CachedServices = bthDevice.OriginalCachedServices;
        bthDevice.DeleteOriginalCachedServices();

        ResetDevice(bthDevice, parentDevice, ct);
    }

    private static void ResetDevice(BthPortDevice bthDevice, PnPDevice parentDevice, CancellationToken ct)
    {
        try
        {
            // disconnect device
            using HostRadio radio = new();
            radio.DisconnectRemoteDevice(bthDevice.RemoteAddress);
        }
        catch (HostRadioException hre)
        {
            if (hre.NativeErrorCode != (uint)WIN32_ERROR.ERROR_DEVICE_NOT_CONNECTED)
            {
                throw;
            }
        }

        // disable in a separate task and continue
        _ = Task.Run(parentDevice.Disable, ct);

        //int maxRetries = 5;

        //while (!ct.IsCancellationRequested && maxRetries-- > 0)
        //{
        //    // enforces reloading patched records from registry
        //    try
        //    {
        //        _logger.LogInformation("attemping to disable parent device");
        //        parentDevice.Disable();
        //        Thread.Sleep(1000);
        //        _logger.LogInformation("attempting to re enable parent device");
        //        parentDevice.Enable();

        //        break;
        //    }
        //    catch (ConfigManagerException cme)
        //    {
        //        if (cme.Value != (uint)CONFIGRET.CR_REMOVE_VETOED)
        //        {
        //            // unexpected error
        //            throw;
        //        }

        //        Thread.Sleep(1000);
        //    }
        //}
    }

    private static BthPortDevice GetBthDevice(string instanceId)
    {
        return GetBthDevice(instanceId, out _);
    }

    private static BthPortDevice GetBthDevice(string instanceId, out PnPDevice parent)
    {
        PnPDevice hidDevice = PnPDevice.GetDeviceByInstanceId(instanceId);

        string parentId = hidDevice.GetProperty<string>(DevicePropertyKey.Device_Parent);

        PnPDevice parentDevice = PnPDevice.GetDeviceByInstanceId(parentId);

        PhysicalAddress remoteAddress =
            PhysicalAddress.Parse(parentDevice.GetProperty<string>(BluetoothDeviceAddressProperty));

        BthPortDevice bthDevice = BthPort.Devices.FirstOrDefault(d => d.RemoteAddress.Equals(remoteAddress));

        if (bthDevice is null)
        {
            throw new FilterServiceException(
                $"Wireless device with address {remoteAddress.ToFriendlyName()} not found.");
        }

        parent = parentDevice;

        return bthDevice;
    }
}
