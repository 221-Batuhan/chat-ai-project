using Microsoft.AspNetCore.Mvc;
using ChatApi.Data;
using ChatApi.Models;
using System.Threading.Tasks;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ChatApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessagesController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IHttpClientFactory _httpFactory;
        private readonly AIConfig _aiConfig;

        public MessagesController(AppDbContext db, IHttpClientFactory httpFactory, AIConfig aiConfig)
        {
            _db = db;
            _httpFactory = httpFactory;
            _aiConfig = aiConfig;
        }

        [HttpGet]
        public async Task<IActionResult> GetMessages()
        {
            var list = await _db.Messages
                .AsNoTracking()
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
            return Ok(list);
        }

        [HttpPost]
        public async Task<IActionResult> PostMessage([FromBody] CreateMessageRequest request)
        {
            Console.WriteLine("POST /api/messages received");
            if (request == null || string.IsNullOrWhiteSpace(request.Text))
                return BadRequest("Mesaj boş olamaz.");

            var msg = new Message
            {
                Nickname = string.IsNullOrWhiteSpace(request.Nickname) ? "anon" : request.Nickname,
                Text = request.Text,
                CreatedAt = DateTime.UtcNow,
                Sentiment = null,       // nullable, artık 400 hatası yok
                SentimentScore = 0.0
            };

            _db.Messages.Add(msg);
            await _db.SaveChangesAsync();
            Console.WriteLine($"Message saved id={msg.Id} nickname={msg.Nickname}");

            var aiUrl = Environment.GetEnvironmentVariable("AI_SERVICE_URL")
                        ?? _aiConfig.ServiceUrl
                        ?? "https://your-hf-space-url/api/predict";

            Console.WriteLine($"AI URL resolved: {aiUrl}");
            try
            {
                var client = _httpFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(15);
                Console.WriteLine($"text resolved: {msg.Text}");
                var tryPayloads = new[]
                {
                    JsonSerializer.Serialize(new { inputs = msg.Text }),
                    JsonSerializer.Serialize(new { data = new[] { msg.Text } })
                };

                HttpResponseMessage? resp = null;
                string? respJson = null;

                // Build possible endpoints to try
                var urlsToTry = new System.Collections.Generic.List<string>();
                if (!string.IsNullOrWhiteSpace(aiUrl)) urlsToTry.Add(aiUrl.TrimEnd('/'));
                try
                {
                    var u = new Uri(urlsToTry[0]);
                    var root = $"{u.Scheme}://{u.Host}{(u.IsDefaultPort ? string.Empty : ":" + u.Port)}";
                    if (!urlsToTry[0].EndsWith("/run/predict")) urlsToTry.Add(root + "/run/predict");
                    if (!urlsToTry[0].EndsWith("/api/predict")) urlsToTry.Add(root + "/api/predict");
                }
                catch { }

                foreach (var url in urlsToTry.Distinct())
                {
                    Console.WriteLine($"AI call: url={url} messageId={msg.Id}");
                    foreach (var payload in tryPayloads)
                    {
                        var content = new StringContent(payload, Encoding.UTF8, "application/json");
                        try
                        {
                            Console.WriteLine($"AI call: trying payload {payload}");
                            resp = await client.PostAsync(url, content);
                            Console.WriteLine($"AI call: status={(int)resp.StatusCode} {resp.ReasonPhrase}");
                            if (resp.IsSuccessStatusCode)
                            {
                                respJson = await resp.Content.ReadAsStringAsync();
                                Console.WriteLine($"AI call: body length={respJson?.Length}");
                                if (!string.IsNullOrWhiteSpace(respJson)) break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("AI call try failed: " + ex.Message);
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(respJson)) break;
                }

                if (!string.IsNullOrWhiteSpace(respJson))
                {
                    using var doc = JsonDocument.Parse(respJson);
                    var root = doc.RootElement;

                    string? label = null;
                    double score = 0.0;

                    if (root.TryGetProperty("label", out var pLabel))
                    {
                        label = pLabel.GetString();
                        if (root.TryGetProperty("score", out var pScore)) score = pScore.GetDouble();
                    }
                    else if (root.TryGetProperty("data", out var pData) && pData.ValueKind == JsonValueKind.Array)
                    {
                        var first = pData[0];
                        if (first.TryGetProperty("label", out var fLabel)) label = fLabel.GetString();
                        if (first.TryGetProperty("score", out var fScore)) score = fScore.GetDouble();
                    }
                    else if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                    {
                        var first = root[0];
                        if (first.TryGetProperty("label", out var fLabel2)) label = fLabel2.GetString();
                        if (first.TryGetProperty("score", out var fScore2)) score = fScore2.GetDouble();
                    }
                    Console.WriteLine($"AI parsed: label={label} score={score}");
                    if (!string.IsNullOrWhiteSpace(label))
                    {
                        msg.Sentiment = label.ToLowerInvariant();
                        msg.SentimentScore = score;
                        _db.Messages.Update(msg);
                        await _db.SaveChangesAsync();
                        Console.WriteLine($"AI parsed: id={msg.Id} sentiment={msg.Sentiment} score={msg.SentimentScore}");
                    }
                    else
                    {
                        Console.WriteLine("AI parse: could not extract label/score from response");
                    }
                }
                else
                {
                    // Try Hugging Face Spaces (Gradio) two-step flow if URL looks like hf.space
                    try
                    {
                        var baseUri = new Uri(aiUrl);
                        var isHf = baseUri.Host.EndsWith(".hf.space", StringComparison.OrdinalIgnoreCase);
                        if (isHf)
                        {
                            var rootUrl = $"{baseUri.Scheme}://{baseUri.Host}{(baseUri.IsDefaultPort ? string.Empty : ":" + baseUri.Port)}";
                            var callUrl = rootUrl + "/gradio_api/call/predict";
                            Console.WriteLine($"AI HF call: POST {callUrl}");

                            var payload = JsonSerializer.Serialize(new { data = new[] { msg.Text } });
                            var content = new StringContent(payload, Encoding.UTF8, "application/json");
                            var respCall = await client.PostAsync(callUrl, content);
                            Console.WriteLine($"AI HF call status: {(int)respCall.StatusCode} {respCall.ReasonPhrase}");
                            if (respCall.IsSuccessStatusCode)
                            {
                                var callBody = await respCall.Content.ReadAsStringAsync();
                                Console.WriteLine($"AI HF call body: {callBody}");
                                string? eventId = null;
                                try
                                {
                                    using var callDoc = JsonDocument.Parse(callBody);
                                    var r = callDoc.RootElement;
                                    if (r.TryGetProperty("event_id", out var e1)) eventId = e1.GetString();
                                    else if (r.TryGetProperty("eventId", out var e2)) eventId = e2.GetString();
                                }
                                catch { }

                                if (!string.IsNullOrWhiteSpace(eventId))
                                {
                                    var getUrl = rootUrl + "/gradio_api/call/predict/" + eventId;
                                    Console.WriteLine($"AI HF result GET: {getUrl}");
                                    var respGet = await client.GetAsync(getUrl);
                                    Console.WriteLine($"AI HF result status: {(int)respGet.StatusCode} {respGet.ReasonPhrase}");
                                    var body = await respGet.Content.ReadAsStringAsync();
                                    Console.WriteLine($"AI HF result body len={body?.Length}");
                                    Console.WriteLine($"AI HF result body content: {body}");

                                    
                                    // Parse SSE format response
                                    string? label = null; double score = 0.0;
                                    bool parsed = false;
                                    
                                    // Try to extract JSON from SSE format
                                    var lines = body?.Split('\n') ?? Array.Empty<string>();
                                    foreach (var line in lines)
                                    {
                                        var trimmed = line.Trim();
                                        // Look for lines that start with "data: " and contain JSON
                                        if (trimmed.StartsWith("data: "))
                                        {
                                            var jsonPart = trimmed.Substring(6).Trim(); // Remove "data: " prefix
                                            if (jsonPart.StartsWith("[") && jsonPart.EndsWith("]"))
                                            {
                                                try
                                                {
                                                    using var rd = JsonDocument.Parse(jsonPart);
                                                    (label, score) = ExtractLabelScore(rd.RootElement);
                                                    parsed = !string.IsNullOrWhiteSpace(label);
                                                    if (parsed) break;
                                                }
                                                catch { }
                                            }
                                        }
                                    }
                                    
                                    // Fallback: try parsing the entire body as JSON
                                    if (!parsed)
                                    {
                                        try
                                        {
                                            using var rd = JsonDocument.Parse(body);
                                            (label, score) = ExtractLabelScore(rd.RootElement);
                                            parsed = !string.IsNullOrWhiteSpace(label);
                                        }
                                        catch { }
                                    }

                                    if (parsed)
                                    {
                                        msg.Sentiment = label!.ToLowerInvariant();
                                        msg.SentimentScore = score;
                                        _db.Messages.Update(msg);
                                        await _db.SaveChangesAsync();
                                        Console.WriteLine($"AI parsed (HF): id={msg.Id} sentiment={msg.Sentiment} score={msg.SentimentScore}");
                                    }
                                    else
                                    {
                                        Console.WriteLine("AI HF parse: could not extract label/score");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("AI HF call: event_id not found");
                                }
                            }
                        }
                    }
                    catch (Exception ex2)
                    {
                        Console.WriteLine("AI HF flow error: " + ex2.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("AI call error: " + ex.Message);
            }

            return CreatedAtAction(nameof(GetMessages), new { id = msg.Id }, msg);
        }
        private static (string? label, double score) ExtractLabelScore(JsonElement root)
        {
            Console.WriteLine("ExtractLabelScore");
            Console.WriteLine(root);            
            // JSON bir array ise ilk elemanı al
            if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
            {
                var first = root[0];

                string? label = first.TryGetProperty("label", out var labelProp)
                    ? labelProp.GetString()
                    : null;

                double score = first.TryGetProperty("score", out var scoreProp)
                    ? scoreProp.GetDouble()
                    : 0.0;

                return (label, score);
            }

            // Eğer array değilse veya boşsa varsayılan değer dön
            return (null, 0.0);
        }
    }
    
    public class CreateMessageRequest
    {
        public string? Nickname { get; set; }
        public string? Text { get; set; }
    }
}
