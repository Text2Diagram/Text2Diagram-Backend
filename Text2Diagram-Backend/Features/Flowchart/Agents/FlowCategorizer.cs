using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Text2Diagram_Backend.Common.Abstractions;

namespace Text2Diagram_Backend.Features.Flowchart.Agents;

public class FlowCategorizer
{
    private readonly ILLMService _llmService;
    private readonly ILogger<FlowCategorizer> _logger;

    public FlowCategorizer(ILLMService llmService, ILogger<FlowCategorizer> logger)
    {
        _llmService = llmService;
        _logger = logger;
    }

    private FlowCategories ExtractJsonFlows(string textContent)
    {
        try
        {
            var jsonMatch = Regex.Match(textContent, @"```json\n([\s\S]*?)\n```", RegexOptions.Multiline);
            if (!jsonMatch.Success)
            {
                _logger.LogError("No JSON block found in the text content.");
                throw new InvalidOperationException("No JSON block found in the input text.");
            }

            var jsonString = jsonMatch.Groups[1].Value.Trim();

            FlowCategories flowCategories;
            try
            {
                flowCategories = JsonSerializer.Deserialize<FlowCategories>(jsonString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? throw new InvalidOperationException("Deserialized flow categories is null.");
            }
            catch (JsonException ex)
            {
                _logger.LogError("Failed to deserialize JSON: {0}", ex.Message);
                throw new InvalidOperationException("Invalid JSON format.", ex);
            }

            // Validate basic flow
            if (string.IsNullOrEmpty(flowCategories.BasicFlow))
            {
                _logger.LogError("Basic flow is missing or empty.");
                throw new InvalidOperationException("Use case must contain exactly one basic flow.");
            }

            // Validate unique flow names
            var allFlowNames = flowCategories.AlternativeFlows.Select(f => f.Name)
                .Concat(flowCategories.ExceptionFlows.Select(f => f.Name))
                .ToHashSet();
            if (allFlowNames.Count != flowCategories.AlternativeFlows.Count + flowCategories.ExceptionFlows.Count)
            {
                _logger.LogError("Duplicate flow names detected.");
                throw new InvalidOperationException("Duplicate flow names detected.");
            }

            _logger.LogInformation("Successfully extracted and deserialized flows: {0} alternative flows, {1} exception flows.",
                flowCategories.AlternativeFlows.Count, flowCategories.ExceptionFlows.Count);

            return flowCategories;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to extract JSON flows: {0}", ex.Message);
            throw new InvalidOperationException("Failed to extract JSON flows.", ex);
        }
    }

    public async Task<FlowCategories> CategorizeFlowsAsync(string useCaseDescription)
    {
        try
        {
            var prompt = """
                You are an expert Flowchart Analyzer.
                Analyze the provided use case description and categorize it into three parts:
                - basicFlow: The primary path of the purchase process, described as a single string with numbered steps and any sub-bullets.
                - alternativeFlows: Variations of the main process (e.g., selecting all items from one store, purchasing from the product detail page), as an array of objects with 'name' and 'description' properties.
                - exceptionFlows: Error cases (e.g., out-of-stock items, invalid selections), as an array of objects with 'name' and 'description' properties.

                ### Rules:
                - Preserve the original text of the basic flow, including step numbering and sub-bullets (e.g., order grouping rules).
                - For each alternative and exception flow, generate a meaningful 'name' (camelCase, e.g., 'SelectAllFromStore') based on the first action, key condition, or primary focus of the flow.
                - Each flow's 'description' should include the full text, including step numbering, sub-bullets, or labels.
                - Ensure names are unique, descriptive, and avoid generic terms like 'Flow1'.
                - Return a valid JSON object with the structure: { "basicFlow": string, "alternativeFlows": [{ "name": string, "description": string }], "exceptionFlows": [{ "name": string, "description": string }] }.
                - If a section is missing, set the corresponding field to an empty string (basicFlow) or empty array (alternativeFlows, exceptionFlows).
                - Handle formatting variations (e.g., extra whitespace, missing colons) robustly.
                """
                +
                $"""
                Use case description:
                {useCaseDescription}
                """
                +
                """
                ### Example Input:
                # Use Case: Purchase
                Description: This feature allows users to purchase items added to their shopping cart or from the product detail page.
                Actor: User
                Preconditions: None
                Postconditions: The user can complete the checkout process.
                Basic Flow:
                1. The user is on the shopping cart page and has added items to the cart.
                2. The user selects items for checkout by clicking the checkbox before each item.
                3. The system displays the summary of costs.
                4. The user clicks the 'Checkout' button.
                5. The system processes the checkout.
                6. The user is redirected to a page showing one or more new orders for the selected items.
                   - Products from different shops are grouped into separate orders.
                   - Products from the same shop are grouped into a single order.
                Alternative Flows:
                1. Shopping Cart Page: The user can select all items from one store by clicking the checkbox at the head of the store.
                2. Product Detail Page:
                   - The user views the product detail page.
                   - The user clicks the 'Buy Now' button.
                   - If the product has multiple options, the user selects one available option before adding it to the cart.
                   - The user adjusts the quantity of the product using the minus or plus buttons next to the quantity field.
                   - The user clicks the 'Checkout' button.
                   - The system processes the checkout.
                   - The user is redirected to a page showing one order for the selected item.
                Exception Flows:
                1. The user cannot click the checkbox for a product that is out of stock or removed by the seller, even if it is in the shopping cart.
                2. When purchasing from the product detail page:
                   - The user cannot purchase a product with multiple options without selecting one available option.
                   - The user cannot purchase a product with a quantity exceeding the current stock or less than one.
                   - The user cannot purchase a product with no stock or an out-of-stock option for products with multiple options.
                   - The 'Checkout' button is disabled if the selected product is invalid.
                
                ### Example Output:
                ```json
                {
                    "basicFlow": "1. The user is on the shopping cart page and has added items to the cart.\n2. The user selects items for checkout by clicking the checkbox before each item.\n3. The system displays the summary of costs.\n4. The user clicks the 'Checkout' button.\n5. The system processes the checkout.\n6. The user is redirected to a page showing one or more new orders for the selected items.\n   - Products from different shops are grouped into separate orders.\n   - Products from the same shop are grouped into a single order.",
                    "alternativeFlows": [
                        {
                            "name": "SelectAllFromStore",
                            "description": "1. Shopping Cart Page: The user can select all items from one store by clicking the checkbox at the head of the store."
                        },
                        {
                            "name": "PurchaseFromProductDetail",
                            "description": "2. Product Detail Page:\n   - The user views the product detail page.\n   - The user clicks the 'Buy Now' button.\n   - If the product has multiple options, the user selects one available option before adding it to the cart.\n   - The user adjusts the quantity of the product using the minus or plus buttons next to the quantity field.\n   - The user clicks the 'Checkout' button.\n   - The system processes the checkout.\n   - The user is redirected to a page showing one order for the selected item."
                        }
                    ],
                    "exceptionFlows": [
                        {
                            "name": "OutOfStockItem",
                            "description": "1. The user cannot click the checkbox for a product that is out of stock or removed by the seller, even if it is in the shopping cart."
                        },
                        {
                            "name": "InvalidProductSelection",
                            "description": "2. When purchasing from the product detail page:\n   - The user cannot purchase a product with multiple options without selecting one available option.\n   - The user cannot purchase a product with a quantity exceeding the current stock or less than one.\n   - The user cannot purchase a product with no stock or an out-of-stock option for products with multiple options.\n   - The 'Checkout' button is disabled if the selected product is invalid."
                        }
                    ]
                }
                """;

            var chatHistory = new ChatHistory();
            chatHistory.AddUserMessage(prompt);
            var response = await _llmService.GenerateContentAsync(prompt);
            var textContent = response.Content;
            _logger.LogDebug("LLM response:\n{0}", textContent);

            if (string.IsNullOrEmpty(textContent))
            {
                _logger.LogError("Empty response received from chat completion service.");
                throw new InvalidOperationException("Failed to categorize flows: Empty response.");
            }

            return ExtractJsonFlows(textContent);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to categorize flows: {0}", ex.Message);
            throw new InvalidOperationException("Failed to categorize flows.", ex);
        }
    }
}

public class FlowDescription
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}

public class FlowCategories
{
    [JsonPropertyName("basicFlow")]
    public string BasicFlow { get; set; } = string.Empty;

    [JsonPropertyName("alternativeFlows")]
    public List<FlowDescription> AlternativeFlows { get; set; } = new List<FlowDescription>();

    [JsonPropertyName("exceptionFlows")]
    public List<FlowDescription> ExceptionFlows { get; set; } = new List<FlowDescription>();

    public void Deconstruct(
        out string basicFlow,
        out List<FlowDescription> alternativeFlows,
        out List<FlowDescription> exceptionFlows)
    {
        basicFlow = BasicFlow;
        alternativeFlows = AlternativeFlows;
        exceptionFlows = ExceptionFlows;
    }
}