using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Crm.Sdk.Messages;
using Crm.ClientWebAPI.Models;

namespace Crm.ClientWebAPI.Services;

public class CrmOperationsHelper
{
    private readonly CrmServiceManager _serviceManager;

    public CrmOperationsHelper(CrmServiceManager serviceManager)
    {
        _serviceManager = serviceManager;
    }

    private Microsoft.PowerPlatform.Dataverse.Client.ServiceClient Client => _serviceManager.Service;

    #region WhoAmI

    public Guid WhoAmI()
    {
        var request = new WhoAmIRequest();
        var response = (WhoAmIResponse)Client.Execute(request);
        return response.UserId;
    }

    #endregion

    #region Retrieve Latest Records

    public Entity? RetrieveLatestAccount()
    {
        var query = new QueryByAttribute
        {
            EntityName = "account",
            ColumnSet = new ColumnSet("name"),
            Attributes = { "statecode" },
            Values = { 0 },
            Orders = { new OrderExpression("createdon", OrderType.Descending) },
            TopCount = 1
        };

        var result = Client.RetrieveMultiple(query);
        return result.Entities.FirstOrDefault();
    }

    public Entity? RetrieveLatestContact()
    {
        var query = new QueryByAttribute
        {
            EntityName = "contact",
            ColumnSet = new ColumnSet("fullname"),
            Attributes = { "statecode" },
            Values = { 0 },
            Orders = { new OrderExpression("createdon", OrderType.Descending) },
            TopCount = 1
        };

        var result = Client.RetrieveMultiple(query);
        return result.Entities.FirstOrDefault();
    }

    public Entity? RetrieveLatestClient()
    {
        var query = new QueryByAttribute
        {
            EntityName = "new_client",
            ColumnSet = new ColumnSet("new_name"),
            Attributes = { "statecode" },
            Values = { 0 },
            Orders = { new OrderExpression("createdon", OrderType.Descending) },
            TopCount = 1
        };

        var result = Client.RetrieveMultiple(query);
        return result.Entities.FirstOrDefault();
    }

    public Entity? RetrieveLatestFactory()
    {
        var query = new QueryByAttribute
        {
            EntityName = "new_factory",
            ColumnSet = new ColumnSet("new_name"),
            Attributes = { "statecode" },
            Values = { 0 },
            Orders = { new OrderExpression("createdon", OrderType.Descending) },
            TopCount = 1
        };

        var result = Client.RetrieveMultiple(query);
        return result.Entities.FirstOrDefault();
    }

    #endregion

    #region Get CRM Record URL

    public string GetCrmRecordUrl(string entityName, Guid entityId)
    {
        var baseUrl = Client.ConnectedOrgUriActual.Authority;
        return $"https://{baseUrl}/main.aspx?etn={entityName}&pagetype=entityrecord&id=%7B{entityId}%7D";
    }

    public string GetCrmRecordUrl(Entity entity)
    {
        var baseUrl = Client.ConnectedOrgUriActual.Authority;
        return $"https://{baseUrl}/main.aspx?etn={entity.LogicalName}&pagetype=entityrecord&id=%7B{entity.Id}%7D";
    }

    #endregion

    #region Entity Info Helpers

    public EntityInfo GetEntityInfo(Entity entity)
    {
        var info = new EntityInfo
        {
            LogicalName = entity.LogicalName,
            Id = entity.Id
        };

        foreach (var attribute in entity.Attributes)
        {
            info.Attributes.Add(GetAttributeInfo(entity, attribute.Key, attribute.Value));
        }

        return info;
    }

    public EntityCollectionInfo GetEntityCollectionInfo(
        EntityCollection entityCollection, string displayAttributeName = "name")
    {
        var info = new EntityCollectionInfo
        {
            EntityName = entityCollection.EntityName,
            Count = entityCollection.Entities.Count
        };

        foreach (var entity in entityCollection.Entities)
        {
            info.Entities.Add(new EntityBasicInfo
            {
                LogicalName = entity.LogicalName,
                Id = entity.Id,
                DisplayName = entity.GetAttributeValue<string>(displayAttributeName)
            });
        }

        return info;
    }

    private EntityAttributeInfo GetAttributeInfo(
        Entity entity, string attributeName, object attributeValue)
    {
        var attributeType = attributeValue.GetType();

        if (attributeType == typeof(AliasedValue))
        {
            return GetAttributeInfo(
                entity, attributeName, ((AliasedValue)attributeValue).Value);
        }

        var info = new EntityAttributeInfo
        {
            AttributeName = attributeName,
            AttributeType = attributeType.Name
        };

        if (attributeType == typeof(string) || attributeType == typeof(Guid)
            || attributeType == typeof(int) || attributeType == typeof(decimal))
        {
            info.Value = attributeValue.ToString();
        }
        else if (attributeType == typeof(EntityReference))
        {
            var entityRef = entity.GetAttributeValue<EntityReference>(attributeName);
            info.Value = $"(id:{entityRef.Id};type:{entityRef.LogicalName};name:{entityRef.Name})";
        }
        else if (attributeType == typeof(OptionSetValue))
        {
            info.Value = entity.GetAttributeValue<OptionSetValue>(attributeName).Value.ToString();
            info.FormattedValue = GetFormattedValue(entity, attributeName);
        }
        else if (attributeType == typeof(DateTime))
        {
            info.Value = entity.GetAttributeValue<DateTime>(attributeName)
                .ToString("yyyy-MM-dd:HH:mm:ss");
            info.FormattedValue = GetFormattedValue(entity, attributeName);
        }
        else if (attributeType == typeof(bool))
        {
            info.Value = entity.GetAttributeValue<bool>(attributeName).ToString();
            info.FormattedValue = GetFormattedValue(entity, attributeName);
        }
        else if (attributeType == typeof(Money))
        {
            info.Value = entity.GetAttributeValue<Money>(attributeName).Value.ToString();
            info.FormattedValue = GetFormattedValue(entity, attributeName);
        }
        else
        {
            info.Value = "unknown attribute type";
        }

        return info;
    }

    private static string? GetFormattedValue(Entity entity, string attributeName)
    {
        return entity.FormattedValues.ContainsKey(attributeName)
            ? entity.FormattedValues[attributeName]
            : null;
    }

    public T GetAliasedAttributeValue<T>(Entity entity, string attributeName)
    {
        if (!entity.Contains(attributeName))
            return default!;
        if (entity[attributeName].GetType() != typeof(AliasedValue))
            throw new ArgumentException($"attribute {attributeName} is not aliased type");
        return (T)(entity.GetAttributeValue<AliasedValue>(attributeName).Value);
    }

    #endregion

    #region Paging Helpers

    public List<Entity> RetrieveAllRecords(QueryExpression basicQuery)
    {
        var entityList = new List<Entity>();
        int currentPageNumber = 1;
        int pageSize = 5000;
        var query = GetQuery(basicQuery, currentPageNumber, pageSize, null);

        while (true)
        {
            var result = Client.RetrieveMultiple(query);
            entityList.AddRange(result.Entities);

            if (result.MoreRecords)
            {
                currentPageNumber++;
                query = GetQuery(basicQuery, currentPageNumber, pageSize, result.PagingCookie);
            }
            else
            {
                break;
            }
        }

        return entityList;
    }

    public List<Entity> RetrieveAllRecordsByFetchXml(string fetchXml, int pageSize = 5000)
    {
        var list = new List<Entity>();
        var page = 1;

        var fetchNode = System.Xml.Linq.XElement.Parse(fetchXml);
        fetchNode.SetAttributeValue("page", page);
        fetchNode.SetAttributeValue("count", pageSize);

        while (true)
        {
            var result = Client.RetrieveMultiple(new FetchExpression(fetchNode.ToString()));
            list.AddRange(result.Entities);

            if (!result.MoreRecords)
                break;

            page++;
            fetchNode.SetAttributeValue("page", page);
            fetchNode.SetAttributeValue("paging-cookie", result.PagingCookie);
        }

        return list;
    }

    private static QueryExpression GetQuery(
        QueryExpression query, int pageNumber, int pageSize, string? pageCookie)
    {
        query.PageInfo = new PagingInfo
        {
            Count = pageSize,
            PageNumber = pageNumber,
            PagingCookie = pageCookie
        };
        return query;
    }

    #endregion

    #region Concurrency

    public void UpdateRecordIfRowVersion(Entity updateEntity)
    {
        if (string.IsNullOrEmpty(updateEntity.RowVersion))
            throw new ArgumentNullException(nameof(updateEntity),
                "RowVersion is not provided for the entity to be updated");

        var request = new UpdateRequest
        {
            Target = updateEntity,
            ConcurrencyBehavior = ConcurrencyBehavior.IfRowVersionMatches
        };

        try
        {
            Client.Execute(request);
        }
        catch (System.ServiceModel.FaultException<OrganizationServiceFault> ex)
        {
            switch (ex.Detail.ErrorCode)
            {
                case -2147088254:
                    throw new InvalidOperationException(
                        $"Operation Failed, {updateEntity.LogicalName} with id " +
                        $"{updateEntity.Id} was updated by others");
                case -2147088253:
                    throw new InvalidOperationException(
                        $"Operation Failed, OptimisticConcurrency Not Enabled for " +
                        $"{updateEntity.LogicalName}");
                default:
                    throw;
            }
        }
    }

    #endregion
}
