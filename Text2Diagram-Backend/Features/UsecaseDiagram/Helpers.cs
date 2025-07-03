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

        public static UseCaseDiagram ApplyInstructions(UseCaseDiagram model, Instructions instructions)
        {
            foreach (var instruction in instructions.instructions)
            {
                var action = instruction.Action.ToString();
                var target = instruction.Target.ToString();

                switch (action)
                {
                    case "add":
                        model = AddToModel(model, instruction, target);
                        break;

                    case "remove":
                        model = RemoveFromModel(model, instruction, target);
                        break;

                    case "update":
                        model = UpdateModel(model, instruction, target);
                        break;
                }
            }
            return model;
        }

        private static UseCaseDiagram AddToModel(UseCaseDiagram model, UserFeedBack i, string target)
        {
            foreach(var package in model.Packages)
            {
                switch (target)
                {
                    case "actor":
                        if (!package.Actors.Any(a => a.Name == i.Name))
                            package.Actors.Add(new Actor { Name = i.Name! });
                        break;

                    case "usecase":
                        if (!package.UseCases.Any(u => u.Name == i.Name))
                            package.UseCases.Add(new UseCase { Name = i.Name! });
                        break;

                    case "association":
                        if (!package.Associations.Any(a => a.Actor == i.Actor && a.UseCase == i.UseCase))
                            package.Associations.Add(new Association { Actor = i.Actor!, UseCase = i.UseCase! });
                        break;

                    case "include":
                        package.Includes.Add(new Include { BaseUseCase = i.BaseUseCase!, IncludedUseCase = i.IncludedUseCase! });
                        break;

                    case "extend":
                        package.Extends.Add(new Extend { BaseUseCase = i.BaseUseCase!, ExtendedUseCase = i.ExtendedUseCase! });
                        break;
                }
            }
            return model;
        }

        private static UseCaseDiagram RemoveFromModel(UseCaseDiagram model, UserFeedBack i, string target)
        {
            foreach(var package in model.Packages)
            {
                switch (target)
                {
                    case "actor":
                        package.Actors.RemoveAll(a => a.Name == i.Name);
                        package.Associations.RemoveAll(a => a.Actor == i.Name);
                        break;

                    case "usecase":
                        package.UseCases.RemoveAll(u => u.Name == i.Name);
                        package.Associations.RemoveAll(a => a.UseCase == i.Name);
                        package.Includes.RemoveAll(x => x.BaseUseCase == i.Name || x.IncludedUseCase == i.Name);
                        package.Extends.RemoveAll(x => x.BaseUseCase == i.Name || x.ExtendedUseCase == i.Name);
                        break;

                    case "association":
                        package.Associations.RemoveAll(a => a.Actor == i.Actor && a.UseCase == i.UseCase);
                        break;

                    case "include":
                        package.Includes.RemoveAll(x => x.BaseUseCase == i.BaseUseCase && x.IncludedUseCase == i.IncludedUseCase);
                        break;

                    case "extend":
                        package.Extends.RemoveAll(x => x.BaseUseCase == i.BaseUseCase && x.ExtendedUseCase == i.ExtendedUseCase);
                        break;
                }
            }
            return model;
        }

        private static UseCaseDiagram UpdateModel(UseCaseDiagram model, UserFeedBack i, string target)
        {
            foreach(var package in model.Packages)
            {
                switch (target)
                {
                    case "actor":
                        var actor = package.Actors.FirstOrDefault(a => a.Name == i.Name);
                        if (actor != null) actor.Name = i.NewName!;
                        foreach (var assoc in package.Associations.Where(a => a.Actor == i.Name))
                            assoc.Actor = i.NewName!;
                        break;

                    case "usecase":
                        var usecase = package.UseCases.FirstOrDefault(u => u.Name == i.Name);
                        if (usecase != null) usecase.Name = i.NewName!;
                        foreach (var assoc in package.Associations.Where(a => a.UseCase == i.Name))
                            assoc.UseCase = i.NewName!;
                        foreach (var inc in package.Includes)
                        {
                            if (inc.BaseUseCase == i.Name) inc.BaseUseCase = i.NewName!;
                            if (inc.IncludedUseCase == i.Name) inc.IncludedUseCase = i.NewName!;
                        }
                        foreach (var ext in package.Extends)
                        {
                            if (ext.BaseUseCase == i.Name) ext.BaseUseCase = i.NewName!;
                            if (ext.ExtendedUseCase == i.Name) ext.ExtendedUseCase = i.NewName!;
                        }
                        break;
                }
            }
            return model;
        }
    }
}
