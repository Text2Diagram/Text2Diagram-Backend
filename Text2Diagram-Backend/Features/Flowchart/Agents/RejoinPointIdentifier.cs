using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
using System.Text.Json;
using Text2Diagram_Backend.Common.Abstractions;
using Text2Diagram_Backend.Common.Hubs;
using Text2Diagram_Backend.Features.Flowchart.Components;
using Text2Diagram_Backend.Middlewares;

namespace Text2Diagram_Backend.Features.Flowchart.Agents;

public class RejoinPointIdentifier
{
    private readonly ILLMService _llmService;
    private readonly IHubContext<ThoughtProcessHub> _hubContext;
    private readonly ILogger<RejoinPointIdentifier> _logger;

    public RejoinPointIdentifier(ILLMService llmService,
        IHubContext<ThoughtProcessHub> hubContext,
                                 ILogger<RejoinPointIdentifier> logger)
    {
        _llmService = llmService ?? throw new ArgumentNullException(nameof(llmService));
        _hubContext = hubContext;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<Flow>> AddRejoinPointsAsync(List<Flow> flows)
    {
        var basicFlow = flows.FirstOrDefault(f => f.FlowType == FlowType.Basic)
            ?? throw new InvalidOperationException("Basic flow required.");
        var subFlows = flows.Where(f => f.FlowType is FlowType.Alternative or FlowType.Exception).ToList();
        var modifiedFlows = flows.Select(f => new Flow
        (
            f.Name,
            f.FlowType,
            [.. f.Nodes],
            [.. f.Edges]
        )).ToList();

        var sw = Stopwatch.StartNew();
        await _hubContext.Clients.Client(SignalRContext.ConnectionId).SendAsync("FlowchartStepStart", "Determining rejoin points...");

        foreach (var subFlow in subFlows)
        {
            var endNodes = subFlow.Nodes.Where(n => n.Type == NodeType.End).ToList();
            foreach (var endNode in endNodes)
            {
                var prompt = $"""
                    Given the basic flow nodes: {JsonSerializer.Serialize(basicFlow.Nodes)},
                    and a subflow named '{subFlow.Name}' (Type: {subFlow.FlowType}) with end node label '{endNode.Label}',
                    determine if this subflow should rejoin the basic flow and, if so, identify the exact basic flow node ID (e.g., 'basic_flow_input_1') to rejoin.
                    The node ID must match one of the provided basic flow node IDs exactly, without any prefixes or modifications.
                    For example:
                    - For 'LocationServicesDisabled' (Exception) with end node 'User provides location, allowing process to continue', rejoin to 'basic_flow_input_1' because the user provides a location and continues entering pickup details.
                    - For 'ChooseSpecificDriver' (Alternative) with end node 'Driver is successfully assigned', rejoin to 'basic_flow_process_2' because the driver assignment completes and the flow continues with the driver accepting the ride.
                    - For 'ChooseSpecificDriver' (Alternative) with end node 'Driver is not available, assignment fails', do not rejoin (return empty string) as the flow terminates with a failure notification.
                    - For 'ScheduleRide' (Alternative) with any end node, do not rejoin (return empty string) as the ride is queued until the scheduled time triggers a new driver search.
                    - For 'NoDriversAvailable' (Exception) with any end node, do not rejoin (return empty string) as the flow terminates with user notification.
                    - For 'PaymentIssue' (Exception) with any end node, do not rejoin (return empty string) as the user must resolve payment issues separately.
                    """
                    +
                    """
                    Return JSON: { "RejoinNodeId": "", "ShouldRejoin": true/false }
                    """;
                try
                {
                    var response = await _llmService.GenerateContentAsync(prompt);
                    _logger.LogDebug("LLM response for subflow {SubflowName}, end node {EndNodeId}: {Response}",
                        subFlow.Name, endNode.Id, response.Content);
                    var jsonResponse = FlowchartHelpers.ValidateJson(response.Content);
                    if (jsonResponse == null)
                    {
                        _logger.LogWarning("Invalid JSON response for rejoin point of subflow {SubflowName}, end node {EndNodeId}. Response: {Response}. Skipping.",
                            subFlow.Name, endNode.Id, response.Content);
                        continue;
                    }

                    var shouldRejoin = jsonResponse["ShouldRejoin"]?.GetValue<bool>() ?? false;
                    var rejoinNodeId = jsonResponse["RejoinNodeId"]?.GetValue<string>()?.Trim() ?? string.Empty;

                    if (!shouldRejoin)
                    {
                        _logger.LogInformation("Subflow {SubflowName} end node {EndNodeId} does not rejoin the basic flow per LLM response.",
                            subFlow.Name, endNode.Id);
                        continue;
                    }

                    // Validate and correct rejoinNodeId
                    if (string.IsNullOrEmpty(rejoinNodeId))
                    {
                        _logger.LogWarning("Empty rejoin node ID for subflow {SubflowName}, end node {EndNodeId} despite ShouldRejoin=true. Skipping.",
                            subFlow.Name, endNode.Id);
                        continue;
                    }

                    // Check if rejoinNodeId has a subflow prefix and remove it
                    var correctedRejoinNodeId = rejoinNodeId;
                    var subFlowsStartWithPrefix = subFlows.Where(s => rejoinNodeId.StartsWith($"{s.Name}_"));
                    if (subFlowsStartWithPrefix.Any())
                    {
                        var subFlowWithPrefix = subFlowsStartWithPrefix.First();
                        correctedRejoinNodeId = rejoinNodeId.Substring(subFlowWithPrefix.Name.Length + 1);
                        _logger.LogDebug("Removed subflow prefix '{Prefix}' from rejoin node ID '{OriginalId}' to get '{CorrectedId}' for subflow {SubflowName}.",
                            subFlowWithPrefix.Name, rejoinNodeId, correctedRejoinNodeId, subFlow.Name);
                    }

                    // Ensure correctedRejoinNodeId is a valid basic flow node ID
                    if (!basicFlow.Nodes.Any(n => n.Id == correctedRejoinNodeId))
                    {
                        _logger.LogWarning("Invalid rejoin node ID '{RejoinNodeId}' (corrected from '{OriginalId}') for subflow {SubflowName}, end node {EndNodeId}. Expected a basic flow node ID. Skipping.",
                            correctedRejoinNodeId, rejoinNodeId, subFlow.Name, endNode.Id);
                        continue;
                    }

                    // Add rejoin edge
                    var modifiedSubflow = modifiedFlows.First(f => f.Name == subFlow.Name);
                    var sourceId = $"{subFlow.Name}_{endNode.Id}";
                    if (!modifiedSubflow.Edges.Any(e => e.SourceId == sourceId && e.TargetId == correctedRejoinNodeId))
                    {
                        modifiedSubflow.Edges.Add(new FlowEdge
                        (
                            sourceId,
                            correctedRejoinNodeId,
                            EdgeType.Arrow,
                            "Rejoin"
                        ));
                        _logger.LogDebug("Added rejoin edge from {SubflowEndNodeId} to {RejoinNodeId} for subflow {SubflowName}.",
                            sourceId, correctedRejoinNodeId, subFlow.Name);
                    }
                    else
                    {
                        _logger.LogDebug("Rejoin edge from {SubflowEndNodeId} to {RejoinNodeId} for subflow {SubflowName} already exists. Skipping.",
                            sourceId, correctedRejoinNodeId, subFlow.Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to identify rejoin point for subflow {SubflowName}, end node {EndNodeId}: {ErrorMessage}",
                        subFlow.Name, endNode.Id, ex.Message);
                }
            }
        }

        sw.Stop();
        await _hubContext.Clients.Client(SignalRContext.ConnectionId).SendAsync("FlowchartStepDone", sw.ElapsedMilliseconds);

        return modifiedFlows;
    }
}