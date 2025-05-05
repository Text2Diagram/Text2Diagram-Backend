using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;

namespace Text2Diagram_Backend.Authentication
{
    public class FirebaseTokenVerifier
    {
        public FirebaseTokenVerifier()
        {
            // Initialize Firebase Admin SDK
            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile("text2diagram-k21-firebase-adminsdk-fbsvc-007926e426.json")
            });
        }

        public async Task<FirebaseToken> VerifyIdTokenAsync(string idToken)
        {
            try
            {
                FirebaseToken decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);
                return decodedToken;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Token verification failed: {ex.Message}");
                throw new UnauthorizedAccessException("Invalid ID token.");
            }
        }
    }
} 