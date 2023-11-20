using System;
using Microsoft.Xrm.Sdk;
using Crm.Common;
using Crm.Common.Concurrency;


namespace Crm.ClientApp.Operations.CrmOperations
{
    public class ConcurrencyOperation : CrmOperationBase, ICrmOperation
    {
        public void Execute()
        {
            Console.WriteLine("Update account and row version matches");
            var accountEntity = RetrieveLatestAccount();
            Console.WriteLine($"account retrived name=>{accountEntity.GetAttributeValue<string>("name")}; row version=>{accountEntity.RowVersion}");

            var updateAccountEntity = accountEntity.NewEntity(true);
            updateAccountEntity["name"] = $"accountEntity Row Version {accountEntity.RowVersion}";
            crmServiceClient.UpdateRecordIfRowVersion(updateAccountEntity);
            var previousRowVersion = accountEntity.RowVersion;
            Console.WriteLine("account updated");

            Console.WriteLine("Update account and row version dismatches");
            accountEntity = RetrieveLatestAccount();
            Console.WriteLine($"account retrived name=>{accountEntity.GetAttributeValue<string>("name")}; row version=>{accountEntity.RowVersion}");

            updateAccountEntity = accountEntity.NewEntity();
            updateAccountEntity["name"] = $"accountEntity Row Version {accountEntity.RowVersion}";
            updateAccountEntity.RowVersion = previousRowVersion;
            crmServiceClient.UpdateRecordIfRowVersion(updateAccountEntity);
        }        
    }
}
