using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using KokoroAi.Backend.Models;

namespace KokoroAi.Backend.Data;

public class KokoroDbContext : DbContext
{
    public KokoroDbContext(DbContextOptions<KokoroDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<PersonalitySettings> PersonalitySettings => Set<PersonalitySettings>();
    public DbSet<Outfit> Outfits => Set<Outfit>();
    public DbSet<UserOutfit> UserOutfits => Set<UserOutfit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Clave compuesta para UserOutfit
        modelBuilder.Entity<UserOutfit>()
            .HasKey(uo => new { uo.UserId, uo.OutfitId });

        modelBuilder.Entity<UserOutfit>()
            .HasOne(uo => uo.User)
            .WithMany(u => u.UserOutfits)
            .HasForeignKey(uo => uo.UserId);

        modelBuilder.Entity<UserOutfit>()
            .HasOne(uo => uo.Outfit)
            .WithMany(o => o.UserOutfits)
            .HasForeignKey(uo => uo.OutfitId);

        // Relación 1 a 1 entre User y PersonalitySettings
        modelBuilder.Entity<User>()
            .HasOne(u => u.PersonalitySettings)
            .WithOne(p => p.User)
            .HasForeignKey<PersonalitySettings>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public static async Task SeedAsync(KokoroDbContext context)
    {
        var defaultUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var user = await context.Users.Include(u => u.PersonalitySettings).FirstOrDefaultAsync(u => u.Id == defaultUserId);

        if (user == null)
        {
            user = new User
            {
                Id = defaultUserId,
                Name = "Matias",
                IsVip = false,
                CreatedAt = DateTime.UtcNow
            };
            await context.Users.AddAsync(user);

            var settings = new PersonalitySettings
            {
                Id = Guid.NewGuid(),
                UserId = defaultUserId,
                AffectLevel = 0.75f,
                VoiceTone = "Hana (Suave)",
                VoicePitch = 1.05f
            };
            await context.PersonalitySettings.AddAsync(settings);
            await context.SaveChangesAsync();
        }

        // Definición de los nuevos outfits generados a partir de las imágenes de Kokoro
        var newOutfits = new List<Outfit>
        {
            new()
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333331"),
                Name = "Uniforme Escolar Seifuku",
                Description = "Clásico uniforme escolar japonés con camisa marinera, lazo rojo y falda azul plisada.",
                Category = "Uniforme Escolar",
                ImageUrl = "/outfits/uniforme_escolar.png",
                IsVipOnly = false
            },
            new()
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333332"),
                Name = "Ropa Casual Suéter Lo-Fi",
                Description = "Buzo oversize lavanda pastel con shorts y calcetines a rayas, ideal para estudiar con café caliente.",
                Category = "Casual",
                ImageUrl = "/outfits/casual_lofi.png",
                IsVipOnly = false
            },
            new()
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "Pijama Kigurumi Gatito",
                Description = "Enterizo afelpado rosa con capucha de orejitas de gato, colita y patitas adorables para descansar.",
                Category = "Pijama",
                ImageUrl = "/outfits/pijama_kigurumi.jpg",
                IsVipOnly = false
            },
            new()
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333334"),
                Name = "Deportivo Fitness Runner",
                Description = "Conjunto deportivo con top morado, calzas técnicas verdes y chaqueta ligera para salir a correr.",
                Category = "Deportivo",
                ImageUrl = "/outfits/deportivo.jpg",
                IsVipOnly = false
            },
            new()
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333335"),
                Name = "Kimono Ceremonial de Seda",
                Description = "Suntuoso kimono tradicional carmesí y violeta con grullas doradas y tocado kanzashi artesanal.",
                Category = "Tradicional",
                ImageUrl = "/outfits/kimono_tradicional.jpg",
                IsVipOnly = false
            },
            new()
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333336"),
                Name = "Maid Clásica Victoriana",
                Description = "Vestido de sirvienta de alta costura con delantal blanco de encaje y cofia clásica.",
                Category = "Especial VIP",
                ImageUrl = "/outfits/maid.jpg",
                IsVipOnly = true
            },
            new()
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333337"),
                Name = "Yukata Festivo de Verano",
                Description = "Yukata tradicional para festivales nocturnos Matsuri con flores de cerezo, obi morado y abanico.",
                Category = "Tradicional VIP",
                ImageUrl = "/outfits/yukata_festivo.jpg",
                IsVipOnly = true
            },
            new()
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333338"),
                Name = "Cyberpunk Neon Idol",
                Description = "Atuendo futurista translúcido con visor holográfico y detalles lumínicos reactivos neon violeta y rosa.",
                Category = "Futurista VIP",
                ImageUrl = "/outfits/cyberpunk_neon.jpg",
                IsVipOnly = true
            }
        };

        // Eliminar outfits antiguos si difieren
        var existingOutfits = await context.Outfits.ToListAsync();
        var newOutfitIds = newOutfits.Select(o => o.Id).ToHashSet();

        if (existingOutfits.Any(o => !newOutfitIds.Contains(o.Id)) || existingOutfits.Count != newOutfits.Count)
        {
            context.UserOutfits.RemoveRange(context.UserOutfits);
            context.Outfits.RemoveRange(existingOutfits);
            await context.SaveChangesAsync();

            await context.Outfits.AddRangeAsync(newOutfits);
            await context.SaveChangesAsync();

            // Desbloquear y equipar el atuendo escolar por defecto
            var userOutfit = new UserOutfit
            {
                UserId = defaultUserId,
                OutfitId = newOutfits[0].Id,
                IsEquipped = true,
                AcquiredAt = DateTime.UtcNow
            };
            await context.UserOutfits.AddAsync(userOutfit);
            await context.SaveChangesAsync();
        }
    }
}
