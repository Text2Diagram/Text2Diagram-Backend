using Text2Diagram_Backend.Features.Flowchart;

namespace Text2Diagram_Backend.Features.UsecaseDiagram.Separate
{
    public class ExtractPackage
    {
        public static string GetExtractPackagePrompt(string input)
        {
            var prompt = $"""
                You are helping to organize elements of a Use Case Diagram into logical **packages** (a.k.a. subsystems or modules). These packages will be used to structure the diagram based on functionality or domain boundaries.

                ### TASK:
                Given the following lists of:
                - **Actors** (external entities),
                - **UseCases** (functionalities the system provides),
                - **Associations** (interactions between actors and use cases),
                - **Includes** (common reusable use cases),
                - **Extends** (optional or conditional use cases),

                Group the related elements into packages (a.k.a. boundaries). Each package should contain:
                - a name that reflects the functionality area (e.g. "User Management", "Payments"),
                - a list of Actors,
                - a list of UseCases,
                - Associations between the two,
                - and Include/Extend relationships related to the UseCases in that package.

                {Prompts.LanguageRules}

                {input}
                """

                + """
                ### INSTRUCTIONS:
                - A package should group related use cases and their associated actors.               
                - Include/Extend relationships should stay inside the package if both base and target are in the same area; otherwise, keep them in the most relevant package.
                - Try to minimize duplication across packages.
                - Package names should be descriptive and relevant (e.g., “Appointment Management”, “Authentication”).
                - Use case name must contain only words and spaces, avoid slashes, parentheses, commas, semicolons, ... or any other punctuation marks.

                ### FORMAT:
                Return the result as valid JSON like below:
                ```json
                {
                  "Packages": [
                    {
                      "Name": "BOUNDARY NAME",
                      "Actors": [
                        { "Name": "ACTOR_NAME_1" },
                        { "Name": "ACTOR_NAME_2" }
                      ],
                      "UseCases": [
                        { "Name": "USE CASE NAME 1" },
                        { "Name": "USE CASE NAME 2" }
                      ],
                      "Associations": [
                        { "Actor": "ACTOR_NAME", "UseCase": "USE CASE NAME" }
                      ],
                      "Includes": [
                        { "BaseUseCase": "BASE USE CASE NAME", "IncludedUseCase": "INCLUDED USE CASE NAME" }
                      ],
                      "Extends": [
                        { "BaseUseCase": "BASE USE CASE NAME", "ExtendedUseCase": "EXTENDED USE CASE NAME" }
                      ]
                    }
                  ]
                }
                ```
                """
                    +
                """
                ### EXAMPLE:
                INPUT:
                {
                  "Actors": [
                    { "Name": "Patient" },
                    { "Name": "Doctor" },
                    { "Name": "System Administrator" }
                  ],
                  "UseCases": [
                    { "Name": "Register" },
                    { "Name": "Schedule Appointment" },
                    { "Name": "Issue Prescription" },
                    { "Name": "Send Message" },
                    { "Name": "Receive Notification" }
                  ],
                  "Associations": [
                    { "Actor": "Patient", "UseCase": "Register" },
                    { "Actor": "Doctor", "UseCase": "Issue Prescription" },
                    { "Actor": "Doctor", "UseCase": "Send Message" }
                  ],
                  "Includes": [
                    { "BaseUseCase": "Register", "IncludedUseCase": "Enter Patient Information" }
                  ],
                  "Extends": [
                    { "BaseUseCase": "Send Message", "ExtendedUseCase": "Receive Notification" }
                  ]
                }               
                OUTPUT:
                ```json
                {
                  "Packages": [
                    {
                      "Name": "Patient Services",
                      "Actors": [
                        { "Name": "Patient" }
                      ],
                      "UseCases": [
                        { "Name": "Register" }
                      ],
                      "Associations": [
                        { "Actor": "Patient", "UseCase": "Register" }
                      ],
                      "Includes": [
                        { "BaseUseCase": "Register", "IncludedUseCase": "Enter Patient Information" }
                      ],
                      "Extends": []
                    },
                    {
                      "Name": "Clinical Operations",
                      "Actors": [
                        { "Name": "Doctor" }
                      ],
                      "UseCases": [
                        { "Name": "Issue Prescription" },
                        { "Name": "Send Message" },
                        { "Name": "Receive Notification" }
                      ],
                      "Associations": [
                        { "Actor": "Doctor", "UseCase": "Issue Prescription" },
                        { "Actor": "Doctor", "UseCase": "Send Message" }
                      ],
                      "Includes": [],
                      "Extends": [
                        { "BaseUseCase": "Send Message", "ExtendedUseCase": "Receive Notification" }
                      ]
                    }
                  ]
                }
                ```
                """;
            return prompt;
        }
    }
}
