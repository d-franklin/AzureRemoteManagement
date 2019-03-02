using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Extensions.Configuration;

namespace AzureRemoteManagement
{
  public class Program
  {
    public static async Task Main(string[] args)
    {
      IConfigurationBuilder builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddUserSecrets<ManagementExample>();
      IConfigurationRoot configuration = builder.Build();
      IConfigurationSection c = configuration.GetSection("ManagementExample");
      IConfigurationSection clientId = c.GetSection("ClientId");
      IConfigurationSection clientSecret = c.GetSection("ClientSecret");
      IConfigurationSection tenantId = c.GetSection("TenantId");

      AzureCredentials credentials = SdkContext.AzureCredentialsFactory.FromServicePrincipal(clientId.Value, clientSecret.Value, tenantId.Value, AzureEnvironment.AzureGlobalCloud);
      IAzure azure = Azure
        .Configure()
        .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
        .Authenticate(credentials)
        .WithDefaultSubscription();

      string rgName = SdkContext.RandomResourceName("test_", 4);

      try
      {
        // List resource groups
        Console.WriteLine("Resource Groups:");
        IPagedCollection<IResourceGroup> rgs = await azure.ResourceGroups.ListAsync();
        if (rgs.Any())
        {
          foreach (IResourceGroup l in rgs)
          {
            Console.WriteLine($"-- {l.Name}");
          }
        }
        else
        {
          Console.WriteLine("-- No Resource Groups");
        }

        
        // Create resource group
        await azure.ResourceGroups
          .Define(rgName)
          .WithRegion(Region.USWest)
          .CreateAsync();
        Console.WriteLine($"Created Resource Group: {rgName}");


        // Create storage account.
        string saName = SdkContext.RandomResourceName("test", 4);
        await azure.StorageAccounts
          .Define(saName)
          .WithRegion(Region.USWest)
          .WithExistingResourceGroup(rgName)
          .CreateAsync();
        Console.WriteLine($"Created Storage Account: {saName}");

        Console.WriteLine();
        Console.WriteLine("Press any key to delete all resource groups...");
        Console.ReadLine();
      }
      catch (Exception exception)
      {
        Console.WriteLine($"Exception: {exception.Message}");
      }
      finally
      {
        try
        {
          // Delete all resource groups
          Console.WriteLine($"Deleting Resource Groups:");
          foreach (IResourceGroup l in await azure.ResourceGroups.ListAsync())
          {
            await azure.ResourceGroups.DeleteByNameAsync(l.Name);
            Console.WriteLine($"-- {rgName} DELETED");
          }
        }
        catch (Exception exception)
        {
          Console.WriteLine($"Exception: {exception.Message}");
        }
      }

      Console.ReadLine();
    }
  }
}
