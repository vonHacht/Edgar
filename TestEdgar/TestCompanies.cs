using Edgar.Config;
using Edgar.Companies;

using System.Runtime;

namespace TestEdgar
{
    public class TestCompanies
    {
        private AppSettings? _appSettings;
        private CompaniesService? _companyService;

        private readonly string _edgarRoot = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Edgar");

        [Fact]
        public void Test1()
        {
            _appSettings = AppSettings.Load(_edgarRoot);

            _companyService = new CompaniesService(_appSettings);

        }
    }
}
