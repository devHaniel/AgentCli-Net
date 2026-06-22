using DotNetEnv;
using GeminiCLI.model;
using GeminiCLI.service;


Env.Load();
 
// ─── CONFIG --key ─────────────────────────────────────────────────────────────
if (args.Length >= 3 && args[0] == "config")
{
    switch (args[1])
    {
        case "--key":
            ConfigService.GuardarApiKey(args[2]);
            Console.WriteLine("✓ Gemini API Key guardada.");
            return;
        case "--groq":
            ConfigService.GuardarGroqKey(args[2]);
            Console.WriteLine("✓ Groq API Key guardada.");
            return;
        case "--openai":
            ConfigService.GuardarOpenAIKey(args[2]);
            Console.WriteLine("✓ OpenAI API Key guardada.");
            return;
    }
}
 
// ─── CARGAR KEYS ──────────────────────────────────────────────────────────────
string? geminiKey = ConfigService.ObtenerApiKey();
string? groqKey   = ConfigService.ObtenerGroqKey();
string? openAIKey = ConfigService.ObtenerOpenAIKey();
 
// Al menos una key debe existir
if (string.IsNullOrWhiteSpace(geminiKey) &&
    string.IsNullOrWhiteSpace(groqKey)   &&
    string.IsNullOrWhiteSpace(openAIKey))
{
    MostrarError("No hay ninguna API Key configurada.");
    MostrarInfo("agentecli config --key    TU_GEMINI_KEY");
    MostrarInfo("agentecli config --groq   TU_GROQ_KEY");
    MostrarInfo("agentecli config --openai TU_OPENAI_KEY");
    return;
}
 
// ─── SERVICIOS BASE ───────────────────────────────────────────────────────────
var http             = new HttpClient();
var historialService = new HistorialService();
var contextoService  = new UserContextService();
 
// ─── AGENTES DISPONIBLES ──────────────────────────────────────────────────────
var agentes = new List<IAgente>();
 
if (!string.IsNullOrWhiteSpace(geminiKey))
    agentes.Add(new GeminiAgente(http, geminiKey));
 
if (!string.IsNullOrWhiteSpace(groqKey))
    agentes.Add(new GroqAgente(http, groqKey));
 
if (!string.IsNullOrWhiteSpace(openAIKey))
    agentes.Add(new OpenAIAgente(http, openAIKey));
 
// ─── PANTALLA PRINCIPAL ───────────────────────────────────────────────────────
Console.Clear();
MostrarBanner();
 
IAgente? agenteSeleccionado = null;
string   tituloSeleccionado = string.Empty;
bool     salir              = false;
 
MostrarMenuPrincipal(agentes);
 
while (!salir)
{
    Console.Write("  Opción > ");
    string? input = Console.ReadLine()?.Trim().ToLower();
 
    switch (input)
    {
        // ── Agentes ───────────────────────────────────────────────
        case "1" when agentes.Count >= 1:
        case "2" when agentes.Count >= 2:
        case "3" when agentes.Count >= 3:
            int idx = int.Parse(input!) - 1;
            agenteSeleccionado = agentes[idx];
            MostrarExito($"Agente seleccionado: {agenteSeleccionado.Nombre}");
            MostrarMenuSecundario();
            break;
 
        // ── Nuevo historial ───────────────────────────────────────
        case "n" when agenteSeleccionado != null:
            tituloSeleccionado = FlujoNuevoHistorial(historialService);
            if (!string.IsNullOrWhiteSpace(tituloSeleccionado)) salir = true;
            break;
 
        case "n":
            MostrarError("Primero selecciona un agente (1, 2 o 3).");
            break;
 
        // ── Historial existente ───────────────────────────────────
        case "h" when agenteSeleccionado != null:
            tituloSeleccionado = FlujoSeleccionarHistorial(historialService);
            if (!string.IsNullOrWhiteSpace(tituloSeleccionado)) salir = true;
            break;
 
        case "h":
            MostrarError("Primero selecciona un agente (1, 2 o 3).");
            break;
 
        // ── Salir ─────────────────────────────────────────────────
        case "exit":
        case "salir":
        case "e":
            MostrarDespedida();
            return;
 
        default:
            MostrarError("Opción no válida.");
            break;
    }
}
 
// ─── INICIAR CHAT ─────────────────────────────────────────────────────────────
historialService.Cargar(tituloSeleccionado);
 
var agenteService = new AgenteService(
    agenteSeleccionado!,
    historialService,
    contextoService
);
 
Console.Clear();
MostrarEncabezadoChat(tituloSeleccionado, agenteSeleccionado!);
 
// Mostrar mensajes previos una vez cargado el historial real
if (historialService.Historial.Mensajes != null &&
    historialService.Historial.Mensajes.Count > 0)
{
    MostrarHistorial(historialService.Historial);
}
 
while (true)
{
    Console.Write($"\n  Tú  ›  ");
    string? userInput = Console.ReadLine();
 
    if (userInput == null) continue;
 
    // ── Comando: cambiar agente ───────────────────────────────────
    if (userInput.StartsWith("/agente", StringComparison.OrdinalIgnoreCase))
    {
        agenteSeleccionado = FlujoSelectorAgente(agentes);
        if (agenteSeleccionado != null)
        {
            agenteService.CambiarAgente(agenteSeleccionado);
            MostrarEncabezadoChat(tituloSeleccionado, agenteSeleccionado);
        }
        continue;
    }
 
    if (userInput.Equals("exit",  StringComparison.OrdinalIgnoreCase) ||
        userInput.Equals("salir", StringComparison.OrdinalIgnoreCase))
    {
        MostrarDespedida();
        break;
    }
 
    if (string.IsNullOrWhiteSpace(userInput)) continue;
 
    Console.WriteLine();
    Console.Write($"  {agenteService.AgenteActual.Icono}   ›  ");
 
    var (exito, respuesta) = await agenteService.PreguntarAsync(userInput);
 
    if (!exito)
    {
        Console.WriteLine();
        MostrarError(respuesta);
        MostrarInfo("Escribe /agente para cambiar de agente.");
        continue;
    }
 
    // Mostrar respuesta con sangría en múltiples líneas
    string[] lineas = respuesta.Split('\n');
    for (int i = 0; i < lineas.Length; i++)
    {
        if (i == 0) Console.WriteLine(lineas[i]);
        else        Console.WriteLine("         " + lineas[i]);
    }
 
    MostrarSeparador();
}
 
// ═════════════════════════════════════════════════════════════════════════════
//   FLUJOS
// ═════════════════════════════════════════════════════════════════════════════
 
static IAgente? FlujoSelectorAgente(List<IAgente> agentes)
{
    Console.WriteLine();
    int ancho = 62;
 
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine($"  ┌{new string('─', ancho)}┐");
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine($"  │  {"CAMBIAR AGENTE",-60}│");
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine($"  ├{new string('─', ancho)}┤");
 
    for (int i = 0; i < agentes.Count; i++)
        EscribirOpcionAgente(i + 1, agentes[i]);
 
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine($"  └{new string('─', ancho)}┘");
    Console.ResetColor();
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.Write("  Selecciona › ");
    Console.ResetColor();
 
    string? sel = Console.ReadLine();
 
    if (!int.TryParse(sel, out int op) || op < 1 || op > agentes.Count)
    {
        MostrarError("Opción no válida.");
        return null;
    }
 
    MostrarExito($"Cambiado a: {agentes[op - 1].Nombre}");
    return agentes[op - 1];
}
 
static string FlujoNuevoHistorial(HistorialService hs)
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.Write("  Nombre del historial › ");
    Console.ResetColor();
 
    string titulo = Console.ReadLine()?.Trim() ?? "";
 
    if (string.IsNullOrWhiteSpace(titulo))
    {
        MostrarError("El nombre no puede estar vacío.");
        return string.Empty;
    }
 
    MostrarExito($"Historial '{titulo}' listo.");
    return titulo;
}
 
static string FlujoSeleccionarHistorial(HistorialService hs)
{
    var historiales = hs.ObtenerHistoriales();
 
    if (historiales.Count == 0)
    {
        MostrarError("No hay historiales. Usa [N] para crear uno nuevo.");
        return string.Empty;
    }
 
    int ancho = 62;
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine($"  ┌{new string('─', ancho)}┐");
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine($"  │  {"HISTORIALES GUARDADOS",-60}│");
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine($"  ├{new string('─', ancho)}┤");
 
    for (int i = 0; i < historiales.Count; i++)
    {
        // Conteo de mensajes como info extra
        int total = historiales[i].Mensajes?.Count ?? 0;
        string info = total == 1 ? "1 mensaje" : $"{total} mensajes";
 
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("  │  ");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write($"[{i + 1}]");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write($"  {historiales[i].Titulo,-44}");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($"{info,12}");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("│");
    }
 
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine($"  └{new string('─', ancho)}┘");
    Console.ResetColor();
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.Write("  Selecciona número › ");
    Console.ResetColor();
 
    if (!int.TryParse(Console.ReadLine(), out int op) ||
        op < 1 || op > historiales.Count)
    {
        MostrarError("Número fuera de rango.");
        return string.Empty;
    }
 
    var seleccionado = historiales[op - 1];
 
    MostrarExito($"Historial '{seleccionado.Titulo}' cargado.");
    return seleccionado.Titulo;
}
 
static void MostrarHistorial(Historial historial)
{
    int ancho = 62;
 
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine($"  ┌{new string('─', ancho)}┐");
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine($"  │  Conversación anterior: {historial.Titulo,-35}│");
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine($"  └{new string('─', ancho)}┘");
    Console.ResetColor();
    Console.WriteLine();
 
    foreach (var msg in historial.Mensajes)
    {
        // ── Tú ────────────────────────────────────────────────
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("  Tú  ›  ");
        Console.ForegroundColor = ConsoleColor.White;
        ImprimirConSangria(msg.Tu, "         ");
 
        Console.WriteLine();
 
        // ── IA ────────────────────────────────────────────────
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("  IA  ›  ");
        Console.ForegroundColor = ConsoleColor.Gray;
        ImprimirConSangria(msg.Ia, "         ");
 
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  " + new string('·', 60));
        Console.ResetColor();
    }
}
 
static void ImprimirConSangria(string texto, string sangria)
{
    string[] lineas = (texto ?? "").Split('\n');
    for (int i = 0; i < lineas.Length; i++)
    {
        if (i == 0) Console.WriteLine(lineas[i]);
        else        Console.WriteLine(sangria + lineas[i]);
    }
    Console.ResetColor();
}
 
// ═════════════════════════════════════════════════════════════════════════════
//   HELPERS UI
// ═════════════════════════════════════════════════════════════════════════════
 
static void MostrarBanner()
{
    int    ancho = 62;
    string linea = new string('═', ancho);
 
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"  ╔{linea}╗");
    Console.WriteLine($"  ║{"",62}║");
    Console.WriteLine("  ║  █████╗  ██████╗ ███████╗███╗  ██╗████████╗███████╗ ║");
    Console.WriteLine("  ║ ██╔══██╗██╔════╝ ██╔════╝████╗ ██║╚══██╔══╝██╔════╝ ║");
    Console.WriteLine("  ║ ███████║██║  ███╗█████╗  ██╔██╗██║   ██║   █████╗   ║");
    Console.WriteLine("  ║ ██╔══██║██║   ██║██╔══╝  ██║╚████║   ██║   ██╔══╝   ║");
    Console.WriteLine("  ║ ██║  ██║╚██████╔╝███████╗██║ ╚███║   ██║   ███████╗ ║");
    Console.WriteLine("  ║ ╚═╝  ╚═╝ ╚═════╝ ╚══════╝╚═╝  ╚══╝   ╚═╝   ╚══════╝ ║");
    Console.WriteLine($"  ║{"",62}║");
    Console.ForegroundColor = ConsoleColor.DarkCyan;
    Console.WriteLine("  ║          ┌──────────────────────────────────┐         ║");
    Console.WriteLine("  ║          │   C L I  —  N E T   v2.0.0      │         ║");
    Console.WriteLine("  ║          └──────────────────────────────────┘         ║");
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"  ║{"",62}║");
    Console.WriteLine($"  ╚{linea}╝");
    Console.ResetColor();
    Console.WriteLine();
}
 
static void MostrarMenuPrincipal(List<IAgente> agentes)
{
    int    ancho = 62;
    string sep   = new string('─', ancho);
 
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine($"  ┌{sep}┐");
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine($"  │  {"SELECCIONA UN AGENTE",-60}│");
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine($"  ├{sep}┤");
 
    for (int i = 0; i < agentes.Count; i++)
        EscribirOpcionAgente(i + 1, agentes[i]);
 
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine($"  ├{sep}┤");
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine($"  │  {"LUEGO ELIGE UNA ACCIÓN",-60}│");
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine($"  ├{sep}┤");
 
    EscribirOpcionMenu("N", "nuevo",     "Iniciar una nueva conversación");
    EscribirOpcionMenu("H", "historial", "Continuar una conversación anterior");
    EscribirOpcionMenu("E", "exit",      "Salir de la aplicación");
 
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine($"  └{sep}┘");
    Console.ResetColor();
    Console.WriteLine();
}
 
static void MostrarMenuSecundario()
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine("  ─── Ahora elige: [N] Nuevo  [H] Historial  [E] Salir ───");
    Console.ResetColor();
    Console.WriteLine();
}
 
static void EscribirOpcionAgente(int num, IAgente agente)
{
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.Write("  │  ");
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.Write($"[{num}]");
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write($" {agente.Icono} ");
    Console.ForegroundColor = ConsoleColor.White;
    Console.Write($"{agente.Nombre,-54}");
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine("│");
}
 
static void EscribirOpcionMenu(string tecla, string cmd, string desc)
{
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.Write("  │  ");
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.Write($"[{tecla}]");
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write($" {cmd,-12}");
    Console.ForegroundColor = ConsoleColor.Gray;
    Console.Write($"  {desc,-42}");
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine("│");
}
 
static void MostrarEncabezadoChat(string titulo, IAgente agente)
{
    int    ancho = 62;
    string linea = new string('═', ancho);
 
    Console.Clear();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"  ╔{linea}╗");
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine($"  ║  {agente.Icono} {agente.Nombre,-18}  │  {titulo,-36}║");
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"  ╠{linea}╣");
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine($"  ║  exit = salir  │  /agente = cambiar IA{"",-37}║");
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"  ╚{linea}╝");
    Console.ResetColor();
}
 
static void MostrarSeparador()
{
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine("  " + new string('·', 60));
    Console.ResetColor();
}
 
static void MostrarError(string msg)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"  ✗ {msg}");
    Console.ResetColor();
}
 
static void MostrarInfo(string msg)
{
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine($"  » {msg}");
    Console.ResetColor();
}
 
static void MostrarExito(string msg)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"  ✓ {msg}");
    Console.ResetColor();
}
 
static void MostrarDespedida()
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("  ╔══════════════════════════════════════════════════════════════╗");
    Console.WriteLine("  ║           ¡Hasta pronto! — AgenteCli v2.0.0                ║");
    Console.WriteLine("  ╚══════════════════════════════════════════════════════════════╝");
    Console.ResetColor();
    Console.WriteLine();
}