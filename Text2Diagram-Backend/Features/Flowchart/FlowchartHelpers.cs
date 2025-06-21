using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Text2Diagram_Backend.Features.Flowchart.Components;

namespace Text2Diagram_Backend.Features.Flowchart;

public static class FlowchartHelpers
{
    public static JsonNode ValidateJson(string content)
    {
        string jsonResult = string.Empty;
        var codeFenceMatch = Regex.Match(content, @"```json\s*(.*?)\s*```", RegexOptions.Singleline);
        if (codeFenceMatch.Success)
        {
            jsonResult = codeFenceMatch.Groups[1].Value.Trim();
        }
        else
        {
            throw new InvalidOperationException("No valid JSON found in the response.");
        }

        var jsonNode = JsonNode.Parse(jsonResult);
        if (jsonNode == null)
        {
            throw new InvalidOperationException("Failed to parse JSON response from the model.");
        }


        return jsonNode;

    }

    public static List<FlowNode> ExtractNodes(string input)
    {
        var validNodeTypes = NodeType.GetNames(typeof(NodeType)).ToList();

        var jsonNode = ValidateJson(input);

        if (jsonNode is not JsonArray jsonArray)
        {
            throw new InvalidOperationException("JSON response is not an array.");
        }

        var nodes = new List<FlowNode>();
        foreach (var node in jsonArray)
        {
            if (node == null) continue;

            var id = node["Id"]?.ToString();
            var label = node["Label"]?.ToString();
            var type = node["Type"]?.ToString();

            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(label) || string.IsNullOrEmpty(type))
            {
                continue;
            }

            if (validNodeTypes.Contains(type))
            {
                nodes.Add(new FlowNode()
                {
                    Id = id,
                    Label = label,
                    Type = Enum.Parse<NodeType>(type)
                });
            }
        }

        return nodes;
    }

    public static List<FlowEdge> ExtractEdges(string input)
    {
        var validNodeTypes = EdgeType.GetNames(typeof(EdgeType)).ToList();

        var jsonNode = ValidateJson(input);
        if (jsonNode is not JsonArray jsonArray)
        {
            throw new InvalidOperationException("JSON response is not an array.");
        }
        var edges = new List<FlowEdge>();
        foreach (var edge in jsonArray)
        {
            if (edge == null) continue;
            var sourceId = edge["SourceId"]?.ToString();
            var targetId = edge["TargetId"]?.ToString();
            var type = edge["Type"]?.ToString();
            var label = edge["Label"]?.ToString();
            if (string.IsNullOrEmpty(sourceId) || string.IsNullOrEmpty(targetId) || string.IsNullOrEmpty(type))
            {
                continue;
            }


            if (!validNodeTypes.Contains(type))
            {
                continue;
            }

            edges.Add(new FlowEdge()
            {
                SourceId = sourceId,
                TargetId = targetId,
                Type = Enum.Parse<EdgeType>(type),
                Label = label
            });
        }
        return edges;
    }
}
