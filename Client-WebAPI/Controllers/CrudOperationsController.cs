using Microsoft.AspNetCore.Mvc;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Messages;
using Crm.ClientWebAPI.Models;
using Crm.ClientWebAPI.Services;

namespace Crm.ClientWebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CrudOperationsController : ControllerBase
{
    private readonly CrmServiceManager _serviceManager;
    private readonly CrmOperationsHelper _helper;

    public CrudOperationsController(
        CrmServiceManager serviceManager, CrmOperationsHelper helper)
    {
        _serviceManager = serviceManager;
        _helper = helper;
    }

    #region Basic CRUD

    /// <summary>
    /// Create, Retrieve, Update and Delete an account record
    /// </summary>
    [HttpPost("basic")]
    public IActionResult BasicCrud()
    {
        try
        {
            var client = _serviceManager.Service;

            // Create
            var entity = new Entity("account");
            entity["name"] = $"Test Account CRUD-{DateTime.Now:yyyy-MM-dd:HH:mm:ss}";
            entity["description"] = "Desc of Test Account CRUD";
            entity["numberofemployees"] = 450;
            entity["industrycode"] = new OptionSetValue(1);
            entity["creditlimit"] = new Money(300000);
            entity["donotphone"] = false;

            var contactEntity = _helper.RetrieveLatestContact();
            if (contactEntity != null)
                entity["primarycontactid"] = contactEntity.ToEntityReference();

            entity["lastonholdtime"] = new DateTime(2023, 10, 18, 17, 0, 0);

            var entityId = client.Create(entity);

            // Retrieve
            var cols = new ColumnSet(
                "name", "description", "numberofemployees", "industrycode",
                "creditlimit", "donotphone", "primarycontactid", "lastonholdtime");
            var retrievedEntity = client.Retrieve("account", entityId, cols);
            var entityInfo = _helper.GetEntityInfo(retrievedEntity);

            // Update
            var updateEntity = new Entity("account", entityId);
            updateEntity["description"] = $"Updated {DateTime.Now}";
            updateEntity["creditlimit"] = new Money(300400);
            client.Update(updateEntity);

            // Delete
            client.Delete("account", entityId);

            return Ok(OperationResult<object>.Ok(new
            {
                CreatedId = entityId,
                RetrievedInfo = entityInfo,
                Message = "Account created, retrieved, updated, and deleted successfully"
            }));
        }
        catch (Exception ex)
        {
            return StatusCode(500, OperationResult.Fail(ex.Message));
        }
    }

    #endregion

    #region RetrieveMultiple

    /// <summary>
    /// Retrieve multiple records using QueryByAttribute
    /// </summary>
    [HttpGet("retrieve-multiple/query-by-attribute")]
    public IActionResult RetrieveMultipleByQueryByAttribute()
    {
        try
        {
            var client = _serviceManager.Service;
            var query = new QueryByAttribute
            {
                EntityName = "account",
                ColumnSet = new ColumnSet("name"),
                Attributes = { "statecode", "donotemail" },
                Values = { 0, false },
                Orders = { new OrderExpression("createdon", OrderType.Descending) },
                TopCount = 10
            };

            var result = client.RetrieveMultiple(query);
            return Ok(OperationResult<object>.Ok(
                _helper.GetEntityCollectionInfo(result)));
        }
        catch (Exception ex)
        {
            return StatusCode(500, OperationResult.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Retrieve multiple records using QueryExpression
    /// </summary>
    [HttpGet("retrieve-multiple/query-expression")]
    public IActionResult RetrieveMultipleByQueryExpression()
    {
        try
        {
            var client = _serviceManager.Service;
            var query = new QueryExpression
            {
                EntityName = "account",
                ColumnSet = new ColumnSet("name"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("statecode", ConditionOperator.Equal, 0),
                        new ConditionExpression("donotemail", ConditionOperator.Equal, false),
                        new ConditionExpression("name", ConditionOperator.NotNull),
                        new ConditionExpression("createdon", ConditionOperator.ThisYear)
                    }
                },
                Orders = { new OrderExpression("createdon", OrderType.Descending) },
                TopCount = 10
            };

            var result = client.RetrieveMultiple(query);
            return Ok(OperationResult<object>.Ok(
                _helper.GetEntityCollectionInfo(result)));
        }
        catch (Exception ex)
        {
            return StatusCode(500, OperationResult.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Retrieve multiple records with column comparison (firstname == lastname)
    /// </summary>
    [HttpGet("retrieve-multiple/column-comparison")]
    public IActionResult RetrieveMultipleWithColumnComparison()
    {
        try
        {
            var client = _serviceManager.Service;
            var query = new QueryExpression
            {
                EntityName = "systemuser",
                ColumnSet = new ColumnSet("fullname"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("isdisabled", ConditionOperator.Equal, false),
                        new ConditionExpression(
                            "firstname", ConditionOperator.Equal, true, "lastname")
                    }
                },
                Orders = { new OrderExpression("createdon", OrderType.Descending) },
                TopCount = 10
            };

            var result = client.RetrieveMultiple(query);
            return Ok(OperationResult<object>.Ok(
                _helper.GetEntityCollectionInfo(result, "fullname")));
        }
        catch (Exception ex)
        {
            return StatusCode(500, OperationResult.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Retrieve contacts with parent account and primary contact info
    /// </summary>
    [HttpGet("retrieve-multiple/link-entity")]
    public IActionResult RetrieveMultipleWithLinkEntity()
    {
        try
        {
            var client = _serviceManager.Service;
            var query = new QueryExpression
            {
                EntityName = "contact",
                ColumnSet = new ColumnSet("fullname"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("statecode", ConditionOperator.Equal, 0),
                        new ConditionExpression("parentcustomerid", ConditionOperator.NotNull)
                    }
                },
                Orders = { new OrderExpression("createdon", OrderType.Descending) },
                TopCount = 5
            };

            var linkEntityAccount = new LinkEntity(
                "contact", "account", "parentcustomerid", "accountid", JoinOperator.Inner)
            {
                EntityAlias = "parentaccount",
                Columns = new ColumnSet("name"),
            };

            var linkEntityPrimaryContact = new LinkEntity(
                "account", "contact", "primarycontactid", "contactid", JoinOperator.Inner)
            {
                EntityAlias = "parentaccount.primarycontact",
                Columns = new ColumnSet("fullname"),
            };

            linkEntityAccount.LinkEntities.Add(linkEntityPrimaryContact);
            query.LinkEntities.Add(linkEntityAccount);

            var result = client.RetrieveMultiple(query);
            var entities = result.Entities.Select(e =>
                _helper.GetEntityInfo(e)).ToList();

            return Ok(OperationResult<object>.Ok(
                new { Count = entities.Count, Entities = entities }));
        }
        catch (Exception ex)
        {
            return StatusCode(500, OperationResult.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Retrieve recent leads with no phone call activity
    /// </summary>
    [HttpGet("retrieve-multiple/recent-leads-no-phone")]
    public IActionResult RetrieveRecentLeadsNoPhoneCall()
    {
        try
        {
            var client = _serviceManager.Service;
            var query = new QueryExpression
            {
                EntityName = "lead",
                ColumnSet = new ColumnSet("subject"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("statecode", ConditionOperator.Equal, 0),
                    }
                },
                Orders = { new OrderExpression("createdon", OrderType.Descending) },
                TopCount = 5
            };

            var linkEntity = new LinkEntity(
                "lead", "phonecall", "leadid", "regardingobjectid", JoinOperator.LeftOuter)
            {
                EntityAlias = "phonecall",
                LinkCriteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("activityid", ConditionOperator.Null)
                    }
                }
            };

            query.LinkEntities.Add(linkEntity);

            var result = client.RetrieveMultiple(query);
            var entities = result.Entities.Select(e =>
                _helper.GetEntityInfo(e)).ToList();

            return Ok(OperationResult<object>.Ok(
                new { Count = entities.Count, Entities = entities }));
        }
        catch (Exception ex)
        {
            return StatusCode(500, OperationResult.Fail(ex.Message));
        }
    }

    #endregion

    #region FetchXml

    /// <summary>
    /// Retrieve accounts using FetchXml
    /// </summary>
    [HttpGet("fetchxml/accounts")]
    public IActionResult FetchXmlAccounts()
    {
        try
        {
            var client = _serviceManager.Service;
            var query = @"
            <fetch mapping='logical'>
                <entity name='account'>
                    <attribute name='accountid'/>
                    <attribute name='name'/>
                    <filter type='and'>
                        <condition attribute='statecode' operator='eq' value='0'/>
                        <condition attribute='donotemail' operator='eq' value='false'/>
                        <condition attribute='name' operator='not-null'/>
                        <condition attribute='createdon' operator='this-year'/>
                    </filter>
                    <order attribute='createdon' descending='true'/>
                </entity>
            </fetch>";

            var result = client.RetrieveMultiple(new FetchExpression(query));
            return Ok(OperationResult<object>.Ok(
                _helper.GetEntityCollectionInfo(result)));
        }
        catch (Exception ex)
        {
            return StatusCode(500, OperationResult.Fail(ex.Message));
        }
    }

    /// <summary>
    /// FetchXml with column comparison (firstname == lastname)
    /// </summary>
    [HttpGet("fetchxml/column-comparison")]
    public IActionResult FetchXmlColumnComparison()
    {
        try
        {
            var client = _serviceManager.Service;
            var query = @"
            <fetch mapping='logical' top='10'>
                <entity name='systemuser'>
                    <attribute name='fullname' />
                    <filter>
                        <condition attribute='isdisabled' operator='eq' value='false' />
                        <condition attribute='firstname' operator='eq' valueof='lastname' />
                    </filter>
                    <order attribute='createdon' descending='true' />
                </entity>
            </fetch>";

            var result = client.RetrieveMultiple(new FetchExpression(query));
            return Ok(OperationResult<object>.Ok(
                _helper.GetEntityCollectionInfo(result, "fullname")));
        }
        catch (Exception ex)
        {
            return StatusCode(500, OperationResult.Fail(ex.Message));
        }
    }

    /// <summary>
    /// FetchXml with link entity
    /// </summary>
    [HttpGet("fetchxml/link-entity")]
    public IActionResult FetchXmlLinkEntity()
    {
        try
        {
            var client = _serviceManager.Service;
            var query = @"
            <fetch mapping='logical' top='10'>
                <entity name='contact'>
                    <attribute name='fullname'/>
                    <filter>
                        <condition attribute='statecode' operator='eq' value='0'/>
                        <condition attribute='parentcustomerid' operator='not-null'/>
                    </filter>
                    <order attribute='createdon' descending='true'/>
                    <link-entity name='account' alias='parentaccount'
                                 to='parentcustomerid' from='accountid' link-type='inner'>
                        <attribute name='name'/>
                        <link-entity name='contact'
                                     alias='parentaccount.primarycontact'
                                     to='primarycontactid' from='contactid'
                                     link-type='inner'>
                            <attribute name='fullname'/>
                        </link-entity>
                    </link-entity>
                </entity>
            </fetch>";

            var result = client.RetrieveMultiple(new FetchExpression(query));
            var entities = result.Entities.Select(e =>
                _helper.GetEntityInfo(e)).ToList();

            return Ok(OperationResult<object>.Ok(
                new { Count = entities.Count, Entities = entities }));
        }
        catch (Exception ex)
        {
            return StatusCode(500, OperationResult.Fail(ex.Message));
        }
    }

    /// <summary>
    /// FetchXml: recent leads with no phone call
    /// </summary>
    [HttpGet("fetchxml/recent-leads-no-phone")]
    public IActionResult FetchXmlRecentLeadsNoPhone()
    {
        try
        {
            var client = _serviceManager.Service;
            var query = @"
            <fetch mapping='logical' top='5'>
                <entity name='lead'>
                    <attribute name='subject'/>
                    <filter>
                        <condition attribute='statecode' operator='eq' value='0'/>
                    </filter>
                    <order attribute='createdon' descending='true'/>
                    <link-entity name='phonecall' alias='phonecall'
                                 to='leadid' from='regardingobjectid'
                                 link-type='outer'>
                        <filter>
                            <condition attribute='activityid' operator='null'/>
                        </filter>
                    </link-entity>
                </entity>
            </fetch>";

            var result = client.RetrieveMultiple(new FetchExpression(query));
            var entities = result.Entities.Select(e =>
                _helper.GetEntityInfo(e)).ToList();

            return Ok(OperationResult<object>.Ok(
                new { Count = entities.Count, Entities = entities }));
        }
        catch (Exception ex)
        {
            return StatusCode(500, OperationResult.Fail(ex.Message));
        }
    }

    #endregion

    #region FetchXml Aggregation

    /// <summary>
    /// Get total contacts count using FetchXml aggregation
    /// </summary>
    [HttpGet("fetchxml/aggregation/total-contacts")]
    public IActionResult GetTotalContacts()
    {
        try
        {
            var client = _serviceManager.Service;
            var query = @"
            <fetch distinct='false' mapping='logical' aggregate='true'>
               <entity name='contact'>
                  <attribute name='contactid' aggregate='count' alias='totalContact'/>
                  <filter>
                     <condition attribute='createdon' operator='last-x-years' value='3'/>
                  </filter>
               </entity>
            </fetch>";

            var result = client.RetrieveMultiple(new FetchExpression(query));
            var total = _helper.GetAliasedAttributeValue<int>(
                result.Entities[0], "totalContact");

            return Ok(OperationResult<object>.Ok(new { TotalContacts = total }));
        }
        catch (Exception ex)
        {
            return StatusCode(500, OperationResult.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Get contacts grouped by account using FetchXml aggregation
    /// </summary>
    [HttpGet("fetchxml/aggregation/contacts-by-account")]
    public IActionResult GetContactsGroupByAccount()
    {
        try
        {
            var client = _serviceManager.Service;
            var query = @"
            <fetch distinct='false' mapping='logical' aggregate='true'>
               <entity name='contact'>
                  <attribute name='contactid' aggregate='count' alias='totalContact'/>
                  <filter>
                     <condition attribute='createdon' operator='last-x-years' value='3'/>
                     <condition attribute='parentcustomerid' operator='not-null'/>
                  </filter>
                  <link-entity name='account' from='accountid' to='parentcustomerid'>
                     <attribute name='name' alias='accountName' groupby='true' />
                      <order alias='accountName' descending='false'/>
                  </link-entity>
               </entity>
            </fetch>";

            var result = client.RetrieveMultiple(new FetchExpression(query));
            var data = result.Entities.Select(e => new
            {
                AccountName = _helper.GetAliasedAttributeValue<string>(e, "accountName"),
                TotalContacts = _helper.GetAliasedAttributeValue<int>(e, "totalContact")
            }).ToList();

            return Ok(OperationResult<object>.Ok(data));
        }
        catch (Exception ex)
        {
            return StatusCode(500, OperationResult.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Get lead total estimation amount grouped by account
    /// </summary>
    [HttpGet("fetchxml/aggregation/leads-by-account")]
    public IActionResult GetLeadTotalByAccount()
    {
        try
        {
            var client = _serviceManager.Service;
            var query = @"
            <fetch distinct='false' mapping='logical' aggregate='true'>
               <entity name='lead'>
                  <attribute name='estimatedamount' aggregate='sum'
                             alias='totalEstimatedAmount'/>
                  <attribute name='leadid' aggregate='count' alias='totalLeadCount'/>
                  <filter>
                     <condition attribute='parentaccountid' operator='not-null'/>
                  </filter>
                  <link-entity name='account' from='accountid' to='parentaccountid'>
                     <attribute name='name' alias='accountName' groupby='true' />
                     <order alias='accountName' descending='false'/>
                  </link-entity>
               </entity>
            </fetch>";

            var result = client.RetrieveMultiple(new FetchExpression(query));
            var data = result.Entities.Select(e => new
            {
                AccountName = _helper.GetAliasedAttributeValue<string>(e, "accountName"),
                TotalLeadCount = _helper.GetAliasedAttributeValue<int>(e, "totalLeadCount"),
                TotalEstimatedAmount =
                    _helper.GetAliasedAttributeValue<Money>(e, "totalEstimatedAmount").Value
            }).ToList();

            return Ok(OperationResult<object>.Ok(data));
        }
        catch (Exception ex)
        {
            return StatusCode(500, OperationResult.Fail(ex.Message));
        }
    }

    #endregion

    #region RetrieveMultiple with Paging

    /// <summary>
    /// Retrieve all accounts using QueryExpression with paging
    /// </summary>
    [HttpGet("paging/accounts")]
    public IActionResult RetrieveAllAccounts()
    {
        try
        {
            var basicQuery = new QueryExpression
            {
                EntityName = "account",
                ColumnSet = new ColumnSet("name"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("statecode", ConditionOperator.Equal, 0),
                        new ConditionExpression("name", ConditionOperator.NotNull),
                    }
                },
                Orders = { new OrderExpression("createdon", OrderType.Descending) },
            };

            var entityList = _helper.RetrieveAllRecords(basicQuery);
            return Ok(OperationResult<object>.Ok(new
            {
                TotalCount = entityList.Count,
                Entities = entityList.Select(e => new
                {
                    e.Id,
                    Name = e.GetAttributeValue<string>("name")
                }).ToList()
            }));
        }
        catch (Exception ex)
        {
            return StatusCode(500, OperationResult.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Retrieve all accounts using FetchXml with paging
    /// </summary>
    [HttpGet("paging/accounts-fetchxml")]
    public IActionResult RetrieveAllAccountsByFetchXml()
    {
        try
        {
            var fetchXml = @"
            <fetch mapping='logical'>
              <entity name='account'>
                <attribute name='name'/>
                <filter>
                  <condition attribute='statecode' operator='eq' value='0'/>
                  <condition attribute='createdon' operator='last-x-years' value='10'/>
                </filter>
                <order attribute='createdon' descending='true'/>
              </entity>
            </fetch>";

            var entityList = _helper.RetrieveAllRecordsByFetchXml(fetchXml);
            return Ok(OperationResult<object>.Ok(new
            {
                TotalCount = entityList.Count,
                Entities = entityList.Select(e => new
                {
                    e.Id,
                    Name = e.GetAttributeValue<string>("name")
                }).ToList()
            }));
        }
        catch (Exception ex)
        {
            return StatusCode(500, OperationResult.Fail(ex.Message));
        }
    }

    #endregion

    #region Retrieve with Related Data

    /// <summary>
    /// Retrieve account with related owner and child contacts
    /// </summary>
    [HttpGet("related-data/account/{accountId:guid}")]
    public IActionResult RetrieveAccountWithRelatedData(Guid accountId)
    {
        try
        {
            var client = _serviceManager.Service;

            var relationAccountOwner = new Relationship("user_accounts");
            var queryOwner = new QueryExpression
            {
                EntityName = "systemuser",
                ColumnSet = new ColumnSet("domainname")
            };

            var relationAccountChildContact = new Relationship("contact_customer_accounts");
            var queryContact = new QueryExpression
            {
                EntityName = "contact",
                ColumnSet = new ColumnSet("fullname")
            };

            var request = new RetrieveRequest
            {
                Target = new EntityReference("account", accountId),
                ColumnSet = new ColumnSet("name"),
                RelatedEntitiesQuery = new RelationshipQueryCollection
                {
                    { relationAccountOwner, queryOwner },
                    { relationAccountChildContact, queryContact }
                }
            };

            var result = (RetrieveResponse)client.Execute(request);
            var accountEntity = result.Entity;

            var response = new
            {
                AccountName = accountEntity.GetAttributeValue<string>("name"),
                Owner = accountEntity.RelatedEntities.ContainsKey(relationAccountOwner)
                    ? accountEntity.RelatedEntities[relationAccountOwner].Entities[0]
                        .GetAttributeValue<string>("domainname")
                    : null,
                Contacts = accountEntity.RelatedEntities
                    .ContainsKey(relationAccountChildContact)
                    ? accountEntity.RelatedEntities[relationAccountChildContact].Entities
                        .Select(c => c.GetAttributeValue<string>("fullname")).ToList()
                    : new List<string>()
            };

            return Ok(OperationResult<object>.Ok(response));
        }
        catch (Exception ex)
        {
            return StatusCode(500, OperationResult.Fail(ex.Message));
        }
    }

    #endregion
}
