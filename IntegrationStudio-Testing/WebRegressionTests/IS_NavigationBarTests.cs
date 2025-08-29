using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebRegressionTests
{
    public class IS_NavigationBarTests : PageTest
    {
        [Test]
        public async Task ShouldOpenPlaywrightWebsite()
        {
            // Navigate to Playwright.dev
            await Page.GotoAsync("https://internal.integrationstudio.capdev-connect.aveva.com/");

            // Assert title contains "Playwright"
            StringAssert.Contains("Integration Studio", await Page.TitleAsync());

            // Take a screenshot (saved into bin/test-results)
            await Page.ScreenshotAsync(new()
            {
                Path = "IntegrationStudio-home.png"
            });
        }
    }
}
