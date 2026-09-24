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
        var apiKey = _configuration["Gemini:ApiKey"] ?? "AIzaSyBZXVlcldQdJkQguzw-cWOB0b0G4PtVrRg";
        var model = _configuration["Gemini:Model"] ?? "gemini-3.6-flash";
        
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

        var systemPrompt = $@"
Eres Kokoro AI, una waifu virtual y asistente de vida inteligente con estética anime y Live2D.
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

INSTRUCCIÓN:
Responde como Kokoro AI analizando la solicitud del usuario en formato JSON:
{{
  ""reply"": ""Tu respuesta en español como Kokoro AI (máximo 2 o 3 oraciones, muy expresiva según tu personalidad)"",
  ""expression"": ""Expresión facial ('Blushing Smile', 'Pout', 'Gentle Smile', 'Focused Serene', 'Happy', 'Surprised', 'Thinking')"",
  ""detectedIntent"": ""Intención detectada ('ORDER_FOOD', 'STUDY_MODE', 'MUSIC_CONTROL', 'CHANGE_OUTFIT', 'CHAT', 'GREETING')"",
  ""suggestedAction"": ""Acción ('order_food', 'study_mode', 'toggle_music', o null)""
}}";

        try
        {
            var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

            var requestPayload = new
            {
                contents = new[]
                {
                    new
                    {
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
                    temperature = 0.8,
                    maxOutputTokens = 1500
                }
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(requestPayload),
                Encoding.UTF8,
                "application/json");

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
                        // Intentar parsear JSON limpio o extraer entre llaves { ... }
                        var cleanedJson = ExtractJson(textPart);
                        
                        try
                        {
                            var parsed = JsonSerializer.Deserialize<GeminiParsedResponse>(cleanedJson, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                            if (parsed != null && !string.IsNullOrWhiteSpace(parsed.Reply))
                            {
                                return new ChatResponseDto
                                {
                                    Reply = parsed.Reply,
                                    PersonalityMode = personalityMode,
                                    AvatarExpression = !string.IsNullOrWhiteSpace(parsed.Expression) 
                                        ? parsed.Expression 
                                        : (affect >= 0.7f ? "Blushing Smile" : "Gentle Smile"),
                                    DetectedIntent = parsed.DetectedIntent ?? "CHAT",
                                    SuggestedAction = parsed.SuggestedAction,
                                    IsAiGenerated = true,
                                    Timestamp = DateTime.UtcNow
                                };
                            }
                        }
                        catch (Exception parseEx)
                        {
                            _logger.LogWarning(parseEx, "No se pudo deserializar JSON estructurado de Gemini, usando texto sin procesar.");
                        }

                        // Si falló el deserializado JSON, usamos el texto directo de Gemini
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
                _logger.LogWarning("Gemini API falló con status {StatusCode}: {Error}", response.StatusCode, errContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al conectar con Gemini API. Activando fallback offline.");
        }

        // Fallback local en caso de error de red o timeout
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

        if (affect >= 0.7f)
        {
            expression = "Blushing Smile";
            if (msg.Contains("ramen") || msg.Contains("hambre") || msg.Contains("comida"))
                reply = $"¡Hmph! Ya sabía que tendrías hambre. Te abrí PedidosYa en tu teléfono... ¡no te acostumbres, baka!";
            else if (msg.Contains("hola"))
                reply = $"¡H-Hola {userName}! No te ilusiones pensando que te estaba esperando, ¿bien?";
            else
                reply = $"Mmm... lo que digas, {userName}. Pero cuenta conmigo si me necesitas.";
        }
        else if (affect <= 0.35f)
        {
            expression = "Focused Serene";
            reply = $"Entendido, {userName}. Analicé tu mensaje y he registrado la solicitud en tu asistente de productividad.";
        }
        else
        {
            expression = "Gentle Smile";
            reply = $"¡Te escucho {userName}! Me alegra que charlemos. ¿Quieres que preparemos algo de música o un café?";
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
