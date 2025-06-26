using System.Text.Json.Nodes;
using Text2Diagram_Backend.Features.UsecaseDiagram.Components;

namespace Text2Diagram_Backend.Features.UsecaseDiagram.Separate
{
    public class ExtractRelationship
    {
        public static string GetExtractRelationshipPrompt(string input, List<UseCase> useCases)
        {
            var useCaseList = string.Empty;
            foreach (var useCase in useCases)
            {
                useCaseList = string.Join(", ", useCase.Name);
            }
            var prompt = $"""
                You are analyzing a software requirement specification in order to build a Use Case Diagram.

                ### TASK:
                From the following software requirement description and list of use cases, identify **include** and **extend** relationships between the use cases.
                Description:{input}
                Usecases:[{useCaseList}]
                """
                +
                """
                ### INSTRUCTIONS:

                - A **«include»** relationship represents a required sub-functionality that is reused across multiple use cases (e.g., "Place Order" includes "Process Payment").
                - A **«extend»** relationship represents an optional or conditional behavior that extends a base use case under certain conditions (e.g., "Login" might be extended by "Two-Factor Authentication").
                - Focus only on meaningful and intentional interactions — not implementation details.
                - Use only use case names that appear in the provided list.
                - You must not invent or assume new use cases.
                - Each relation must include exactly two use cases.

                ### FORMAT:
                Return the result in the following JSON structure:
                ```json
                {
                  "Includes": [
                    { "BaseUseCase": "Base", "IncludedUseCase": "Included" }
                  ],
                  "Extends": [
                    { "BaseUseCase": "Base", "ExtendedUseCase": "Extension" }
                  ]
                }
                ```
                """
                    +
                """
                ### EXAMPLE:
                INPUT:
                Description:
                "The Hospital Management System (HMS) is designed to streamline operations across a mid-size hospital. Patients must be able to register online, schedule appointments, and access their medical records. Doctors should log in to view appointments, update diagnoses, and issue prescriptions.
                Receptionists are responsible for confirming appointments and entering patient information. The laboratory staff upload test results and notify doctors when reports are available.
                An external insurance system will be integrated to validate coverage and process claims automatically. The system should generate reports for hospital administrators, including usage statistics, revenue, and doctor performance.
                System administrators will manage user roles, data backups, and system logs. The system must also support secure communication between doctors and patients via a built-in messaging feature.
                Users should receive notifications via email or SMS for appointment confirmations, lab results, and prescription availability."

                UseCases:
                [
                "Register",
                "Schedule Appointment",
                "Access Medical Records",
                "View Appointments",
                "Update Diagnosis",
                "Issue Prescription",
                "Confirm Appointment",
                "Enter Patient Information",
                "Upload Test Results",
                "Notify Doctor",
                "Validate Insurance Coverage",
                "Process Insurance Claim",
                "Generate Reports",
                "Manage User Roles",
                "Backup Data",
                "Manage Logs",
                "Send Message",
                "Receive Notification"
                ]
                OUTPUT:
                ```json
                {
                  "Includes": [
                    { "BaseUseCase": "Schedule Appointment", "IncludedUseCase": "Confirm Appointment" },
                    { "BaseUseCase": "Process Insurance Claim", "IncludedUseCase": "Validate Insurance Coverage" },
                    { "BaseUseCase": "Register", "IncludedUseCase": "Enter Patient Information" }
                  ],
                  "Extends": [
                    { "BaseUseCase": "Send Message", "ExtendedUseCase": "Receive Notification" }
                  ]
                }
                
                ```
                """;
            return prompt;
        }

        public static List<Include> GetIncludes(JsonNode jsonNode)
        {
            var includes = new List<Include>();
            if (jsonNode?["Includes"] is JsonArray includesArray)
            {
                foreach (var include in includesArray)
                {
                    var baseUseCase = include["BaseUseCase"]?.ToString();
                    var includedUseCase = include["IncludedUseCase"]?.ToString();
                    if (string.IsNullOrEmpty(baseUseCase) || string.IsNullOrEmpty(includedUseCase))
                    {
                        throw new InvalidOperationException("Usecase name cannot be null or empty.");
                    }
                    includes.Add(new Include { BaseUseCase = baseUseCase, IncludedUseCase = includedUseCase });
                }
            }
            return includes;
        }

        public static List<Extend> GetExtends(JsonNode jsonNode)
        {
            var extends = new List<Extend>();
            if (jsonNode?["Extends"] is JsonArray extendsArray)
            {
                foreach (var extend in extendsArray)
                {
                    var baseUseCase = extend["BaseUseCase"]?.ToString();
                    var extendedUseCase = extend["ExtendedUseCase"]?.ToString();
                    if (string.IsNullOrEmpty(baseUseCase) || string.IsNullOrEmpty(extendedUseCase))
                    {
                        throw new InvalidOperationException("Usecase name cannot be null or empty.");
                    }
                    extends.Add(new Extend { BaseUseCase = baseUseCase, ExtendedUseCase = extendedUseCase });
                }
            }
            return extends;
        }
    }
}
