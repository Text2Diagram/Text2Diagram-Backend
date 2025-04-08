using LangChain.Providers.Ollama;
using Text2Diagram_Backend;
using Text2Diagram_Backend.Common;
using Text2Diagram_Backend.Common.Abstractions;
using Text2Diagram_Backend.Common.Implementations;
using Text2Diagram_Backend.Flowchart;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHostedService<NodeServerBackgroundService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Exception Handling
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Configure Ollama
builder.Services.AddSingleton<OllamaProvider>();
builder.Services.AddSingleton<FlowchartDiagramGenerator>();
builder.Services.AddSingleton<IDiagramGeneratorFactory, DiagramGeneratorFactory>();
builder.Services.AddSingleton<UseCaseSpecAnalyzer>();

builder.Services.AddSingleton<ISyntaxValidator, MermaidSyntaxValidator>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
