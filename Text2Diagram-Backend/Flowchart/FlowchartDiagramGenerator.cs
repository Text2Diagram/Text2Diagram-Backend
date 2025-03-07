﻿using LangChain.Providers;
using LangChain.Providers.Ollama;
using System.Text;
using System.Text.Json;
using Text2Diagram_Backend.Abstractions;
using Text2Diagram_Backend.Common;

namespace Text2Diagram_Backend.Flowchart;

public class FlowchartDiagramGenerator : IDiagramGenerator
{
    private readonly OllamaChatModel llm;
    private readonly ILogger<FlowchartDiagramGenerator> logger;
    private readonly UseCaseSpecAnalyzer analyzer;
    private readonly ISyntaxValidator syntaxValidator;

    public FlowchartDiagramGenerator(
        ILogger<FlowchartDiagramGenerator> logger,
        OllamaProvider provider,
        IConfiguration configuration,
        UseCaseSpecAnalyzer analyzer,
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

        logger.LogInformation("Flowchart Generation Result: {result}", result);

        //var isSyntaxValid = await syntaxValidator.ValidateAsync(result);

        return PostProcess(result);
    }


    private async Task<string> GetPromptAsync(UseCaseElements useCaseElements)
    {
        var filePath = Path.Combine(AppContext.BaseDirectory, "Flowchart", "flowchart.md");
        var guidance = await File.ReadAllTextAsync(filePath, Encoding.UTF8);

        var jsonData = JsonSerializer.Serialize(
            useCaseElements,
            new JsonSerializerOptions { WriteIndented = true });

        logger.LogInformation("Flowchart Generation Structured Data: {jsonData}", jsonData);

        return $"""
        You are a Flowchart Generator agent. Your task is to generate a valid Mermaid.js flowchart based on the structured data provided.

        Documentation: 
        The following documentation outlines the syntax and rules for creating Mermaid.js flowcharts. You must strictly adhere to this syntax:
        {guidance}

        Mapping Rules:
        1. Actors: Represent actors as swimlanes or labels.
        2. Triggers: Map triggers to Terminator nodes (stadium shape).
        3. Main Flow Steps: Represent steps as rectangles (process nodes).
        4. Decisions: Use diamonds for decision points.
        5. Data Inputs/Outputs: Use parallelograms for data inputs/outputs.
        6. Alternative Flows: Group alternative flows into subgraphs.
        7. Exception Flows: Group exception flows into subgraphs.

        Instructions:
        1. Use ONLY the provided structured data to create the flowchart.
        2. Follow the syntax and guidelines from the documentation precisely.
        3. Ensure the output is a valid Mermaid.js code block.
        4. Do NOT include any explanations, comments, or additional text outside the Mermaid.js code.
        5. Use the following conventions:
            - Start with a Terminator node for the trigger.
            - Connect steps sequentially in the main flow.
            - Use decision nodes for branching logic.
            - Group alternative and exception flows into subgraphs.
            - Link alternative and exception flows back to the main flow where applicable.

        """ +
        """
        Example Input:
        {
          "Actors": ["User", "System"],
          "Triggers": ["User clicks 'Login' button"],
          "MainFlow": [
            "User enters username",
            "User enters password",
            "System validates credentials"
          ],
          "AlternativeFlows": {
            "Forgot Password": [
              "User clicks 'Forgot Password'",
              "User enters email",
              "System sends reset email"
            ]
          },
          "ExceptionFlows": {
            "Invalid Password": [
              "System shows error message",
              "User retries password entry"
            ]
          },
          "Decisions": ["System validates credentials"]
        }

        Example Output:
        graph TD
        %% Start Terminator Node
        Start([Start: User clicks 'Login']) --> A[User enters username]

        %% Main Flow Steps
        A --> B[User enters password]
        B --> C{System validates credentials?}

        %% Decision Node for Valid Credentials
        C --|Valid|--> D[Login successful]

        %% Exception Flow for Invalid Password
        C --|Invalid|--> E[System shows error message]
        E --> F[User retries password entry]
        F --> B

        %% Alternative Flow for Forgot Password
        subgraph Forgot_Password
            G[User clicks 'Forgot Password'] --> H[User enters email]
            H --> I[User clicks 'Submit']
            I --> J[System sends reset email]
        end

        %% Link to Alternative Flow from Main Flow
        C -->|Forgot Password| G
        """ +
        $"""
        Input:
        {jsonData}
        Generate and return ONLY the Mermaid.js code. Do NOT include explanations, comments, or additional text outside the Mermaid.js code.
        Output:
        """;
    }

    private string PostProcess(string output)
    {
        return output.Contains("```mermaid")
                ? output.Split(["```mermaid", "```"], StringSplitOptions.RemoveEmptyEntries)[0]
                : output;

    }
}
