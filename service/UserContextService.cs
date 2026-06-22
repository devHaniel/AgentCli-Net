using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GeminiCLI.model;

namespace GeminiCLI.service
{
    public class UserContextService
    {
        private readonly string carpeta = "data";
        private readonly string ruta = Path.Combine("data", "context.json");

        public UserContext Contexto { get; private set; } = new();
        public UserContextService()
        {
            Cargar();
        }

        private void Cargar()
        {
            // Crear carpeta si no existe
            if (!Directory.Exists(carpeta))
            {
                Directory.CreateDirectory(carpeta);
            }


            // Crear archivo inicial si no existe
            if (!File.Exists(ruta))
            {
                Contexto = new UserContext();
                Guardar();
                return;
            }


            string json = File.ReadAllText(ruta);


            Contexto =
                JsonSerializer.Deserialize<UserContext>(json)
                ?? new UserContext();
        }

        public void Guardar()
        {

            string json =
                JsonSerializer.Serialize(
                    Contexto,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });


            File.WriteAllText(ruta, json);
        }

        public void Agregar(
    string categoria,
    string informacion_resumida,
    string informacion)
        {
            Console.WriteLine(
                $"Guardando: {categoria} - {informacion_resumida}"
            );
            bool existe = Contexto.Items.Any(x =>
                x.Categoria.Equals(categoria, StringComparison.OrdinalIgnoreCase)
                &&
                x.Informacion.Equals(informacion_resumida, StringComparison.OrdinalIgnoreCase)
            );


            if (existe)
                return;


            Contexto.Items.Add(new ContextItem
            {
                Categoria = categoria,
                Informacion = informacion
            });


            Guardar();
        }
    }
}