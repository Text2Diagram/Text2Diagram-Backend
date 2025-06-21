using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace Text2Diagram_Backend.Authentication
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class FirebaseAuthenticationAttribute : TypeFilterAttribute
    {
        public FirebaseAuthenticationAttribute() : base(typeof(FirebaseAuthenticationFilter))
        {
        }

        private class FirebaseAuthenticationFilter : IAsyncActionFilter
        {
            private readonly FirebaseTokenVerifier _tokenVerifier;

            public FirebaseAuthenticationFilter(FirebaseTokenVerifier tokenVerifier)
            {
                _tokenVerifier = tokenVerifier;
            }

            public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
            {
                var token = context.HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

                if (string.IsNullOrEmpty(token))
                {
                    context.Result = new UnauthorizedResult();
                    return;
                }

                try
                {
                    var decodedToken = await _tokenVerifier.VerifyIdTokenAsync(token);

                    // Add user information to HttpContext
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, decodedToken.Uid),
                    };

                    // Add additional claims if available
                    if (decodedToken.Claims.TryGetValue("email", out var email))
                    {
                        claims.Add(new Claim(ClaimTypes.Email, email.ToString()));
                    }

                    if (decodedToken.Claims.TryGetValue("name", out var name))
                    {
                        claims.Add(new Claim(ClaimTypes.Name, name.ToString()));
                    }

                    if (decodedToken.Claims.TryGetValue("picture", out var picture))
                    {
                        claims.Add(new Claim("picture", picture.ToString()));
                    }

                    if (decodedToken.Claims.TryGetValue("role", out var role))
                    {
                        claims.Add(new Claim(ClaimTypes.Role, role.ToString()));
                    }

                    context.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "firebase"));

                    await next();
                }
                catch
                {
                    context.Result = new UnauthorizedResult();
                }
            }
        }
    }
} 