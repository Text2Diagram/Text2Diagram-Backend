using Microsoft.SemanticKernel;
using Text2Diagram_Backend.Features.Flowchart;

namespace Text2Diagram_Backend.Features.Sequence.NewWay
{
    public static class Step2_CombineFlow
    {
        //private readonly Kernel kernel;
        //private readonly ILogger<AnalyzerForSequence> logger;
        //public Step2_CombineFlow(Kernel kernel, ILogger<AnalyzerForSequence> logger)
        //{
        //	this.kernel = kernel;
        //	this.logger = logger;
        //}

        public static string CombineFlowsPromt(string flowUseCases)
        {
            return @"
			You are a senior software architect.
" + Prompts.LanguageRules + @"
				You are given a use case with 3 flows: basicFlow, alternativeFlows, and exceptionFlows.

				Please analyze and merge these flows into a single sequential list of steps that represents the full logic of this use case execution — including all main, alternative, and exceptional paths.

				- Maintain the correct logical order.
				- Label alternative or exception steps clearly, e.g. ""Alternative: ..."" or ""Exception: ...""
				- Preserve necessary conditions or branches where relevant.

				Respond in the following format:

				{
				  ""useCase"": ""<use case name>"",
				  ""combinedFlow"": [
					""1. <Step from basic or alternative>"",
					""Alternative: <optional step>"",
					""Exception: <error condition if any>"",
					...
				  ]
				}

				Use the data below:
			" + flowUseCases;
        }
    }
}
