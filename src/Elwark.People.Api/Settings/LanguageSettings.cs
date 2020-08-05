using System;
using System.Globalization;

namespace Elwark.People.Api.Settings
{
    public class LanguageSettings
    {
        public LanguageSettings()
        {
            Default = CultureInfo.CurrentCulture;
            Languages = Array.Empty<CultureInfo>();
            ParameterName = string.Empty;
        }

        public LanguageSettings(CultureInfo @default, CultureInfo[] languages, string parameterName)
        {
            Default = @default;
            Languages = languages;
            ParameterName = parameterName;
        }

        public CultureInfo Default { get; set; }
        public CultureInfo[] Languages { get; set; }
        public string ParameterName { get; set; }
    }
}