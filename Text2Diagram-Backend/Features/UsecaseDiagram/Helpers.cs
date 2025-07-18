﻿using System.IO.Packaging;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Text2Diagram_Backend.Features.UsecaseDiagram.Components;
using Text2Diagram_Backend.Features.UsecaseDiagram.Separate;

namespace Text2Diagram_Backend.Features.UsecaseDiagram
{
    public class Helpers
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
                throw new InvalidOperationException("Failed to parse JSON response from the model");
            }
            return jsonNode;

        }      
        public static string NormalizeActorName(string actorName)
        {
            return string.Join('_', actorName.Split(' '));
        }
    }
}
