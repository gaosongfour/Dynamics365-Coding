namespace Crm.ClientWebAPI.Models;

public class CrmConfig
{
    public string CrmUrl { get; set; } = string.Empty;
    public string CrmAppId { get; set; } = string.Empty;
    public string DiscoveryServiceUri { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserPwd { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public string CrmConnRawString { get; set; } = string.Empty;

    public string CrmConnString =>
        string.Format(CrmConnRawString, UserName, UserPwd, CrmUrl, CrmAppId, RedirectUri);
}
