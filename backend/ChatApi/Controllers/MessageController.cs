using Microsoft.AspNetCore.Mvc;
using ChatApi.Data;
using ChatApi.Models;
using System.Threading.Tasks;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using Microsoft.EntityFrameworkCore;

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
        public async Task<IActionResult> PostMessage([FromBody] Message incoming)
        {
            if (incoming == null || string.IsNullOrWhiteSpace(incoming.Text))
                return BadRequest("Mesaj boş olamaz.");

            var msg = new Message
            {
                Nickname = string.IsNullOrWhiteSpace(incoming.Nickname) ? "anon" : incoming.Nickname,
                Text = incoming.Text,
                CreatedAt = DateTime.UtcNow,
                Sentiment = null,       // nullable, artık 400 hatası yok
                SentimentScore = 0.0
            };

            _db.Messages.Add(msg);
            await _db.SaveChangesAsync();

            var aiUrl = Environment.GetEnvironmentVariable("AI_SERVICE_URL")
                        ?? _aiConfig.ServiceUrl
                        ?? "https://your-hf-space-url/api/predict/";

            try
            {
                var client = _httpFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(15);

                var tryPayloads = new[]
                {
                    JsonSerializer.Serialize(new { inputs = msg.Text }),
                    JsonSerializer.Serialize(new { data = new[] { msg.Text } })
                };

                HttpResponseMessage? resp = null;
                string? respJson = null;

                Console.WriteLine($"AI call: url={aiUrl} messageId={msg.Id}");
                foreach (var payload in tryPayloads)
                {
                    var content = new StringContent(payload, Encoding.UTF8, "application/json");
                    try
                    {
                        Console.WriteLine($"AI call: trying payload {payload}");
                        resp = await client.PostAsync(aiUrl, content);
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
            }
            catch (Exception ex)
            {
                Console.WriteLine("AI call error: " + ex.Message);
            }

            return CreatedAtAction(nameof(GetMessages), new { id = msg.Id }, msg);
        }
    }
}
