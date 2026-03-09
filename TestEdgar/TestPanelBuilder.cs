using System;
using System.Collections.Generic;
using System.Text;

using Edgar.Companies;
using Edgar.Config;
using Edgar.Pipeline;

namespace TestEdgar
{
    public class TestPanelBuilder
    {
        [Fact]
        public async Task TestCIK()
        {
            AppSettings settings = AppSettings.Load(Utilities.EdgarRoot);

            PanelBuilder panelBuilder = new PanelBuilder(settings);

            string testCik = "0001175535";

            var result = panelBuilder.RunAsync(testCik);

            Console.WriteLine(result);
        }
    }
}
