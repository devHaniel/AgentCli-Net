using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GeminiCLI.service
{
      public interface IAgente
    {
        /// <summary>Nombre visible en el menú (ej: "Gemini 2.5 Flash")</summary>
        string Nombre { get; }
 
        /// <summary>Símbolo de 1 carácter para el encabezado del chat</summary>
        string Icono { get; }
 
        /// <summary>Verifica si el agente tiene credenciales configuradas</summary>
        bool EstaConfigurado { get; }
 
        /// <summary>Envía un mensaje y retorna la respuesta como string</summary>
        Task<(bool exito, string respuesta)> PreguntarAsync(string mensaje);
    }
}