using System;
using Crm.ClientApp.Operations.CrmOperations;

namespace Crm.ClientApp.Operations
{
    class Program
    {
        static void Main(string[] args)
        {
            //ICrmOperation crmOperation = new UserSecurityOperation();
            //ICrmOperation crmOperation = new PrincipalAccessOperationn();
            //ICrmOperation crmOperation = new BasicCRUDOperation();
            // ICrmOperation crmOperation = new RetrieveMultipleOperation();
            //ICrmOperation crmOperation = new FetchXmlOperations();
            ICrmOperation crmOperation = new FetchXmlAggregationOperation();

            try
            {
                crmOperation.Execute();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine("The End");
            Console.ReadKey();
        }
    }
}