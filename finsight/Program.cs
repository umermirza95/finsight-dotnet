using finsight.Components;
using Finsight.Interface;
using Finsight.Repositories;
using Finsight.Service;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;


FirebaseApp.Create(new AppOptions()
{
    Credential = GoogleCredential.FromFile("serviceAccount.json")
});

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuthentication("firebase")
.AddScheme<AuthenticationSchemeOptions, FirebaseAuthenticationHandler>("firebase", options => { })
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/login";
});
builder.Services.AddAuthorizationBuilder()
.AddPolicy("RequireAuthenticatedUser", policy => policy.RequireAuthenticatedUser().AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme))
.AddPolicy("FirebaseIdToken", policy => policy.RequireAuthenticatedUser().AddAuthenticationSchemes("firebase"));

builder.Services.AddSingleton<FirestoreDb>((provider) =>
{
    return FirestoreDb.Create("finsight-f8c69");
});
builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(builder =>
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader());
    });
builder.Services.AddScoped<FSICategoryRepository, FSCategoryRepository>();
builder.Services.AddScoped<FSITransactionRepository, FSTransactionRepository>();
builder.Services.AddScoped<FSTransactionService>();
builder.Services.AddScoped<FSCurrencyConverter>();
builder.Services.AddSingleton<FSISecretsProvider, GCPSecretsProvider>();
builder.Services.AddHttpClient();
builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();


var app = builder.Build();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();
