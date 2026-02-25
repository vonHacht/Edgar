


namespace TestEdgar
{
    public class TestProgram
    {
        [Fact]
        public async Task TestMain()
        {
            await Program.Main(Utilities.ArgsCheck);
        }

    }
}
