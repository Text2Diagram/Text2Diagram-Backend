using LangChain.Providers;
using LangChain.Providers.Ollama;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Runtime.Intrinsics.X86;
using System;
using System.Text;
using System.Text.Json;
using Text2Diagram_Backend.Abstractions;
using Text2Diagram_Backend.Common;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Text2Diagram_Backend.Common.Abstractions;

namespace Text2Diagram_Backend.UsecaseDiagram;

public class UsecaseDiagramGenerator : IDiagramGenerator
{
    private readonly OllamaChatModel llm;
    private readonly ILogger<UsecaseDiagramGenerator> logger;
    private readonly UseCaseSpecAnalyzerForUsecaseDiagram analyzer;
    private readonly ISyntaxValidator syntaxValidator;

    public UsecaseDiagramGenerator(
        ILogger<UsecaseDiagramGenerator> logger,
        OllamaProvider provider,
        IConfiguration configuration,
        UseCaseSpecAnalyzerForUsecaseDiagram analyzer,
        ISyntaxValidator syntaxValidator)
    {
        var llmName = configuration["Ollama:LLM"] ?? throw new InvalidOperationException("LLM was not defined.");
        llm = new OllamaChatModel(provider, id: llmName);
        this.logger = logger;
        this.analyzer = analyzer;
        this.syntaxValidator = syntaxValidator;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="input">Use case specifications or BPMN files.</param>
    /// <returns>Generated Mermaid Code for Flowchart Diagram</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<string> GenerateAsync(string input)
    {
        var elements = await analyzer.AnalyzeAsync(input);

        var prompt = await GetPromptAsync(elements);
        var result = await llm.GenerateAsync(prompt);

        logger.LogInformation("UsecaseDiagram Generation Result: {result}", result);

        //var isSyntaxValid = await syntaxValidator.ValidateAsync(result);

        return result;
    }


    private async Task<string> GetPromptAsync(UseCaseDiagramElements useCaseElements)
    {
        var filePath = Path.Combine("D:\\FinalProject\\Text2Diagram-Backend\\Text2Diagram-Backend", "UsecaseDiagram", "usecasediagram.md");
        var guidance = await File.ReadAllTextAsync(filePath, Encoding.UTF8);

        var jsonData = JsonSerializer.Serialize(
            useCaseElements,
            new JsonSerializerOptions { WriteIndented = true });

        logger.LogInformation("UsecaseDiagram Generation Structured Data: {jsonData}", jsonData);

        return $"""
        You are a Use Case Diagram Generator agent.Generate a PlantUML use case diagram strictly following these rules:

        Mapping Rules:
        1. Actors: Represent actors using the "actor" keyword.
        2. Use Cases: Represent use cases using the "usecase" keyword.
        3. Relationships:
           - Association: Represent interactions between actors and use cases with "-->".
           - Include: Represent included use cases with "..>" and "<<include>>".
           - Extend: Represent extended use cases with "..>" and "<<extend>>".
        4. Grouping: Use packages to group related use cases.
        5. Visibility:
           - Public use cases are displayed normally.
           - Optional or extended flows should use "<<extend>>".
           - Reusable flows should use "<<include>>".

        Syntax Documentation: 
        The following documentation outlines the syntax and rules for creating Mermaid.js flowcharts. You must strictly adhere to this syntax:
        {guidance}

        Instructions:
        1. Strictly use the structured data below.Do NOT include use cases from other sources.
        2. Follow the syntax rules from the documentation exactly.
        3. Ensure the output is a valid PlantUML code block.
        4. Do not include any explanations, comments, or additional text outside the PlantUML code.
        5. Ensure all actors and use cases are connected logically.
        6. Only include items from the structured data.Do not add extra use cases, actors, or flows.
        

        """ +
        """
        Example Input:
        {
            "Actors": ["Customer", "Cashier", "Manager"],
          "UseCases": [
            "Place Order",
            "Make Payment",
            "Cancel Order",
            "Manage Products",
            "View Sales Report"
          ],
          "Associations": {
                "Customer": ["Place Order", "Cancel Order"],
            "Cashier": ["Make Payment"],
            "Manager": ["Manage Products", "View Sales Report"]
          },
          "Includes": {
                "Place Order": ["Make Payment"]
          },
          "Extends": {
                "Cancel Order": ["Place Order"]
          },
          "Groups": {
                "Order Management": ["Place Order", "Cancel Order", "Make Payment"],
            "Admin Functions": ["Manage Products", "View Sales Report"]
          }
        }

        Example Output:
        @startuml
        actor "Customer" as Customer
        actor "Cashier" as Cashier
        actor "Manager" as Manager

        package "Order Management" {
            usecase "Place Order" as UC1
            usecase "Make Payment" as UC2
            usecase "Cancel Order" as UC3

            UC1 ..> UC2 : "<<include>>"
            UC3 ..> UC1 : "<<extend>>"
        }

        package "Admin Functions" {
            usecase "Manage Products" as UC4
            usecase "View Sales Report" as UC5
        }

        Customer --> UC1
        Customer --> UC3
        Cashier --> UC2
        Manager --> UC4
        Manager --> UC5
        @enduml


        """ +
        $"""
        # Structured Data:
        {jsonData}

        Generate and return only the PlantUML code. Do not include explanations, comments, or additional text outside the PlantUML code.
        
        """
        ;
    }
    //private string PostProcess(string output)
    //{
    //    output = output.Trim();
    //    return output.Contains("```mermaid")
    //            ? output.Split(["```mermaid", "```"], StringSplitOptions.RemoveEmptyEntries)[1]
    //            : output;

    //}
}