using LangChain.Providers;
using LangChain.Providers.Ollama;
using System.Text.Json;
using System.Text.RegularExpressions;
using Text2Diagram_Backend.Common.Implementations;

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
            You are a Use Case Analyzer. Extract elements from this specification:
            {useCaseSpec}

            """ +
            """
            EXTRACTION RULES:
            1. Actors: 
               - List all explicit actors (e.g., "Customer", "Admin")
               - Add "System" if steps involve automated actions (e.g., "System validates credentials")
            2. Triggers: 
               - Identify initiating actions (e.g., "User clicks Submit", "System receives notification")
               - Exclude preconditions/postconditions
            3. Decisions: 
               - Capture conditional checks (e.g., "Validate payment method")
               - Include validation rules (e.g., "Check inventory > 0")
               - Format as questions ending with "?"
            4. Flows:
               - MainFlow: Core success path from trigger to completion
               - AlternativeFlows: Named variations (e.g., "Pay with PayPal")
               - ExceptionFlows: Error paths (e.g., "Invalid credentials", "Payment Failed")

            STRUCTURE REQUIREMENTS:
            - Use EXACT field names: Actors, Triggers, Decisions, MainFlow, AlternativeFlows, ExceptionFlows
            - Decisions must be yes/no questions ("Credentials valid?" NOT "Check credentials")

            Output JSON format:
            {
              "Actors": [actor1, actor2...],
              "Triggers": [trigger1, trigger2...],
              "Decisions": [decision1, decision2...],
              "MainFlow": [main_steps...],
              "AlternativeFlows": { "FlowName": [steps...] },
              "ExceptionFlows": { "FlowName": [steps...] }
            }

            EXAMPLE:
            INPUT:
            Use Case: Purchase  
            Description: This feature allows users to purchase items added to their shopping cart or from the product detail page.  
            Actor: User  
            Preconditions: None  
            Postconditions: The user can complete the checkout process.
            Basic Flow:
            1. The user is on the shopping cart page and has added items to the cart.
            2. The user selects items for checkout by clicking the checkbox before each item.
            3. The system displays the summary of costs.
            4. The user clicks the "Checkout" button.
            5. The system processes the checkout.
            6. The user is redirected to a page showing one or more new orders for the selected items.
               - Products from different shops are grouped into separate orders.
               - Products from the same shop are grouped into a single order.
            Alternative Flows:
            1. Shopping Cart Page:  
               - The user can select all items from one store by clicking the checkbox at the head of the store.
            2. Product Detail Page:
               - The user views the product detail page.
               - The user clicks the "Buy Now" button.
               - If the product has multiple options, the user selects one available option before adding it to the cart.
               - The user adjusts the quantity of the product using the minus or plus buttons next to the quantity field.
               - The user clicks the "Checkout" button.
               - The system processes the checkout.
               - The user is redirected to a page showing one order for the selected item.
            Exception Flows:
            1. The user cannot click the checkbox for a product that is out of stock or removed by the seller, even if it is in the shopping cart.
            2. When purchasing from the product detail page:
               - The user cannot purchase a product with multiple options without selecting one available option.
               - The user cannot purchase a product with a quantity exceeding the current stock or less than one.
               - The user cannot purchase a product with no stock or an out-of-stock option for products with multiple options.
               - The "Checkout" button is disabled if the selected product is invalid.
            
            OUTPUT:
            {
              "Actors": ["User", "System"],
              "Triggers": ["User clicks 'Checkout' button", "User clicks 'Buy Now' button"],
              "Decisions": ["Is product out-of-stock?", "Has a product with multiple options been selected?", "Is the selected quantity valid?"],
              "MainFlow": [
                "User is on the shopping cart page and has added items to the cart",
                "User selects items for checkout by clicking the checkbox before each item",
                "System displays the summary of costs",
                "User clicks 'Checkout' button",
                "System processes the checkout",
                "User is redirected to a page showing new orders for the selected items",
                "Products from different shops are grouped into separate orders",
                "Products from the same shop are grouped into a single order"
              ],
              "AlternativeFlows": {
                "Shopping Cart Page": [
                  "User selects all items from one store by clicking the checkbox at the head of the store"
                ],
                "Product Detail Page": [
                  "User views the product detail page",
                  "User clicks 'Buy Now' button",
                  "If product has multiple options, user selects one available option before adding to cart",
                  "User adjusts quantity using minus or plus buttons",
                  "User clicks 'Checkout' button",
                  "System processes the checkout",
                  "User is redirected to a page showing one order for the selected item"
                ]
              },
              "ExceptionFlows": {
                "Invalid Selection in Cart": [
                  "User cannot click checkbox for a product that is out of stock or removed by the seller"
                ],
                "Invalid Purchase from Product Detail": [
                  "User cannot purchase a product with multiple options without selecting one available option",
                  "User cannot purchase a product with a quantity exceeding current stock or less than one",
                  "User cannot purchase a product with no stock or an out-of-stock option for products with multiple options",
                  "Checkout button is disabled if the selected product is invalid"
                ]
              }
            }

            Ensure the output is a valid JSON object. Do NOT include any explanations, comments, or additional text outside the JSON object.
            """;
    }

    private UseCaseElements? ParseAndValidateResponse(string response)
    {
        try
        {
            string pattern = @"
                        \{
                        (?>            
                            [^{}]+     
                          | (?<open>\{)  
                          | (?<-open>\}) 
                        )*              
                        (?(open)(?!))  
                        \}
                    ";

            Match match = Regex.Match(
                response,
                pattern,
                RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline
            );

            var json = match.Groups[1].Value.Trim();

            if (string.IsNullOrWhiteSpace(json))
            {
                json = match.Groups[0].Value.Trim();
            }

            var result = JsonSerializer.Deserialize<UseCaseElements>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result == null)
                throw new FormatException("Failed to parse response.");

            if (result.Actors == null || !result.Actors.Any())
                throw new FormatException("Actors are required.");

            if (result.Triggers == null || !result.Triggers.Any())
                throw new FormatException("Triggers are required.");

            if (result.MainFlow == null || !result.MainFlow.Any())
                throw new FormatException("MainFlow is required.");


            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to parse use case analysis response");
            throw;
        }
    }
}
