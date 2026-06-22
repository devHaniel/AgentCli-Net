using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GeminiCLI.model
{
    public class Historial
    {
        public string Titulo {get; set;} = "";
         public List<Mensajes> Mensajes { get; set; } = new();
    }
}