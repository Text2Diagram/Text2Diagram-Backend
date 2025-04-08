using LangChain.Providers;
using LangChain.Providers.Ollama;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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
    /// Generates a flowchart diagram in Mermaid.js format from use case specifications or BPMN files.
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

        var isSyntaxValid = await syntaxValidator.ValidateAsync(result);

        return PostProcess(result);
    }


    private async Task<string> GetPromptAsync(UseCaseElements useCaseElements)
    {
        var filePath = Path.Combine(AppContext.BaseDirectory, "Flowchart", "flowchart.md");
        var guidance = await File.ReadAllTextAsync(filePath, Encoding.UTF8);

        var jsonData = JsonSerializer.Serialize(
            useCaseElements,
            new JsonSerializerOptions { WriteIndented = true });

        return $"""
        You are a Flowchart Generator. Create a Mermaid.js flowchart from the structured data below, following these STRICT RULES:

        INSTRUCTIONS:
        1. Actors: Represent each actor as a labeled swimlane or node.
        2. Triggers: Convert triggers into Terminator nodes (stadium shape).
        3. Main Flow: Render every step in the "MainFlow" as a rectangle (process node) and ensure sequential connections.
        4. Decisions: Identify decision points from the "Decisions" list and represent them as diamond-shaped nodes. Link decision outcomes logically.
        5. Alternative Flows: For each entry in "AlternativeFlows", create a subgraph named exactly as the key (use underscores instead of spaces if necessary). Include all steps in sequence and ensure they connect back to the main flow at the appropriate point.
        6. Exception Flows: For each entry in "ExceptionFlows", create a subgraph named exactly as the key. Represent error conditions as branches off of the relevant decision or flow step, ensuring proper feedback loops where indicated.
        7. Syntax Requirements: Use the syntax rules specified in the documentation below exactly. Do not add extra nodes or flows that are not present in the JSON data.
        8. Output Format: Return only the Mermaid.js code block. Do not include any comments, explanations, or extra text outside of the Mermaid.js code.
        
        Below is an example input and the corresponding expected structure. Use it as a reference for your formatting:

        """ +
        """
        EXAMPLE INPUT:
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

        EXAMPLE OUTPUT:
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
        # INPUT:
        {jsonData}

        Generate and return only the complete Mermaid.js code block with no additional text.
        """;
    }

    private string PostProcess(string output)
    {
        var match = Regex.Match(output, @"```mermaid\s*([\s\S]*?)\s*```");
        var extracted = match.Groups[1].Value.Trim();

        if (string.IsNullOrWhiteSpace(extracted))
        {
            extracted = match.Groups[0].Value.Trim();
        }

        return extracted;
    }
}
