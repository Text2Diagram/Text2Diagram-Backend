using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

namespace Text2Diagram_Backend.LLMGeminiService;

public class GoogleAuthHandler : DelegatingHandler
{
    private readonly GoogleServiceAccountTokenProvider _tokenProvider;

    public GoogleAuthHandler(IOptionsMonitor<GeminiOptions> optionsMonitor, string configName)
    {
        var options = optionsMonitor.Get(configName);
        _tokenProvider = new GoogleServiceAccountTokenProvider(options.ServiceAccountJsonPath, "https://www.googleapis.com/auth/cloud-platform");
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var accessToken = await _tokenProvider.GetAccessTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await base.SendAsync(request, cancellationToken);
    }
}