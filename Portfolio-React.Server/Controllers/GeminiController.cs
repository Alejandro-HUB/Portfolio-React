using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Portfolio_React.Server.Models.Gemini;
using System.Text;

namespace Portfolio_React.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GeminiController : ControllerBase
    {
        private readonly ILogger<GeminiController> _logger;
        private readonly IHttpClientFactory _clientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string _API_KEY = "";
        private const double _pricePerMillionInputTokens = 7.0;
        private const double _pricePerMillionOutputTokens = 21.0;
        private const double _maxSpendingLimit = 2.0;
        private double _accumulatedCost = 0.0;
        private double _estimatedInputCost = 0.0;
        private double _estimatedOutputCost = 0.0;
        private const string ACCUMULATED_COST_KEY = "AccumulatedCost";
        private const string SESSION_ID = "SessionId";

        public GeminiController(ILogger<GeminiController> logger, IHttpClientFactory clientFactory,
            IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _clientFactory = clientFactory;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpPost(Name = "GetGeminiResponse")]
        public async Task<IActionResult> GetGeminiResponse([FromBody] GeminiRequest geminiRequest)
        {
            try
            {
                var sessionId = GetOrCreateSessionId();

                var conversationHistory = GetConversationHistory(sessionId);

                string apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent?key={_API_KEY}";

                var client = _clientFactory.CreateClient();

                var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
                if (conversationHistory != null && conversationHistory.Any())
                {
                    geminiRequest.contents.InsertRange(0, conversationHistory[sessionId].Select(msg => new MessagePart { role = msg.role, parts = new List<Part> { new Part { text = msg.text } } }));
                }
                request.Content = new StringContent(JsonConvert.SerializeObject(geminiRequest), Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, $"API Error: {response.StatusCode}");
                }

                var responseData = await response.Content.ReadAsStringAsync();

                var parsedResponse = JsonConvert.DeserializeObject<GeminiResponse>(responseData);

                var bestCandidate = parsedResponse?.candidates?.OrderByDescending(c => c.safetyRatings?.Max(r => r.probability)).FirstOrDefault();

                if (bestCandidate?.content != null)
                {
                    var bestText = bestCandidate.content?.parts?.FirstOrDefault()?.text;

                    UpdateConversationHistory(sessionId, geminiRequest, bestCandidate, conversationHistory);

                    var estimatedTokenUsage = CalculateTokenUsage(geminiRequest, bestCandidate);

                    UpdateAccumulatedCost(estimatedTokenUsage);

                    if (_accumulatedCost > _maxSpendingLimit)
                    {
                        return StatusCode(402, "Spending limit exceeded. Please upgrade your plan or try later.");
                    }

                    return Ok(new { text = bestText });
                }
                else
                {
                    return StatusCode(500, "Error: No suitable response found.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching data from Gemini API");
                return StatusCode(500, "Error: Unable to reach Gemini API. Check your network and API key.");
            }
        }

        private string GetOrCreateSessionId()
        {
            if (!_httpContextAccessor.HttpContext.Items.ContainsKey(SESSION_ID))
            {
                var sessionId = Guid.NewGuid().ToString();
                _httpContextAccessor.HttpContext.Items[SESSION_ID] = sessionId;
            }

            return (string)_httpContextAccessor.HttpContext.Items[SESSION_ID];
        }

        private Dictionary<string, List<GeminiMessage>> GetConversationHistory(string sessionId)
        {
            if (!_httpContextAccessor.HttpContext.Items.ContainsKey(sessionId))
            {
                _httpContextAccessor.HttpContext.Items[sessionId] = new Dictionary<string, List<GeminiMessage>>();
            }

            return (Dictionary<string, List<GeminiMessage>>)_httpContextAccessor.HttpContext.Items[sessionId];
        }

        private void UpdateConversationHistory(string sessionId, GeminiRequest geminiRequest, Candidate bestCandidate, Dictionary<string, List<GeminiMessage>> conversationHistory)
        {
            if (!conversationHistory.ContainsKey(sessionId))
            {
                conversationHistory[sessionId] = new List<GeminiMessage>();
            }

            geminiRequest.contents.InsertRange(0, conversationHistory[sessionId].Select(msg => new MessagePart { role = msg.role, parts = new List<Part> { new Part { text = msg.text } } }));

            conversationHistory[sessionId].Add(new GeminiMessage("user", geminiRequest.contents.Last().parts[0].text));
            conversationHistory[sessionId].Add(new GeminiMessage("gemini", bestCandidate.content.parts[0].text));
            _httpContextAccessor.HttpContext.Items[sessionId] = conversationHistory;
        }

        private int CalculateTokenUsage(GeminiRequest geminiRequest, Candidate bestCandidate)
        {
            var estimatedTokenUsage = geminiRequest.contents.Sum(c => c.parts.Sum(p => p.text.Length)) +
                                      bestCandidate.content.parts.Sum(p => p.text.Length);

            // Very approximate, need to refine this logic
            int estimatedInputTokenCount = estimatedTokenUsage;
            int estimatedOutputTokenCount = estimatedTokenUsage / 2; // Assume response is roughly half the prompt length

            _estimatedInputCost = (estimatedInputTokenCount / 1000000) * _pricePerMillionInputTokens;
            _estimatedOutputCost = (estimatedOutputTokenCount / 1000000) * _pricePerMillionOutputTokens;

            return estimatedTokenUsage;
        }

        private void UpdateAccumulatedCost(int estimatedTokenUsage)
        {
            _accumulatedCost = _httpContextAccessor.HttpContext.Items.ContainsKey(ACCUMULATED_COST_KEY)
                ? (double)_httpContextAccessor.HttpContext.Items[ACCUMULATED_COST_KEY]
                : 0.0;

            _accumulatedCost += (_estimatedInputCost + _estimatedOutputCost);
            _httpContextAccessor.HttpContext.Items[ACCUMULATED_COST_KEY] = _accumulatedCost;
        }
    }
}
