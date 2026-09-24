using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KokoroAi.Backend.Data;
using KokoroAi.Backend.Models;
using KokoroAi.Backend.Models.DTOs;

namespace KokoroAi.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WardrobeController : ControllerBase
{
    private readonly KokoroDbContext _context;
    private static readonly Guid DefaultUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public WardrobeController(KokoroDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lista el guardarropa de Kokoro y el estado de la suscripción VIP (Pantalla 2)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<WardrobeResponseDto>> GetWardrobe()
    {
        var user = await _context.Users
            .Include(u => u.UserOutfits)
            .FirstOrDefaultAsync(u => u.Id == DefaultUserId);

        if (user == null)
        {
            return NotFound("Usuario no encontrado.");
        }

        var allOutfits = await _context.Outfits.ToListAsync();

        var equippedOutfitId = user.UserOutfits
            .FirstOrDefault(uo => uo.IsEquipped)?.OutfitId;

        var userUnlockedOutfitIds = user.UserOutfits
            .Select(uo => uo.OutfitId)
            .ToHashSet();

        var outfitDtos = allOutfits.Select(o =>
        {
            // Un traje es desbloqueado si el usuario lo tiene registrado o si es VIP y el traje es VIP
            bool isUnlocked = userUnlockedOutfitIds.Contains(o.Id) || (user.IsVip && o.IsVipOnly) || !o.IsVipOnly;
            bool isEquipped = o.Id == equippedOutfitId;

            return new OutfitItemDto
            {
                Id = o.Id,
                Name = o.Name,
                Description = o.Description,
                Category = o.Category,
                ImageUrl = o.ImageUrl,
                IsVipOnly = o.IsVipOnly,
                IsUnlocked = isUnlocked,
                IsEquipped = isEquipped
            };
        }).ToList();

        int? daysRemaining = null;
        if (user.IsVip && user.VipSubscriptionEnd.HasValue)
        {
            var diff = (user.VipSubscriptionEnd.Value - DateTime.UtcNow).Days;
            daysRemaining = Math.Max(0, diff);
        }

        var response = new WardrobeResponseDto
        {
            VipBanner = new VipBannerDto
            {
                IsVipActive = user.IsVip,
                Title = user.IsVip ? "Kokoro Plus VIP Activo ✨" : "Kokoro Plus VIP",
                Subtitle = user.IsVip 
                    ? $"Disfrutando de todos los trajes y voces neuronales. ({daysRemaining} días restantes)"
                    : "Desbloquea trajes exclusivos, voces neuronales avanzadas y modo afecto sin límites.",
                CtaButtonText = user.IsVip ? "Suscripción Gestionada" : "Activar Prueba Gratis de 7 Días",
                DaysRemaining = daysRemaining
            },
            Outfits = outfitDtos
        };

        return Ok(response);
    }

    /// <summary>
    /// Equipa un traje en el avatar de Kokoro
    /// </summary>
    [HttpPost("equip/{outfitId}")]
    public async Task<ActionResult<EquipOutfitResponseDto>> EquipOutfit(Guid outfitId)
    {
        var user = await _context.Users
            .Include(u => u.UserOutfits)
            .FirstOrDefaultAsync(u => u.Id == DefaultUserId);

        if (user == null)
        {
            return NotFound("Usuario no encontrado.");
        }

        var outfit = await _context.Outfits.FindAsync(outfitId);
        if (outfit == null)
        {
            return NotFound("Atuendo no encontrado.");
        }

        // Si es exclusivo VIP y el usuario no tiene suscripción activa
        if (outfit.IsVipOnly && !user.IsVip)
        {
            return BadRequest(new EquipOutfitResponseDto
            {
                Success = false,
                Message = $"'{outfit.Name}' es un atuendo exclusivo de Kokoro Plus VIP. Activa tu prueba gratis de 7 días para usarlo."
            });
        }

        // Desequipar todos los anteriores
        foreach (var uo in user.UserOutfits)
        {
            uo.IsEquipped = false;
        }

        // Marcar como adquirido y equipado
        var targetUserOutfit = user.UserOutfits.FirstOrDefault(uo => uo.OutfitId == outfitId);
        if (targetUserOutfit == null)
        {
            targetUserOutfit = new UserOutfit
            {
                UserId = user.Id,
                OutfitId = outfit.Id,
                IsEquipped = true,
                AcquiredAt = DateTime.UtcNow
            };
            user.UserOutfits.Add(targetUserOutfit);
        }
        else
        {
            targetUserOutfit.IsEquipped = true;
        }

        await _context.SaveChangesAsync();

        return Ok(new EquipOutfitResponseDto
        {
            Success = true,
            Message = $"¡Kokoro se ha cambiado al atuendo '{outfit.Name}'!",
            EquippedOutfit = new OutfitItemDto
            {
                Id = outfit.Id,
                Name = outfit.Name,
                Description = outfit.Description,
                Category = outfit.Category,
                ImageUrl = outfit.ImageUrl,
                IsVipOnly = outfit.IsVipOnly,
                IsUnlocked = true,
                IsEquipped = true
            }
        });
    }

    /// <summary>
    /// Activa la prueba gratuita de 7 días de Kokoro Plus VIP (CTA Banner)
    /// </summary>
    [HttpPost("subscribe-trial")]
    public async Task<ActionResult<SubscribeVipResponseDto>> SubscribeTrial()
    {
        var user = await _context.Users.FindAsync(DefaultUserId);
        if (user == null)
        {
            return NotFound("Usuario no encontrado.");
        }

        user.IsVip = true;
        user.VipSubscriptionEnd = DateTime.UtcNow.AddDays(7);

        await _context.SaveChangesAsync();

        return Ok(new SubscribeVipResponseDto
        {
            Success = true,
            Message = "¡Felicidades! Has activado tu prueba gratuita de 7 días de Kokoro Plus VIP. Todos los atuendos exclusivos han sido desbloqueados.",
            ExpiresAt = user.VipSubscriptionEnd
        });
    }
}
