using Microsoft.EntityFrameworkCore;
using WordBattleGame.Data;
using WordBattleGame.Repositories;
using WordBattleGame.Services;

namespace WordBattleGame.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(
                    configuration.GetConnectionString(
                        "DefaultConnection"
                        )
                )
            );
            services.AddScoped<IPlayerRepository, PlayerRepository>();
            services.AddScoped<IAuthRepository, AuthRepository>();
            services.AddScoped<IWordGeneratorService, WordGeneratorService>();
            return services;
        }
    }
}