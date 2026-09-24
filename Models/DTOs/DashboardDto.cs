using System;
using System.Collections.Generic;

namespace KokoroAi.Backend.Models.DTOs;

public class DashboardResponseDto
{
    public string UserName { get; set; } = string.Empty;
    public bool IsVip { get; set; }
    
    // Avatar Hero
    public AvatarHeroDto Avatar { get; set; } = new();

    // Contextual Proactive Bubble
    public ProactiveDialogueDto ProactiveDialogue { get; set; } = new();

    // Spotify Lo-Fi Music Widget
    public SpotifyWidgetDto SpotifyWidget { get; set; } = new();

    // Quick Action Pill Buttons
    public List<QuickActionPillDto> QuickActions { get; set; } = new();
}

public class AvatarHeroDto
{
    public string Name { get; set; } = "Kokoro AI";
    public string EquippedOutfitName { get; set; } = "Uniforme Escolar";
    public string AvatarImageUrl { get; set; } = "https://images.unsplash.com/photo-1578632767115-351597cf2477?w=600";
    public string CurrentExpression { get; set; } = "Soft Smile";
}

public class ProactiveDialogueDto
{
    public string Message { get; set; } = string.Empty;
    public string Subtext { get; set; } = string.Empty;
    public string SuggestedAction { get; set; } = string.Empty;
    public string SuggestedActionLabel { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class SpotifyWidgetDto
{
    public string TrackTitle { get; set; } = "Lo-Fi Beats to Relax/Study to";
    public string Artist { get; set; } = "Kokoro Lo-Fi Radio";
    public string AlbumCoverUrl { get; set; } = "https://images.unsplash.com/photo-1518609878373-06d740f60d8b?w=200";
    public bool IsPlaying { get; set; } = true;
    public int CurrentProgressSeconds { get; set; } = 45;
    public int TotalDurationSeconds { get; set; } = 180;
}

public class QuickActionPillDto
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

public class QuickActionRequestDto
{
    public string ActionId { get; set; } = string.Empty;
}

public class QuickActionResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Payload { get; set; }
}
