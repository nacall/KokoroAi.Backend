using System;
using System.Collections.Generic;

namespace KokoroAi.Backend.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public bool IsVip { get; set; } = false;
    public DateTime? VipSubscriptionEnd { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public PersonalitySettings? PersonalitySettings { get; set; }
    public ICollection<UserOutfit> UserOutfits { get; set; } = new List<UserOutfit>();
}
