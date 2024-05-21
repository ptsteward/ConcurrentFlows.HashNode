using Azure.Identity;
using Microsoft.Extensions.Configuration;

namespace ConcurrentFlows.AzureBusSeries.Part4;

public static class CredentialBuilder
{
    public static DefaultAzureCredential CreateDefaultCredential(
        this IConfiguration config)
    {
        var tenantId = config["TenantId"];
        return new(new DefaultAzureCredentialOptions()
        {
            TenantId = tenantId
        }.SetVisualStudioCredentialingOnly());
    }

    public static DefaultAzureCredentialOptions SetVisualStudioCredentialingOnly(
        this DefaultAzureCredentialOptions options)
    {
        options.ExcludeAzureCliCredential = true;
        options.ExcludeAzureDeveloperCliCredential = true;
        options.ExcludeAzurePowerShellCredential = true;
        options.ExcludeEnvironmentCredential = true;
        options.ExcludeInteractiveBrowserCredential = true;
        options.ExcludeVisualStudioCodeCredential = true;
        options.ExcludeWorkloadIdentityCredential = true;
        options.ExcludeManagedIdentityCredential = true;
        options.ExcludeSharedTokenCacheCredential = true;
        return options;
    }
}
