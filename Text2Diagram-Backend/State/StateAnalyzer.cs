using LangChain.Providers;
using LangChain.Providers.Ollama;
using System.Text.Json;
using Text2Diagram_Backend.Common.Abstractions;
using Text2Diagram_Backend.Common.Implementations;

namespace Text2Diagram_Backend.State;

/// <summary>
/// Generates State Elements from the Use Case Elements.
/// </summary>
public class StateAnalyzer : IAnalyzer<StateElements>
{
    private readonly OllamaChatModel llm;
    private readonly ILogger<UseCaseSpecAnalyzer> logger;
    private readonly IAnalyzer<UseCaseElements> analyzer;

    public StateAnalyzer(
        OllamaProvider provider,
        IConfiguration configuration,
        ILogger<UseCaseSpecAnalyzer> logger,
        IAnalyzer<UseCaseElements> analyzer)
    {
        var llmName = configuration["Ollama:LLM"] ?? throw new InvalidOperationException("LLM was not defined.");
        llm = new OllamaChatModel(provider, id: llmName);
        this.logger = logger;
        this.analyzer = analyzer;
    }

    public async Task<StateElements> AnalyzeAsync(string spec)
    {
        var useCaseElements = await analyzer.AnalyzeAsync(spec);
        var prompt = GetPrompt(useCaseElements);
        var response = await llm.GenerateAsync(prompt);

        logger.LogInformation("Architecture Analysis Response: {response}", response);

        var stateElements = ParseAndValidateResponse(response);
        if (stateElements == null)
        {
            throw new FormatException("Error while analyzing states of system.");
        }

        return stateElements;
    }

    private string GetPrompt(UseCaseElements useCaseElements)
    {
        return $"""
            You are a State Analyzer agent. Generate StateElements (States, Events, Transitions) from the UseCaseElements below.

            Input UseCaseElements:  
            {JsonSerializer.Serialize(
                    useCaseElements,
                    new JsonSerializerOptions { WriteIndented = true })}  
            """ +
            """
            Follow these rules strictly:
            1. States:  
               - Infer states from use case steps and decision points.  
               - Example: "User enters username" → State: `CredentialsValidated`.  
            2. Events:  
               - Extract actions from main/alternative/exception flows (e.g., "Click Login", "Retry password").  
            3. Transitions:  
               - Map steps to transitions with `Source`, `Target`, `Event`, and `Guard`.  
               - Use `[*]` for initial state (e.g., `[*] → NotAuthenticated`).  
               - Guards are optional (only include if conditions exist in the use case).  
            4. No Hallucination:  
               - Do NOT add states/events not present in the UseCaseElements.
            
            The output should be in JSON format with the following fields:
            {
              "States": ["State1", "State2", ...],
              "Events": ["Event1", "Event2", ...],
              "Transitions": [
                {
                  "Source": "State1",
                  "Target": "State2",
                  "Event": "Event1",
                  "Guard": "Condition1"
                },
                ...
              ]
            }

            Example input:
            {
              "MainFlow": ["User enters username", "User enters password", "System validates credentials"],
              "ExceptionFlows": {
                "Invalid Password": ["System shows error", "User retries password entry"]
              }
            }

            Example output:  
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

            Generate and return ONLY the StateElements JSON. Do NOT include explanations or comments.
            """;
    }

    private StateElements ParseAndValidateResponse(string response)
    {
        try
        {
            response = response.Trim();
            var json = response.Contains("```json")
                ? response.Split(["```json", "```"], StringSplitOptions.RemoveEmptyEntries)[1]
                : response;

            logger.LogInformation("State Analysis Response: {response}", json);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<StateElements>(json, options);

            if (result == null
                || result.States == null
                || result.Events == null
                || result.Transitions == null)
                throw new FormatException("Failed to parse state response");

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to parse state response");
            throw;
        }
    }
}
