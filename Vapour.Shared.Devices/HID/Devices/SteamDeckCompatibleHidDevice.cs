﻿using Microsoft.Extensions.Logging;

using Vapour.Shared.Devices.HID.DeviceInfos;
using Vapour.Shared.Devices.HID.Devices.Reports;
using Vapour.Shared.Devices.HID.InputTypes.SteamDeck.Feature;
using Vapour.Shared.Devices.HID.InputTypes.SteamDeck.In;

namespace Vapour.Shared.Devices.HID.Devices;

public class SteamDeckCompatibleHidDevice : CompatibleHidDevice
{
    public SteamDeckCompatibleHidDevice(ILogger<SteamDeckCompatibleHidDevice> logger, List<DeviceInfo> deviceInfos)
        : base(logger, deviceInfos)
    {
    }

    public override InputSourceReport InputSourceReport { get; } = new SteamDeckCompatibleInputReport();

    protected override Type InputDeviceType => typeof(SteamDeckDeviceInfo);

    protected override void OnInitialize()
    {
        Serial = ReadSerial(FeatureConstants.SerialFeatureId);

        if (Serial is null)
        {
            throw new ArgumentException("Could not retrieve a valid serial number.");
        }

        Logger.LogInformation("Got serial {Serial} for {Device}", Serial, this);
    }

    public override void ProcessInputReport(ReadOnlySpan<byte> input)
    {
        if (input[InConstants.ReportIdIndex] == InConstants.ReportId)
        {
            InputSourceReport.Parse(input);
        }
    }
}
