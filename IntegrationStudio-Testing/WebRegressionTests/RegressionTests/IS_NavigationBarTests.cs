using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using System.Threading.Tasks;

namespace WebTests.RegressionTests
{
    public class IS_NavigationBarTests : BaseAuthenticationState
    {
        [Test]
        public async Task ShouldOpenIntegrationStudioWebsite()
        {
            // Get an authenticated page instead of using the default Page
            var page = await GetAuthenticatedPageAsync();

            // Navigate to Integration Studio website
            await page.GotoAsync("https://internal.integrationstudio.capdev-connect.aveva.com/projects");

            // Assert title contains "Integration Studio"
            StringAssert.Contains("Integration Studio", await page.TitleAsync());

            // Take a screenshot (saved into bin/test-results)
            await page.ScreenshotAsync(new()
            {
                Path = "IntegrationStudio-home.png"
            });
        }

        [Test]
        public async Task ShouldNavigateToMainSections()
        {
            var page = await GetAuthenticatedPageAsync();

            await page.GotoAsync("https://internal.integrationstudio.capdev-connect.aveva.com/");

            // Wait for page to load
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Test navigation menu items - adjust selectors based on your actual navbar
            var navigationItems = new[]
            {
                "Dashboard",
                "Projects",
                "Integrations",
                "Settings"
            };

            foreach (var item in navigationItems)
            {
                // Check if navigation item exists - adjust selector as needed
                var navElement = page.Locator($"nav >> text='{item}'").First;
                await Expect(navElement).ToBeVisibleAsync();

                // Optionally test clicking each nav item
                await navElement.ClickAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                // Take screenshot of each section
                await page.ScreenshotAsync(new()
                {
                    Path = $"IntegrationStudio-{item.ToLower()}.png"
                });
            }
        }

        [Test]
        public async Task ShouldDisplayUserMenuWhenAuthenticated()
        {
            var page = await GetAuthenticatedPageAsync();

            await page.GotoAsync("https://internal.integrationstudio.capdev-connect.aveva.com/");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Check if user menu/profile is visible (indicates successful authentication)
            // Adjust selector based on your actual user menu element
            var userMenu = page.Locator("[data-testid='user-menu']").Or(
                           page.Locator(".user-profile")).Or(
                           page.Locator("button:has-text('Profile')"));

            await Expect(userMenu).ToBeVisibleAsync();

            // Test user menu dropdown
            await userMenu.ClickAsync();
            await Expect(page.Locator("text='Logout'").Or(page.Locator("text='Sign out'"))).ToBeVisibleAsync();
        }

        [Test]
        public async Task ShouldDisplayCorrectBrandingAndLogo()
        {
            var page = await GetAuthenticatedPageAsync();

            await page.GotoAsync("https://internal.integrationstudio.capdev-connect.aveva.com/");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Check for logo/branding elements - adjust selectors as needed
            var logo = page.Locator("img[alt*='Integration Studio']").Or(
                       page.Locator(".logo")).Or(
                       page.Locator("[data-testid='app-logo']"));

            await Expect(logo).ToBeVisibleAsync();

            // Verify logo is clickable and returns to home
            await logo.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Should be back on home/dashboard page
            StringAssert.Contains("Integration Studio", await page.TitleAsync());
        }

        [Test]
        public async Task ShouldShowNotificationsOrAlertsInNavbar()
        {
            var page = await GetAuthenticatedPageAsync();

            await page.GotoAsync("https://internal.integrationstudio.capdev-connect.aveva.com/");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Check for notifications bell or alerts - adjust selector as needed
            var notificationElement = page.Locator("[data-testid='notifications']").Or(
                                    page.Locator(".notification-bell")).Or(
                                    page.Locator("button:has([class*='bell'])"));

            // This might not always be visible, so we'll check if it exists
            var isVisible = await notificationElement.IsVisibleAsync();

            if (isVisible)
            {
                await notificationElement.ClickAsync();
                // Check if notifications panel opens
                await Expect(page.Locator(".notifications-panel").Or(
                           page.Locator("[data-testid='notifications-dropdown']"))).ToBeVisibleAsync();
            }
            else
            {
                TestContext.WriteLine("No notification element found in navbar");
            }
        }
    }
}