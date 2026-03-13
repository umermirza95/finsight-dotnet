using System.Text.Json;
using System.Text.Json.Serialization;
using Finsight.Commands;
using Finsight.Interfaces;
using Finsight.Models;
using Finsight.Enums;
using Mscc.GenerativeAI;
using Microsoft.EntityFrameworkCore;
using Mscc.GenerativeAI.Types;

namespace Finsight.Services
{
    public class FSGeminiService(
        IDbContextFactory<AppDbContext> dbFactory,
        ICategoryService categoryService,
        GenerativeModel model,
        ILogger<FSGeminiService> logger) : ILLMService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;
        private readonly ICategoryService _categoryService = categoryService;
        private readonly GenerativeModel _model = model;
        private readonly ILogger<FSGeminiService> _logger = logger;

        public async Task<FSTransactionSuggestion?> CreateTransactionSuggestionAsync(string email, string userId)
        {
            _logger.LogInformation("Creating transaction suggestion for user {UserId}", userId);
            var categories = await _categoryService.GetCategoriesAsync(userId);
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

                        Email Body: {email}
                        Current Date: {DateTime.Now:yyyy-MM-dd}";

            _logger.LogInformation("Prompt for Gemini: {Prompt}", prompt);

            try
            {
                var response = await _model.GenerateContent(prompt, new GenerationConfig
                {
                    ResponseMimeType = "application/json"
                });

                _logger.LogInformation("Raw response from Gemini: {GeminiResponse}", response.Text);
                if (string.IsNullOrEmpty(response.Text))
                {
                    _logger.LogWarning("Received empty response from Gemini for user {UserId}", userId);
                    return null;
                }
                var cleanJson = response.Text.Replace("```json", "").Replace("```", "").Trim();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter() }
                };

                var suggestion = JsonSerializer.Deserialize<FSTransactionSuggestion>(cleanJson, options);

                if (suggestion == null) return null;

                if (suggestion.Type != FSTransactionType.expense)
                {
                    return null;
                }

                suggestion.Id = Guid.NewGuid();
                suggestion.FSUserId = userId;
                suggestion.UpdatedAt = DateTime.UtcNow;

                using var context = await _dbFactory.CreateDbContextAsync();
                context.FSTransactionSuggestions.Add(suggestion);
                await context.SaveChangesAsync();

                return suggestion;
            }
            catch (JsonException ex)
            {
                _logger.LogError("Failed to parse Gemini {response}", ex.Message);
                return null;
            }
        }
    }
}