using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Text2Diagram_Backend.Common.Abstractions;
using Text2Diagram_Backend.Features.UsecaseDiagram.Components;
using Text2Diagram_Backend.Features.UsecaseDiagram.Separate;

namespace Text2Diagram_Backend.Features.UsecaseDiagram
{
    public class RegenerateUsecaseDiagram
    {
        private readonly ILLMService _llmService;
        private readonly ILogger<RegenerateUsecaseDiagram> _logger;
        private readonly UsecaseDiagramGenerator _useCaseDiagramGenerator;

        public RegenerateUsecaseDiagram(ILLMService lLMService, ILogger<RegenerateUsecaseDiagram> logger, UsecaseDiagramGenerator useCaseDiagramGenerator)
        {
            _llmService = lLMService;
            _logger = logger;
            _useCaseDiagramGenerator = useCaseDiagramGenerator;
        }

        public async Task<string> RegenerateAsync(string feedback, string diagramjson)
        {
            _logger.LogInformation("Regenerating flowchart with feedback: {Feedback}", feedback);

            var response = await ApplyCommandsAsync(feedback);
            var feedbackNode = Helpers.ValidateJson(response);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };
            var diagram = JsonSerializer.Deserialize<UseCaseDiagram>(diagramjson, options);
            var instruction = JsonSerializer.Deserialize<Instructions>(feedbackNode, options);
            var newUseCaseDiagram = Helpers.ApplyInstructions(diagram, instruction);
            var planUMLCode = _useCaseDiagramGenerator.GeneratePlantUMLCode(newUseCaseDiagram);
            _logger.LogInformation("Regenerated Use Case Diagram with feedback: {Feedback}", feedback);
            return planUMLCode;
        }

        private async Task<string> ApplyCommandsAsync(string feedback)
        {
            _logger.LogInformation("Regenerating use case diagram with feedback: {Feedback}", feedback);
            var prompt = $"""
            You are helping to update a Use Case Model based on user feedback.

            ### TASK:
            Given the following natural language feedback from the user, convert it into a structured instruction:
           
            FEEDBACK:
            "{feedback}"
            """
            +
            """
            ### INSTRUCTIONS:
            - Your task is to **interpret the user's feedback** and return a valid JSON instruction describing:
              - What action to perform (`Add`, `Remove`, `Update`)
              - On which target (`Actor`, `UseCase`, `Association`, `Include`, `Extend`, `Package`)
              - The fields needed to execute that action.

            - The JSON must contain the following fields depending on the target:

            | Target        | Action Types       | Required Fields                                                        |
            |---------------|--------------------|------------------------------------------------------------------------|
            | Actor         | Add/Remove/Update  | `Name` (and `NewName` for update)                                      |
            | UseCase       | Add/Remove/Update  | `Name` (and `NewName` for update)                                      |
            | Association   | Add/Remove         | `Actor`, `UseCase`                                                     |
            | Include       | Add/Remove         | `BaseUseCase`, `IncludedUseCase`                                      |
            | Extend        | Add/Remove         | `BaseUseCase`, `ExtendedUseCase`                                      |
            | Package       | Add/Remove/Update  | `Name` (and optionally: `NewName`, `UseCases`, `Actors`)              |

            - You can only return one instruction per output.
            - If the action is `"Update"`, include both the current `Name` and `NewName`.
            
            """
            +
            """
            ### FORMAT:
            ```json
            "Instructions": [
                {
                  "Action": "Add" | "Remove" | "Update",
                  "Target": "Actor" | "UseCase" | "Association" | "Include" | "Extend" | "Package",
                  // Optional fields depending on Target:
                  "Name": "string",
                  "NewName": "string",
                  "Actor": "string",
                  "UseCase": "string",
                  "BaseUseCase": "string",
                  "IncludedUseCase": "string",
                  "ExtendedUseCase": "string",
                  "UseCases": ["string", ...],
                  "Actors": ["string", ...]
                }
            ]
            ```
            """
            +
            """"
            ### EXAMPLE 1:
            INPUT:
            - Feedback: "Add a new use case called 'Track Order'", "Link the Customer to the Track Order use case", "Group user login and logout into a package called 'Authentication Services'"
            OUTPUT:
            ```json
            "Instructions": [
                {
                  "Action": "Add",
                  "Target": "UseCase",
                  "Name": "Track_Order"
                },
                {
                  "Action": "Add",
                  "Target": "Association",
                  "Actor": "Customer",
                  "UseCase": "Track_Order"
                },
                {
                  "Action": "Add",
                  "Target": "Package",
                  "Name": "Authentication_Services",
                  "UseCases": ["Log_In", "Log_Out"]
                }
            ]
            ```
            """";
            var response = await _llmService.GenerateContentAsync(prompt);
            return response.Content;
        }
    }
}
