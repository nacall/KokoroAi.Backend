using System;
using System.Collections.Generic;

namespace KokoroAi.Backend.Models;

public class Outfit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "Casual";
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsVipOnly { get; set; } = false;

    // Navigation properties
    public ICollection<UserOutfit> UserOutfits { get; set; } = new List<UserOutfit>();
}
