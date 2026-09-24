using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KokoroAi.Backend.Data;
using KokoroAi.Backend.Models.DTOs;
using KokoroAi.Backend.Services;

namespace KokoroAi.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly KokoroDbContext _context;
    private readonly IGeminiChatService _geminiService;
    private static readonly Guid DefaultUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public ChatController(KokoroDbContext context, IGeminiChatService geminiService)
    {
        _context = context;
        _geminiService = geminiService;
    }

    /// <summary>
    /// Envía un mensaje a Kokoro y recibe una respuesta inteligente analizada con Gemini AI
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ChatResponseDto>> SendMessage([FromBody] ChatRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("El mensaje no puede estar vacío.");
        }

        var user = await _context.Users
            .Include(u => u.PersonalitySettings)
            .Include(u => u.UserOutfits)
                .ThenInclude(uo => uo.Outfit)
            .FirstOrDefaultAsync(u => u.Id == DefaultUserId);

        if (user == null)
        {
            return NotFound("Usuario no encontrado.");
        }

        var settings = user.PersonalitySettings ?? new Models.PersonalitySettings
        {
            UserId = user.Id,
            AffectLevel = 0.5f,
            VoiceTone = "Hana (Suave)",
            VoicePitch = 1.0f
        };

        var equippedOutfit = user.UserOutfits.FirstOrDefault(uo => uo.IsEquipped)?.Outfit;

        // Llamada a Gemini AI integrando análisis de intenciones y modulación de personalidad
        var response = await _geminiService.GenerateCompanionResponseAsync(
            user,
            settings,
            equippedOutfit,
            request.Message.Trim());

        return Ok(response);
    }
}
