using Microsoft.AspNetCore.Mvc;
using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;
using Crm.ClientWebAPI.Models;
using Crm.ClientWebAPI.Services;

namespace Crm.ClientWebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CrmOperationsController : ControllerBase
{
    private readonly CrmServiceManager _serviceManager;
    private readonly CrmOperationsHelper _helper;

    public CrmOperationsController(
        CrmServiceManager serviceManager, CrmOperationsHelper helper)
    {
        _serviceManager = serviceManager;
        _helper = helper;
    }

    #region WhoAmI

    /// <summary>
    /// Get current connected user information
    /// </summary>
    [HttpGet("whoami")]
    public IActionResult WhoAmI()
    {
        try
        {
            var userId = _helper.WhoAmI();
            return Ok(OperationResult<object>.Ok(new { UserId = userId }));
        }
        catch (Exception ex)
        {
            return StatusCode(500, OperationResult.Fail(ex.Message));
        }
    }

    #endregion

    #region Send Email

    /// <summary>
    /// Create and send email activity
    /// </summary>
    [HttpPost("email/send")]
    public IActionResult SendEmail([FromBody] EmailRequest? request = null)
    {
        try
        {
            var client = _serviceManager.Service;
            var templatePath = Path.Combine(
                System.AppContext.BaseDirectory, "Templates", "EmailTemplates", "SendEmailTemplate.html");

            if (!System.IO.File.Exists(templatePath))
                return BadRequest(OperationResult.Fail($"Email template not found: {templatePath}"));

            var emailHtml = System.IO.File.ReadAllText(templatePath);

            // Get current user
            var currentUserId = request?.FromUserId ?? _helper.WhoAmI();
            var fromUser = new Entity("activityparty");
            fromUser["partyid"] = new EntityReference("systemuser", currentUserId);

            // Get latest account
            var accountEntity = _helper.RetrieveLatestAccount();
            if (accountEntity == null)
                return BadRequest(OperationResult.Fail("No account found"));

            var toPartyAccount = new Entity("activityparty");
            toPartyAccount["partyid"] = accountEntity.ToEntityReference();

            // Get latest contact
            var contactEntity = _helper.RetrieveLatestContact();
            if (contactEntity == null)
                return BadRequest(OperationResult.Fail("No contact found"));

            var toPartyContact = new Entity("activityparty");
            toPartyContact["partyid"] = contactEntity.ToEntityReference();

            // Get top 10 orders
            var orderQuery = new Microsoft.Xrm.Sdk.Query.QueryByAttribute("new_order")
            {
                ColumnSet = new Microsoft.Xrm.Sdk.Query.ColumnSet(
                    "new_ordernumber", "new_clientid", "new_productionfactoryid"),
                Attributes = { "statecode" },
                Values = { 0 },
                Orders =
                {
                    new Microsoft.Xrm.Sdk.Query.OrderExpression(
                        "createdon", Microsoft.Xrm.Sdk.Query.OrderType.Descending)
                },
                TopCount = 10
            };
            var orderCollection = client.RetrieveMultiple(orderQuery);

            // Build order table HTML
            var tableData = new System.Text.StringBuilder();
            foreach (var orderEntity in orderCollection.Entities)
            {
                var orderNumber = orderEntity.GetAttributeValue<string>("new_ordernumber");
                var clientName = orderEntity.FormattedValues.Contains("new_clientid")
                    ? orderEntity.FormattedValues["new_clientid"] : null;
                var factoryName = orderEntity.FormattedValues.Contains("new_productionfactoryid")
                    ? orderEntity.FormattedValues["new_productionfactoryid"] : null;
                var recordUrl = _helper.GetCrmRecordUrl(orderEntity);
                tableData.AppendLine(
                    $"<tr><td><a href='{recordUrl}'>{orderNumber}</a></td>" +
                    $"<td>{clientName}</td><td>{factoryName}</td></tr>");
            }

            var tableHtml = $@"
            <table>
                <thead>
                    <tr><th>Order Number</th><th>Client Name</th><th>Factory</th></tr>
                </thead>
                <tbody>
                    {tableData}
                </tbody>
            </table>";

            var fullname = contactEntity.GetAttributeValue<string>("fullname");
            emailHtml = emailHtml.Replace("{contact_fullname}", fullname);
            emailHtml = emailHtml.Replace("{table_order}", tableHtml);

            // Create email entity
            var emailEntity = new Entity("email");
            emailEntity["from"] = new[] { fromUser };
            emailEntity["to"] = new[] { toPartyAccount, toPartyContact };
            emailEntity["subject"] = request?.Subject
                ?? $"Test email to {accountEntity.GetAttributeValue<string>("name")}";
            emailEntity["directioncode"] = true;
            emailEntity["regardingobjectid"] = accountEntity.ToEntityReference();
            emailEntity["description"] = request?.Body ?? emailHtml;

            var emailId = client.Create(emailEntity);

            return Ok(OperationResult<object>.Ok(
                new { EmailId = emailId },
                $"Email created with Id {emailId}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, OperationResult.Fail(ex.Message));
        }
    }

    #endregion

    #region ExecuteMultiple

    /// <summary>
    /// Execute multiple requests in batch using ExecuteMultipleRequest
    /// </summary>
    [HttpPost("batch/execute-multiple")]
    public IActionResult ExecuteMultiple([FromQuery] int requestNumber = 100)
    {
        try
        {
            var client = _serviceManager.Service;
            var request = new Microsoft.Xrm.Sdk.Messages.ExecuteMultipleRequest
            {
                Settings = new Microsoft.Xrm.Sdk.ExecuteMultipleSettings
                {
                    ReturnResponses = true,
                    ContinueOnError = true
                },
                Requests = new OrganizationRequestCollection()
            };

            for (int i = 1; i <= requestNumber; i++)
            {
                var entity = new Entity("lead");
                entity["subject"] = $"Lead-ExeMultiple-{i}";
                request.Requests.Add(
                    new Microsoft.Xrm.Sdk.Messages.CreateRequest { Target = entity });
            }

            var result = (Microsoft.Xrm.Sdk.Messages.ExecuteMultipleResponse)client.Execute(request);

            var responses = new List<object>();
            foreach (var response in result.Responses)
            {
                if (response.Fault != null)
                {
                    responses.Add(new
                    {
                        Index = response.RequestIndex,
                        Error = response.Fault.Message
                    });
                }
                else if (response.Response.ResponseName == "Create")
                {
                    responses.Add(new
                    {
                        Index = response.RequestIndex,
                        Id = ((Microsoft.Xrm.Sdk.Messages.CreateResponse)response.Response).id
                    });
                }
                else
                {
                    responses.Add(new
                    {
                        Index = response.RequestIndex,
                        ResponseName = response.Response.ResponseName
                    });
                }
            }

            return Ok(OperationResult<object>.Ok(
                new { ProcessedCount = responses.Count, Responses = responses }));
        }
        catch (Exception ex)
        {
            return StatusCode(500, OperationResult.Fail(ex.Message));
        }
    }

    #endregion

    #region ExecuteTransaction

    /// <summary>
    /// Execute transaction request to create order and order items atomically
    /// </summary>
    [HttpPost("batch/execute-transaction")]
    public IActionResult ExecuteTransaction()
    {
        try
        {
            var client = _serviceManager.Service;

            var clientEntity = _helper.RetrieveLatestClient();
            if (clientEntity == null)
                return BadRequest(OperationResult.Fail("No client entity found"));

            var factoryEntity = _helper.RetrieveLatestFactory();
            if (factoryEntity == null)
                return BadRequest(OperationResult.Fail("No factory entity found"));

            var request = new Microsoft.Xrm.Sdk.Messages.ExecuteTransactionRequest
            {
                Requests = new OrganizationRequestCollection(),
                ReturnResponses = true
            };

            // Create order
            var orderEntity = new Entity("new_order");
            orderEntity.Id = Guid.NewGuid();
            orderEntity["new_clientid"] = clientEntity.ToEntityReference();
            orderEntity["new_productionfactoryid"] = factoryEntity.ToEntityReference();
            request.Requests.Add(
                new Microsoft.Xrm.Sdk.Messages.CreateRequest { Target = orderEntity });

            // Create order items
            foreach (var amount in new[] { 3000m, 2000m, 1000m })
            {
                var orderItemEntity = new Entity("new_orderitem");
                orderItemEntity["new_orderid"] =
                    new EntityReference("new_order", orderEntity.Id);
                orderItemEntity["new_price"] = orderItemEntity["new_amount"] =
                    new Money(amount);
                orderItemEntity["new_quantity"] = 1m;
                request.Requests.Add(
                    new Microsoft.Xrm.Sdk.Messages.CreateRequest { Target = orderItemEntity });
            }

            var response = (Microsoft.Xrm.Sdk.Messages.ExecuteTransactionResponse)
                client.Execute(request);

            var createdIds = response.Responses
                .Select(r => ((Microsoft.Xrm.Sdk.Messages.CreateResponse)r).id)
                .ToList();

            return Ok(OperationResult<object>.Ok(
                new { OrderId = orderEntity.Id, CreatedItemIds = createdIds }));
        }
        catch (System.ServiceModel.FaultException<OrganizationServiceFault> ex)
        {
            if (ex.Detail is Microsoft.Xrm.Sdk.ExecuteTransactionFault transactionFault)
            {
                return StatusCode(500, OperationResult.Fail(
                    $"Transaction failed at request {transactionFault.FaultedRequestIndex + 1}: " +
                    ex.Detail.Message));
            }
            return StatusCode(500, OperationResult.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, OperationResult.Fail(ex.Message));
        }
    }

    #endregion

    #region Concurrency

    /// <summary>
    /// Test optimistic concurrency by updating an account with row version matching
    /// </summary>
    [HttpPost("concurrency/test")]
    public IActionResult TestConcurrency()
    {
        try
        {
            var accountEntity = _helper.RetrieveLatestAccount();
            if (accountEntity == null)
                return BadRequest(OperationResult.Fail("No account found"));

            var accountName = accountEntity.GetAttributeValue<string>("name");
            var rowVersion = accountEntity.RowVersion;

            // Update with matching row version
            var updateEntity = new Entity(
                accountEntity.LogicalName, accountEntity.Id);
            updateEntity.RowVersion = rowVersion;
            updateEntity["name"] = $"Updated Row Version {rowVersion}";
            _helper.UpdateRecordIfRowVersion(updateEntity);

            return Ok(OperationResult<object>.Ok(new
            {
                AccountId = accountEntity.Id,
                PreviousName = accountName,
                RowVersion = rowVersion,
                Message = "Account updated with row version matching"
            }));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(OperationResult.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, OperationResult.Fail(ex.Message));
        }
    }

    #endregion

    #region Organization Discovery

    /// <summary>
    /// Discover global organizations using the configured credentials
    /// </summary>
    [HttpGet("organizations")]
    public async Task<IActionResult> GetOrganizations()
    {
        try
        {
            var crmConfig = _serviceManager.Config;
            var clientCredential = new System.ServiceModel.Description.ClientCredentials();
            clientCredential.UserName.UserName = crmConfig.UserName;
            clientCredential.UserName.Password = crmConfig.UserPwd;

            var discoverResult = await Microsoft.PowerPlatform.Dataverse.Client.ServiceClient
                .DiscoverOnlineOrganizationsAsync(
                    discoveryServiceUri: new Uri(crmConfig.DiscoveryServiceUri),
                    clientCredentials: clientCredential,
                    clientId: crmConfig.CrmAppId,
                    redirectUri: new Uri(crmConfig.RedirectUri),
                    isOnPrem: false,
                    authority: null,
                    promptBehavior: Microsoft.PowerPlatform.Dataverse.Client.Auth.PromptBehavior.Auto,
                    useDefaultCreds: false,
                    tokenCacheStorePath: null,
                    logger: null);

            var orgDetailCollection = discoverResult.OrganizationDetailCollection;
            var orgs = orgDetailCollection.Select(o => new
            {
                o.FriendlyName,
                o.OrganizationId,
                o.OrganizationVersion,
                WebApplicationUrl = o.Endpoints[
                    Microsoft.Xrm.Sdk.Discovery.EndpointType.WebApplication]
            }).ToList();

            return Ok(OperationResult<object>.Ok(
                new { Count = orgs.Count, Organizations = orgs }));
        }
        catch (Exception ex)
        {
            return StatusCode(500, OperationResult.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Connect to a specific organization by index
    /// </summary>
    [HttpPost("organizations/connect")]
    public async Task<IActionResult> ConnectToOrganization([FromQuery] int orgIndex = 0)
    {
        try
        {
            var crmConfig = _serviceManager.Config;
            var clientCredential = new System.ServiceModel.Description.ClientCredentials();
            clientCredential.UserName.UserName = crmConfig.UserName;
            clientCredential.UserName.Password = crmConfig.UserPwd;

            var discoverResult = await Microsoft.PowerPlatform.Dataverse.Client.ServiceClient
                .DiscoverOnlineOrganizationsAsync(
                    discoveryServiceUri: new Uri(crmConfig.DiscoveryServiceUri),
                    clientCredentials: clientCredential,
                    clientId: crmConfig.CrmAppId,
                    redirectUri: new Uri(crmConfig.RedirectUri),
                    isOnPrem: false,
                    authority: null,
                    promptBehavior: Microsoft.PowerPlatform.Dataverse.Client.Auth.PromptBehavior.Auto,
                    useDefaultCreds: false,
                    tokenCacheStorePath: null,
                    logger: null);

            var orgDetailCollection = discoverResult.OrganizationDetailCollection;
            if (orgIndex < 0 || orgIndex >= orgDetailCollection.Count)
                return BadRequest(OperationResult.Fail(
                    $"Invalid org index. Valid range: 0-{orgDetailCollection.Count - 1}"));

            var orgDetail = orgDetailCollection[orgIndex];
            var connectedClient = new Microsoft.PowerPlatform.Dataverse.Client.ServiceClient(
                crmConfig.CrmConnString);

            if (connectedClient.IsReady)
            {
                return Ok(OperationResult<object>.Ok(new
                {
                    Name = connectedClient.ConnectedOrgFriendlyName,
                    OrgId = connectedClient.ConnectedOrgId
                }));
            }

            return BadRequest(OperationResult.Fail(connectedClient.LastError));
        }
        catch (Exception ex)
        {
            return StatusCode(500, OperationResult.Fail(ex.Message));
        }
    }

    #endregion
}
