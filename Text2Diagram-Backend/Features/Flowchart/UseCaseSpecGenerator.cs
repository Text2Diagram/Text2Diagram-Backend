using System.Text.Json;
using Text2Diagram_Backend.Common.Abstractions;
using Text2Diagram_Backend.Features.Flowchart.Agents;

namespace Text2Diagram_Backend.Features.Flowchart;

public class UseCaseSpecGenerator
{
    private readonly ILLMService _llmService;

    public UseCaseSpecGenerator(
        ILLMService llmService)
    {
        _llmService = llmService;
    }

    public async Task<List<string>> GenerateUseCaseSpecAsync(string description)
    {
        var response = await _llmService.GenerateContentAsync(GetPrompt(description));
        var json = FlowchartHelpers.ValidateJson(response.Content);
        return JsonSerializer.Deserialize<List<string>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Deserialized use case specifications failed.");
    }

    private string GetPrompt(string description)
    {
        return $"""
            You are an expert System Analyst specializing in creating detailed and structured use case specifications.

            # TASK
            Generate comprehensive use case specifications for the following system description:
            {description}

            # FORMAT REQUIREMENTS
            Your use case specification MUST follow this exact structure:
            Use Case: [Concise title describing the primary action]
            Description: [Brief summary of what this use case accomplishes]
            Actor: [Primary actor(s) who initiate or participate in this use case]
            Preconditions: [Conditions that must be true before the use case begins]
            Postconditions: [Conditions that will be true after the use case completes successfully]
            Basic Flow:
            1. [First step in the main success scenario]
            2. [Second step]
            3. [And so on...]
                - [Sub-points or clarifications if needed]
                - [Additional details]
            Alternative Flows:
            1. [Name of first alternative flow]:
                - [Description of trigger condition]
                - [Steps in this alternative flow]
            2. [Name of second alternative flow]:
                - [Description of trigger condition]
                - [Steps in this alternative flow]
            Exception Flows:
            1. [Name or condition of first exception]:
                - [How the system handles this exception]
                - [Steps to recover or alternate path]

            # EXAMPLE
            Here is an example of a well-structured use case:
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
            1. Shopping Cart Page: The user can select all items from one store by clicking the checkbox at the head of the store.
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

            # GUIDELINES FOR QUALITY
            1. Be specific and detailed in each step
            2. Use clear, concise language
            3. Include all relevant actor-system interactions
            4. Identify all possible alternative and exception flows
            5. Ensure logical flow from preconditions through basic flow to postconditions
            6. Use consistent terminology throughout
            7. Focus on WHAT happens, not HOW it happens technically
            8. Maintain the exact formatting structure provided

            # OUTPUT
            An array of strings, each string representing a use case specification in the format described above.
            Provide ONLY the use case specification without any additional explanations or commentary.
            """;
    }
}
