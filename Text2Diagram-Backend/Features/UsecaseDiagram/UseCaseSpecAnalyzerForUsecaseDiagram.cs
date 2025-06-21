using System.Text.Json;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Text2Diagram_Backend.Features.UsecaseDiagram.Components;

namespace Text2Diagram_Backend.Features.UsecaseDiagram;

public class UseCaseSpecAnalyzerForUsecaseDiagram
{
    private readonly Kernel kernel;
    private const int MaxRetries = 3;
    //private readonly OllamaChatModel llm;
    private readonly ILogger<UseCaseSpecAnalyzerForUsecaseDiagram> logger;

    public UseCaseSpecAnalyzerForUsecaseDiagram(
        Kernel kernel,
        ILogger<UseCaseSpecAnalyzerForUsecaseDiagram> logger)
    {
        this.kernel = kernel;
        this.logger = logger;
    }

    //public async Task<string> AnalyzeRequirementAsync(string requirement)
    //{
    //    var prompt = GetAnalysisRequirementPrompt(requirement);
    //    var response = await llm.GenerateAsync(prompt);
    //    var result = response.ToString().Trim();
    //    var json = "";
    //    if (result.Contains("```json"))
    //    {
    //        var parts = result.Split(new[] { "```json", "```" }, StringSplitOptions.RemoveEmptyEntries);
    //        json = parts[0];
    //    }

    //    logger.LogInformation("Use Case Analysis Response: {response}", json);

    //    return json;
    //}

    public async Task<UseCaseDiagram> AnalyzeAsync(string useCaseSpec)
    {
        if (string.IsNullOrWhiteSpace(useCaseSpec))
        {
            logger.LogError("Use case specification is empty or null.");
            throw new ArgumentException("Use case specification cannot be empty.", nameof(useCaseSpec));
        }
        string? errorMessage = null;


        var prompt = GetAnalysisPrompt(useCaseSpec, errorMessage);
        var chatService = kernel.GetRequiredService<IChatCompletionService>();
        var history = new ChatHistory();
        history.AddUserMessage(prompt);

        var response = await chatService.GetChatMessageContentAsync(history, kernel: kernel);
        var textContent = response.Content ?? "";

        logger.LogInformation(" Raw response:\n{Content}", textContent);

        // Extract JSON from response
        string jsonResult;
        var codeFenceMatch = Regex.Match(textContent, @"```json\s*(.*?)\s*```", RegexOptions.Singleline);
        if (codeFenceMatch.Success)
        {
            jsonResult = codeFenceMatch.Groups[1].Value.Trim();
        }
        else
        {
            // Fallback to raw JSON
            var rawJsonMatch = Regex.Match(textContent, @"\{[\s\S]*\}", RegexOptions.Singleline);
            if (!rawJsonMatch.Success)
            {
                errorMessage = "No valid JSON found in response. Expected JSON in code fences (```json ... ```) or raw JSON object.";
                logger.LogWarning(" Raw response:\n{Content}", errorMessage);
            }
            jsonResult = rawJsonMatch.Value.Trim();
        }
        if (string.IsNullOrWhiteSpace(jsonResult))
        {
            errorMessage = "Extracted JSON is empty.";
            logger.LogWarning(" Raw response:\n{Content}", errorMessage);
        }

        logger.LogInformation(" Raw response:\n{Content}", jsonResult);

        // Validate JSON structure before deserialization
        jsonResult = Regex.Replace(jsonResult, @",(?=\s*[\}\]])", "");
        var jsonNode = JsonNode.Parse(jsonResult);
        if (jsonNode == null)
        {
            errorMessage = "Failed to parse JSON. Ensure the response is valid JSON.";
            logger.LogWarning(" Raw response:\n{Content}", errorMessage);
        }

        var packages = jsonNode?["Packages"] as JsonArray;

        if (packages == null)
        {
            errorMessage = "Missing 'Packages' array.";
            logger.LogWarning(" Raw response:\n{Content}", errorMessage);
        }
        else
        {
            foreach (var package in packages)
            {
                // Basic validation
                if (package?["Actors"] is not JsonArray || package?["UseCases"] is not JsonArray)
                {
                    errorMessage = "Missing 'Actors' or 'UseCases' array.";
                    logger.LogWarning(" Raw response:\n{Content}", errorMessage);
                }
            }
        }

        var diagram = JsonSerializer.Deserialize<UseCaseDiagram>(jsonResult, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        });

        if (diagram == null)
        {
            errorMessage = "Deserialization returned null.";
            logger.LogWarning(" Raw response:\n{Content}", errorMessage);
        }

        return diagram;

        //for (int attempt = 1; attempt <= MaxRetries; attempt++)
        //{
        //    try
        //    {
        //        var prompt = GetAnalysisPrompt(useCaseSpec, errorMessage);
        //        var chatService = kernel.GetRequiredService<IChatCompletionService>();
        //        var history = new ChatHistory();
        //        history.AddUserMessage(prompt);

        //        var response = await chatService.GetChatMessageContentAsync(history, kernel: kernel);
        //        var textContent = response.Content ?? "";

        //        logger.LogInformation("Attempt {Attempt}: Raw response:\n{Content}", attempt, textContent);

        //        // Extract JSON from response
        //        string jsonResult;
        //        var codeFenceMatch = Regex.Match(textContent, @"```json\s*(.*?)\s*```", RegexOptions.Singleline);
        //        if (codeFenceMatch.Success)
        //        {
        //            jsonResult = codeFenceMatch.Groups[1].Value.Trim();
        //        }
        //        else
        //        {
        //            // Fallback to raw JSON
        //            var rawJsonMatch = Regex.Match(textContent, @"\{[\s\S]*\}", RegexOptions.Singleline);
        //            if (!rawJsonMatch.Success)
        //            {
        //                errorMessage = "No valid JSON found in response. Expected JSON in code fences (```json ... ```) or raw JSON object.";
        //                logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
        //                continue;
        //            }
        //            jsonResult = rawJsonMatch.Value.Trim();
        //        }
        //        if (string.IsNullOrWhiteSpace(jsonResult))
        //        {
        //            errorMessage = "Extracted JSON is empty.";
        //            logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
        //            continue;
        //        }

        //        logger.LogInformation("Attempt {attempt}: Extracted JSON: {json}", attempt, jsonResult);

        //        // Validate JSON structure before deserialization
        //        var jsonNode = JsonNode.Parse(jsonResult);
        //        if (jsonNode == null)
        //        {
        //            errorMessage = "Failed to parse JSON. Ensure the response is valid JSON.";
        //            logger.LogWarning("Attempt {attempt}: {error}", attempt, errorMessage);
        //            continue;
        //        }

        //        // Basic validation
        //        if (jsonNode["Actors"] is not JsonArray || jsonNode["UseCases"] is not JsonArray)
        //        {
        //            errorMessage = "Missing 'Actors' or 'UseCases' array.";
        //            logger.LogWarning("Attempt {Attempt}: {Error}", attempt, errorMessage);
        //            continue;
        //        }

        //        var diagram = JsonSerializer.Deserialize<UseCaseDiagram>(jsonResult, new JsonSerializerOptions
        //        {
        //            PropertyNameCaseInsensitive = true,
        //            AllowTrailingCommas = true,
        //            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        //        });

        //        if (diagram == null)
        //        {
        //            errorMessage = "Deserialization returned null.";
        //            logger.LogWarning("Attempt {Attempt}: {Error}", attempt, errorMessage);
        //            continue;
        //        }

        //        logger.LogInformation("Attempt {Attempt}: Successfully parsed UseCase diagram.", attempt);
        //        return diagram;
        //    }
        //    catch (JsonException ex)
        //    {
        //        errorMessage = $"JSON parsing error: {ex.Message}";
        //        logger.LogWarning("Attempt {Attempt}: {Error}", attempt, errorMessage);
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.LogError(ex, "Unexpected error during UseCase analysis on attempt {Attempt}", attempt);
        //    }
        //}

        //throw new FormatException($"Failed to parse UseCase diagram after {MaxRetries} attempts.");
    }



    private string GetAnalysisPrompt(string useCaseSpec, string? errorMessage = null)
    {
        var prompt = $"""
            You are an expert UseCaseDigram Analyzer that extracts and structures use case specifications into flowchart diagram components within the Text2Diagram_Backend.Features.UsecaseDiagram namespace.

            ### TASK:
            Analyze the following use case specification and generate a flowchart representation:
            {useCaseSpec}
            """
            +
            """
            ### INSTRUCTIONS:
            - Identify Actors (users or external systems interacting with the system).
            - Identify Use Cases (actions the system performs for actors).
            - Define Associations between Actors and the Use Cases they participate in.
            - Identify Include relationships where one Use Case's functionality is always included within another.
            - Identify Extend relationships where one Use Case optionally extends the behavior of another under certain conditions.
            - Define System Packages to group related all Actors, Use Cases, Associations, Include relationships, Extend relationships logically.
            - Return only the structured JSON object in the following format, wrapped in code fences:

            """
             +
           """
            ```json
            {
              "Packages": [
                {
                  "Name": "BOUNDARY_NAME",
                  "Actors": [{ "Name": "ACTOR_NAME_1", "Name": "ACTOR_NAME_2" }],
                  "UseCases": [{ "Name": "USE_CASE_NAME_1", "Name": "USE_CASE_NAME_2" }],
                  "Associations": [
                    { "Actor": "ACTOR_NAME", "UseCase": "USE_CASE_NAME" }
                  ],
                  "Includes": [
                    { "BaseUseCase": "BASE_USE_CASE_NAME", "IncludedUseCase": "INCLUDED_USE_CASE_NAME" }
                  ],
                  "Extends": [
                    { "BaseUseCase": "BASE_USE_CASE_NAME", "ExtendedUseCase": "EXTENDED_USE_CASE_NAME" }
                  ],
                }
              ],
            }
            ```

            ### SCHEMA DETAILS:
            - Actor: {{Name: string}}
            - UseCase: {{Name: string}
            - Association: {{Actor: string, UseCase: string}} (Names must match defined Actors and UseCases)
            - Include: {{BaseUseCase: string, IncludedUseCase: string}} (Names must match defined UseCases)
            - Extend: {{BaseUseCase: string, ExtendedUseCase: string}} (Names must match defined UseCases)
            - Package: {{Name: string, UseCases: List<UseCase>, Actors: List<Actor>, Associations: List<Association>, Includes: List<Include>, Extends: List<Extend>}} (UseCases, Actors, ... must match schemas above)

            ### REQUIREMENTS:
            - Actor and Use Case names should be descriptive, typically starting with a capital letter. If it's usecase, using spaces between each word (e.g., "Process Payment", "Add To Cart"), if it's actor, using underscores between each word (e.g., "Online_Shopper", "Restaurant_Receptionist").
            - Descriptions should be concise and clear.
            - All referenced names in Associations, Includes, Extends, and Boundaries must correspond exactly to names defined in the Actors or UseCases lists.
            - The diagram must contain at least one Actor and one Use Case if the description implies interaction. If the description is too vague for interaction, return empty lists for Actors and UseCases.
            - All fields shown in the format are required (except Groups, which should be an empty list). Descriptions should not be empty.
            - Do not include any text outside the JSON code fences.
            """
            +
            """
            ### EXAMPLE:
            INPUT:
            A simple online bookstore system where customers can search for books and place orders. Administrators manage the book inventory. The order placement process includes payment processing.

            OUTPUT:
            ```json
            {
              "Packages": [
                {
                  "Name": "Online Bookstore System",
                  "Actors": [
                    { "Name": "Customer" },
                    { "Name": "Administrator" }
                  ],
                  "UseCases": [
                    { "Name": "Search for Books"},
                    { "Name": "Place Order"},
                    { "Name": "Process Payment"},
                    { "Name": "Manage Inventory"}
                  ],
                  "Associations": [
                    { "Actor": "Customer", "UseCase": "Search for Books" },
                    { "Actor": "Customer", "UseCase": "Place Order" },
                    { "Actor": "Administrator", "UseCase": "Manage Inventory" }
                  ],
                  "Includes": [
                     { "BaseUseCase": "Place Order", "IncludedUseCase": "Process Payment" }
                  ],
                  "Extends": [],
                }
              ],
            }
            ```
            """;

        if (!string.IsNullOrEmpty(errorMessage))
        {
            prompt += $"\n\n### PREVIOUS ERROR:\n{errorMessage}\nPlease correct your output.";
        }

        return prompt;

    }


    //private string GetAnalysisRequirementPrompt(string Requirement)
    //{
    //    return $"""
    //        You are a System Analyst. Based on the following system requirements, generate a detailed use case specification following industry standards.
    //         Extract elements described below from this system requirement: {Requirement}

    //         Transformation Rules:

    //            Use Case Name: Identify the primary action the system must perform.

    //            Description: Provide a brief summary of the use case's purpose.

    //            Actors: List all actors involved.

    //            Preconditions: Define the conditions that must be met before the use case can be executed.

    //            Main Flow: Write a step-by-step list from initiation to completion.

    //            Alternative Flows: If there are variations in the process, list them clearly.

    //            Exception Flows: Identify error or failure scenarios and how they are handled.

    //            System Constraints: Include performance, security, or UI requirements if applicable.

    //        Example system requirement: A food delivery application allows customers to browse restaurants, add items to their cart, and place an order. 
    //        The system should validate the customer's address and process payments before confirming the order.
    //        Expected Output for this system requirement:
    //        Use Case: Place Food Order  
    //        Description: ustomers can browse available restaurants, add food items to their cart, and place an order for delivery.
    //        Actors: Customer, Payment Gateway
    //        Preconditions:
    //            - The user is logged into their account.  
    //            - The restaurant is open and available for orders.
    //        Main Flow:
    //            1. The customer browses the list of available restaurants.  
    //            2. The customer selects a restaurant and views its menu.  
    //            3. The customer adds food items to their cart.  
    //            4. The customer proceeds to checkout.  
    //            5. The system prompts the customer to enter or confirm their delivery address.  
    //            6. The system validates the address and delivery availability.  
    //            7. The customer selects a payment method and enters payment details.  
    //            8. The system sends payment details to the Payment Gateway.  
    //            9. The Payment Gateway processes the payment and sends confirmation.  
    //            10. The system confirms the order and displays an order confirmation message.
    //        Alternative Flows:
    //            Customer Wants to Modify Cart Before Checkout:
    //              1. The customer removes or adds more items before proceeding to checkout.  

    //            Customer Chooses Cash on Delivery (COD) Instead of Online Payment:
    //              1. The customer selects "Cash on Delivery" as the payment method.  
    //              2. The system confirms the order without processing an online payment.  
    //        Exception Flows:
    //            Invalid Address:
    //              1. The system notifies the customer that the address is invalid or outside the delivery area.  
    //              2. The customer updates the address and retries.  

    //            Payment Fails:
    //              1. The Payment Gateway returns a failed transaction response.  
    //              2. The system displays an error message and prompts the customer to retry with another payment method.  

    //            Restaurant is Closed:
    //              1. The system notifies the customer that the restaurant is currently unavailable.  
    //              2. The customer selects a different restaurant or tries again later.
    //        System Constraints:
    //            - Payment must be completed within 5 minutes of checkout to confirm the order.  
    //            - The system should encrypt customer payment details.  
    //            - The maximum order value for online payment is $500.  
    //        """;
    //}

    //private UseCaseDiagramElements? ParseAndValidateResponse(string response)
    //{
    //    try
    //    {
    //        response = response.Trim();
    //        var json = "";
    //        if (response.Contains("```json"))
    //        {
    //            var parts = response.Split(new[] { "```json", "```" }, StringSplitOptions.RemoveEmptyEntries);
    //            json = parts[0];
    //        }

    //        logger.LogInformation("Use Case Analysis Response: {response}", json);

    //        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    //        var result = JsonSerializer.Deserialize<UseCaseDiagramElements>(json, options);

    //        if (result == null)
    //            throw new FormatException("MainFlow is missing or empty");

    //        return result;
    //    }
    //    catch (Exception ex)
    //    {
    //        logger.LogError(ex, "Failed to parse use case analysis response");
    //        throw;
    //    }
    //}
}

