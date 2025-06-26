using System.Text.Json.Nodes;
using Text2Diagram_Backend.Features.UsecaseDiagram.Components;

namespace Text2Diagram_Backend.Features.UsecaseDiagram.Separate
{
    public class ExtractUsecase
    {
        public static string GetExtractUseCasePrompt(string input)
        {
            var prompt = $"""
                You are analyzing a software requirement specification in order to build a Use Case Diagram.

                ### TASK:
                From the following software requirement description, extract all **use cases** — these are distinct functionalities or services the system provides in response to an actor's interaction.

                {input}
                """
            +
            """
                ### INSTRUCTIONS:
                - A use case describes a single interaction goal or functional behavior that the system must support.
                - Use case names should be **short, action-oriented phrases** (e.g., "Place Order", "Search Products").
                - Do not include UI descriptions, internal processes, or implementation details.
                - Avoid vague or overly technical terms.

                ### FORMAT:
                Return the result in the following JSON structure:
                ```json
                {
                  "UseCases": [
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
                  "UseCases": [
                    { "Name": "Register Online" },
                    { "Name": "Schedule Appointment" },
                    { "Name": "Access Medical Records" },
                    { "Name": "View Appointments" },
                    { "Name": "Update Diagnoses" },
                    { "Name": "Issue Prescriptions" },
                    { "Name": "Confirm Appointments" },
                    { "Name": "Enter Patient Information" },
                    { "Name": "Upload Test Results" },
                    { "Name": "Notify Doctor of Lab Results" },
                    { "Name": "Validate Insurance Coverage" },
                    { "Name": "Process Claims" },
                    { "Name": "Generate Reports" },
                    { "Name": "Manage User Roles" },
                    { "Name": "Backup Data" },
                    { "Name": "View System Logs" },
                    { "Name": "Send Notifications" },
                    { "Name": "Secure Messaging" }
                  ]
                }
                ```
                """;
            return prompt;
        }

        public static List<UseCase> GetUseCases(JsonNode jsonNode)
        {
            var usecasesNode = jsonNode["UseCases"];
            if (usecasesNode is not JsonArray jsonArray)
            {
                throw new InvalidOperationException("JSON response is not an array.");
            }
            var usecases = new List<UseCase>();
            foreach (var usecase in jsonArray)
            {
                var name = usecase["Name"]?.ToString();
                if (string.IsNullOrWhiteSpace(name))
                {
                    throw new InvalidOperationException("Usecase name cannot be null or empty.");
                }
                usecases.Add(new UseCase { Name = name});
            }
            return usecases;
        }
    }
}
