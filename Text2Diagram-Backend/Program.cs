﻿using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.SemanticKernel;
using Newtonsoft.Json.Serialization;
using Text2Diagram_Backend.Authentication;
using Text2Diagram_Backend.Common.Abstractions;
using Text2Diagram_Backend.Common.Hubs;
using Text2Diagram_Backend.Common.Implementations;
using Text2Diagram_Backend.Data;
using Text2Diagram_Backend.Features.ERD;
using Text2Diagram_Backend.Features.Flowchart;
using Text2Diagram_Backend.Features.Flowchart.Agents;
using Text2Diagram_Backend.Features.Sequence;
using Text2Diagram_Backend.Features.UsecaseDiagram;
using Text2Diagram_Backend.HttpHandlers;
using Text2Diagram_Backend.LLMGeminiService;
using Text2Diagram_Backend.Middlewares;
using Text2Diagram_Backend.Services;

var builder = WebApplication.CreateBuilder(args);

//builder.WebHost.UseUrls("https://0.0.0.0:5000");

// Add services to the container.
//builder.Services.AddHostedService<NodeServerBackgroundService>();

builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
});

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
    var togetherAIApiKey = builder.Configuration["TogetherAI:ApiKey"] ?? throw new InvalidOperationException("TogetherAI:ApiKey was not defined.");
    var togetherAIModelId = builder.Configuration["TogetherAI:ModelId"] ?? throw new InvalidOperationException("TogetherAI:ModelId was not defined.");
    var togetherAIEndpoint = "https://api.together.xyz/v1";

    //var unifyAIApiKey = builder.Configuration["UnifyAI:ApiKey"] ?? throw new InvalidOperationException("UnifyAI:ApiKey was not defined.");
    //var unifyAIModelId = builder.Configuration["UnifyAI:ModelId"] ?? throw new InvalidOperationException("UnifyAI:ModelId was not defined.");
    //var unifyAIEndpoint = "https://api.unify.ai/v0";
    var httpClient = new HttpClient
    {
        BaseAddress = new Uri(togetherAIEndpoint),
        Timeout = TimeSpan.FromMinutes(5)
    };
#pragma warning disable SKEXP0070
    var kernel = Kernel.CreateBuilder()
        .AddOpenAIChatCompletion(
            modelId: togetherAIModelId,
            apiKey: togetherAIApiKey,
            httpClient: httpClient
        )
        //.AddOllamaChatCompletion(llmId, httpClient)
        .Build();
#pragma warning restore
    return kernel;
});

// Add Firebase Authentication
builder.Services.AddSingleton<FirebaseTokenVerifier>();

builder.Services.AddScoped<IDiagramGeneratorFactory, DiagramGeneratorFactory>();
//builder.Services.AddSingleton<ISyntaxValidator, MermaidSyntaxValidator>();


builder.Services.AddSingleton<UseCaseSpecGenerator>();

// Add Firebase Authentication
builder.Services.AddSingleton<FirebaseTokenVerifier>();

// Add LLM Gemini Service
builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection("Gemini"));
builder.Services.AddScoped<GoogleServiceAccountTokenProvider>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var serviceAccountPath = configuration["Gemini:ServiceAccountJsonPath"]
        ?? throw new ArgumentNullException("Gemini:ServiceAccountJsonPath is not configured");
    var audience = "https://www.googleapis.com/auth/cloud-platform";
    return new GoogleServiceAccountTokenProvider(serviceAccountPath, audience);
});

builder.Services.AddHttpClient("GeminiClient", client =>
{
    client.Timeout = TimeSpan.FromMinutes(5);
}).AddHttpMessageHandler<GoogleAuthHandler>();

builder.Services.AddTransient<GoogleAuthHandler>();

builder.Services.AddScoped<ILLMService, GeminiService>();

builder.Services.AddSingleton<UseCaseSpecGenerator>();
// Register flowchart components
builder.Services.AddScoped<FlowchartDiagramGenerator>();
builder.Services.AddScoped<ERDiagramGenerator>();
builder.Services.AddScoped<SequenceDiagramGenerator>();
builder.Services.AddScoped<UseCaseSpecAnalyzerForFlowchart>();
builder.Services.AddScoped<AnalyzerForER>();
builder.Services.AddScoped<AnalyzerForSequence>();
builder.Services.AddScoped<UseCaseSpecAnalyzerForFlowchart>();
builder.Services.AddScoped<UsecaseDiagramGenerator>();
builder.Services.AddScoped<UseCaseSpecAnalyzerForUsecaseDiagram>();

builder.Services.AddScoped<BasicFlowExtractor>();
builder.Services.AddScoped<AlternativeFlowExtractor>();
builder.Services.AddScoped<ExceptionFlowExtractor>();
builder.Services.AddScoped<FlowCategorizer>();
builder.Services.AddScoped<DecisionNodeInserter>();
builder.Services.AddScoped<RejoinPointIdentifier>();


builder.Services.AddScoped<RegenerateFlowchartDiagramAgent>();
builder.Services.AddScoped<RegenerateUsecaseDiagram>();


builder.Services.AddHttpContextAccessor();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Software Diagram Generator Api",
        Version = "v1"
    });
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
                .WithOrigins(["http://localhost:5173"])
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
    });
});

builder.Services.AddSignalR();

var app = builder.Build();

app.UseCors();
// Configure the HTTP request pipeline.

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Software Diagram Generator Api v1");
});

app.UseReprLogging();

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseMiddleware<SignalRContextMiddleware>();

app.MapControllers();

app.MapHub<ThoughtProcessHub>("/hubs/thought");

app.Run();
