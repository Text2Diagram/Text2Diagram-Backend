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
                - a name that reflects the functionality area (e.g. "User_Management", "Payments"),
                - a list of Actors,
                - a list of UseCases,
                - Associations between the two,
                - and Include/Extend relationships related to the UseCases in that package.

                {input}
                """

                + """
                ### INSTRUCTIONS:
                - A package should group related use cases and their associated actors.
                - All names (package name, actor name, use case name) must use **Pascal_Snake_Case**, meaning words are separated by `_` and each word is **capitalized** (e.g., `Register_For_Course`).
                - Include/Extend relationships should stay inside the package if both base and target are in the same area; otherwise, keep them in the most relevant package.
                - Try to minimize duplication across packages.
                - Package names should be descriptive and relevant and follow the Pascal_Snake_Case format. (e.g., “Appointment_Management”, “Authentication”).

                ### FORMAT:
                Return the result as valid JSON like below:
                ```json
                {
                  "Packages": [
                    {
                      "Name": "BOUNDARY_NAME",
                      "Actors": [
                        { "Name": "ACTOR_NAME_1" },
                        { "Name": "ACTOR_NAME_2" }
                      ],
                      "UseCases": [
                        { "Name": "USE_CASE_NAME_1" },
                        { "Name": "USE_CASE_NAME_2" }
                      ],
                      "Associations": [
                        { "Actor": "ACTOR_NAME", "UseCase": "USE_CASE_NAME" }
                      ],
                      "Includes": [
                        { "BaseUseCase": "BASE_USE_CASE_NAME", "IncludedUseCase": "INCLUDED_USE_CASE_NAME" }
                      ],
                      "Extends": [
                        { "BaseUseCase": "BASE_USE_CASE_NAME", "ExtendedUseCase": "EXTENDED_USE_CASE_NAME" }
                      ]
                    }
                  ]
                }
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
                      "Name": "Patient_Services",
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
                        { "BaseUseCase": "Register", "IncludedUseCase": "Enter_Patient_Information" }
                      ],
                      "Extends": []
                    },
                    {
                      "Name": "Clinical_Operations",
                      "Actors": [
                        { "Name": "Doctor" }
                      ],
                      "UseCases": [
                        { "Name": "Issue_Prescription" },
                        { "Name": "Send_Message" },
                        { "Name": "Receive_Notification" }
                      ],
                      "Associations": [
                        { "Actor": "Doctor", "UseCase": "Issue_Prescription" },
                        { "Actor": "Doctor", "UseCase": "Send_Message" }
                      ],
                      "Includes": [],
                      "Extends": [
                        { "BaseUseCase": "Send_Message", "ExtendedUseCase": "Receive_Notification" }
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
