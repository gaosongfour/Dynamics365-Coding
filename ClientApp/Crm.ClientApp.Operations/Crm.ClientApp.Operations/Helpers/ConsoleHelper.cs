using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.ClientApp.Operations.Helpers
{
    public static class ConsoleHelper
    {
        /// <summary>
        /// Parse user input string into Guid and return it
        /// </summary>
        /// <param name="promptMessage">message to guide user what to input</param>
        /// <returns></returns>
        public static Guid GetGuidUserInput(string promptMessage)
        {
            Guid id = Guid.Empty;
            while (id == Guid.Empty)
            {
                Console.WriteLine(promptMessage);
                var input = Console.ReadLine();
                var parseResult = Guid.TryParse(input, out id);
            }
            return id;
        }
    }
}