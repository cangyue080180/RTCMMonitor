using System.Configuration;

namespace RTCMClient.helper
{
    public class AppConfigHelper
    {
        public static string JieShouJiCom = "JieShouJiCom";
        public static string JieShouJiBaudrate = "JieShouJiBaudrate";
        public static string DianTaiCom = "DianTaiCom";
        public static string DianTaiBaudrate = "DianTaiBaudrate";
        public static string IpAddress = "IpAddress";
        public static string Port = "Port";
        public static string BaseLongitude = "BaseLongitude";
        public static string BaseLatitude = "BaseLatitude";
        public static string ErrorCount = "ErrorCount";

        public static string GetAppConfig(string key)
        {
            return ConfigurationManager.AppSettings[key];
        }

        public static void UpdateAppConfig(string newKey, string newValue)
        {
            bool isModified = false;
            foreach (string key in ConfigurationManager.AppSettings)
            {
                if (key == newKey)
                {
                    isModified = true;
                }
            }

            // Open App.Config of executable
            Configuration config =
                ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            // You need to remove the old settings object before you can replace it
            if (isModified)
            {
                config.AppSettings.Settings.Remove(newKey);
            }
            // Add an Application Setting.
            config.AppSettings.Settings.Add(newKey, newValue);
            // Save the changes in App.config file.
            config.Save(ConfigurationSaveMode.Modified);
            // Force a reload of a changed section.
            ConfigurationManager.RefreshSection("appSettings");
        }
    }
}