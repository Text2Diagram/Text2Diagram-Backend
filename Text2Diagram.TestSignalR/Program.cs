using Microsoft.AspNetCore.SignalR.Client;

var connection = new HubConnectionBuilder()
    .WithUrl("https://localhost:7200/hubs/thought")
    .Build();

// Handle events
connection.On<object>("StepGenerated", (data) =>
{
    Console.WriteLine($"Step received: {data}");
});

// Start connection
await connection.StartAsync();
Console.WriteLine("✅ SignalR Connected. ConnectionId: " + connection.ConnectionId);

using var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Add("X-Connection-Id", connection.ConnectionId);
httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer eyJhbGciOiJSUzI1NiIsImtpZCI6Ijg3NzQ4NTAwMmYwNWJlMDI2N2VmNDU5ZjViNTEzNTMzYjVjNThjMTIiLCJ0eXAiOiJKV1QifQ.eyJuYW1lIjoiQ8aw4budbmcgTmd1eeG7hW4gQ2jDrSIsInBpY3R1cmUiOiJodHRwczovL2xoMy5nb29nbGV1c2VyY29udGVudC5jb20vYS9BQ2c4b2NKSjU0Z1hJYk05YUo2d3R0Q21QT1dlRjRZQjNwVllueVpyc1ZIUGstMXdOVjFqPXM5Ni1jIiwiaXNzIjoiaHR0cHM6Ly9zZWN1cmV0b2tlbi5nb29nbGUuY29tL3RleHQyZGlhZ3JhbS1rMjEiLCJhdWQiOiJ0ZXh0MmRpYWdyYW0tazIxIiwiYXV0aF90aW1lIjoxNzUxNzI1MTc1LCJ1c2VyX2lkIjoiMXVDV3lJQ0JSb05Zd1Frc2g2MmFwcTk1R3V2MSIsInN1YiI6IjF1Q1d5SUNCUm9OWXdRa3NoNjJhcHE5NUd1djEiLCJpYXQiOjE3NTE3MjUxNzUsImV4cCI6MTc1MTcyODc3NSwiZmlyZWJhc2UiOnsiaWRlbnRpdGllcyI6eyJnb29nbGUuY29tIjpbIjExMTEyOTQwNTgyNTkzNDM0NzI2MCJdfSwic2lnbl9pbl9wcm92aWRlciI6Imdvb2dsZS5jb20ifX0.jjdlSWMyoxrOH9qA0TITDkXGi2HG263-Zd99PRtZ8x-fk2QUQwyFPW7TTTCLjvlpiYNyURFv7Ep60NqVyLf4kEUw1r9FKRZoFf2KLuF7GGJCuhjjV4vLc5AvdlVF2dN3xhrH4MaFlsc9EJM6gpVULLa5QHoOgFde4n9BVF07OD34I7u_wVmGBOwDpdfKWIf05k081Xbfje70bfejcj4jR7YqTwbE5z4nXejvb3Py_Ds6IEzX9W28jZmwrdj7LmyejnmV32OB59oFumshpZaWAx_XmAv8EYXmF0AleAvhx8BHyUzkjztL6XICGs3sy-2YDZkxIndhClxpqX2B7tHEYg");

var formData = new MultipartFormDataContent();
formData.Add(new StringContent("flowchart"), "diagramType");
formData.Add(new StringContent(
    @"Use Case: Purchase
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
   - The 'Checkout' button is disabled if the selected product is invalid."
), "textInput");

var response = await httpClient.PostAsync("https://localhost:7200/generators", formData);
var responseContent = await response.Content.ReadAsStringAsync();
Console.WriteLine("Response: " + responseContent);
Console.ReadLine();
