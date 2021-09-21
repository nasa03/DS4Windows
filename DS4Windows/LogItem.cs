﻿using System;

namespace DS4WinWPF
{
    public class LogItem
    {
        public DateTime Datetime { get; set; }

        public string Message { get; set; }

        public bool Warning { get; set; }

        public string Color => Warning ? "Red" : "Black";
    }
}