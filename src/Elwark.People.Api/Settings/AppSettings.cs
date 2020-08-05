namespace Elwark.People.Api.Settings
{
    public class AppSettings
    {
        public AppSettings()
        {
            Key = string.Empty;
            Iv = string.Empty;
        }

        public AppSettings(string key, string iv)
        {
            Key = key;
            Iv = iv;
        }

        public string Key { get; set; }

        public string Iv { get; set; }
    }
}