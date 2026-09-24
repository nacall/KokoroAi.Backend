using System;

namespace KokoroAi.Backend.Models;

public class PersonalitySettings
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Nivel de Afecto de 0.0f (Profesional) a 1.0f (Tsundere/Cariñosa)
    /// </summary>
    public float AffectLevel { get; set; } = 0.5f;

    /// <summary>
    /// Selector de voz neuronal: ej. "Ami (Neutra)", "Hana (Suave)", "Rei (Enérgica)"
    /// </summary>
    public string VoiceTone { get; set; } = "Ami (Neutra)";

    /// <summary>
    /// Tono / Pitch de la voz (0.5 a 1.5)
    /// </summary>
    public float VoicePitch { get; set; } = 1.0f;

    // Navigation property
    public User? User { get; set; }
}
