using System.Text.Json;
using System.Text.Json.Serialization;
using Finsight.Commands;
using Finsight.Interfaces;
using Finsight.Models;
using Finsight.Enums;
using Mscc.GenerativeAI;
using Microsoft.EntityFrameworkCore;
using Mscc.GenerativeAI.Types;
using Microsoft.Extensions.Configuration;

namespace Finsight.Services
{
    public class FSGeminiService(
        IDbContextFactory<AppDbContext> dbFactory,
        ICategoryService categoryService,
        GenerativeModel model,
        IConfiguration configuration,
        ILogger<FSGeminiService> logger) : ILLMService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;
        private readonly ICategoryService _categoryService = categoryService;
        private readonly GenerativeModel _model = model;
        private readonly IConfiguration _configuration = configuration;
        private readonly ILogger<FSGeminiService> _logger = logger;



        public async Task<FSTransactionSuggestion?> CreateTransactionSuggestionAsync(FSTransactionEmail email)
        {
            _logger.LogInformation("Creating transaction suggestion from email {emailId}", email.Id);
            var categories = await _categoryService.GetCategoriesAsync(email.UserId);
            var categoryContext = categories.Select(c => new
            {
                c.Id,
                c.Name,
                Subs = c.SubCategories.Select(s => new { s.Id, s.Name })
            });
            var schemaTemplate = @"{
                                    ""Amount"": decimal,
                                    ""FSCategoryId"": ""Guid or null"",
                                    ""FSSubCategoryId"": ""Guid or null"",
                                    ""Mode"": ""card|cash|transfer|online"",
                                    ""Date"": ""yyyy-MM-dd"",
                                    ""FSCurrencyCode"": ""3-letter code"",
                                    ""Type"": ""income|expense"",
                                    ""Comment"": ""string"",
                                    ""TransactionExternalId"": ""string""
                                }";

            // 2. Inject that template into your prompt
            var prompt = $@"
                        Extract financial transaction data from the following email.
                        Return ONLY a JSON object that matches this specific schema:
                        {schemaTemplate}

                        Rules:
                        - Mapping: Use these Category/SubCategory IDs: {JsonSerializer.Serialize(categoryContext)}
                        - Nulls: Use null for any field you cannot confidently determine.
                        - Format: Return raw JSON only. No markdown formatting.

                        Email Body: {email.Html}
                        Current Date: {DateTime.Now:yyyy-MM-dd}";

            try
            {
                var response = await _model.GenerateContent(prompt, new GenerationConfig
                {
                    ResponseMimeType = "application/json"
                });

                _logger.LogInformation("Raw response from Gemini: {GeminiResponse} for email {emailId}", response.Text, email.Id);
               
                var cleanJson = response.Text?.Replace("```json", "").Replace("```", "").Trim();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter() }
                };

                var suggestion = JsonSerializer.Deserialize<FSTransactionSuggestion>(cleanJson ?? "", options);

                if (suggestion == null) return null;

                if (suggestion.Type != FSTransactionType.expense)
                {
                    _logger.LogInformation("Gemini response for email {emailId} is not an expense transaction. Ignoring.", email.Id);
                    return null;
                }

                suggestion.Id = Guid.NewGuid();
                suggestion.FSUserId = email.UserId;
                suggestion.UpdatedAt = DateTime.UtcNow;

                using var context = await _dbFactory.CreateDbContextAsync();
                context.FSTransactionSuggestions.Add(suggestion);
                await context.SaveChangesAsync();

                return suggestion;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to parse Gemini {response}", ex.InnerException != null ? ex.InnerException.Message : ex.Message);
                return null;
            }
        }
        public async Task<List<FSImportedTransaction>> ParseBankStatementAsync(Stream pdfStream, string userId)
        {
            

            var schemaTemplate = @"[
                {
                    ""Description"": ""string"",
                    ""Amount"": decimal,
                    ""Date"": ""yyyy-MM-dd"",
                    ""BankName"": ""string or null"",
                    ""FSCurrencyCode"": ""3-letter code"",
                    ""Type"": ""income|expense""
                }
            ]";

            var prompt = $@"
                Extract all financial transactions from the attached bank statement PDF.
                Return ONLY a JSON array of objects that match this specific schema:
                {schemaTemplate}

                Rules:
                - Type should be 'income' for deposits/credits, and 'expense' for withdrawals/debits.
                - Format: Return raw JSON only. No markdown formatting.
            ";

            try
            {
                using var memoryStream = new MemoryStream();
                await pdfStream.CopyToAsync(memoryStream);
                var pdfBytes = memoryStream.ToArray();

                var requestPayload = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new object[]
                            {
                                new { text = prompt },
                                new { inlineData = new { mimeType = "application/pdf", data = Convert.ToBase64String(pdfBytes) } }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        responseMimeType = "application/json"
                    }
                };

                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromMinutes(5); // 5 minutes timeout for large PDFs
                
                var apiKey = _configuration["Gemini:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    throw new InvalidOperationException("Gemini:ApiKey is missing in configuration.");
                }

                var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";
                var jsonPayload = JsonSerializer.Serialize(requestPayload);
                var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

                HttpResponseMessage? httpResponse = null;
                int maxRetries = 3;
                for (int i = 0; i < maxRetries; i++)
                {
                    httpResponse = await httpClient.PostAsync(url, content);
                    if (httpResponse.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        if (i == maxRetries - 1) break;
                        _logger.LogWarning("Gemini API rate limit hit (429). Retrying in 15 seconds... (Attempt {Attempt}/{Max})", i + 1, maxRetries);
                        await Task.Delay(TimeSpan.FromSeconds(15));
                        content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json"); // Recreate in case it was disposed
                        continue;
                    }
                    break;
                }
                httpResponse?.EnsureSuccessStatusCode();

                var responseString = await httpResponse.Content.ReadAsStringAsync();
                var geminiResponseDoc = JsonDocument.Parse(responseString);
                var responseText = geminiResponseDoc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                _logger.LogInformation("Raw response from Gemini for bank statement: {GeminiResponse}", responseText);

                var cleanJson = responseText?.Replace("```json", "").Replace("```", "").Trim();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter() }
                };

                var parsedDtos = JsonSerializer.Deserialize<List<FSImportedTransactionDto>>(cleanJson ?? "[]", options);
                
                if (parsedDtos == null || !parsedDtos.Any())
                {
                    return new List<FSImportedTransaction>();
                }

                using var context = await _dbFactory.CreateDbContextAsync();

                var finalTransactions = new List<FSImportedTransaction>();
                
                // Keep track of counts per date+amount to handle multiple identical transactions on the same day
                var localCounts = new Dictionary<string, int>();

                var allConstructedTxs = new List<FSImportedTransaction>();

                foreach (var dto in parsedDtos)
                {
                    var tx = new FSImportedTransaction
                    {
                        Id = "", // Set later
                        FSUserId = userId,
                        Description = dto.Description,
                        Amount = dto.Amount,
                        Date = dto.Date,
                        BankName = dto.BankName,
                        FSCurrencyCode = dto.FSCurrencyCode,
                        Type = dto.Type,
                        CreatedAt = DateTime.UtcNow
                    };
                    tx.IsDeleted = false;
                    
                    var baseIdKey = $"{tx.Date:yyyyMMdd}_{tx.Amount}";
                    if (!localCounts.ContainsKey(baseIdKey))
                    {
                        localCounts[baseIdKey] = 0;
                    }
                    var index = localCounts[baseIdKey];
                    localCounts[baseIdKey]++;
                    
                    // The ID is composite of date and amount, appending index if > 0
                    tx.Id = index == 0 ? baseIdKey : $"{baseIdKey}_{index}";

                    allConstructedTxs.Add(tx);
                }

                var newIds = allConstructedTxs.Select(t => t.Id).Distinct().ToList();
                var existingIds = await context.FSImportedTransactions
                    .Where(t => t.FSUserId == userId && newIds.Contains(t.Id))
                    .Select(t => t.Id)
                    .ToListAsync();
                
                var existingIdsSet = new HashSet<string>(existingIds);

                foreach (var tx in allConstructedTxs)
                {
                    if (!existingIdsSet.Contains(tx.Id))
                    {
                        finalTransactions.Add(tx);
                        context.FSImportedTransactions.Add(tx);
                    }
                }

                if (finalTransactions.Any())
                {
                    await context.SaveChangesAsync();
                }

                return finalTransactions;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to parse bank statement PDF: {error}", ex.InnerException != null ? ex.InnerException.Message : ex.Message);
                return new List<FSImportedTransaction>();
            }
        }
    }

    public class FSImportedTransactionDto
    {
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateOnly Date { get; set; }
        public string? BankName { get; set; }
        public string FSCurrencyCode { get; set; } = string.Empty;
        public FSTransactionType Type { get; set; }
    }
}