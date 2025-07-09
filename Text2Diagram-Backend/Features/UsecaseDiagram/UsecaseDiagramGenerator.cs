using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text;
using System.Text.Json;
using Text2Diagram_Backend.Common.Abstractions;
using Text2Diagram_Backend.Data;
using Text2Diagram_Backend.Data.Models;
using Text2Diagram_Backend.Features.UsecaseDiagram.Components;
using Text2Diagram_Backend.Features.UsecaseDiagram.Separate;
using Text2Diagram_Backend.Migrations;

namespace Text2Diagram_Backend.Features.UsecaseDiagram;

public class UsecaseDiagramGenerator : IDiagramGenerator
{
    private readonly ILogger<UsecaseDiagramGenerator> logger;
    private readonly UseCaseSpecAnalyzerForUsecaseDiagram analyzer;
    private readonly ApplicationDbContext dbContext;
    private readonly ILLMService1 _llmService;

    public UsecaseDiagramGenerator(
        ILogger<UsecaseDiagramGenerator> logger,
        UseCaseSpecAnalyzerForUsecaseDiagram analyzer,
        ApplicationDbContext dbContext,
        ILLMService1 llmService)
    {
        this.logger = logger;
        this.analyzer = analyzer;
        this.dbContext = dbContext;
        _llmService = llmService;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="input">Use case specifications or BPMN files.</param>
    /// <returns>Generated Mermaid Code for Flowchart Diagram</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<DiagramContent> GenerateAsync(string input)
    {
        try
        {
            // Extract and generate diagram structure directly from input
            var diagram = await analyzer.AnalyzeAsync(input);            // Generate Mermaid syntax
            // Generate planUML syntax
            string planUMLCode = GeneratePlantUMLCode(diagram);

            logger.LogInformation("Generated PlanUML Code:\n{PlantUMLCode}", planUMLCode);

            // Validate and correct if needed
            return new DiagramContent
            {
                mermaidCode = planUMLCode,
                diagramJson = JsonConvert.SerializeObject(diagram)
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating Usecase diagram");
            throw;
        }
    }

	public async Task<DiagramContent> ReGenerateAsync(string feedback, string diagramJson)
	{
        logger.LogInformation("Regenerating flowchart with feedback: {Feedback}", feedback);
        try
        {
            // Extract and generate diagram structure directly from input
            var diagram = await analyzer.AnalyzeRegenAsync(feedback, diagramJson);          
            // Generate planUML syntax
            string planUMLCode = GeneratePlantUMLCode(diagram);

            logger.LogInformation("Generated PlanUML Code:\n{PlantUMLCode}", planUMLCode);

            // Validate and correct if needed
            return new DiagramContent
            {
                mermaidCode = planUMLCode,
                diagramJson = JsonConvert.SerializeObject(diagram)
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating Usecase diagram");
            throw;
        }

        //var response = await ApplyCommandsAsync(feedback);
        //var feedbackNode = Helpers.ValidateJson(response);
        //var options = new JsonSerializerOptions
        //{
        //    PropertyNameCaseInsensitive = true,
        //    AllowTrailingCommas = true,
        //    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        //};
        //var diagram = System.Text.Json.JsonSerializer.Deserialize<UseCaseDiagram>(diagramJson, options);
        //var instruction = System.Text.Json.JsonSerializer.Deserialize<Instructions>(feedbackNode, options);
        //var newUseCaseDiagram = Helpers.ApplyInstructions(diagram, instruction);
        //var planUMLCode = GeneratePlantUMLCode(newUseCaseDiagram);
        //return new DiagramContent()
        //{
        //    mermaidCode = planUMLCode,
        //    diagramJson = JsonConvert.SerializeObject(newUseCaseDiagram)
        //};
	}

    public string GeneratePlantUMLCode(UseCaseDiagram diagram)
    {
        var puml = new StringBuilder();

        // Start PlantUML definition
        puml.AppendLine("@startuml");
        puml.AppendLine("left to right direction");

        var useCasesInBoundaries = new HashSet<string>();
        if (diagram.Packages != null && diagram.Packages.Any())
        {
            foreach (var package in diagram.Packages)
            {
                // Use quotes for boundary names to handle spaces
                puml.AppendLine($"package \"{(package.Name)}\" {{");
                /*if (package.UseCases != null)
                {
                    foreach (var useCase in package.UseCases)
                    {
                        puml.AppendLine($"  usecase \"{EscapePlantUmlString(useCase)}\"as {CreateAlias(useCase)}");//
                        useCasesInBoundaries.Add(useCase);
                    }
                }*/

                //Only need to define Association and Relationships

                // Associations (Actor <--> UseCase)
                if (package.Associations != null && package.Associations.Any())
                {
                    foreach (var assoc in package.Associations)
                    {
                        puml.AppendLine($"{Helpers.NormalizeActorName(EscapePlantUmlString(assoc.Actor))} --> ({assoc.UseCase})");
                    }
                    puml.AppendLine();
                }

                // Includes (Base ..> Included : <<include>>)
                if (package.Includes != null && package.Includes.Any())
                {
                    foreach (var include in package.Includes)
                    {
                        puml.AppendLine($"({include.BaseUseCase}) ..> ({include.IncludedUseCase}) : <<include>>");
                    }
                    puml.AppendLine();
                }

                // Extends (Base <.. Extended : <<extend>>) - Note the direction
                if (package.Extends != null && package.Extends.Any())
                {
                    foreach (var extend in package.Extends)
                    {
                        puml.AppendLine($"({extend.BaseUseCase}) <.. ({extend.ExtendedUseCase}) : <<extend>>");
                    }
                    puml.AppendLine();
                }
                puml.AppendLine();
                puml.AppendLine("}");
            }
        }   

        puml.AppendLine("@enduml");

        return puml.ToString();

    }



    private string EscapePlantUmlString(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        return input.Replace("\"", "\\\"");
    }

    private async Task<string> ApplyCommandsAsync(string feedback)
    {
        logger.LogInformation("Regenerating use case diagram with feedback: {Feedback}", feedback);
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
            {
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
                  "Actors": ["string", ...],
                  "PackageName": "string"
                }
            ]
            }
            ```
            """
        +
        """"
            ### EXAMPLE 1:
            INPUT:
            - Feedback: "Add a new use case called 'Track Order' in the package 'Order_Services'", "Actor Customer in System Administration should also has usecase 'Track_Order' in the package 'Order_Services'"", "Group user login and logout into a package called 'Authentication Services'"
            OUTPUT:
            ```json
            "Instructions": [
                {
                  "Action": "Add",
                  "Target": "UseCase",
                  "Name": "Track_Order",
                  "PackageName": "Order_Services"
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
                  "PackageName": "Authentication_Services",
                  "UseCases": ["Log_In", "Log_Out"]
                }
            ]
            ```
            """";
        var response = await _llmService.GenerateContentAsync(prompt);
        return response.Content;
    }
}
