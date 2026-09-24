using System.Collections.Generic;

namespace KokoroAi.Backend.Models.DTOs;

public class PersonalitySettingsDto
{
    public float AffectLevel { get; set; } = 0.5f;
    public string AffectLabel { get; set; } = "Equilibrada";
    public string VoiceTone { get; set; } = "Ami (Neutra)";
    public float VoicePitch { get; set; } = 1.0f;
    public List<string> AvailableVoiceTones { get; set; } = new()
    {
        "Ami (Neutra)",
        "Hana (Suave)",
        "Rei (Enérgica)",
        "Kagami (Tsundere VIP)",
        "Yuki (Elegante VIP)"
    };
}

public class UpdatePersonalityRequestDto
{
    public float AffectLevel { get; set; }
    public string VoiceTone { get; set; } = "Ami (Neutra)";
    public float VoicePitch { get; set; } = 1.0f;
}
