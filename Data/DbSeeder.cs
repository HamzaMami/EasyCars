using EasyCars.Models;
using EasyCars.Models.Enums;
using Microsoft.AspNetCore.Identity;

namespace EasyCars.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<Utilisateur>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            // Seed roles
            string[] roles = { "Client", "AgenceAdmin", "SuperAdmin" };
            foreach (var role in roles)
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));

            // Seed SuperAdmin
            if (await userManager.FindByEmailAsync("admin@easycars.fr") == null)
            {
                var admin = new Utilisateur { Nom = "Super Admin", UserName = "admin@easycars.fr", Email = "admin@easycars.fr", NiveauMembre = NiveauMembre.Or, PointsFidelite = 5000 };
                await userManager.CreateAsync(admin, "Admin123!");
                await userManager.AddToRoleAsync(admin, "SuperAdmin");
            }

        }
    }
}
