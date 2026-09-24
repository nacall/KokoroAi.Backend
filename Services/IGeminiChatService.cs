using System.Threading.Tasks;
using KokoroAi.Backend.Models;
using KokoroAi.Backend.Models.DTOs;

namespace KokoroAi.Backend.Services;

public interface IGeminiChatService
{
    Task<ChatResponseDto> GenerateCompanionResponseAsync(
        User user,
        PersonalitySettings settings,
        Outfit? equippedOutfit,
        string userMessage);
}
