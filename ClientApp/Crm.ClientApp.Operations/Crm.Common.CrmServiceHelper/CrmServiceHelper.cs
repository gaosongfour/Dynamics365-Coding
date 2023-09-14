using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Tooling.Connector;


namespace Crm.Common.CrmService
{
    public class CrmServiceHelper
    {
        private static CrmServiceClient crmServiceClient;

        public static CrmServiceClient GetCrmServiceClient(string crmConnString)
        {
            if (crmServiceClient == null || crmServiceClient.IsReady == false)
            {
                crmServiceClient = new CrmServiceClient(crmConnString);
                if (crmServiceClient.IsReady == false)
                    throw new Exception($"Crm Service Client Init Error=>{crmServiceClient.LastCrmError}");
            }

            return crmServiceClient;
        }
    }
}
