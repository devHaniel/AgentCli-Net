using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GeminiCLI.model;

namespace GeminiCLI.service
{
    /// <summary>
    /// Orquestador principal. Gestiona historial, memoria y
    /// delega las llamadas al IAgente activo.
    /// </summary>
    public class AgenteService
    {
        private IAgente _agente;
        private readonly HistorialService _historial;
        private readonly UserContextService _contexto;
        private readonly List<string> _historialReciente = new();
 
        private int _mensajesDesdeMemoria = 0;
        private const int LIMITE_MEMORIA  = 3;
 
        public IAgente AgenteActual => _agente;
 
        public AgenteService(
            IAgente agente,
            HistorialService historial,
            UserContextService contexto)
        {
            _agente   = agente;
            _historial = historial;
            _contexto  = contexto;
        }
 
        // ── Cambiar agente en caliente ────────────────────────────
        public void CambiarAgente(IAgente nuevoAgente)
        {
            _agente = nuevoAgente;
        }
 
        // ─────────────────────────────────────────────────────────
        //   ENVIAR MENSAJE
        // ─────────────────────────────────────────────────────────
 
        public async Task<(bool exito, string respuesta)> PreguntarAsync(string prompt)
        {
            try
            {
                string contexto  = ObtenerContexto();
                string historial = _historial.ObtenerConversacion();
 
                string mensajeFinal = $@"
                Memoria del usuario:
                {contexto}
                
                Conversación anterior:
                {historial}
                
                Nuevo mensaje:
                {prompt}
                ";
                _historialReciente.Add($"Usuario: {prompt}");
 
                var (exito, respuesta) = await _agente.PreguntarAsync(mensajeFinal);
 
                if (!exito)
                    return (false, respuesta);
 
                _historialReciente.Add($"Asistente: {respuesta}");
                _mensajesDesdeMemoria++;
 
                _historial.Nuevo(prompt, respuesta);
 
                if (_mensajesDesdeMemoria >= LIMITE_MEMORIA)
                {
                    await ActualizarMemoriaAsync();
                    _mensajesDesdeMemoria = 0;
                    _historialReciente.Clear();
                }
 
                return (true, respuesta);
            }
            catch (Exception ex)
            {
                return (false, $"Error inesperado: {ex.Message}");
            }
        }
 
        // ─────────────────────────────────────────────────────────
        //   MEMORIA
        // ─────────────────────────────────────────────────────────
 
        private async Task ActualizarMemoriaAsync()
        {
            try
            {
                string contextoActual       = ObtenerContexto();
                string conversacionReciente =
                    string.Join("\n", _historialReciente);
 
                string prompt = $@"
                Analiza la conversación reciente.
                Extrae únicamente información permanente sobre el usuario.
                
                Categorías válidas:
                - Carrera
                - Profesión
                - Tecnologías
                - Objetivos
                - Gustos
                
                Devuelve EXCLUSIVAMENTE JSON válido sin markdown.
                
                Ejemplo:
                [
                {{
                    ""Categoria"": ""Tecnologías"",
                    ""Informacion"": ""C#"",
                    ""Razon"": ""Lo mencionó varias veces""
                }}
                ]
                
                Si no hay información nueva: []
                
                Contexto actual:
                {contextoActual}
                
                Conversación reciente:
                {conversacionReciente}
                ";
                var (exito, resultado) = await _agente.PreguntarAsync(prompt);
 
                if (!exito) return;
 
                // Limpiar posible markdown
                resultado = resultado
                    .Replace("```json", "")
                    .Replace("```", "")
                    .Trim();
 
                int inicio = resultado.IndexOf('[');
                int fin    = resultado.LastIndexOf(']');
 
                if (inicio < 0 || fin < 0) return;
 
                resultado = resultado[inicio..(fin + 1)];
 
                var memorias =
                    JsonSerializer.Deserialize<List<MemoriaItem>>(resultado);
 
                if (memorias == null) return;
 
                foreach (var memoria in memorias)
                {
                    bool existe = _contexto.Contexto.Items.Any(x =>
                        x.Categoria.Equals(
                            memoria.Categoria,
                            StringComparison.OrdinalIgnoreCase)
                        &&
                        x.Informacion.Equals(
                            memoria.Informacion,
                            StringComparison.OrdinalIgnoreCase));
 
                    if (!existe)
                        _contexto.Agregar(
                            memoria.Categoria,
                            memoria.Informacion,
                            memoria.Razon);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error actualizando memoria: {ex.Message}");
            }
        }
 
        private string ObtenerContexto()
        {
            try
            {
                return string.Join(
                    "\n",
                    _contexto.Contexto.Items.Select(x =>
                        $"{x.Categoria}: {x.Informacion}")
                );
            }
            catch { return ""; }
        }
    }
}