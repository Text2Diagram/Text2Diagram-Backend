using System.Text;
using Text2Diagram_Backend.Common.Abstractions;
using Text2Diagram_Backend.Features.UsecaseDiagram.Components;

namespace Text2Diagram_Backend.Features.UsecaseDiagram;

public class UsecaseDiagramGenerator : IDiagramGenerator
{
    private readonly ILogger<UsecaseDiagramGenerator> logger;
    private readonly UseCaseSpecAnalyzerForUsecaseDiagram analyzer;

    public UsecaseDiagramGenerator(
        ILogger<UsecaseDiagramGenerator> logger,
        UseCaseSpecAnalyzerForUsecaseDiagram analyzer)
    {
        this.logger = logger;
        this.analyzer = analyzer;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="input">Use case specifications or BPMN files.</param>
    /// <returns>Generated Mermaid Code for Flowchart Diagram</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<string> GenerateAsync(string input)
    {
        try
        {
            // Extract and generate diagram structure directly from input
            var diagram = await analyzer.AnalyzeAsync(input);

            // Generate Mermaid syntax
            string planUMLCode = GeneratePlantUMLCode(diagram);

            logger.LogInformation("Generated PlanUML Code:\n{PlantUMLCode}", planUMLCode);

            // Validate and correct if needed
            return planUMLCode;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating Usecase diagram");
            throw;
        }
    }

    private string GeneratePlantUMLCode(UseCaseDiagram diagram)
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

        /*//Actors
        if (diagram.Actors != null && diagram.Actors.Any())
        {
            foreach (var actor in diagram.Actors)
            {
                puml.AppendLine($"actor \"{EscapePlantUmlString(actor)}\" ");//as {CreateAlias(actor)}
            }
            puml.AppendLine(); 
        }

        // Usecases
        if (diagram.UseCases != null)
        {
            bool addedStandaloneUseCase = false;
            foreach (var useCase in diagram.UseCases)
            {
                if (!useCasesInBoundaries.Contains(useCase))
                {
                    puml.AppendLine($"usecase \"{EscapePlantUmlString(useCase)}\" as {CreateAlias(useCase)}");
                    addedStandaloneUseCase = true;
                }
            }
            if (addedStandaloneUseCase)
            {
                puml.AppendLine(); 
            }
        }

        // Associations (Actor <--> UseCase)
        if (diagram.Associations != null && diagram.Associations.Any())
        {
            foreach (var assoc in diagram.Associations)
            {
                // Use aliases for cleaner connections
                puml.AppendLine($"{EscapePlantUmlString(assoc.Actor)} --> {CreateAlias(assoc.UseCase)}");
            }
            puml.AppendLine();
        }

        // Includes (Base ..> Included : <<include>>)
        if (diagram.Includes != null && diagram.Includes.Any())
        {
            foreach (var include in diagram.Includes)
            {
                // Use aliases
                puml.AppendLine($"{CreateAlias(include.BaseUseCase)} ..> {CreateAlias(include.IncludedUseCase)} : <<include>>");
            }
            puml.AppendLine();
        }

        // Extends (Base <.. Extended : <<extend>>) - Note the direction
        if (diagram.Extends != null && diagram.Extends.Any())
        {
            foreach (var extend in diagram.Extends)
            {
                // Use aliases
                puml.AppendLine($"{CreateAlias(extend.BaseUseCase)} <.. {CreateAlias(extend.ExtendedUseCase)} : <<extend>>");
            }
            puml.AppendLine();
        }*/

        puml.AppendLine("@enduml");

        return puml.ToString();

    }

    private string CreateAlias(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "_"; 
        var safeName = System.Text.RegularExpressions.Regex.Replace(name, @"[\s\W]+", "_");

        if (char.IsDigit(safeName[0]))
        {
            safeName = "_" + safeName;
        }

        return string.IsNullOrWhiteSpace(safeName) ? "_" : safeName;
    }


    private string EscapePlantUmlString(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        return input.Replace("\"", "\\\"");
    }

    //private async Task<string> GetPromptAsync(UseCaseDiagramElements useCaseElements)
    //{
    //    var filePath = Path.Combine("D:\\FinalProject\\Text2Diagram-Backend\\Text2Diagram-Backend", "UsecaseDiagram", "usecasediagram.md");
    //    var guidance = await File.ReadAllTextAsync(filePath, Encoding.UTF8);

    //    var jsonData = JsonSerializer.Serialize(
    //        useCaseElements,
    //        new JsonSerializerOptions { WriteIndented = true });

    //    logger.LogInformation("UsecaseDiagram Generation Structured Data: {jsonData}", jsonData);

    //    return $"""
    //    You are a Use Case Diagram Generator agent.Generate a PlantUML use case diagram strictly following these rules:

    //    Mapping Rules:
    //    1. Actors: Represent actors using the "actor" keyword.
    //    2. Use Cases: Represent use cases using the "usecase" keyword.
    //    3. Relationships:
    //       - Association: Represent interactions between actors and use cases with "-->".
    //       - Include: Represent included use cases with "..>" and "<<include>>".
    //       - Extend: Represent extended use cases with "..>" and "<<extend>>".
    //    4. Grouping: Use packages to group related use cases.
    //    5. Visibility:
    //       - Public use cases are displayed normally.
    //       - Optional or extended flows should use "<<extend>>".
    //       - Reusable flows should use "<<include>>".

    //    Syntax Documentation: 
    //    The following documentation outlines the syntax and rules for creating Mermaid.js flowcharts. You must strictly adhere to this syntax:
    //    {guidance}

    //    Instructions:
    //    1. Strictly use the structured data below.Do NOT include use cases from other sources.
    //    2. Follow the syntax rules from the documentation exactly.
    //    3. Ensure the output is a valid PlantUML code block.
    //    4. Do not include any explanations, comments, or additional text outside the PlantUML code.
    //    5. Ensure all actors and use cases are connected logically.
    //    6. Only include items from the structured data.Do not add extra use cases, actors, or flows.


    //    """ +
    //    """
    //    Example Input:
    //    {
    //        "Actors": ["Customer", "Cashier", "Manager"],
    //      "UseCases": [
    //        "Place Order",
    //        "Make Payment",
    //        "Cancel Order",
    //        "Manage Products",
    //        "View Sales Report"
    //      ],
    //      "Associations": {
    //            "Customer": ["Place Order", "Cancel Order"],
    //        "Cashier": ["Make Payment"],
    //        "Manager": ["Manage Products", "View Sales Report"]
    //      },
    //      "Includes": {
    //            "Place Order": ["Make Payment"]
    //      },
    //      "Extends": {
    //            "Cancel Order": ["Place Order"]
    //      },
    //      "Groups": {
    //            "Order Management": ["Place Order", "Cancel Order", "Make Payment"],
    //        "Admin Functions": ["Manage Products", "View Sales Report"]
    //      }
    //    }

    //    Example Output:
    //    @startuml
    //    actor "Customer" as Customer
    //    actor "Cashier" as Cashier
    //    actor "Manager" as Manager

    //    package "Order Management" {
    //        usecase "Place Order" as UC1
    //        usecase "Make Payment" as UC2
    //        usecase "Cancel Order" as UC3

    //        UC1 ..> UC2 : "<<include>>"
    //        UC3 ..> UC1 : "<<extend>>"
    //    }

    //    package "Admin Functions" {
    //        usecase "Manage Products" as UC4
    //        usecase "View Sales Report" as UC5
    //    }

    //    Customer --> UC1
    //    Customer --> UC3
    //    Cashier --> UC2
    //    Manager --> UC4
    //    Manager --> UC5
    //    @enduml


    //    """ +
    //    $"""
    //    # Structured Data:
    //    {jsonData}

    //    Generate and return only the PlantUML code. Do not include explanations, comments, or additional text outside the PlantUML code.

    //    """
    //    ;
    //}
    //private string PostProcess(string output)
    //{
    //    output = output.Trim();
    //    return output.Contains("```mermaid")
    //            ? output.Split(["```mermaid", "```"], StringSplitOptions.RemoveEmptyEntries)[1]
    //            : output;

    //}
}
