using System;
using System.Collections.Generic;

namespace KokoroAi.Backend.Models.DTOs;

public class WardrobeResponseDto
{
    public VipBannerDto VipBanner { get; set; } = new();
    public List<OutfitItemDto> Outfits { get; set; } = new();
}

public class VipBannerDto
{
    public bool IsVipActive { get; set; } = false;
    public string Title { get; set; } = "Kokoro Plus VIP";
    public string Subtitle { get; set; } = "Desbloquea trajes exclusivos, voces neuronales avanzadas y modo afecto sin límites.";
    public string CtaButtonText { get; set; } = "Activar Prueba Gratis de 7 Días";
    public int? DaysRemaining { get; set; }
}

public class OutfitItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsVipOnly { get; set; }
    public bool IsUnlocked { get; set; }
    public bool IsEquipped { get; set; }
}

public class EquipOutfitResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public OutfitItemDto? EquippedOutfit { get; set; }
}

public class SubscribeVipResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
}
