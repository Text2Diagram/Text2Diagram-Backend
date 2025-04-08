using Grpc.Net.Client;
using Text2Diagram_Backend.Abstractions;
using Text2Diagram_Backend.Protos;

namespace Text2Diagram_Backend.Common;

public class MermaidSyntaxValidator : ISyntaxValidator
{
    public async Task<DiagramValidationResult> ValidateAsync(string code)
    {
        var channel = GrpcChannel.ForAddress("http://localhost:50051");
        var client = new MermaidService.MermaidServiceClient(channel);

        var request = new MermaidRequest { Syntax = code };
        var response = await client.ValidateSyntaxAsync(request);

        return response.Valid ? DiagramValidationResult.Valid()
            : DiagramValidationResult.Invalid(response.Message);
    }
}
