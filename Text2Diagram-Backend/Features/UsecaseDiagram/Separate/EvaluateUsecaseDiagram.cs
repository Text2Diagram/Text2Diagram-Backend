using Text2Diagram_Backend.Features.Flowchart;

namespace Text2Diagram_Backend.Features.UsecaseDiagram.Separate
{
    public class EvaluateUsecaseDiagram
    {
        public static string PromptEvaluateUsecaseDiagram(string input, string diagramjson)
        {
            string prompt = $"""
            You are an expert software modeling AI specializing in validating **Use Case Diagrams** based on natural language requirements.
            ---
            ### TASK:
            Your job is to analyze the **correctness**, **completeness**, and **logical consistency** of a Use Case Diagram represented as structured JSON, based on a given user requirement description.

            You must evaluate whether the diagram:
            1. **Faithfully represents** the key actors and use cases described in the user input.
            2. **Includes valid associations** between actors and use cases.
            3. **Properly applies** `<<include>>` and `<<extend>>` relationships where appropriate.
            4. **Avoids invalid or unnecessary actors/use cases** that are not mentioned or implied in the user input.
            5. **Groups use cases meaningfully** under appropriate packages (if applicable).
            6. Uses correct and consistent naming for actors and use cases.
            """ + """
            ### OUTPUT:
            Provide your structured evaluation in the following format:
            ```json
            {
              ""IsAccurate"": true | false,
              ""MissingElements"": [""list any missing actors, use cases, or relationships""],
              ""IncorrectElements"": [""list any wrongly added or misrepresented elements""],
              ""Suggestions"": [""suggest specific corrections or improvements""],
              ""Commentary"": ""a brief explanation (2–5 sentences) summarizing your evaluation""
            }
            ```
            """
            + $"""
            {Prompts.LanguageRules}
            INPUT DESCRIPTION (User Requirement): {input}
            ER DIAGRAM (Json Result)" + {diagramjson}
            """;
            return prompt;
        }
    }
}
