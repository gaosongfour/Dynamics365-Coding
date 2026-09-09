using Microsoft.AspNetCore.Mvc;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Crm.Sdk.Messages;
using Crm.ClientWebAPI.Models;
using Crm.ClientWebAPI.Services;

namespace Crm.ClientWebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SecurityOperationsController : ControllerBase
{
    private readonly CrmServiceManager _serviceManager;
    private readonly CrmOperationsHelper _helper;

    public SecurityOperationsController(
        CrmServiceManager serviceManager, CrmOperationsHelper helper)
    {
        _serviceManager = serviceManager;
        _helper = helper;
    }

    #region User Security

    /// <summary>
    /// Get current user's security roles
    /// </summary>
    [HttpGet("user/roles")]
    public IActionResult GetUserRoles([FromQuery] Guid? userId = null)
    {
        try
        {
            var client = _serviceManager.Service;
            var targetUserId = userId ?? _helper.WhoAmI();

            var query = new QueryExpression
            {
                EntityName = "role",
                ColumnSet = new ColumnSet("name")
            };

            var linkEntity = new LinkEntity(
                "role", "systemuserroles", "roleid", "roleid", JoinOperator.Inner);
            linkEntity.LinkCriteria = new FilterExpression();
            linkEntity.LinkCriteria.AddCondition(
                "systemuserid", ConditionOperator.Equal, targetUserId);
            query.LinkEntities.Add(linkEntity);

            var result = client.RetrieveMultiple(query);
            var roles = result.Entities
                .Select(e => e.GetAttributeValue<string>("name"))
                .ToList();

            return Ok(OperationResult<object>.Ok(new
            {
                UserId = targetUserId,
                RoleCount = roles.Count,
                Roles = roles
            }));
        }
        catch (Exception ex)
        {
            return StatusCode(500, OperationResult.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Get teams the user belongs to
    /// </summary>
    [HttpGet("user/teams")]
    public IActionResult GetUserTeams([FromQuery] Guid? userId = null)
    {
        try
        {
            var client = _serviceManager.Service;
            var targetUserId = userId ?? _helper.WhoAmI();

            var query = new QueryExpression
            {
                EntityName = "team",
                ColumnSet = new ColumnSet("name", "isdefault", "teamtype")
            };

            var linkEntity = new LinkEntity(
                "team", "teammembership", "teamid", "teamid", JoinOperator.Inner);
            linkEntity.LinkCriteria = new FilterExpression();
            linkEntity.LinkCriteria.AddCondition(
                "systemuserid", ConditionOperator.Equal, targetUserId);
            query.LinkEntities.Add(linkEntity);

            var result = client.RetrieveMultiple(query);
            var teams = result.Entities.Select(e => new UserTeamInfo
            {
                Name = e.GetAttributeValue<string>("name"),
                IsDefault = e.FormattedValues.ContainsKey("isdefault")
                    ? e.FormattedValues["isdefault"] : null,
                TeamType = e.FormattedValues.ContainsKey("teamtype")
                    ? e.FormattedValues["teamtype"] : null
            }).ToList();

            return Ok(OperationResult<object>.Ok(new
            {
                UserId = targetUserId,
                TeamCount = teams.Count,
                Teams = teams
            }));
        }
        catch (Exception ex)
        {
            return StatusCode(500, OperationResult.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Get active users by role name(s)
    /// </summary>
    [HttpGet("user/by-role")]
    public IActionResult GetUsersByRole([FromQuery] string roleNames)
    {
        try
        {
            var client = _serviceManager.Service;
            var roleNameArray = roleNames.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (roleNameArray.Length == 0)
                return BadRequest(OperationResult.Fail("roleNames parameter is required"));

            var roleNameValueString = string.Join(
                "", roleNameArray.Select(r => $"<value>{r.Trim()}</value>"));

            var fetchXml = $@"
            <fetch mapping='logical'>
              <entity name='systemuser'>
                <attribute name='domainname'/>
                <attribute name='fullname'/>
                <filter>
                  <condition attribute='isdisabled' operator='eq' value='false'/>
                </filter>
                <link-entity name='systemuserroles' alias='ur'
                             to='systemuserid' from='systemuserid' link-type='inner'>
                  <link-entity name='role' alias='r'
                               to='roleid' from='roleid' link-type='inner'>
                    <attribute name='name'/>
                    <filter>
                      <condition attribute='name' operator='in'>
                       {roleNameValueString}
                      </condition>
                    </filter>
                  </link-entity>
                </link-entity>
              </entity>
            </fetch>";

            var result = client.RetrieveMultiple(new FetchExpression(fetchXml));
            var users = result.Entities.Select(e => new
            {
                FullName = e.GetAttributeValue<string>("fullname"),
                Role = _helper.GetAliasedAttributeValue<string>(e, "r.name")
            }).ToList();

            return Ok(OperationResult<object>.Ok(users));
        }
        catch (Exception ex)
        {
            return StatusCode(500, OperationResult.Fail(ex.Message));
        }
    }

    #endregion

    #region User Settings

    /// <summary>
    /// Get user settings
    /// </summary>
    [HttpGet("user/settings")]
    public IActionResult GetUserSettings([FromQuery] Guid? userId = null)
    {
        try
        {
            var client = _serviceManager.Service;
            var targetUserId = userId ?? _helper.WhoAmI();

            var query = new QueryByAttribute
            {
                EntityName = "usersettings",
                ColumnSet = new ColumnSet(true),
                Attributes = { "systemuserid" },
                Values = { targetUserId }
            };

            var result = client.RetrieveMultiple(query);
            if (result.Entities.Count != 1)
                return NotFound(OperationResult.Fail(
                    $"User settings not found for user {targetUserId}"));

            var settings = result.Entities.First();
            var info = new UserSettingsInfo
            {
                CurrencySymbol = settings.GetAttributeValue<string>("currencysymbol"),
                DateFormatString = settings.GetAttributeValue<string>("dateformatstring"),
                DecimalSymbol = settings.GetAttributeValue<string>("decimalsymbol"),
                PagingLimit = settings.GetAttributeValue<int>("paginglimit"),
                TimeZoneCode = settings.GetAttributeValue<int>("timezonecode"),
                UiLanguageId = settings.GetAttributeValue<int>("uilanguageid")
            };

            return Ok(OperationResult<object>.Ok(info));
        }
        catch (Exception ex)
        {
            return StatusCode(500, OperationResult.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Get user license information
    /// </summary>
    [HttpGet("user/license")]
    public IActionResult GetUserLicense([FromQuery] Guid? userId = null)
    {
        try
        {
            var client = _serviceManager.Service;
            var targetUserId = userId ?? _helper.WhoAmI();

            var request = new RetrieveUserLicenseInfoRequest
            {
                SystemUserId = targetUserId
            };
            var response = (RetrieveUserLicenseInfoResponse)client.Execute(request);

            var licenses = response.licenseInfo.ServicePlans.Select(sp => new Models.UserLicenseInfo
            {
                DisplayName = sp.DisplayName,
                Name = sp.Name
            }).ToList();

            return Ok(OperationResult<object>.Ok(licenses));
        }
        catch (Exception ex)
        {
            return StatusCode(500, OperationResult.Fail(ex.Message));
        }
    }

    #endregion

    #region Principal Access (Sharing)

    /// <summary>
    /// Get current user's access rights on a specific record
    /// </summary>
    [HttpGet("principal-access")]
    public IActionResult GetPrincipalAccess(
        [FromQuery] string entityName,
        [FromQuery] Guid entityId,
        [FromQuery] Guid? principalId = null,
        [FromQuery] string? principalEntityName = null)
    {
        try
        {
            var client = _serviceManager.Service;
            var principal = principalId ?? _helper.WhoAmI();
            var principalType = principalEntityName ?? "systemuser";

            var request = new RetrievePrincipalAccessRequest
            {
                Principal = new EntityReference(principalType, principal),
                Target = new EntityReference(entityName, entityId)
            };

            var response = (RetrievePrincipalAccessResponse)client.Execute(request);

            return Ok(OperationResult<object>.Ok(new
            {
                AccessRights = response.AccessRights.ToString()
            }));
        }
        catch (Exception ex)
        {
            return StatusCode(500, OperationResult.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Get all shared principals and their access rights on a record
    /// </summary>
    [HttpGet("shared-access")]
    public IActionResult GetSharedAccess(
        [FromQuery] string entityName, [FromQuery] Guid entityId)
    {
        try
        {
            var client = _serviceManager.Service;

            var request = new RetrieveSharedPrincipalsAndAccessRequest
            {
                Target = new EntityReference(entityName, entityId)
            };

            var response = (RetrieveSharedPrincipalsAndAccessResponse)client.Execute(request);
            var accesses = response.PrincipalAccesses.Select(pa => new PrincipalAccessInfo
            {
                PrincipalType = pa.Principal.LogicalName,
                PrincipalId = pa.Principal.Id,
                AccessMask = pa.AccessMask.ToString()
            }).ToList();

            return Ok(OperationResult<object>.Ok(new
            {
                Count = accesses.Count,
                Accesses = accesses
            }));
        }
        catch (Exception ex)
        {
            return StatusCode(500, OperationResult.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Share a record with a principal (user/team)
    /// </summary>
    [HttpPost("share")]
    public IActionResult ShareRecord([FromBody] ShareRecordRequest request)
    {
        try
        {
            var client = _serviceManager.Service;
            var principalType = request.PrincipalEntityName ?? "systemuser";

            var grantRequest = new GrantAccessRequest
            {
                PrincipalAccess = new PrincipalAccess
                {
                    AccessMask = request.AccessRights,
                    Principal = new EntityReference(principalType, request.PrincipalId)
                },
                Target = new EntityReference(request.EntityName, request.EntityId)
            };

            client.Execute(grantRequest);

            return Ok(OperationResult.Ok(
                $"Access rights {request.AccessRights} granted"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, OperationResult.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Modify shared access rights on a record
    /// </summary>
    [HttpPost("share/modify")]
    public IActionResult ModifyShareAccess([FromBody] ShareRecordRequest request)
    {
        try
        {
            var client = _serviceManager.Service;
            var principalType = request.PrincipalEntityName ?? "systemuser";

            var modifyRequest = new ModifyAccessRequest
            {
                PrincipalAccess = new PrincipalAccess
                {
                    AccessMask = request.AccessRights,
                    Principal = new EntityReference(principalType, request.PrincipalId)
                },
                Target = new EntityReference(request.EntityName, request.EntityId)
            };

            client.Execute(modifyRequest);

            return Ok(OperationResult.Ok(
                $"Access rights modified to {request.AccessRights}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, OperationResult.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Revoke (unshare) access rights from a principal
    /// </summary>
    [HttpPost("unshare")]
    public IActionResult UnshareRecord([FromBody] UnshareRecordRequest request)
    {
        try
        {
            var client = _serviceManager.Service;
            var revokeeType = request.RevokeeEntityName ?? "systemuser";

            var revokeRequest = new RevokeAccessRequest
            {
                Target = new EntityReference(request.EntityName, request.EntityId),
                Revokee = new EntityReference(revokeeType, request.RevokeeId)
            };

            client.Execute(revokeRequest);

            return Ok(OperationResult.Ok("Access rights revoked"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, OperationResult.Fail(ex.Message));
        }
    }

    #endregion

    #region Field Security

    /// <summary>
    /// Create field permissions for all field security profiles
    /// </summary>
    [HttpPost("field-security/permissions")]
    public IActionResult CreateFieldPermissions([FromBody] FieldPermissionInfo request)
    {
        try
        {
            var client = _serviceManager.Service;

            // Get all field security profiles
            var profileQuery = new QueryExpression("fieldsecurityprofile")
            {
                ColumnSet = new ColumnSet("name")
            };
            var profileResult = client.RetrieveMultiple(profileQuery);

            // Get existing permissions
            var permissionQuery = new QueryExpression("fieldpermission")
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression(
                            "entityname", ConditionOperator.Equal, request.EntityName),
                        new ConditionExpression(
                            "attributelogicalname", ConditionOperator.Equal,
                            request.AttributeName),
                    }
                }
            };
            var permissionResult = client.RetrieveMultiple(permissionQuery);

            var results = new List<object>();
            foreach (var profileEntity in profileResult.Entities)
            {
                var profileName = profileEntity.GetAttributeValue<string>("name");
                try
                {
                    if (permissionResult.Entities.Any(p =>
                        p.GetAttributeValue<EntityReference>("fieldsecurityprofileid").Id
                        == profileEntity.Id))
                    {
                        results.Add(new
                        {
                            ProfileId = profileEntity.Id,
                            ProfileName = profileName,
                            Status = "exists"
                        });
                    }
                    else
                    {
                        var entity = new Entity("fieldpermission");
                        entity["fieldsecurityprofileid"] =
                            profileEntity.ToEntityReference();
                        entity["entityname"] = request.EntityName;
                        entity["attributelogicalname"] = request.AttributeName;
                        entity["canread"] = new OptionSetValue(4);
                        entity["cancreate"] = new OptionSetValue(0);
                        entity["canupdate"] = new OptionSetValue(0);

                        var permissionId = client.Create(entity);
                        results.Add(new
                        {
                            ProfileId = profileEntity.Id,
                            ProfileName = profileName,
                            Status = "created",
                            PermissionId = permissionId
                        });
                    }
                }
                catch (Exception ex)
                {
                    results.Add(new
                    {
                        ProfileId = profileEntity.Id,
                        ProfileName = profileName,
                        Status = "error",
                        Error = ex.Message
                    });
                }
            }

            return Ok(OperationResult<object>.Ok(results));
        }
        catch (Exception ex)
        {
            return StatusCode(500, OperationResult.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Get all field security profiles
    /// </summary>
    [HttpGet("field-security/profiles")]
    public IActionResult GetFieldSecurityProfiles()
    {
        try
        {
            var client = _serviceManager.Service;
            var query = new QueryExpression("fieldsecurityprofile")
            {
                ColumnSet = new ColumnSet("name")
            };

            var result = client.RetrieveMultiple(query);
            var profiles = result.Entities.Select(e => new
            {
                Id = e.Id,
                Name = e.GetAttributeValue<string>("name")
            }).ToList();

            return Ok(OperationResult<object>.Ok(profiles));
        }
        catch (Exception ex)
        {
            return StatusCode(500, OperationResult.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Get field security permissions for a specific entity and attribute
    /// </summary>
    [HttpGet("field-security/permissions")]
    public IActionResult GetFieldSecurityPermissions(
        [FromQuery] string entityName, [FromQuery] string attributeName)
    {
        try
        {
            var client = _serviceManager.Service;
            var query = new QueryExpression("fieldpermission")
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression(
                            "entityname", ConditionOperator.Equal, entityName),
                        new ConditionExpression(
                            "attributelogicalname", ConditionOperator.Equal, attributeName),
                    }
                }
            };

            var result = client.RetrieveMultiple(query);
            var permissions = result.Entities.Select(e => new
            {
                Id = e.Id,
                ProfileId = e.GetAttributeValue<EntityReference>(
                    "fieldsecurityprofileid")?.Id,
                ProfileName = e.GetAttributeValue<EntityReference>(
                    "fieldsecurityprofileid")?.Name,
                EntityName = e.GetAttributeValue<string>("entityname"),
                AttributeName = e.GetAttributeValue<string>("attributelogicalname"),
                CanRead = e.GetAttributeValue<OptionSetValue>("canread")?.Value,
                CanCreate = e.GetAttributeValue<OptionSetValue>("cancreate")?.Value,
                CanUpdate = e.GetAttributeValue<OptionSetValue>("canupdate")?.Value
            }).ToList();

            return Ok(OperationResult<object>.Ok(permissions));
        }
        catch (Exception ex)
        {
            return StatusCode(500, OperationResult.Fail(ex.Message));
        }
    }

    #endregion
}
