using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Text;
using System.IO;

namespace Crm.ClientApp.Operations.CrmOperations
{
    /// <summary>
    /// Send Email 
    /// </summary>
    public class SendEmailOperation : CrmOperationBase, ICrmOperation
    {
        private const string EmailTemplateFileRelativePath = @"Templates\EmailTemplates\SendEmailTemplate.html";

        private string emailHtml;
        public void Execute()
        {
            CreateAndSendEmail();
        }

        private void LoadEmailTemplateFile()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), EmailTemplateFileRelativePath);
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Email template file not found=>{filePath}");
            emailHtml = File.ReadAllText(filePath);
        }

        private void CreateAndSendEmail()
        {
            LoadEmailTemplateFile();
            var emailId = CreateEmailActivity();
            //SendEmail(emailId); - Test Purpose do not send email
        }

        private Guid CreateEmailActivity()
        {
            var entity = new Entity("email");

            //from
            var currentUserId = WhoAmI();
            var fromUser = GetActivityParty("systemuser", currentUserId);
            entity["from"] = new Entity[] { fromUser };

            //to
            var accountEntity = RetrieveLatestAccount();
            var toPartyAccount = GetActivityParty(accountEntity.ToEntityReference());

            var contactEntity = RetrieveLatestContact();
            var toPartyContact = GetActivityParty(contactEntity.ToEntityReference());
            entity["to"] = new Entity[] { toPartyAccount, toPartyContact };

            entity["subject"] = $"Test email to {accountEntity.GetAttributeValue<string>("name")}";
            entity["directioncode"] = true;
            entity["regardingobjectid"] = accountEntity.ToEntityReference();
            entity["description"] = BuildHtmlEmailBody(accountEntity, contactEntity, GetTop10ClientOrder());

            var emailId = crmServiceClient.Create(entity);
            Console.WriteLine($"Email with Id {emailId} created");
            return emailId;
        }

        private void SendEmail(Guid emailId)
        {
            var request = new SendEmailRequest
            {
                EmailId = emailId,
                IssueSend = true,
            };

            var response = (SendBulkMailResponse)crmServiceClient.Execute(request);
        }

        private Entity GetActivityParty(string entityName, Guid entityId)
        {
            var entity = new Entity("activityparty");
            entity["partyid"] = new EntityReference(entityName, entityId);
            return entity;
        }

        private Entity GetActivityParty(EntityReference entityRef)
        {
            var entity = new Entity("activityparty");
            entity["partyid"] = entityRef;
            return entity;
        }

        private EntityCollection GetTop10ClientOrder()
        {
            var query = new QueryByAttribute("new_order")
            {
                ColumnSet = new ColumnSet("new_ordernumber", "new_clientid", "new_productionfactoryid"),
                Attributes = { "statecode" },
                Values = { 0 },
                Orders = { new OrderExpression("createdon", OrderType.Descending) },
                TopCount = 10
            };

            return crmServiceClient.RetrieveMultiple(query);
        }

        private string BuildHtmlEmailBody(Entity accountEntity, Entity contactEntity, EntityCollection orderCollection)
        {
            var fullname = contactEntity.GetAttributeValue<string>("fullname");
            var orderTableHtml = BuildHtmlOrderTable(orderCollection);

            emailHtml = emailHtml.Replace("{contact_fullname}", fullname);
            emailHtml = emailHtml.Replace("{table_order}", orderTableHtml);

            return emailHtml;
        }

        private string BuildHtmlOrderTable(EntityCollection orderCollection)
        {
            var tableData = new StringBuilder();
            foreach (var orderEntity in orderCollection.Entities)
            {
                var orderNumber = orderEntity.GetAttributeValue<string>("new_ordernumber");
                var clientName = orderEntity.FormattedValues.Contains("new_clientid") ? orderEntity.FormattedValues["new_clientid"] : null;
                var factoryName = orderEntity.FormattedValues.Contains("new_productionfactoryid") ? orderEntity.FormattedValues["new_productionfactoryid"] : null;
                tableData.AppendLine($"<tr><td>{BuildHtmlLink(orderNumber, GetCrmRecordUrl(orderEntity))}</td><td>{clientName}</td><td>{factoryName}</td></tr>");
            }

            var tableHtml = $@"
            <table>
                <thead>
                    <tr><th>Order Number</th><th>Client Name</th><th>Factory</th></tr>
                </thead>
                <tbody>
            {tableData}
                </tbody>
            </table>
            ";

            return tableHtml;
        }

        private string BuildHtmlLink(string linkText, string linkUrl)
        {
            return $"<a href='{linkUrl}'>{linkText}</a>";
        }
    }
}
