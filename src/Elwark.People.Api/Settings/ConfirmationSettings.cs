using System;

namespace Elwark.People.Api.Settings
{
    public class ConfirmationSettings
    {
        public ConfirmationCodeRangeOptions CodeRange { get; set; } = null!;
        public ConfirmationOptions Code { get; set; } = null!;

        public ConfirmationOptions Link { get; set; } = null!;
    }

    public class ConfirmationOptions
    {
        public TimeSpan Delay { get; set; }

        public TimeSpan Lifetime { get; set; }
    }

    public class ConfirmationCodeRangeOptions
    {
        public int Min { get; set; } = 1000;

        public int Max { get; set; } = 9999;
    }
}