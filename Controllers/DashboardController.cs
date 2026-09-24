using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KokoroAi.Backend.Data;
using KokoroAi.Backend.Models.DTOs;

namespace KokoroAi.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly KokoroDbContext _context;
    private static readonly Guid DefaultUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public DashboardController(KokoroDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Obtiene el estado consolidado de la Pantalla 1 (Home & Dashboard Contextual)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<DashboardResponseDto>> GetDashboard()
    {
        var user = await _context.Users
            .Include(u => u.PersonalitySettings)
            .Include(u => u.UserOutfits)
                .ThenInclude(uo => uo.Outfit)
            .FirstOrDefaultAsync(u => u.Id == DefaultUserId);

        if (user == null)
        {
            return NotFound("Usuario no encontrado.");
        }

        var equippedOutfit = user.UserOutfits.FirstOrDefault(uo => uo.IsEquipped)?.Outfit;

        // Generación dinámica del mensaje contextual según la hora del día y nivel de afecto
        var now = DateTime.Now;
        string greetingTime;
        string suggestedDish;

        if (now.Hour >= 20 || now.Hour < 6)
        {
            greetingTime = "Buenas noches";
            suggestedDish = "tu ramen favorito";
        }
        else if (now.Hour >= 12 && now.Hour < 16)
        {
            greetingTime = "Buenas tardes";
            suggestedDish = "un bentō delicioso";
        }
        else
        {
            greetingTime = "Buenos días";
            suggestedDish = "un café con tostadas";
        }

        var affect = user.PersonalitySettings?.AffectLevel ?? 0.5f;
        string proactiveMessage;
        string proactiveSubtext;

        if (affect >= 0.7f)
        {
            proactiveMessage = $"{greetingTime} {user.Name}... ¡No es que haya estado esperándote ni nada! Pero llevas horas frente a la pantalla. ¿Te pido {suggestedDish} en PedidosYa?";
            proactiveSubtext = "Kokoro notó que llevas más de 2 horas trabajando sin parar.";
        }
        else if (affect <= 0.3f)
        {
            proactiveMessage = $"{greetingTime}, {user.Name}. Según tu cronograma y nivel de actividad, sugiero una pausa para comer. ¿Deseas ordenar {suggestedDish} vía PedidosYa?";
            proactiveSubtext = "Sugerencia proactiva basada en tu calendario de productividad.";
        }
        else
        {
            proactiveMessage = $"{greetingTime} {user.Name}, ¿te pido {suggestedDish} en PedidosYa? Noté que llevas 2 horas trabajando.";
            proactiveSubtext = "Te traje un té para concentrarte y puse tu playlist favorita.";
        }

        var response = new DashboardResponseDto
        {
            UserName = user.Name,
            IsVip = user.IsVip,
            Avatar = new AvatarHeroDto
            {
                Name = "Kokoro AI",
                EquippedOutfitName = equippedOutfit?.Name ?? "Uniforme Escolar Seifuku",
                AvatarImageUrl = equippedOutfit?.ImageUrl ?? "/outfits/uniforme_escolar.png",
                CurrentExpression = affect >= 0.7f ? "Blushing Smile" : "Soft Warm Smile"
            },
            ProactiveDialogue = new ProactiveDialogueDto
            {
                Message = proactiveMessage,
                Subtext = proactiveSubtext,
                SuggestedAction = "ORDER_PEDIDOSYA_RAMEN",
                SuggestedActionLabel = "Pedir Ramen en PedidosYa",
                Timestamp = DateTime.UtcNow
            },
            SpotifyWidget = new SpotifyWidgetDto
            {
                TrackTitle = "Lo-Fi Beats to Relax/Study to",
                Artist = "Kokoro Lo-Fi Radio",
                AlbumCoverUrl = "https://images.unsplash.com/photo-1518609878373-06d740f60d8b?w=200",
                IsPlaying = true,
                CurrentProgressSeconds = 45,
                TotalDurationSeconds = 180
            },
            QuickActions = new List<QuickActionPillDto>
            {
                new() { Id = "order_food", Label = "Order Food", Icon = "ramen_dining", Category = "Delivery" },
                new() { Id = "study_mode", Label = "Study Mode", Icon = "menu_book", Category = "Productivity" },
                new() { Id = "wardrobe", Label = "Wardrobe", Icon = "checkroom", Category = "Customization" },
                new() { Id = "alarm", Label = "Alarm", Icon = "alarm", Category = "Utilities" }
            }
        };

        return Ok(response);
    }

    /// <summary>
    /// Dispara una acción rápida de la pantalla principal (Zero-UI)
    /// </summary>
    [HttpPost("action")]
    public ActionResult<QuickActionResultDto> TriggerAction([FromBody] QuickActionRequestDto request)
    {
        return request.ActionId.ToLowerInvariant() switch
        {
            "order_food" or "order_pedidosya_ramen" => Ok(new QuickActionResultDto
            {
                Success = true,
                Message = "¡Pedido de Tonkotsu Ramen enviado a PedidosYa! Llegará en aproximadamente 25 minutos.",
                Payload = new { OrderId = "PY-" + Random.Shared.Next(10000, 99999), Status = "Cooking", EstimatedMinutes = 25 }
            }),
            "study_mode" => Ok(new QuickActionResultDto
            {
                Success = true,
                Message = "Modo Estudio activado. Notificaciones silenciadas y temporizador Pomodoro de 25 min iniciado.",
                Payload = new { Mode = "Pomodoro", Minutes = 25, BackgroundTrack = "Midnight Lo-Fi" }
            }),
            "toggle_music" => Ok(new QuickActionResultDto
            {
                Success = true,
                Message = "Estado de reproducción alternado con Spotify.",
                Payload = new { IsPlaying = true }
            }),
            _ => Ok(new QuickActionResultDto
            {
                Success = true,
                Message = $"Acción '{request.ActionId}' ejecutada con éxito por Kokoro."
            })
        };
    }
}
