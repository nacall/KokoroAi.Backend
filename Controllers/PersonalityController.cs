using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KokoroAi.Backend.Data;
using KokoroAi.Backend.Models;
using KokoroAi.Backend.Models.DTOs;

namespace KokoroAi.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PersonalityController : ControllerBase
{
    private readonly KokoroDbContext _context;
    private static readonly Guid DefaultUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public PersonalityController(KokoroDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Obtiene los ajustes de voz y personalidad de Kokoro (Pantalla 3)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PersonalitySettingsDto>> GetPersonality()
    {
        var settings = await _context.PersonalitySettings
            .FirstOrDefaultAsync(p => p.UserId == DefaultUserId);

        if (settings == null)
        {
            return NotFound("Ajustes de personalidad no encontrados.");
        }

        string affectLabel;
        if (settings.AffectLevel >= 0.75f)
        {
            affectLabel = "Tsundere Afectuosa";
        }
        else if (settings.AffectLevel >= 0.45f)
        {
            affectLabel = "Cálida y Acompañante";
        }
        else
        {
            affectLabel = "Profesional y Enfocada";
        }

        return Ok(new PersonalitySettingsDto
        {
            AffectLevel = settings.AffectLevel,
            AffectLabel = affectLabel,
            VoiceTone = settings.VoiceTone,
            VoicePitch = settings.VoicePitch,
            AvailableVoiceTones = new List<string>
            {
                "Ami (Neutra)",
                "Hana (Suave)",
                "Rei (Enérgica)",
                "Kagami (Tsundere VIP)",
                "Yuki (Elegante VIP)"
            }
        });
    }

    /// <summary>
    /// Actualiza el slider de afecto y las características de voz de Kokoro (Pantalla 3)
    /// </summary>
    [HttpPut]
    public async Task<ActionResult<PersonalitySettingsDto>> UpdatePersonality([FromBody] UpdatePersonalityRequestDto request)
    {
        var settings = await _context.PersonalitySettings
            .FirstOrDefaultAsync(p => p.UserId == DefaultUserId);

        if (settings == null)
        {
            settings = new PersonalitySettings
            {
                UserId = DefaultUserId
            };
            await _context.PersonalitySettings.AddAsync(settings);
        }

        // Validar rango del slider de afecto (0.0 a 1.0)
        settings.AffectLevel = Math.Clamp(request.AffectLevel, 0.0f, 1.0f);
        settings.VoiceTone = string.IsNullOrWhiteSpace(request.VoiceTone) ? "Ami (Neutra)" : request.VoiceTone;
        settings.VoicePitch = Math.Clamp(request.VoicePitch, 0.5f, 1.5f);

        await _context.SaveChangesAsync();

        string affectLabel = settings.AffectLevel switch
        {
            >= 0.75f => "Tsundere Afectuosa",
            >= 0.45f => "Cálida y Acompañante",
            _ => "Profesional y Enfocada"
        };

        return Ok(new PersonalitySettingsDto
        {
            AffectLevel = settings.AffectLevel,
            AffectLabel = affectLabel,
            VoiceTone = settings.VoiceTone,
            VoicePitch = settings.VoicePitch
        });
    }
}
