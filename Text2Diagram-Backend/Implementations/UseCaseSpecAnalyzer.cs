using LangChain.Providers;
using LangChain.Providers.Ollama;
using System.Text.Json;

namespace Text2Diagram_Backend.Common;

public class UseCaseSpecAnalyzer
{
    private readonly OllamaChatModel llm;
    private readonly ILogger<UseCaseSpecAnalyzer> logger;

    public UseCaseSpecAnalyzer(
        OllamaProvider provider,
        IConfiguration configuration,
        ILogger<UseCaseSpecAnalyzer> logger)
    {
        var llmName = configuration["Ollama:LLM"] ?? throw new InvalidOperationException("LLM was not defined.");
        llm = new OllamaChatModel(provider, id: llmName);
        this.logger = logger;
    }

    public async Task<UseCaseElements> AnalyzeAsync(string useCaseSpec)
    {

        var prompt = GetAnalysisPrompt(useCaseSpec);
        var response = await llm.GenerateAsync(prompt);

        logger.LogInformation("Use Case Analysis Response: {response}", response);

        var useCaseFlows = ParseAndValidateResponse(response);
        if (useCaseFlows == null)
        {
            throw new FormatException("Error while analyzing use case specification.");
        }

        return useCaseFlows;
    }

    private string GetAnalysisPrompt(string useCaseSpec)
    {
        return $"""
            You are a Use Case Analyzer agent. Extract elements described below from this use case specification:
            {useCaseSpec}

            """ +
            """
            Rules: 
            1. Actors: Extract from "Actor" section and infer system actors (e.g., "System").
            2. Triggers: Identify initiating events (e.g., "User clicks 'Login' button").
            3. Data: Capture user inputs/system outputs (e.g., "User enters username").
            4. Decisions: Identify validation/conditional steps (e.g., "System validates credentials").
            5. MainFlow: Steps from Basic Flow (exclude navigation steps like "goes to page").
            6. AlternativeFlows: Map alternative paths (e.g., "Forgot Password").
            7. ExceptionFlows: Map error paths (e.g., "Invalid Password").

            The output should be in JSON format with the following fields:
            {
              "Actors": [actor1, actor2...],
              "Triggers": [trigger1, trigger2...],
              "Data": [data1, data2...],
              "Decisions": [decision1, decision2...],
              "MainFlow": [main_steps...],
              "AlternativeFlows": { "FlowName": [steps...] },
              "ExceptionFlows": { "FlowName": [steps...] }
            }

            Example use case specification:
            Use Case: Login 
            Description: User logs into the system.
            Actor: User  
            Precondition: User has account.
            Postcondition: User is logged in the system successfully.
            Basic Flow:
            1. User goes to the login page.
            2. User enters username.  
            3. User enters password.  
            4. User clicks "Login" button.
            5. System validates credentials.  

            Exception Flow: 
            1. Invalid Password  
                - System shows error message.  
                - User retries password entry.
            2. Invalid Username  
                - System shows error message.  
                - User retries username entry.

            Alternative Flow: 
            1. Forgot Password  
                - User clicks "Forgot Password". 
                - User enters email.
                - User clicks "Submit" button.
                - System sends reset email.  

            Example output for the above example use case specification:  
            {
              "Actors": ["User", "System"],
              "Triggers": ["User clicks 'Login' button"],
              "Data": [
                "User enters username",
                "User enters password",
                "User enters email",
                "System sends reset email"
              ],
              "Decisions": ["System validates credentials"],
              "MainFlow": [
                "User enters username",
                "User enters password",
                "User clicks 'Login' button",
                "System validates credentials"
              ],
              "AlternativeFlows": {
                "Forgot Password": [
                  "User clicks 'Forgot Password'",
                  "User enters email",
                  "User clicks 'Submit' button",
                  "System sends reset email"
                ]
              },
              "ExceptionFlows": {
                "Invalid Password": [
                  "System shows error message",
                  "User retries password entry"
                ],
                "Invalid Username": [
                  "System shows error message",
                  "User retries username entry"
                ]
              }
            }
            """;
    }

    private UseCaseElements? ParseAndValidateResponse(string response)
    {
        try
        {
            response = response.Trim();
            var json = response.Contains("```json")
                ? response.Split(["```json", "```"], StringSplitOptions.RemoveEmptyEntries)[0]
                : response;

            logger.LogInformation("Use Case Analysis Response: {response}", json);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<UseCaseElements>(json, options);

            if (result == null || result.MainFlow == null || result.MainFlow.Count == 0)
                throw new FormatException("MainFlow is missing or empty");

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to parse use case analysis response");
            throw;
        }
    }
}
