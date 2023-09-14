using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Crm.ClientApp.Operations.UserOperations;

namespace Crm.ClientApp.Operations
{
    class Program
    {
        static void Main(string[] args)
        {
            ICrmOperation crmOperation = new UserSettingsOperation();
            crmOperation.Execute();

            Console.ReadKey();
        }
    }
}
