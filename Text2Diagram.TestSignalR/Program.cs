

using Microsoft.AspNetCore.SignalR.Client;

var connection = new HubConnectionBuilder()
    .WithUrl("https://localhost:7200/hubs/thought")
    .Build();

// Test Flowchart
connection.On<string>("GetDomainStep", (step) =>
{
    Console.WriteLine($"Step received: {step}");
});

connection.On<int>("GetDomainStepDone", (time) =>
{
    Console.WriteLine($"Completed in: {time}ms");
});


connection.On<string>("CategorizeFlowsStep", (step) =>
{
    Console.WriteLine($"Step received: {step}");
});

connection.On<int>("CategorizeFlowsStepDone", (time) =>
{
    Console.WriteLine($"Completed in: {time}ms");
});


connection.On<string>("ExtractFlowsStep", (step) =>
{
    Console.WriteLine($"Step received: {step}");
});

connection.On<int>("ExtractFlowsStepDone", (time) =>
{
    Console.WriteLine($"Completed in: {time}ms");
});



connection.On<string>("DetermineInsertionPointsStep", (step) =>
{
    Console.WriteLine($"Step received: {step}");
});

connection.On<int>("DetermineInsertionPointsStepDone", (time) =>
{
    Console.WriteLine($"Completed in: {time}ms");
});

connection.On<string>("DeterminerRejoinPointsStep", (step) =>
{
    Console.WriteLine($"Step received: {step}");
});

connection.On<int>("DeterminerRejoinPointsStepDone", (time) =>
{
    Console.WriteLine($"Completed in: {time}ms");
});


connection.On<string>("GenerateDiagramStep", (step) =>
{
    Console.WriteLine($"Step received: {step}");
});


await connection.StartAsync();
Console.WriteLine("Connected. Waiting for steps...");
Console.ReadLine();