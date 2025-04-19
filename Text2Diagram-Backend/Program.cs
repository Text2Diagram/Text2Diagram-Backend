using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.SemanticKernel.ChatCompletion;
using Text2Diagram_Backend;
using Text2Diagram_Backend.Common.Abstractions;
using Text2Diagram_Backend.Common.Implementations;
using Text2Diagram_Backend.Data;
using Text2Diagram_Backend.Features.Flowchart;
using Text2Diagram_Backend.Services;
using Ollama;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("https://0.0.0.0:5000");

// Add services to the container.
//builder.Services.AddHostedService<NodeServerBackgroundService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Database")
        ?? throw new InvalidOperationException("Connection string 'Database' not found.");
    options.UseNpgsql(connectionString);
});

// Exception Handling
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Configure Ollama
var llmId = builder.Configuration["Ollama:LLM"] ?? throw new InvalidOperationException("LLM was not defined.");
var ollamaEndpoint = builder.Configuration["Ollama:Endpoint"] ?? throw new InvalidOperationException("Ollama endpoint was not defined.");
builder.Services.AddSingleton(sp =>
{
    var httpClient = new HttpClient
    {
        BaseAddress = new Uri(ollamaEndpoint),
        Timeout = TimeSpan.FromMinutes(5)
    };
#pragma warning disable SKEXP0070
    var kernel = Kernel.CreateBuilder()
        .AddOllamaChatCompletion(llmId, httpClient)
        .Build();
#pragma warning restore 
    return kernel;
});

builder.Services.AddSingleton<IDiagramGeneratorFactory, DiagramGeneratorFactory>();
//builder.Services.AddSingleton<ISyntaxValidator, MermaidSyntaxValidator>();


builder.Services.AddSingleton<UseCaseSpecGenerator>();

// Register flowchart components
builder.Services.AddSingleton<FlowchartDiagramGenerator>();
builder.Services.AddSingleton<UseCaseSpecAnalyzerForFlowchart>();
builder.Services.AddSingleton<IAnalyzer<FlowchartDiagram>>(sp => sp.GetRequiredService<UseCaseSpecAnalyzerForFlowchart>());





builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Software Diagram Generator Api", Version = "v1" });

    // Swagger 2.+ support
    //                var security = new Dictionary<string, IEnumerable<string>>
    //                {
    //                    {"Bearer", new string[] { }},
    //                };
    //                
    //                c.AddSecurityDefinition("Bearer",
    //                    new OpenApiSecurityScheme()
    //                    {
    //                        In = ParameterLocation.Header,
    //                        Description = @"JWT Authorization header using the Bearer scheme. \r\n\r\n 
    //                      Enter 'Bearer' [space] and then your token in the text input below.
    //                      \r\n\r\nExample: 'Bearer 12345abcdef'",
    //                        Name = "Authorization", 
    //                        Type = SecuritySchemeType.ApiKey 
    //                    });
    c.AddSecurityDefinition("Authorization", new OpenApiSecurityScheme
    {
        Description = "Api key needed to access the endpoints. Authorization: Bearer xxxx",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                    {
                        new OpenApiSecurityScheme
                        {
                            Name = "Authorization",
                            Type = SecuritySchemeType.ApiKey,
                            In = ParameterLocation.Header,
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Authorization"
                            },
                        },
                        new string[] {}
                    }
            });
});



builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policyBuilder =>
    {
        policyBuilder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors();
// Configure the HTTP request pipeline.
// Enable Swagger in non-development environments
app.UseSwagger();
app.UseSwaggerUI();


app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
