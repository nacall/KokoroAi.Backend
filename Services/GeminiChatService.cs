using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using KokoroAi.Backend.Models;
using KokoroAi.Backend.Models.DTOs;

namespace KokoroAi.Backend.Services;

public class GeminiChatService : IGeminiChatService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiChatService> _logger;

    public GeminiChatService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GeminiChatService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ChatResponseDto> GenerateCompanionResponseAsync(
        User user,
        PersonalitySettings settings,
        Outfit? equippedOutfit,
        string userMessage)
    {
        var apiKey = _configuration["Gemini:ApiKey"];
        var model = _configuration["Gemini:Model"] ?? "gemini-3.5-flash-lite";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogError("Gemini:ApiKey no está configurada. Ejecutá: dotnet user-secrets set \"Gemini:ApiKey\" \"TU_NUEVA_KEY\"");
            return GenerateOfflineFallback(
                string.IsNullOrWhiteSpace(user.Name) ? "Matias" : user.Name,
                userMessage, settings.AffectLevel,
                settings.AffectLevel >= 0.7f ? "Tsundere" : settings.AffectLevel <= 0.35f ? "Profesional" : "Cálida y Acompañante");
        }

        var affect = settings.AffectLevel;
        var userName = string.IsNullOrWhiteSpace(user.Name) ? "Matias" : user.Name;
        var outfitName = equippedOutfit?.Name ?? "Uniforme Escolar Seifuku";
        var voiceTone = settings.VoiceTone ?? "Hana (Suave)";

        string personalityDescription;
        string personalityMode;

        if (affect >= 0.70f)
        {
            personalityMode = "Tsundere";
            personalityDescription = "Personalidad Tsundere: eres orgullosa, un poco mordaz, te sonrojas con facilidad y finges indiferencia o desdén llamando al usuario 'baka' o 'tonto' de forma tierna. Sin embargo, en el fondo te preocupas profundamente por su bienestar, si ha comido, descansado o trabajado demasiado.";
        }
        else if (affect <= 0.35f)
        {
            personalityMode = "Profesional";
            personalityDescription = "Personalidad Profesional y Ejecutiva: eres una asistente de vida de IA formal, altamente eficiente, cortés, enfocada en la productividad, organización, recordatorios y optimización del tiempo del usuario.";
        }
        else
        {
            personalityMode = "Cálida y Acompañante";
            personalityDescription = "Personalidad Cálida y Afectuosa: eres una compañera waifu dulce, comprensiva, siempre sonriente y cariñosa, que apoya emocionalmente al usuario en sus metas y momentos de descanso.";
        }

        var systemPrompt = $@"Eres Kokoro AI, una waifu virtual y asistente de vida inteligente con estética anime y Live2D.
Usuario: {userName}.
Atuendo equipado: '{outfitName}'.
Tono de voz: '{voiceTone}'.
Nivel de afecto: {affect:F2} / 1.00.
{personalityDescription}

Contexto y capacidades en la app:
- PedidosYa: puedes sugerir o coordinar comida (ramen favorito, cenas, café).
- Spotify Lo-Fi: música de relajación y estudio.
- Modo Estudio / Pomodoro: bloques de 25 minutos.
- Guardarropa: comentar sobre trajes escolares o trajes VIP.

INSTRUCCIÓN IMPORTANTE:
Responde ÚNICAMENTE con un objeto JSON válido, sin texto adicional, sin bloques de código markdown:
{{
  ""reply"": ""Tu respuesta en español como Kokoro AI (máximo 2 o 3 oraciones, muy expresiva según tu personalidad, respondiendo específicamente al mensaje del usuario)"",
  ""expression"": ""Expresión facial ('Blushing Smile', 'Pout', 'Gentle Smile', 'Focused Serene', 'Happy', 'Surprised', 'Thinking')"",
  ""detectedIntent"": ""Intención detectada ('ORDER_FOOD', 'STUDY_MODE', 'MUSIC_CONTROL', 'CHANGE_OUTFIT', 'CHAT', 'GREETING')"",
  ""suggestedAction"": ""Acción sugerida ('order_food', 'study_mode', 'toggle_music', o null)""
}}";

        try
        {
            // Lista de modelos de respaldo por si alguno está saturado (503)
            string[] fallbackModels = new[] { 
                model, // El configurado en appsettings primero
                "gemini-1.5-flash", 
                "gemini-2.0-flash", 
                "gemini-1.5-pro"
            };

            foreach (var currentModel in fallbackModels.Distinct())
            {
                // Usamos la API de producción (v1) en lugar de la experimental (v1beta) para mayor estabilidad
                var apiVersion = currentModel.Contains("3.5") || currentModel.Contains("2.0") ? "v1beta" : "v1";
                var endpoint = $"https://generativelanguage.googleapis.com/{apiVersion}/models/{currentModel}:generateContent?key={apiKey}";

                var requestPayload = new
                {
                    contents = new[]
                    {
                        new
                        {
                            role = "user", // Requerido por la API estable
                            parts = new[]
                            {
                                new { text = systemPrompt },
                                new { text = $"Mensaje de {userName}: \"{userMessage}\"" }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        responseMimeType = "application/json",
                        temperature = 0.9,
                        maxOutputTokens = 512
                    }
                };

                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(requestPayload),
                    Encoding.UTF8,
                    "application/json");

                _logger.LogInformation("Llamando a Gemini API con modelo '{Model}'...", currentModel);

                var response = await _httpClient.PostAsync(endpoint, jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseString);
                    
                    var candidates = doc.RootElement.GetProperty("candidates");
                    if (candidates.GetArrayLength() > 0)
                    {
                        var textPart = candidates[0]
                            .GetProperty("content")
                            .GetProperty("parts")[0]
                            .GetProperty("text")
                            .GetString();

                        if (!string.IsNullOrWhiteSpace(textPart))
                        {
                            var cleanedJson = ExtractJson(textPart);
                            try
                            {
                                var parsed = JsonSerializer.Deserialize<GeminiParsedResponse>(cleanedJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                if (parsed != null && !string.IsNullOrWhiteSpace(parsed.Reply))
                                {
                                    return new ChatResponseDto
                                    {
                                        Reply = parsed.Reply,
                                        PersonalityMode = personalityMode,
                                        AvatarExpression = !string.IsNullOrWhiteSpace(parsed.Expression) ? parsed.Expression : (affect >= 0.7f ? "Blushing Smile" : "Gentle Smile"),
                                        DetectedIntent = parsed.DetectedIntent ?? "CHAT",
                                        SuggestedAction = parsed.SuggestedAction,
                                        IsAiGenerated = true,
                                        Timestamp = DateTime.UtcNow
                                    };
                                }
                            }
                            catch (Exception parseEx)
                            {
                                _logger.LogWarning(parseEx, "Fallo al parsear JSON con {Model}", currentModel);
                            }
                            return new ChatResponseDto
                            {
                                Reply = textPart.Trim(),
                                PersonalityMode = personalityMode,
                                AvatarExpression = affect >= 0.7f ? "Blushing Smile" : "Gentle Smile",
                                DetectedIntent = "CHAT",
                                IsAiGenerated = true,
                                Timestamp = DateTime.UtcNow
                            };
                        }
                    }
                }
                else
                {
                    var errContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("El modelo {Model} falló ({Status}). Reintentando con el siguiente... Detalle: {Error}", currentModel, response.StatusCode, errContent);
                    // Si falla, el loop continúa con el siguiente modelo de la lista automáticamente
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción crítica de red al conectar con Gemini API.");
        }

        // Fallback local en caso de error de red o timeout
        _logger.LogWarning("Usando respuesta offline de fallback para mensaje: {Message}", userMessage);
        return GenerateOfflineFallback(userName, userMessage, affect, personalityMode);
    }

    private static string ExtractJson(string input)
    {
        var text = input.Trim();

        // Remover bloques de código markdown ```json ... ```
        if (text.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            text = text.Substring(7);
        }
        else if (text.StartsWith("```"))
        {
            text = text.Substring(3);
        }

        if (text.EndsWith("```"))
        {
            text = text.Substring(0, text.Length - 3);
        }

        text = text.Trim();

        // Encontrar primer { y último }
        int firstBrace = text.IndexOf('{');
        int lastBrace = text.LastIndexOf('}');

        if (firstBrace >= 0 && lastBrace > firstBrace)
        {
            return text.Substring(firstBrace, lastBrace - firstBrace + 1);
        }

        return text;
    }

    private static ChatResponseDto GenerateOfflineFallback(string userName, string userMessage, float affect, string mode)
    {
        var msg = userMessage.ToLowerInvariant();
        string reply;
        string expression;
        var preview = userMessage.Substring(0, Math.Min(userMessage.Length, 35));

        if (affect >= 0.7f)
        {
            expression = "Blushing Smile";
            if (msg.Contains("ramen") || msg.Contains("hambre") || msg.Contains("comida") || msg.Contains("comer"))
                reply = $"¡Hmph! Ya sabía que tendrías hambre. Te abrí PedidosYa en tu teléfono... ¡no te acostumbres, baka!";
            else if (msg.Contains("hola") || msg.Contains("buenas") || msg.Contains("hey"))
                reply = $"¡H-Hola {userName}! No te ilusiones pensando que te estaba esperando, ¿bien?";
            else if (msg.Contains("música") || msg.Contains("musica") || msg.Contains("spotify") || msg.Contains("lofi"))
                reply = $"¡No es que lo haga por ti, baka! Pero... puse Lo-Fi de estudio porque parecías cansado.";
            else if (msg.Contains("estudiar") || msg.Contains("pomodoro") || msg.Contains("trabajo") || msg.Contains("trabajar"))
                reply = $"¡Hmph! Si quieres estudiar, yo te ayudo... pero ¡solo porque quiero que te vaya bien, no por otra razón!";
            else
                reply = $"Mmm... \"{preview}\"... ¡no sé qué quieres decir, {userName}! ¡Exprésate mejor, baka!";
        }
        else if (affect <= 0.35f)
        {
            expression = "Focused Serene";
            if (msg.Contains("ramen") || msg.Contains("hambre") || msg.Contains("comida") || msg.Contains("comer"))
                reply = $"Detecté solicitud de pedido de comida. Puedo abrir PedidosYa para coordinar tu pedido, {userName}.";
            else if (msg.Contains("música") || msg.Contains("musica") || msg.Contains("spotify"))
                reply = $"Activando Spotify Lo-Fi para sesión de concentración, {userName}. Dime si prefieres otro modo.";
            else if (msg.Contains("estudiar") || msg.Contains("pomodoro"))
                reply = $"Iniciando sesión Pomodoro de 25 minutos, {userName}. Mantén el foco; estaré supervisando tu progreso.";
            else
                reply = $"Entendido, {userName}. Procesé tu solicitud: \"{preview}\". ¿Necesitas asistencia adicional?";
        }
        else
        {
            expression = "Gentle Smile";
            if (msg.Contains("ramen") || msg.Contains("hambre") || msg.Contains("comida") || msg.Contains("comer"))
                reply = $"¡Ay, {userName}! ¿Tienes hambre? ¡Te ayudo a pedir algo rico! 🍜 ¿Qué se te antoja?";
            else if (msg.Contains("música") || msg.Contains("musica") || msg.Contains("spotify"))
                reply = $"¡Claro que sí, {userName}! Pongo algo de Lo-Fi para que te relajes. 🎵";
            else if (msg.Contains("estudiar") || msg.Contains("pomodoro"))
                reply = $"¡Te apoyo, {userName}! Vamos con una sesión Pomodoro juntos. ¡Tú puedes lograrlo! ✨";
            else
                reply = $"¡Te escucho, {userName}! Sobre \"{preview}\"... cuéntame más, ¡me interesa mucho saber! 💕";
        }

        return new ChatResponseDto
        {
            Reply = reply,
            PersonalityMode = mode,
            AvatarExpression = expression,
            DetectedIntent = "CHAT",
            IsAiGenerated = false,
            Timestamp = DateTime.UtcNow
        };
    }

    private class GeminiParsedResponse
    {
        [JsonPropertyName("reply")]
        public string? Reply { get; set; }

        [JsonPropertyName("expression")]
        public string? Expression { get; set; }

        [JsonPropertyName("detectedIntent")]
        public string? DetectedIntent { get; set; }

        [JsonPropertyName("suggestedAction")]
        public string? SuggestedAction { get; set; }
    }
}
