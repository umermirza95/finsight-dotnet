using Finsight.Interface;
using Finsight.Repositories;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

FirebaseApp.Create(new AppOptions()
{
    Credential = GoogleCredential.FromFile("serviceAccount.json")
});

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuthentication("firebase").AddScheme<AuthenticationSchemeOptions, FirebaseAuthenticationHandler>("firebase", options => { });
builder.Services.AddSingleton<FirestoreDb>((provider) =>
{
    return FirestoreDb.Create("finsight-f8c69");
});
builder.Services.AddScoped<FSICategoryRepository, FSCategoryRepository>();
builder.Services.AddScoped<FSITransactionRepository, FSTransactionRepository>();
builder.Services.AddControllers().AddNewtonsoftJson();

var app = builder.Build();
app.MapControllers();
app.Run();
