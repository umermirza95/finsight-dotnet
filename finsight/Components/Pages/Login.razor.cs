using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace finsight.Components.Pages
{
    public partial class Login
    {
        [SupplyParameterFromForm]
        private LoginModel loginModel { get; set; } = new();
        [Inject]
        private IHttpContextAccessor HttpContextAccessor { get; set; } = default!;

        [SupplyParameterFromQuery]
        private string? ReturnUrl { get; set; }

        private async Task HandleLogin()
        {
            string firebaseApiKey = await SecretsProvider.GetSecretAsync("FIREBASE_API_KEY");
            var httpClient = HttpClientFactory.CreateClient();
            var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={firebaseApiKey}";
            var payload = new
            {
                email = loginModel.Email,
                password = loginModel.Password,
                returnSecureToken = true
            };
            var response = await httpClient.PostAsJsonAsync(url, payload);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Invalid username or password");
            }
            var result = await response.Content.ReadFromJsonAsync<FirebaseAuthResponse>();
            string userId = result?.LocalId ?? throw new Exception("Failed to acquire user Id");
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties { IsPersistent = true };
            await HttpContextAccessor.HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties
            );
            Navigation.NavigateTo("/test");
        }

        private class FirebaseAuthResponse
        {
            public string LocalId { get; set; } = "";
        }

        private class LoginModel
        {
            [Required, EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required, MinLength(6)]
            public string Password { get; set; } = string.Empty;
        }
    }
}