using System;

namespace KokoroAi.Backend.Models;

public class UserOutfit
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public Guid OutfitId { get; set; }
    public Outfit? Outfit { get; set; }

    public bool IsEquipped { get; set; } = false;
    public DateTime AcquiredAt { get; set; } = DateTime.UtcNow;
}
