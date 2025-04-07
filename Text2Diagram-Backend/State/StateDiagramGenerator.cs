using LangChain.Providers;
using LangChain.Providers.Ollama;
using System.Text;
using System.Text.Json;
using Text2Diagram_Backend.Common.Abstractions;

namespace Text2Diagram_Backend.State;

public class StateDiagramGenerator : IDiagramGenerator
{
    private readonly OllamaChatModel llm;
    private readonly ILogger<StateDiagramGenerator> logger;
    private readonly IAnalyzer<StateElements> analyzer;
    private readonly ISyntaxValidator syntaxValidator;

    public StateDiagramGenerator(
        ILogger<StateDiagramGenerator> logger,
        OllamaProvider provider,
        IConfiguration configuration,
        IAnalyzer<StateElements> analyzer,
        ISyntaxValidator syntaxValidator)
    {
        var llmName = configuration["Ollama:LLM"] ?? throw new InvalidOperationException("LLM was not defined.");
        llm = new OllamaChatModel(provider, id: llmName);
        this.logger = logger;
        this.analyzer = analyzer;
        this.syntaxValidator = syntaxValidator;
    }

    public async Task<string> GenerateAsync(string input)
    {
        var stateElements = await analyzer.AnalyzeAsync(input);

        var prompt = await GetPromptAsync(stateElements);
        var result = await llm.GenerateAsync(prompt);

        logger.LogInformation("Flowchart Generation Result: {result}", result);

        //var isSyntaxValid = await syntaxValidator.ValidateAsync(result);

        return PostProcess(result);
    }

    private async Task<string> GetPromptAsync(StateElements stateElements)
    {
        var filePath = Path.Combine(AppContext.BaseDirectory, "State", "stateDiagram.md");
        var guidance = await File.ReadAllTextAsync(filePath, Encoding.UTF8);

        var jsonData = JsonSerializer.Serialize(
            stateElements,
            new JsonSerializerOptions { WriteIndented = true });

        logger.LogInformation("State Diagram Generation Input: {jsonData}", jsonData);

        return $"""
        You are a State Diagram Generator agent. Generate a Mermaid.js state diagram strictly following these rules:

        Mapping Rules:
        1. States: Map `States` list to state names (e.g., `NotAuthenticated`).
        2. Events: Map `Events` list to transition labels (e.g., `: ValidationSuccess`).
        3. Transitions: 
            - Map `Source` → `Target` to `-->` (e.g., `NotAuthenticated --> Authenticated`).  
            - Include `Event` and `Guard` in transitions:  
                - Format: `: EventName [GuardCondition]` (e.g., `: ValidationSuccess [valid_credentials]`).  
                - Omit `Guard` if empty (e.g., `: Retry`).  
        4. Initial/Final States:
            - If `Source` is `[*]`, start the diagram with `[*] --> TargetState`. 
            - If `Target` is `[*]`, end the diagram with `SourceState --> [*]`.  

        Syntax Documentation: 
        The following documentation outlines the syntax and rules for creating Mermaid.js state diagrams. You must strictly adhere to this syntax:
        {guidance}

        Instructions:
        1. Strictly use the structured data below. Do not add extra states, events, or transitions.  
        2. Follow the syntax rules from the documentation exactly.
        3. Ensure the output is a valid Mermaid.js code block.
        4. Do not include any explanations, comments, or additional text outside the Mermaid.js code.

        """ +
        """
        Example Input:
        {
          "States": ["NotAuthenticated", "CredentialsValidated", "Authenticated", "ErrorState"],
          "Events": ["EnterCredentials", "ValidationSuccess", "ValidationFailure", "Retry"],
          "Transitions": [
            { "Source": "[*]", "Target": "NotAuthenticated", "Event": "Start", "Guard": "" },
            { "Source": "NotAuthenticated", "Target": "CredentialsValidated", "Event": "EnterCredentials", "Guard": "" },
            { "Source": "CredentialsValidated", "Target": "Authenticated", "Event": "ValidationSuccess", "Guard": "valid_credentials" },
            { "Source": "CredentialsValidated", "Target": "ErrorState", "Event": "ValidationFailure", "Guard": "invalid_credentials" },
            { "Source": "ErrorState", "Target": "NotAuthenticated", "Event": "Retry", "Guard": "" }
          ]
        }

        Example Output:
        stateDiagram
            [*] --> NotAuthenticated
            NotAuthenticated --> CredentialsValidated : Enter credentials
            CredentialsValidated --> Authenticated : Validation success
            CredentialsValidated --> ErrorState : Validation failure
            ErrorState --> NotAuthenticated : Retry


        """ +
        $"""
        # Structured Data:
        {jsonData}

        Generate and return only the Mermaid.js code. Do not include explanations, comments, or additional text outside the Mermaid.js code.
        """;
    }

    private string PostProcess(string output)
    {
        output = output.Trim();
        return output.Contains("```mermaid")
                ? output.Split(["```mermaid", "```"], StringSplitOptions.RemoveEmptyEntries)[0]
                : output;

    }
}
