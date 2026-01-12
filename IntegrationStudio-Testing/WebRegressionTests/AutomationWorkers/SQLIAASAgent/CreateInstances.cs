using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Playwright;
using IntegrationStudioTests.Utilities.Models;
using System.IO;
using System.Text.Json;
using WebTests;
using NUnit.Framework;

namespace IntegrationStudioTests.AutomationWorkers.SQLIAASAgent
{
    public class CreateInstances
    {
        private IPage _page;
        private IBrowser _browser;
        private IBrowserContext _context;
        private IPlaywright _playwright;

        private static readonly string BaseAppConfigDirectory = Path.Combine(AppContext.BaseDirectory, "Utilities", "Config", "CoreFunctionalTestsConfig.json");
        private const string StorageStatePath = "auth.json";
        private const string BaseUrl = "https://verify.integrationstudio.dev-connect.aveva.com/";

        public class Config
        {
            public static AdminUserData Settings = JsonSerializer.Deserialize<AdminUserData>(File.ReadAllText(BaseAppConfigDirectory)) ?? throw new Exception("Failed to read config Json");
        }

        public async Task RunAsync(string templateName, int numberOfInstances, int startingIndex = 1)
        {
            try
            {
                // Initialize Playwright and browser
                _playwright = await Playwright.CreateAsync();
                _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = false // Set to true for headless mode
                });

                // Ensure authentication is set up
                await EnsureAuthenticatedAsync();

                // Get authenticated page
                _page = await GetAuthenticatedPageAsync();

                // Verify we're authenticated by checking the URL
                await VerifyAuthenticationAsync();

                // Navigate to projects page
                await GoToProjectsPageAsync();

                // Create multiple instances
                for (int i = startingIndex; i <= numberOfInstances; i++)
                {
                    Console.WriteLine($"Creating instance {i} of {numberOfInstances}...");

                    // Find and select template by name
                    await SelectTemplateByNameAsync(templateName);

                    // Launch instance with ordered name
                    string instanceName = $"instanceno{i}-55";
                    await LaunchInstanceAsync(instanceName);

                    Console.WriteLine($"Instance '{instanceName}' created successfully.");

                    // Wait 5 seconds before next iteration
                    if (i < numberOfInstances)
                    {
                        Console.WriteLine("Waiting 5 seconds before creating next instance...");
                        await Task.Delay(5000);
                    }
                }

                Console.WriteLine($"All {numberOfInstances - startingIndex + 1} instances created successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                // Take screenshot for debugging
                try
                {
                    if (_page != null)
                    {
                        await _page.ScreenshotAsync(new PageScreenshotOptions
                        {
                            Path = $"error-screenshot-{DateTime.Now:yyyyMMdd-HHmmss}.png"
                        });
                        Console.WriteLine($"Error screenshot saved");
                    }
                }
                catch { }

                throw;
            }
            finally
            {
                if (_page != null) await _page.CloseAsync();
                if (_context != null) await _context.CloseAsync();
                if (_browser != null) await _browser.CloseAsync();
                _playwright?.Dispose();
            }
        }

        private async Task EnsureAuthenticatedAsync()
        {
            // Always recreate authentication for reliability
            // Delete old auth file if it exists
            if (File.Exists(StorageStatePath))
            {
                Console.WriteLine("Deleting old authentication state...");
                File.Delete(StorageStatePath);
            }

            await CreateAuthenticationStateAsync();
        }

        private async Task CreateAuthenticationStateAsync()
        {
            var username = Config.Settings.AdminUser.username;
            var password = Config.Settings.AdminUser.password;
            var tenantName = Config.Settings.AdminUser.tenant;

            var context = await _browser.NewContextAsync();
            var page = await context.NewPageAsync();

            try
            {
                Console.WriteLine("Starting authentication process...");

                // Navigate to login page
                await page.GotoAsync(BaseUrl);
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                Console.WriteLine("Loaded login page");

                // Fill in login credentials
                await page.FillAsync("input[name='email']", username);
                Console.WriteLine("Filled email");

                await page.FillAsync("input[name='password']", password);
                Console.WriteLine("Filled password");

                // Submit the login form
                await page.ClickAsync("button[type='submit']");
                Console.WriteLine("Clicked submit button");

                // Wait for tenant selection and click
                var selector = $"[data-test='button_{tenantName}']";
                await page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions
                {
                    Timeout = 30000
                });
                Console.WriteLine($"Found tenant button: {tenantName}");

                await page.ClickAsync(selector);
                Console.WriteLine("Clicked tenant button");

                // Wait for redirect into Integration Studio
                await page.WaitForURLAsync("**/projects", new PageWaitForURLOptions
                {
                    Timeout = 30000
                });
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                Console.WriteLine($"Redirected to projects page: {page.Url}");

                // Save the authentication state
                await context.StorageStateAsync(new BrowserContextStorageStateOptions
                {
                    Path = StorageStatePath
                });

                Console.WriteLine($"Authentication state saved to {StorageStatePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Authentication failed: {ex.Message}");

                // Take screenshot for debugging
                await page.ScreenshotAsync(new PageScreenshotOptions
                {
                    Path = $"auth-error-{DateTime.Now:yyyyMMdd-HHmmss}.png"
                });
                Console.WriteLine("Auth error screenshot saved");

                throw;
            }
            finally
            {
                await page.CloseAsync();
                await context.CloseAsync();
            }
        }

        private async Task<IPage> GetAuthenticatedPageAsync()
        {
            // Create new context with saved authentication state
            _context = await _browser.NewContextAsync(new BrowserNewContextOptions
            {
                StorageStatePath = StorageStatePath
            });

            return await _context.NewPageAsync();
        }

        private async Task VerifyAuthenticationAsync()
        {
            Console.WriteLine("Verifying authentication...");

            // Navigate to a page to verify auth works
            await _page.GotoAsync($"{BaseUrl}projects");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var currentUrl = _page.Url;
            Console.WriteLine($"Current URL: {currentUrl}");

            // Check if we're redirected to login (auth failed)
            if (currentUrl.Contains("signin") || currentUrl.Contains("login"))
            {
                throw new Exception("Authentication verification failed - redirected to login page");
            }

            Console.WriteLine("Authentication verified successfully");
        }

        private async Task GoToProjectsPageAsync()
        {
            await _page.GotoAsync($"{BaseUrl}projects");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            Console.WriteLine("Navigated to Projects page");
        }

        private async Task SelectTemplateByNameAsync(string templateName)
        {
            Console.WriteLine($"Selecting template: {templateName}");

            // Find the table row containing the template name
            var templateRow = _page.Locator($"tr.project-table-row:has-text('{templateName}')");
            await templateRow.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

            // Find the three-dots button (more icon) within that row
            var moreButton = templateRow.Locator("button.project-more-icon");
            await moreButton.ClickAsync();

            Console.WriteLine("Clicked three-dots menu button");

            // Wait for the dropdown menu to appear and click "New instance"
            var newInstanceMenuItem = _page.Locator("li#project-menu-newsession");
            await newInstanceMenuItem.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            await newInstanceMenuItem.ClickAsync();

            Console.WriteLine("Clicked 'New instance' menu item");

            // Wait for the new instance page to load
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        private async Task LaunchInstanceAsync(string instanceName)
        {
            Console.WriteLine($"Launching instance with name: {instanceName}");

            // Wait for the instance name input field to be visible
            var instanceNameInput = _page.Locator("input#textbox-session-name-enter[name='instanceAlias']");
            await instanceNameInput.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

            // Fill in the instance name
            await instanceNameInput.FillAsync(instanceName);
            Console.WriteLine($"Filled instance name: {instanceName}");

            // Click the "Launch instance" button
            var launchButton = _page.Locator("button#create-session-confirm:has-text('Launch instance')");
            await launchButton.ClickAsync();

            Console.WriteLine("Clicked 'Launch instance' button");

            // Wait for the instance to be created (navigation back to projects page)
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Optional: Wait for URL to confirm we're back on projects page
            await _page.WaitForURLAsync($"{BaseUrl}projects", new PageWaitForURLOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });
        }
    }
}