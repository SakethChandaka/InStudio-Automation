using IntegrationStudioTests.AutomationWorkers.SQLIAASAgent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationStudioTests.AutomationWorkers
{
    [TestFixture]
    public class RunAutomationWorker
    {
        [Test]
        public async Task RunInstanceCreation()
        {
            var worker = new CreateInstances();
            await worker.RunAsync("55InstancesSQLIAAS2023", 55, 56);
        }
    }
}
