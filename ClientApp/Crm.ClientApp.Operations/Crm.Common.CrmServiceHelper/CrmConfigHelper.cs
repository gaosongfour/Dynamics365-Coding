using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using System.Text.Json;

namespace Crm.Common.CrmService
{
    public class CrmConfigHelper
    {
        public static string GetCrmConnString(string configPath)
        {
            if (!File.Exists(configPath))
                throw new FileNotFoundException($"CrmConfig File not found=>{configPath}");

            var configText = File.ReadAllText(configPath);

            var crmConfigEntity = JsonSerializer.Deserialize<CrmConfig>(configText);

            var crmConnString = string.Format(crmConfigEntity.crmConnRawString,
                crmConfigEntity.userName,
                crmConfigEntity.userPwd,
                crmConfigEntity.crmUrl,
                crmConfigEntity.crmAppId,
                crmConfigEntity.redirectUri);
            return crmConnString;
        }
    }

    internal class CrmConfig
    {
        public string crmUrl { get; set; }
        public string crmAppId { get; set; }
        public string userName { get; set; }
        public string userPwd { get; set; }
        public string redirectUri { get; set; }
        public string crmConnRawString { get; set; }
    }
}
