using System.Text.Json.Nodes;
using Text2Diagram_Backend.Features.Flowchart;
using Text2Diagram_Backend.Features.UsecaseDiagram.Components;

namespace Text2Diagram_Backend.Features.UsecaseDiagram.Separate
{
    public class ExtractAssociation
    {
        public static string GetExtractAssociationPrompt(string input, List<Actor> actors, List<UseCase> useCases)
        {
            var actorList = string.Empty;
            var useCaseList = string.Empty;
            foreach (var actor in actors)
            {
                actorList = string.Join(", ", actor.Name);
            }

            foreach (var useCase in useCases)
            {
                useCaseList = string.Join(", ", useCase.Name);
            }

            var prompt = $"""
                    You are analyzing a software requirement specification in order to build a Use Case Diagram.

                    ### TASK:
                    From the following software requirement description, identify all **associations** between actors and use cases.
                    An association represents a direct interaction between an actor and a use case — e.g., the actor initiates the use case or is the primary recipient of its result.
                    {Prompts.LanguageRules}
                    Description:
                    {input}
                    Actors:
                    [{actorList}]
                    Usecases:
                    [{useCaseList}]
                    """
                +
                """
                    ### INSTRUCTIONS:
                    - Only include interactions between the provided actors and use cases.
                    - Each association links **one actor** to **one use case**.
                    - Do not infer associations with internal system components or background processes.
                    - Use only the exact actor and use case names provided.

                    ### FORMAT:
                    Return the result in the following JSON structure:
                    ```json
                    {
                      "Associations": [
                        { "Actor": "ActorName", "UseCase": "UseCaseName" },
                        ...
                      ]
                    }
                    ```
                    """
                    +
                """ 
                    ### EXAMPLE:
                    INPUT:
                    Description:"The Hospital Management System (HMS) is designed to streamline operations across a mid-size hospital. Patients must be able to register online, schedule appointments, and access their medical records. Doctors should log in to view appointments, update diagnoses, and issue prescriptions.
                    Receptionists are responsible for confirming appointments and entering patient information. The laboratory staff upload test results and notify doctors when reports are available.
                    An external insurance system will be integrated to validate coverage and process claims automatically. The system should generate reports for hospital administrators, including usage statistics, revenue, and doctor performance.
                    System administrators will manage user roles, data backups, and system logs. The system must also support secure communication between doctors and patients via a built-in messaging feature.
                    Users should receive notifications via email or SMS for appointment confirmations, lab results, and prescription availability.
                    "
                    Actors:
                    ["Patient", "Doctor", "Receptionist", "Laboratory Staff", "Insurance System", "Hospital Administrator", "System Administrator"]
                    Usecases:
                    ["Register Online", "Schedule Appointment", "Access Medical Records", "View Appointments", "Update Diagnoses", "Issue Prescriptions", "Confirm Appointments", "Upload Test Results", "Notify Doctors", "Validate Coverage", "Process Claims", "Generate Reports", "Manage User Roles", "Data Backups", "System Logs", "Secure Communication", "Send Notifications"]

                    OUTPUT:
                    ```json
                    {
                      "Associations": [
                        { "Actor": "Patient", "UseCase": "Register Online" },
                        { "Actor": "Patient", "UseCase": "Schedule Appointment" },
                        { "Actor": "Patient", "UseCase": "Access Medical Records" },

                        { "Actor": "Doctor", "UseCase": "View Appointments" },
                        { "Actor": "Doctor", "UseCase": "Update Diagnoses" },
                        { "Actor": "Doctor", "UseCase": "Issue Prescriptions" },
                        { "Actor": "Doctor", "UseCase": "Secure Messaging" },

                        { "Actor": "Receptionist", "UseCase": "Confirm Appointments" },
                        { "Actor": "Receptionist", "UseCase": "Enter Patient Information" },

                        { "Actor": "Laboratory Staff", "UseCase": "Upload Test Results" },
                        { "Actor": "Laboratory Staff", "UseCase": "Notify Doctor of Report" },

                        { "Actor": "Insurance System", "UseCase": "Validate Insurance Coverage" },
                        { "Actor": "Insurance System", "UseCase": "Process Claims" },

                        { "Actor": "Hospital Administrator", "UseCase": "Generate Reports" },

                        { "Actor": "System Administrator", "UseCase": "Manage User Roles" },
                        { "Actor": "System Administrator", "UseCase": "Perform Data Backups" },
                        { "Actor": "System Administrator", "UseCase": "Manage System Logs" },

                        { "Actor": "User", "UseCase": "Send Notifications" }
                      ]
                    }
                    
                    ```
                    """;
            return prompt;
        }

        public static List<Association> GetAssociations(JsonNode jsonNode)
        {
            var associationNode = jsonNode["Associations"];
            if (associationNode is not JsonArray jsonArray)
            {
                throw new InvalidOperationException("JSON response is not an array.");
            }
            var associations = new List<Association>();
            foreach (var association in jsonArray)
            {
                var actor = association["Actor"]?.ToString();
                var useCase = association["UseCase"]?.ToString();
                if (string.IsNullOrWhiteSpace(actor) || string.IsNullOrWhiteSpace(useCase))
                {
                    throw new InvalidOperationException("Actor name and Usecase cannot be null or empty.");
                }
                associations.Add(new Association { Actor = actor, UseCase = useCase });
            }
            return associations;
        }
    }
}
