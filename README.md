# AgenteCLI — NET

```
 █████╗  ██████╗ ███████╗███╗  ██╗████████╗███████╗
██╔══██╗██╔════╝ ██╔════╝████╗ ██║╚══██╔══╝██╔════╝
███████║██║  ███╗█████╗  ██╔██╗██║   ██║   █████╗
██╔══██║██║   ██║██╔══╝  ██║╚████║   ██║   ██╔══╝
██║  ██║╚██████╔╝███████╗██║ ╚███║   ██║   ███████╗
╚═╝  ╚═╝ ╚═════╝ ╚══════╝╚═╝  ╚══╝   ╚═╝   ╚══════╝
```

CLI de chat con IA para terminal, construido en **.NET 10**. Soporta múltiples agentes (Gemini, Groq, OpenAI), historial de conversaciones persistente y memoria de usuario entre sesiones.

---

## Características

- **Múltiples agentes** — Gemini 2.5 Flash, Llama 4 Scout (Groq) y GPT-4o Mini (OpenAI)
- **Selector de agente** en el menú principal, cambiable también en medio del chat con `/agente`
- **Historial persistente** — guarda y restaura conversaciones anteriores con visualización completa
- **Memoria de usuario** — extrae información relevante del usuario entre sesiones (tecnologías, objetivos, gustos)
- **Manejo de errores** — si un agente falla, muestra el error y permite cambiar sin salir
- **Instalable como tool global** de .NET

---

## Requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Al menos una API Key de los proveedores soportados

---

## Instalación

### Desde el código fuente

```bash
git clone https://github.com/tu-usuario/agentecli-net.git
cd agentecli-net

dotnet pack -c Release

dotnet tool install --global --add-source ./bin/Release agentecli-net
```

### Desinstalar

```bash
dotnet tool uninstall --global agentecli-net
```

### Actualizar

```bash
dotnet pack -c Release
dotnet tool update --global --add-source ./bin/Release agentecli-net
```

---

## Configuración de API Keys

Configura los proveedores que quieras usar. Al menos uno es requerido.

```bash
# Gemini (Google AI Studio — gratis con límites)
agentecli-net config --key TU_GEMINI_KEY

# Groq (Llama 4 Scout — gratis)
agentecli-net config --groq TU_GROQ_KEY

# OpenAI (GPT-4o Mini)
agentecli-net config --openai TU_OPENAI_KEY
```

Las keys se guardan en `~/.agentecli/` de forma local.

### Obtener API Keys

| Proveedor | Enlace | Tier gratuito |
|-----------|--------|---------------|
| Gemini | [ai.google.dev](https://ai.google.dev) | ✓ 15 RPM / 1,500 RPD |
| Groq | [console.groq.com](https://console.groq.com) | ✓ 30 RPM / 14,400 RPD |
| OpenAI | [platform.openai.com](https://platform.openai.com) | $5 crédito inicial |

---

## Uso

```bash
agentecli-net
```

### Flujo del menú

```
1. Selecciona un agente    →  [1] Gemini  [2] Groq  [3] OpenAI
2. Elige una acción        →  [N] Nueva conversación  [H] Historial
3. Chatea
```

### Comandos dentro del chat

| Comando | Descripción |
|---------|-------------|
| `exit` | Salir de la aplicación |
| `/agente` | Cambiar de agente sin salir del chat |

---

## Estructura del proyecto

```
AgenteCLI/
├── Program.cs
└── service/
    ├── IAgente.cs           # Contrato base para todos los agentes
    ├── AgenteService.cs     # Orquestador (memoria + historial)
    ├── GeminiAgente.cs      # Agente Gemini 2.5 Flash
    ├── GroqAgente.cs        # Agente Llama 4 Scout (Groq)
    ├── OpenAIAgente.cs      # Agente GPT-4o Mini (OpenAI)
    ├── ConfigService.cs     # Gestión de API Keys
    ├── HistorialService.cs  # Persistencia de conversaciones
    └── UserContextService.cs # Memoria de usuario
```

---

## Añadir un nuevo agente

Implementa la interfaz `IAgente`:

```csharp
public class MiAgente : IAgente
{
    public string Nombre => "Mi Modelo";
    public string Icono  => "★";
    public bool EstaConfigurado => !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<(bool exito, string respuesta)> PreguntarAsync(string mensaje)
    {
        // Tu lógica de llamada a la API
    }
}
```

Luego agrégalo a la lista en `Program.cs`:

```csharp
if (!string.IsNullOrWhiteSpace(miKey))
    agentes.Add(new MiAgente(http, miKey));
```

---

## Autor

**Haniel** — [@devHaniel](https://github.com/devHaniel)
