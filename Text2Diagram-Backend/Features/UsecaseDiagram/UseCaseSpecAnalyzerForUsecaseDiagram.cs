using LangChain.Providers;
using LangChain.Providers.Ollama;
using System.Text.Json;

namespace Text2Diagram_Backend.Features.UsecaseDiagram;

public class UseCaseSpecAnalyzerForUsecaseDiagram
{
    private readonly OllamaChatModel llm;
    private readonly ILogger<UseCaseSpecAnalyzerForUsecaseDiagram> logger;

    public UseCaseSpecAnalyzerForUsecaseDiagram(
        OllamaProvider provider,
        IConfiguration configuration,
        ILogger<UseCaseSpecAnalyzerForUsecaseDiagram> logger)
    {
        var llmName = configuration["Ollama:LLM"] ?? throw new InvalidOperationException("LLM was not defined.");
        llm = new OllamaChatModel(provider, id: llmName);
        this.logger = logger;
    }

    public async Task<string> AnalyzeRequirementAsync(string requirement)
    {
        var prompt = GetAnalysisRequirementPrompt(requirement);
        var response = await llm.GenerateAsync(prompt);
        var result = response.ToString().Trim();
        var json = "";
        if (result.Contains("```json"))
        {
            var parts = result.Split(new[] { "```json", "```" }, StringSplitOptions.RemoveEmptyEntries);
            json = parts[0];
        }

        logger.LogInformation("Use Case Analysis Response: {response}", json);

        return json;
    }

    public async Task<UseCaseDiagramElements> AnalyzeAsync(string useCaseSpec)
    {
        var useCase = GetAnalysisRequirementPrompt(useCaseSpec);
        logger.LogInformation("Use Case Response: {useCase}", useCase);
        var prompt = GetAnalysisPrompt(useCase);
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
            1. Actors: Extract from 'Actor' section.
            2. UseCases: Identify distinct system functionalities (e.g., 'Place Order', 'Make Payment').
            3. Associations: Map interactions between actors and use cases (e.g., 'Customer' -> 'Place Order').
            4. Includes: Identify use cases that are included in other use cases (e.g., 'Complete Checkout' includes 'Log In').
            5. Extends: Identify optional use cases that extend the behavior of other use cases (e.g., 'Apply Discount' extends 'Make Purchase').
            6. Groups: Group related use cases into functional categories (e.g., 'Order Management', 'Admin Functions').

            The output should be in JSON format with the following fields:
            {
              'Actors': [actor1, actor2...],
              'UseCases': [useCase1, useCase2...],
              'Associations': {
                'Actor1': [useCase1, useCase2...],
                'Actor2': [useCase3...]
              },
              'Includes': {
                'UseCase1': [includedUseCase1, includedUseCase2...]
              },
              'Extends': {
                'UseCase2': [extendedUseCase1...]
              },
              'Groups': {
                'Group1': [useCase1, useCase2...],
                'Group2': [useCase3...]
              }
            }

            Example use case specification:
            Use Case: Online Shopping
            Description: Customer purchases items online.
            Actors: Customer, Cashier, Manager
            Basic Flow:
            1. Customer views items.
            2. Customer makes a purchase.
            3. Customer completes checkout.
            4. System includes 'Log In' before checkout.
            5. Checkout optionally extends 'Apply Discount'.

            Alternative Flow:
            1. Cancel Order
                - Customer cancels order before checkout.

            Exception Flow:
            1. Payment Failure
                - System shows error message.

            Example output for the above use case specification:
            {
              'Actors': ['Customer', 'Cashier', 'Manager'],
              'UseCases': ['View Items', 'Make Purchase', 'Complete Checkout', 'Log In', 'Apply Discount', 'Cancel Order'],
              'Associations': {
                'Customer': ['View Items', 'Make Purchase', 'Complete Checkout', 'Cancel Order'],
                'Cashier': ['Complete Checkout'],
                'Manager': []
              },
              'Includes': {
                'Complete Checkout': ['Log In']
              },
              'Extends': {
                'Complete Checkout': ['Apply Discount']
              },
              'Groups': {
                'Order Management': ['View Items', 'Make Purchase', 'Complete Checkout', 'Cancel Order'],
                'Admin Functions': []
              }
            }
            """;

    }


    private string GetAnalysisRequirementPrompt(string Requirement)
    {
        return $"""
            You are a System Analyst. Based on the following system requirements, generate a detailed use case specification following industry standards.
             Extract elements described below from this system requirement: {Requirement}

             Transformation Rules:

                Use Case Name: Identify the primary action the system must perform.

                Description: Provide a brief summary of the use case's purpose.

                Actors: List all actors involved.

                Preconditions: Define the conditions that must be met before the use case can be executed.

                Main Flow: Write a step-by-step list from initiation to completion.

                Alternative Flows: If there are variations in the process, list them clearly.

                Exception Flows: Identify error or failure scenarios and how they are handled.

                System Constraints: Include performance, security, or UI requirements if applicable.

            Example system requirement: A food delivery application allows customers to browse restaurants, add items to their cart, and place an order. 
            The system should validate the customer's address and process payments before confirming the order.
            Expected Output for this system requirement:
            Use Case: Place Food Order  
            Description: ustomers can browse available restaurants, add food items to their cart, and place an order for delivery.
            Actors: Customer, Payment Gateway
            Preconditions:
                - The user is logged into their account.  
                - The restaurant is open and available for orders.
            Main Flow:
                1. The customer browses the list of available restaurants.  
                2. The customer selects a restaurant and views its menu.  
                3. The customer adds food items to their cart.  
                4. The customer proceeds to checkout.  
                5. The system prompts the customer to enter or confirm their delivery address.  
                6. The system validates the address and delivery availability.  
                7. The customer selects a payment method and enters payment details.  
                8. The system sends payment details to the Payment Gateway.  
                9. The Payment Gateway processes the payment and sends confirmation.  
                10. The system confirms the order and displays an order confirmation message.
            Alternative Flows:
                Customer Wants to Modify Cart Before Checkout:
                  1. The customer removes or adds more items before proceeding to checkout.  

                Customer Chooses Cash on Delivery (COD) Instead of Online Payment:
                  1. The customer selects "Cash on Delivery" as the payment method.  
                  2. The system confirms the order without processing an online payment.  
            Exception Flows:
                Invalid Address:
                  1. The system notifies the customer that the address is invalid or outside the delivery area.  
                  2. The customer updates the address and retries.  

                Payment Fails:
                  1. The Payment Gateway returns a failed transaction response.  
                  2. The system displays an error message and prompts the customer to retry with another payment method.  

                Restaurant is Closed:
                  1. The system notifies the customer that the restaurant is currently unavailable.  
                  2. The customer selects a different restaurant or tries again later.
            System Constraints:
                - Payment must be completed within 5 minutes of checkout to confirm the order.  
                - The system should encrypt customer payment details.  
                - The maximum order value for online payment is $500.  
            """;
    }

    private UseCaseDiagramElements? ParseAndValidateResponse(string response)
    {
        try
        {
            response = response.Trim();
            var json = "";
            if (response.Contains("```json"))
            {
                var parts = response.Split(new[] { "```json", "```" }, StringSplitOptions.RemoveEmptyEntries);
                json = parts[0];
            }

            logger.LogInformation("Use Case Analysis Response: {response}", json);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<UseCaseDiagramElements>(json, options);

            if (result == null)
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