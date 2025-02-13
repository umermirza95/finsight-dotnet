using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;

namespace finsight.Components.Pages
{
    public partial class Login
    {
        [SupplyParameterFromForm]
        private LoginModel loginModel { get; set; } = new();
        private void HandleLogin()
        {
            System.Diagnostics.Debug.Write("called");
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