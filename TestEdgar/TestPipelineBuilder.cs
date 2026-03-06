using System;
using System.Collections.Generic;
using System.Text;

using Edgar.Config;
using Edgar.Pipeline;

namespace TestEdgar
{
    public class TestPipelineBuilder
    {
        [Fact]
        public async Task TestYears()
        {
            PipelineBuilder pipelineBuilder = new PipelineBuilder(Utilities.Settings);

            pipelineBuilder.RunAsync();
        }
    }
}
