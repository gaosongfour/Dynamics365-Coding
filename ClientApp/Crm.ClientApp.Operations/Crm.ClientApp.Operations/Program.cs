using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Crm.ClientApp.Operations.CrmOperations;

namespace Crm.ClientApp.Operations
{
    class Program
    {
        static void Main(string[] args)
        {
            //ICrmOperation crmOperation = new UserSecurityOperation();
            ICrmOperation crmOperation = new PrincipalAccessOperationn();

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