﻿namespace Vapour.Shared.Devices.HID.InputTypes.SteamDeck;

[Flags]
public enum SteamDeckButtons0 : UInt16
{
    L5 = 0b1000000000000000,
    Options = 0b0100000000000000,
    Steam = 0b0010000000000000,
    Menu = 0b0001000000000000,
    DpadDown = 0b0000100000000000,
    DpadLeft = 0b0000010000000000,
    DpadRight = 0b0000001000000000,
    DpadUp = 0b0000000100000000,
    A = 0b0000000010000000,
    X = 0b0000000001000000,
    B = 0b0000000000100000,
    Y = 0b0000000000010000,
    L1 = 0b0000000000001000,
    R1 = 0b0000000000000100,
    L2 = 0b0000000000000010,
    R2 = 0b0000000000000001,
}

internal enum SteamDeckButtonsStick : byte
{
    LeftStick = 0b01000000,
    RightStick = 0b00000100
}