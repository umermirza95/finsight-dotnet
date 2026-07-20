using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Finsight.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Finsight.Services
{
    public class SlackMessagingService : IMessagingService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SlackMessagingService> _logger;

        public SlackMessagingService(HttpClient httpClient, IConfiguration configuration, ILogger<SlackMessagingService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendMessageAsync(string message)
        {
            try
            {
                var webhookUrl = _configuration["Slack:WebhookUrl"];
                if (string.IsNullOrEmpty(webhookUrl) || webhookUrl == "{SLACK_WEBHOOK_URL}")
                {
                    _logger.LogWarning("Slack WebhookUrl is not configured properly. Message: {Message}", message);
                    return;
                }

                var payload = new { text = message };
                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(webhookUrl, content);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to send Slack message. Status: {StatusCode}, Error: {Error}", response.StatusCode, error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while sending Slack message.");
            }
        }
    }
}
