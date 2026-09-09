using Microsoft.Extensions.Options;
using Microsoft.PowerPlatform.Dataverse.Client;
using Crm.ClientWebAPI.Models;

namespace Crm.ClientWebAPI.Services;

public class CrmServiceManager
{
    private readonly CrmConfig _crmConfig;
    private ServiceClient? _serviceClient;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    public CrmServiceManager(IOptions<CrmConfig> crmConfig)
    {
        _crmConfig = crmConfig.Value;
    }

    public ServiceClient Service
    {
        get
        {
            if (!_initialized)
                InitializeAsync().GetAwaiter().GetResult();
            return _serviceClient
                ?? throw new InvalidOperationException("CRM Service Client not initialized");
        }
    }

    public CrmConfig Config => _crmConfig;

    public async Task<CrmConnectionInfo> InitializeAsync()
    {
        await _initLock.WaitAsync();
        try
        {
            if (_initialized && _serviceClient?.IsReady == true)
            {
                return new CrmConnectionInfo
                {
                    OrganizationName = _serviceClient.ConnectedOrgFriendlyName,
                    UserId = GetCurrentUserId()
                };
            }

            _serviceClient = new ServiceClient(_crmConfig.CrmConnString);

            if (!_serviceClient.IsReady)
            {
                return new CrmConnectionInfo
                {
                    Error = $"CRM Service Client Init Error: {_serviceClient.LastError}"
                };
            }

            _initialized = true;

            return new CrmConnectionInfo
            {
                OrganizationName = _serviceClient.ConnectedOrgFriendlyName,
                UserId = GetCurrentUserId()
            };
        }
        finally
        {
            _initLock.Release();
        }
    }

    public bool IsReady => _initialized && _serviceClient?.IsReady == true;

    public string OrgFriendlyName => _serviceClient?.ConnectedOrgFriendlyName ?? "Not Connected";

    private Guid GetCurrentUserId()
    {
        var request = new Microsoft.Crm.Sdk.Messages.WhoAmIRequest();
        var response = (Microsoft.Crm.Sdk.Messages.WhoAmIResponse)_serviceClient!.Execute(request);
        return response.UserId;
    }
}
