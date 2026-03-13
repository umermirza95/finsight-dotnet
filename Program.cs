using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Finsight.Interfaces;
using Finsight.Services;
using Finsight.Models;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.Authorization;
using Mscc.GenerativeAI;
using Mscc.GenerativeAI.Types;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;

var builder = WebApplication.CreateBuilder(args);

// Blazor must have this when using custom auth providers
builder.Services.AddAuthorizationCore();

// Register custom JWT auth provider
builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<JwtAuthenticationStateProvider>());

builder.Services.AddDbContextFactory<AppDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddIdentity<FSUser, IdentityRole>().AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();
builder.Services.AddScoped<IFSUserService, FSUserService>();
builder.Services.AddScoped<ICategoryService, FSCategoryService>();
builder.Services.AddScoped<ITransactionService, FSTransactionService>();
builder.Services.AddScoped<IExchangeRateService, FSExchangeRateService>();
builder.Services.AddScoped<IFileService, FSLinuxFile>();
builder.Services.AddScoped<IBudgetService, FSBudgetService>();
builder.Services.AddScoped<IExchangeRateService, FSExchangeRateService>();
builder.Services.AddHttpClient<IFXAPIService, WiseFXAPIService>();

builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
    logging.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("Finsight"));
    logging.AddOtlpExporter(options => 
    {
        options.Endpoint = new Uri("http://localhost:3100/otlp/v1/logs");
        options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
    });
});

builder.Services.AddSingleton(sp => 
{
    var apiKey = builder.Configuration["Gemini:ApiKey"] 
        ?? throw new InvalidOperationException("Gemini:ApiKey is missing in appsettings.json");
    return new GoogleAI(apiKey);
});
builder.Services.AddScoped(sp => 
{
    var googleAi = sp.GetRequiredService<GoogleAI>();
    return googleAi.GenerativeModel(Model.Gemini25Flash); 
});
builder.Services.AddScoped<ILLMService, FSGeminiService>();



builder.Services.AddAuthentication()
.AddJwtBearer("JwtBearer", options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured")))
    };
});

builder.Services.AddAuthorization();
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddCors(options => options.AddPolicy("CorsPolicy", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers()
.AddJsonOptions(option => { option.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); });

var app = builder.Build();
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Finsight App Started on VPS. Logs are being shipped to Loki.");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();
app.UseCookiePolicy();
app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
