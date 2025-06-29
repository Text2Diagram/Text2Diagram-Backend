using System.Text.Json.Nodes;
using Text2Diagram_Backend.Features.UsecaseDiagram.Components;

namespace Text2Diagram_Backend.Features.UsecaseDiagram.Separate
{
    public class ExtractActor
    {
        public static string GetExtractActorPrompt(string input)
        {
            var prompt = $"""
                You are analyzing a software requirement specification in order to build a Use Case Diagram.

                ### TASK:
                From the following software requirement description, identify all **actors** — external entities (e.g. people, organizations, or external systems) that interact directly with the system.

                {input}
                """
            +
            """
                ### INSTRUCTIONS:
                - Actors are typically external to the system and interact by initiating or receiving actions. If actor has multiple words, using underscores between each word (e.g., "Online_Shopper", "Restaurant_Receptionist").
                - Common actor types include users, roles, departments, or integrated systems.
                - Do **not** include internal system components or features as actors.
                - Group similar users under generalized role names when appropriate (e.g., "Customer", "Doctor").

                ### FORMAT:
                Return the result in the following JSON structure:
                ```json
                {
                  "Actors": [
                    { "Name": "Actor1",
                      "Name": "Actor2",
                    },
                    ...
                  ]
                }
                ```
                """
                +
                """
                ### EXAMPLE:
                INPUT:
                "The Hospital Management System (HMS) is designed to streamline operations across a mid-size hospital. Patients must be able to register online, schedule appointments, and access their medical records. Doctors should log in to view appointments, update diagnoses, and issue prescriptions.
                Receptionists are responsible for confirming appointments and entering patient information. The laboratory staff upload test results and notify doctors when reports are available.
                An external insurance system will be integrated to validate coverage and process claims automatically. The system should generate reports for hospital administrators, including usage statistics, revenue, and doctor performance.
                System administrators will manage user roles, data backups, and system logs. The system must also support secure communication between doctors and patients via a built-in messaging feature.
                Users should receive notifications via email or SMS for appointment confirmations, lab results, and prescription availability.
                "
                OUTPUT:
                ```json
                {
                  "Actors": [
                    { "Name": "Patient" },
                    { "Name": "Doctor" },
                    { "Name": "Receptionist" },
                    { "Name": "Laboratory_Staff" },
                    { "Name": "Insurance_System" },
                    { "Name": "Hospital_Administrator" },
                    { "Name": "System_Administrator" }
                  ]
                }
                ```
                """;
            return prompt;
        }

        public static List<Actor> GetActors(JsonNode jsonNode)
        {
            var actorsNode = jsonNode["Actors"];
            if (actorsNode is not JsonArray jsonArray)
            {
                throw new InvalidOperationException("JSON response is not an array.");
            }
            var actors = new List<Actor>();
            foreach (var actor in jsonArray)
            {
                var name = actor["Name"]?.ToString();
                if (string.IsNullOrWhiteSpace(name))
                {
                    throw new InvalidOperationException("Actor name cannot be null or empty.");
                }
                actors.Add(new Actor { Name = name});
            }
            return actors;
        }
    }

}
