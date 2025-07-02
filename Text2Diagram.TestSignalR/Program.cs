

using Microsoft.AspNetCore.SignalR.Client;

var connection = new HubConnectionBuilder()
    .WithUrl("https://localhost:7200/hubs/thought")
    .Build();

// Test Flowchart
connection.On<string>("FlowchartStepStart", (step) =>
{
    Console.WriteLine($"Step received: {step}");
});

connection.On<int>("FlowchartStepDone", (time) =>
{
    Console.WriteLine($"Completed in: {time}ms");
});

await connection.StartAsync();
Console.WriteLine("Connected. Waiting for steps...");
Console.ReadLine();