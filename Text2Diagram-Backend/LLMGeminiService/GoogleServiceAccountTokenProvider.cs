using Google.Apis.Auth.OAuth2;

namespace Text2Diagram_Backend.LLMGeminiService;

public class GoogleServiceAccountTokenProvider
{
    private readonly GoogleCredential _credential;
    private readonly string _audience;

    public GoogleServiceAccountTokenProvider(string serviceAccountJsonPath, string audience)
    {
        if (string.IsNullOrEmpty(serviceAccountJsonPath))
            throw new ArgumentNullException(nameof(serviceAccountJsonPath));
        if (!File.Exists(serviceAccountJsonPath))
            throw new FileNotFoundException("Service account JSON file not found.", serviceAccountJsonPath);
        if (string.IsNullOrEmpty(audience))
            throw new ArgumentNullException(nameof(audience));

        _credential = GoogleCredential.FromFile(serviceAccountJsonPath)
            .CreateScoped("https://www.googleapis.com/auth/cloud-platform");
        _audience = audience;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (_credential.UnderlyingCredential is ServiceAccountCredential sac)
        {
            var token = await sac.GetAccessTokenForRequestAsync(_audience, cancellationToken);
            return token ?? throw new InvalidOperationException("Failed to obtain access token.");
        }

        throw new InvalidOperationException("Credential is not a ServiceAccountCredential.");
    }
}