using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Crm.Common;

namespace Crm.ClientApp.Operations.CrmOperations
{
    /// <summary>
    /// Create, Retrieve, Update and Delete CRM record
    /// </summary>
    public class BasicCRUDOperation : CrmOperationBase, ICrmOperation
    {
        public void Execute()
        {
            var entityId = CreateRecord();
            var entity = RetrieveRecord("account", entityId);
            UpdateRecord(entity);
            DeleteRecord(entity.LogicalName, entity.Id);
        }

        public Guid CreateRecord()
        {
            var entity = new Entity("account");

            //String
            entity["name"] = $"Test Account CRUD-{DateTime.Now.ToString("yyyy-MM-dd:HH:mm:ss")}";
            //String multiple lines
            entity["description"] = "Desc of Test Account CRUD";
            //Int
            entity["numberofemployees"] = 450;
            //OptionsetValue/Picklist
            entity["industrycode"] = new OptionSetValue(1);
            //Money
            entity["creditlimit"] = new Money(300000);
            //Boolean
            entity["donotphone"] = false;
            //Lookup
            var contactEntity = RetrieveLatestContact();
            if (contactEntity == null) throw new Exception("0 Contact retrieved, please contact administrator");
            entity["primarycontactid"] = contactEntity.ToEntityReference();
            //Datetime  user local time
            entity["lastonholdtime"] = new DateTime(2023, 10, 18, 17, 0, 0);

            var entityId = crmServiceClient.Create(entity);
            Console.WriteLine($"Record {entity.LogicalName} with Id {entityId} created");
            return entityId;
        }

        public Entity RetrieveRecord(string entityName, Guid entityId)
        {
            var cols = new ColumnSet("name", "description", "numberofemployees", "industrycode", "creditlimit", "donotphone", "primarycontactid", "lastonholdtime");
            var entity = crmServiceClient.Retrieve(entityName, entityId, cols);
            entity.PrintAttributes();
            return entity;
        }

        public void UpdateRecord(Entity entity)
        {
            //use a new entity for update and never reuse the entity retrieved
            var updateEntity = new Entity(entity.LogicalName, entity.Id);
            //only assign the value to the fields need to be updated
            updateEntity["description"] = $"Update Record {DateTime.Now.ToString()}";
            updateEntity["creditlimit"] = new Money(300400);

            crmServiceClient.Update(updateEntity);
            Console.WriteLine($"Record {entity.LogicalName} with Id {entity.Id} updated");
        }

        public void DeleteRecord(string entityName, Guid entityId)
        {
            crmServiceClient.Delete(entityName, entityId);
            Console.WriteLine($"Record {entityName} with Id {entityId} deleted");
        }
    }
}