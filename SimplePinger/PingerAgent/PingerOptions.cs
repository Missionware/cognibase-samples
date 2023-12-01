using Missionware.ConfigLib.Attributes;
using Missionware.SharedLib.Config;

namespace PingerAgent
{
    [SettingsSection(Description = @"Contains settings for the pinger.")]
    public class PingerOptions : SettingsSectionBase
    {
        /// <summary>
        ///     Determines the retention duration of the ping measurements in hours
        ///     <para>Default is: 720 (one month) </para>
        /// </summary>
        [Setting(DefaultValue = 720,
            Description = @"Determines the retention duration of the ping measurements in hours")]
        public double RetentionHours { get => getter<double>(); set => setter(value); }
    }
}