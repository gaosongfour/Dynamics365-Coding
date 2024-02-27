using System.IO;
using System.Text.Json;
using System.Configuration;

namespace Crm.Common.CrmService
{
    public class CrmConfigHelper
    {
        /// <summary>
        /// Get Crm Config  from Json Config File
        /// </summary>
        /// <param name="configPath">json config file path, by default, getting from app settings["crmConfigPath"]</param>
        /// <returns></returns>
        public static CrmConfig GetCrmConfig(string configPath = null)
        {
            if (string.IsNullOrEmpty(configPath))
                configPath = ConfigurationManager.AppSettings["crmConfigPath"];

            if (!File.Exists(configPath))
                throw new FileNotFoundException($"CrmConfig File not found=>{configPath}");

            var configText = File.ReadAllText(configPath);

            return JsonSerializer.Deserialize<CrmConfig>(configText);
        }
    }

    public class CrmConfig
    {
        public string crmUrl { get; set; }
        public string crmAppId { get; set; }
        public string discoveryServiceUri { get; set; }
        public string userName { get; set; }
        public string userPwd { get; set; }
        public string redirectUri { get; set; }
        public string crmConnRawString { get; set; }

        public string crmConnString
        {
            get
            {
                return string.Format(crmConnRawString, userName, userPwd, crmUrl, crmAppId, redirectUri);
            }
        }
    }
}
