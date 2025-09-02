using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace WebTests.RegressionTests
{
    public class IS_NavigationBarTests : BaseAuthenticationState
    {
        private async Task<IPage> GoToIntegrationStudioAsync()
        {
            var page = await GetAuthenticatedPageAsync();
            await page.GotoAsync("https://internal.integrationstudio.capdev-connect.aveva.com/");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            return page;
        }

        private async Task<IPage> OpenMenuAsync()
        {
            var page = await GoToIntegrationStudioAsync();
            var menuButton = page.Locator("[aria-label='Open menu']:visible").First;
            await Expect(menuButton).ToBeVisibleAsync();
            await menuButton.ClickAsync();
            var menu = page.Locator("section.mdc-suite-menu.mdc-menu-surface--open");
            await Expect(menu).ToBeVisibleAsync();
            return page;
        }

        [Test]
        public async Task OpenIntegrationStudio_Dashboard()
        {
            // Get an authenticated page instead of using the default Page
            var page = await GoToIntegrationStudioAsync();

            // Navigate to Integration Studio website
            await page.GotoAsync($"{BaseUrl}/projects");

            // Assert title contains "Integration Studio"
            StringAssert.Contains("Integration Studio", await page.TitleAsync());

            // Take a screenshot (saved into bin/test-results)
            await page.ScreenshotAsync(new()
            {
                Path = "IntegrationStudio-home.png"
            });
        }

        [Test]
        public async Task NotificationsButton_ShouldOpenAndClosePanel()
        {
            var page = await GoToIntegrationStudioAsync();

            var notifButton = page.Locator("[aria-label='Notifications']").First;
            await Expect(notifButton).ToBeVisibleAsync();

            await notifButton.ClickAsync();
            var notifWindow = page.Locator("#notifyHub:visible");
            await Expect(notifWindow).ToBeVisibleAsync();

            var closeBtn = page.Locator("#notifyHeader [data-testid='CloseIcon']");
            await closeBtn.ClickAsync();

            await page.ScreenshotAsync(new() { Path = "IntegrationStudio-notifications.png" });
        }

        [Test]
        public async Task HelpButton_ShouldOpenDocumentationInNewTab()
        {
            var page = await GoToIntegrationStudioAsync();

            var helpButton = page.Locator("[aria-label='Help']").First;
            await Expect(helpButton).ToBeVisibleAsync();

            var popupTask = page.WaitForPopupAsync();
            await helpButton.ClickAsync();
            var helpPage = await popupTask;

            await helpPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
            StringAssert.Contains("AVEVA™ Documentation", await helpPage.TitleAsync());

            await helpPage.ScreenshotAsync(new() { Path = "IntegrationStudio-help.png" });
        }

        [Test]
        public async Task OpenMenuButton_ShouldDisplayMenu()
        {
            var page = await GoToIntegrationStudioAsync();

            var menuButton = page.Locator("[aria-label='Open menu']:visible").First;
            await Expect(menuButton).ToBeVisibleAsync();

            await menuButton.ClickAsync();

            var menu = page.Locator("section.mdc-suite-menu.mdc-menu-surface--open");
            await Expect(menu).ToBeVisibleAsync();

            await page.ScreenshotAsync(new() { Path = "IntegrationStudio-openmenu.png" });
        }

        [Test]
        public async Task OpenMenu_ShouldContain_TenantName()
        {
            var page = await OpenMenuAsync();
            await Expect(page.Locator("text=Tenant Test 1")).ToBeVisibleAsync();

        }

        [Test]
        public async Task OpenMenu_ShouldContain_NetworkSpeedTest()
        {
            var page = await OpenMenuAsync();
            await Expect(page.Locator("text=Network Speed Test")).ToBeVisibleAsync();

        }


        [Test]
        public async Task OpenMenu_ShouldContain_Logout()
        {
            var page = await OpenMenuAsync();
            await Expect(page.Locator("text=Log Out")).ToBeVisibleAsync();

        }

        [Test]
        public async Task OpenMenu_NetworkSpeedTest_ShouldOpenSpeedTestWindow()
        {
            var page = await OpenMenuAsync();

            var networkSpeedTestButton = page.Locator("[title = 'Network Speed Test']:visible");

            var popupTask = page.WaitForPopupAsync();
            await networkSpeedTestButton.ClickAsync();
            var networkSpeedTestPage = await popupTask;

            await networkSpeedTestPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Expect(networkSpeedTestPage).ToHaveURLAsync(
                "https://internal.integrationstudio.capdev-connect.aveva.com/speedtest"
            );

            await networkSpeedTestPage.ScreenshotAsync(new() { Path = "IntegrationStudio-help.png" });

        }

        [Test]
        public async Task OpenMenu_Logout_ShouldLogoutUser()
        {
            var page = await OpenMenuAsync();

            var logoutButton = page.Locator("[title = 'Log Out']:visible");
            // Click and wait for any navigation
            var navigationTask = page.WaitForNavigationAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });
            await logoutButton.ClickAsync();
            await navigationTask;

            // Now explicitly wait until we land on the final login page
            await page.WaitForURLAsync(new Regex("https://signin\\.dev-connect\\.aveva\\.com/.*login.*"));

            // Assert
            await Expect(page).ToHaveURLAsync(new Regex("https://signin\\.dev-connect\\.aveva\\.com/.*login.*"));

            await page.ScreenshotAsync(new()
            {
                Path = "IntegrationStudio-logout.png"
            });
        }

    }
}