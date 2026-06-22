using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GeminiCLI.service
{
    public class GroqAgente : IAgente
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
 
        private const string URL   = "https://api.groq.com/openai/v1/chat/completions";
        private const string MODEL = "meta-llama/llama-4-scout-17b-16e-instruct";
 
        public string Nombre => "Llama 4 Scout (Groq)";
        public string Icono  => "▲";
        public bool EstaConfigurado => !string.IsNullOrWhiteSpace(_apiKey);
 
        public GroqAgente(HttpClient http, string apiKey)
        {
            _http   = http;
            _apiKey = apiKey;
        }
 
        public async Task<(bool exito, string respuesta)> PreguntarAsync(string mensaje)
        {
            try
            {
                var body = new
                {
                    model       = MODEL,
                    messages    = new[] { new { role = "user", content = mensaje } },
                    max_tokens  = 1024,
                    temperature = 0.7
                };
 
                string json = JsonSerializer.Serialize(body);
 
                using var request = new HttpRequestMessage(HttpMethod.Post, URL);
                request.Headers.Add("Authorization", $"Bearer {_apiKey}");
                request.Content =
                    new StringContent(json, Encoding.UTF8, "application/json");
 
                var response = await _http.SendAsync(request);
                string raw   = await response.Content.ReadAsStringAsync();
 
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    return (false, "ERROR_429: Límite de cuota de Groq alcanzado.");
 
                using JsonDocument doc = JsonDocument.Parse(raw);
 
                if (doc.RootElement.TryGetProperty("error", out var error))
                    return (false, $"Error Groq: {error.GetProperty("message").GetString()}");
 
                string texto = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "";
 
                return (true, texto);
            }
            catch (HttpRequestException ex)
            {
                return (false, $"Error de red con Groq: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (false, $"Error inesperado Groq: {ex.Message}");
            }
        }
    }
}