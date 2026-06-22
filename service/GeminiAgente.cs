using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GeminiCLI.service
{
     public class GeminiAgente : IAgente
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly string _url;
 
        public string Nombre => "Gemini 2.5 Flash";
        public string Icono  => "◆";
        public bool EstaConfigurado => !string.IsNullOrWhiteSpace(_apiKey);
 
        public GeminiAgente(HttpClient http, string apiKey)
        {
            _http   = http;
            _apiKey = apiKey;
            _url    =
                $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";
        }
 
        public async Task<(bool exito, string respuesta)> PreguntarAsync(
            string mensaje,
            bool jsonMode = false)
        {
            try
            {
                object body = jsonMode
                    ? new
                    {
                        contents = new[]
                        {
                            new { role = "user", parts = new[] { new { text = mensaje } } }
                        },
                        generationConfig = new { responseMimeType = "application/json" }
                    }
                    : new
                    {
                        contents = new[]
                        {
                            new { role = "user", parts = new[] { new { text = mensaje } } }
                        }
                    };
 
                string json = JsonSerializer.Serialize(body);
 
                var response = await _http.PostAsync(
                    _url,
                    new StringContent(json, Encoding.UTF8, "application/json")
                );
 
                string raw = await response.Content.ReadAsStringAsync();
 
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    return (false, "ERROR_429: Límite de cuota de Gemini alcanzado.");
 
                using JsonDocument doc = JsonDocument.Parse(raw);
 
                if (doc.RootElement.TryGetProperty("error", out var error))
                {
                    string msg = error.GetProperty("message").GetString() ?? "";
                    bool esQuota = msg.Contains("429") ||
                                   msg.Contains("quota") ||
                                   msg.Contains("RESOURCE_EXHAUSTED");
                    return (false, esQuota
                        ? $"ERROR_429: {msg}"
                        : $"Error Gemini: {msg}");
                }
 
                if (!doc.RootElement.TryGetProperty("candidates", out var candidates))
                    return (false, "Gemini no devolvió respuesta.");
 
                string texto = candidates[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString() ?? "";
 
                return (true, texto);
            }
            catch (HttpRequestException ex)
            {
                return (false, $"Error de red con Gemini: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (false, $"Error inesperado Gemini: {ex.Message}");
            }
        }
 
        // Implementación explícita de la interfaz
        async Task<(bool exito, string respuesta)> IAgente.PreguntarAsync(string mensaje)
            => await PreguntarAsync(mensaje);
    }
}