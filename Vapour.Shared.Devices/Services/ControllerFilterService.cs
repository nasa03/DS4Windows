﻿using System.Security;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Logging;
using Nefarius.Drivers.Identinator;
using Nefarius.Utilities.DeviceManagement.Drivers;
using Nefarius.Utilities.DeviceManagement.Extensions;
using Nefarius.Utilities.DeviceManagement.PnP;

namespace Vapour.Shared.Devices.Services;
public class ControllerFilterService : IControllerFilterService
{
    /// <summary>
    ///     Regex to strip out version value from INF file.
    /// </summary>
    private static readonly Regex DriverVersionRegex =
        new(@"^DriverVer *=.*,(\d*\.\d*\.\d*\.\d*)", RegexOptions.Multiline);

    private readonly FilterDriver _filterDriver;
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly ILogger<ControllerFilterService> _logger;

    public ControllerFilterService(ILogger<ControllerFilterService> logger)
    {
        _logger = logger;

        try
        {
            _filterDriver = new FilterDriver();
        }
        catch (SecurityException ex)
        {
            _logger.LogError(ex, "To use the rewrite feature, the service must be run as Administrator!");
            throw;
        }
    }

    /// <inheritdoc />
    public bool GetFilterDriverInstalled()
    {
        return !string.IsNullOrEmpty(GetLocalDriverVersion());
    }

    /// <inheritdoc />
    public bool GetFilterDriverEnabled()
    {
        return _filterDriver.IsEnabled;
    }

    /// <inheritdoc />
    public void SetFilterDriverEnabled(bool isEnabled)
    {
        _filterDriver.IsEnabled = isEnabled;
    }

    /// <inheritdoc />
    public void FilterController(string instanceId)
    {
        Tuple<PnPDevice, string> device = GetDeviceToFilter(instanceId);

        //TODO: filter the controller and cycle the port

        RewriteEntry entry = _filterDriver.AddOrUpdateRewriteEntry(device.Item2);
        entry.IsReplacingEnabled = true;
        entry.CompatibleIds = new[]
        {
            @"USB\MS_COMP_WINUSB", @"USB\Class_FF&SubClass_5D&Prot_01", @"USB\Class_FF&SubClass_5D", @"USB\Class_FF"
        };
        entry.Dispose();

        device.Item1.ToUsbPnPDevice().CyclePort();
    }

    /// <inheritdoc />
    public void UnfilterController(string instanceId)
    {
        Tuple<PnPDevice, string> device = GetDeviceToFilter(instanceId);

        //TODO: fill in the unfilter

        RewriteEntry entry = _filterDriver.GetRewriteEntryFor(device.Item2);
        if (entry != null)
        {
            entry.IsReplacingEnabled = false;
            entry.Dispose();
        }
    }

    private static string? GetLocalDriverVersion()
    {
        return DriverStore.ExistingDrivers
            .Where(s => s.Contains("nssidswap", StringComparison.OrdinalIgnoreCase))
            .Select(d => GetInfDriverVersion(File.ReadAllText(d)))
            .MaxBy(k => k)?.ToString();
    }

    /// <summary>
    ///     Extracts the driver version from an INF file.
    /// </summary>
    /// <param name="infContent">The string content of the INF file.</param>
    /// <returns>The detected <see cref="Version" />.</returns>
    private static Version GetInfDriverVersion(string infContent)
    {
        var match = DriverVersionRegex.Match(infContent);

        return Version.Parse(match.Groups[1].Value);
    }

    private Tuple<PnPDevice, string> GetDeviceToFilter(string instanceId)
    {
        PnPDevice device = PnPDevice.GetDeviceByInstanceId(instanceId);
        string[] hardwareIds = device.GetProperty<string[]>(DevicePropertyKey.Device_HardwareIds);
        if (hardwareIds[0].StartsWith("HID"))
        {
            string parentInputDeviceId = device.GetProperty<string>(DevicePropertyKey.Device_Parent);
            device = PnPDevice.GetDeviceByInstanceId(parentInputDeviceId);
            hardwareIds = device.GetProperty<string[]>(DevicePropertyKey.Device_HardwareIds);
        }

        return new Tuple<PnPDevice, string>(device, hardwareIds[0]);
    }
}
