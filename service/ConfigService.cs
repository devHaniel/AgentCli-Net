using System.Text.Json;

namespace GeminiCLI.service
{
    public static class ConfigService
    {
        private static readonly string _dir =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".agentecli"
            );
 
        private static string Ruta(string archivo) =>
            Path.Combine(_dir, archivo);
 
        private static void EnsureDir()
        {
            if (!Directory.Exists(_dir))
                Directory.CreateDirectory(_dir);
        }
 
        // ── Gemini ────────────────────────────────────────────────
        public static void GuardarApiKey(string key)
        {
            EnsureDir();
            File.WriteAllText(Ruta("gemini.key"), key.Trim());
        }
 
        public static string? ObtenerApiKey()
        {
            string ruta = Ruta("gemini.key");
            if (!File.Exists(ruta)) return null;
            string val = File.ReadAllText(ruta).Trim();
            return string.IsNullOrWhiteSpace(val) ? null : val;
        }
 
        // ── Groq ──────────────────────────────────────────────────
        public static void GuardarGroqKey(string key)
        {
            EnsureDir();
            File.WriteAllText(Ruta("groq.key"), key.Trim());
        }
 
        public static string? ObtenerGroqKey()
        {
            // Prioridad: archivo → variable de entorno
            string ruta = Ruta("groq.key");
            if (File.Exists(ruta))
            {
                string val = File.ReadAllText(ruta).Trim();
                if (!string.IsNullOrWhiteSpace(val)) return val;
            }
            return Environment.GetEnvironmentVariable("GROQ_API_KEY");
        }
 
        // ── OpenAI ────────────────────────────────────────────────
        public static void GuardarOpenAIKey(string key)
        {
            EnsureDir();
            File.WriteAllText(Ruta("openai.key"), key.Trim());
        }
 
        public static string? ObtenerOpenAIKey()
        {
            string ruta = Ruta("openai.key");
            if (File.Exists(ruta))
            {
                string val = File.ReadAllText(ruta).Trim();
                if (!string.IsNullOrWhiteSpace(val)) return val;
            }
            return Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        }
    }
}