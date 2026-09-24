using System;

namespace KokoroAi.Backend.Models.DTOs;

public class ChatRequestDto
{
    public string Message { get; set; } = string.Empty;
}

public class ChatResponseDto
{
    public string Reply { get; set; } = string.Empty;
    public string PersonalityMode { get; set; } = string.Empty;
    public string AvatarExpression { get; set; } = string.Empty;
    public string? DetectedIntent { get; set; }
    public string? SuggestedAction { get; set; }
    public bool IsAiGenerated { get; set; } = true;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
