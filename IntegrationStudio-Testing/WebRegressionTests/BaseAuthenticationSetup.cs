using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;
using System.IO;
using System.Text.Json;
using IntegrationStudioTests.Utilities.Models;
using System.ComponentModel;

namespace WebTests
{
  
    public class BaseAuthenticationState : PageTest
    {
        private static readonly string BaseAppConfigDirectory = Path.Combine(AppContext.BaseDirectory, "Utilities", "Config", "CoreFunctionalTestsConfig.json");

        public class Config
        {
            public static AdminUserData Settings = JsonSerializer.Deserialize<AdminUserData>(System.IO.File.ReadAllText(BaseAppConfigDirectory)) ?? throw new Exception("Failed to read config Json");
        }

        private string Username;
        private string Password;
        private string tenantName;

        private const string StorageStatePath = "auth.json";
        public const string BaseUrl = "https://verify.integrationstudio.dev-connect.aveva.com/"; // Replace with your actual login URL


        [OneTimeSetUp]
        public async Task EnsureAuthenticatedAsync()
        {
            Username = Config.Settings.AdminUser.username; // Change to environment variables in Future
            Password = Config.Settings.AdminUser.password; // Change to environment variables
            tenantName = Config.Settings.AdminUser.tenant; // Tenant name

            if (!System.IO.File.Exists(StorageStatePath))
            {
                await CreateAuthenticationStateAsync();
            }
        }

        private async Task CreateAuthenticationStateAsync()
        {
            // Create a new browser context for authentication
            using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false // Set to false for debugging
            });

            var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();

            try
            {
                // Navigate to login page
                await page.GotoAsync(BaseUrl);

                // Wait for login form to be visible
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                // Fill in login credentials - adjust selectors based on your actual login form
                await page.FillAsync("input[name='email']", Username); // Adjust selector as needed
                await page.FillAsync("input[name='password']", Password); // Adjust selector as needed

                // Submit the login form
                await page.ClickAsync("button[type='submit']"); // Adjust selector as needed

                // Wait for successful login - adjust based on your app's behavior after login
                //await page.WaitForURLAsync("https://profile.capdev-connect.aveva.com/solutions?/**"); Adjust expected URL pattern

                // Alternative: Wait for specific element that appears after successful login
                var selector = $"[data-test='button_{tenantName}']";
                await page.WaitForSelectorAsync(selector);

                await page.ClickAsync(selector);

                // 4. Wait for redirect into Integration Studio (very important!)
                await page.WaitForURLAsync("https://internal.integrationstudio.capdev-connect.aveva.com/projects");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                // Optionally check title
                StringAssert.Contains("Integration Studio", await page.TitleAsync());

                // Save the authentication state
                await context.StorageStateAsync(new BrowserContextStorageStateOptions
                {
                    Path = StorageStatePath
                });

                TestContext.WriteLine($"Authentication state saved to {StorageStatePath}");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"Authentication failed: {ex.Message}");
                throw;
            }
            finally
            {
                await page.CloseAsync();
                await context.CloseAsync();
                await browser.CloseAsync();
            }
        }

        protected async Task<IPage> GetAuthenticatedPageAsync()
        {
            // Ensure authentication state exists
            if (!System.IO.File.Exists(StorageStatePath))
            {
                await CreateAuthenticationStateAsync();
            }

            // Create new context with saved authentication state
            var context = await Browser.NewContextAsync(new BrowserNewContextOptions
            {
                StorageStatePath = StorageStatePath
            });

            var page = await context.NewPageAsync();
            return page;
        }

        protected async Task<IBrowserContext> GetAuthenticatedContextAsync()
        {
            // Ensure authentication state exists
            if (!System.IO.File.Exists(StorageStatePath))
            {
                await CreateAuthenticationStateAsync();
            }

            // Create new context with saved authentication state
            return await Browser.NewContextAsync(new BrowserNewContextOptions
            {
                StorageStatePath = StorageStatePath
            });
        }

        [OneTimeTearDown]
        public async Task CleanupAsync()
        {
            // Optionally clean up auth file after all tests
            if (System.IO.File.Exists(StorageStatePath))
            {
                try
                {
                    System.IO.File.Delete(StorageStatePath);
                    TestContext.WriteLine("Authentication state file cleaned up");
                }
                catch (Exception ex)
                {
                    TestContext.WriteLine($"Warning: Could not delete auth file: {ex.Message}");
                }
            }
        }
    }
}