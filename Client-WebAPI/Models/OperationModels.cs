using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Crm.ClientWebAPI.Models;

public class OperationResult<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }

    public static OperationResult<T> Ok(T data, string? message = null) =>
        new() { Success = true, Data = data, Message = message };

    public static OperationResult<T> Fail(string error) =>
        new() { Success = false, Error = error };
}

public class OperationResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }

    public static OperationResult Ok(string? message = null) =>
        new() { Success = true, Message = message };

    public static OperationResult Fail(string error) =>
        new() { Success = false, Error = error };
}

public class EntityAttributeInfo
{
    public string AttributeName { get; set; } = string.Empty;
    public string AttributeType { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? FormattedValue { get; set; }
}

public class EntityInfo
{
    public string LogicalName { get; set; } = string.Empty;
    public Guid Id { get; set; }
    public List<EntityAttributeInfo> Attributes { get; set; } = [];
}

public class EntityCollectionInfo
{
    public string? EntityName { get; set; }
    public int Count { get; set; }
    public List<EntityBasicInfo> Entities { get; set; } = [];
}

public class EntityBasicInfo
{
    public string LogicalName { get; set; } = string.Empty;
    public Guid Id { get; set; }
    public string? DisplayName { get; set; }
}

public class CrmConnectionInfo
{
    public string OrganizationName { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string? Error { get; set; }
}

public class UserTeamInfo
{
    public string Name { get; set; } = string.Empty;
    public string? IsDefault { get; set; }
    public string? TeamType { get; set; }
}

public class UserLicenseInfo
{
    public string DisplayName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class UserSettingsInfo
{
    public string? CurrencySymbol { get; set; }
    public string? DateFormatString { get; set; }
    public string? DecimalSymbol { get; set; }
    public int PagingLimit { get; set; }
    public int TimeZoneCode { get; set; }
    public int UiLanguageId { get; set; }
}

public class PrincipalAccessInfo
{
    public string PrincipalType { get; set; } = string.Empty;
    public Guid PrincipalId { get; set; }
    public string AccessMask { get; set; } = string.Empty;
}

public class ShareRecordRequest
{
    public string EntityName { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public Guid PrincipalId { get; set; }
    public string? PrincipalEntityName { get; set; }
    public AccessRights AccessRights { get; set; } = AccessRights.ReadAccess;
}

public class UnshareRecordRequest
{
    public string EntityName { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public Guid RevokeeId { get; set; }
    public string? RevokeeEntityName { get; set; }
}

public class FieldPermissionInfo
{
    public string EntityName { get; set; } = string.Empty;
    public string AttributeName { get; set; } = string.Empty;
}

public class FetchXmlRequest
{
    public string FetchXml { get; set; } = string.Empty;
    public int PageSize { get; set; } = 5000;
}

public class CreateOrderItemRequest
{
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
}

public class EmailRequest
{
    public Guid? FromUserId { get; set; }
    public Guid? ToAccountId { get; set; }
    public Guid? ToContactId { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
}

public class AggregateResult
{
    public string? GroupByValue { get; set; }
    public int Count { get; set; }
    public decimal? Sum { get; set; }
}
