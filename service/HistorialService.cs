using System.Text.Json;
using GeminiCLI.model;

namespace GeminiCLI.service
{
    public class HistorialService
    {

        private readonly string carpeta = "data";
        private readonly string ruta =
            Path.Combine("data", "historial.json");


        public Historial Historial { get; private set; } = new();



        public List<Historial> ObtenerHistoriales()
        {
            if (!File.Exists(ruta))
                return new();


            return JsonSerializer.Deserialize<List<Historial>>
            (
                File.ReadAllText(ruta)
            )
            ?? new();
        }




        public void Cargar(string titulo)
        {

            Directory.CreateDirectory(carpeta);


            var historiales = ObtenerHistoriales();


            Historial =
                historiales.FirstOrDefault(x =>
                    x.Titulo.Equals(
                        titulo,
                        StringComparison.OrdinalIgnoreCase
                    ))
                ??
                new Historial
                {
                    Titulo = titulo,
                    Mensajes = new List<Mensajes>()
                };


            // Si es nuevo, lo guarda
            if (!historiales.Any(x =>
                x.Titulo.Equals(
                    titulo,
                    StringComparison.OrdinalIgnoreCase
                )))
            {
                Guardar();
            }

        }

        public string ObtenerConversacion()
        {
            return string.Join(
                "\n",
                Historial.Mensajes.Select(x =>
                    $"Usuario: {x.Tu}\nIA: {x.Ia}"
                )
            );
        }

        public void Guardar()
        {

            var historiales =
                ObtenerHistoriales();



            historiales.RemoveAll(x =>
                x.Titulo.Equals(
                    Historial.Titulo,
                    StringComparison.OrdinalIgnoreCase
                ));



            historiales.Add(Historial);



            File.WriteAllText(
                ruta,
                JsonSerializer.Serialize(
                    historiales,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    })
            );

        }




        public void Nuevo(
            string tu,
            string ia)
        {

            Historial.Mensajes.Add(
                new Mensajes
                {
                    Tu = tu,
                    Ia = ia
                }
            );


            Guardar();

        }
    }
}