using System.Security.Claims;
using System.Text.Encodings.Web;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

public class FirebaseAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public FirebaseAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder, new SystemClock())
    {
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

        if (string.IsNullOrEmpty(token))
        {
            return AuthenticateResult.NoResult();
        }

        try
        {
            var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, decodedToken.Uid)
            };

            var identity = new ClaimsIdentity(claims, "firebase");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "firebase");

            return AuthenticateResult.Success(ticket);
        }
        catch
        {
            return AuthenticateResult.Fail("Invalid Firebase token");
        }
    }
}