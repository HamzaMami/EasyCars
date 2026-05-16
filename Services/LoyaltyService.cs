using EasyCars.Models;
using EasyCars.Models.Enums;

namespace EasyCars.Services
{
    public interface ILoyaltyService
    {
        decimal CalculateDiscount(Utilisateur user, decimal baseAmount);
        int CalculatePoints(decimal totalPaid);
        NiveauMembre UpdateMembershipLevel(int points);
    }

    public class LoyaltyService : ILoyaltyService
    {
        public decimal CalculateDiscount(Utilisateur user, decimal baseAmount)
        {
            return user.NiveauMembre switch
            {
                NiveauMembre.Bronze => 0,
                NiveauMembre.Argent => baseAmount * 0.10m, // 10%
                NiveauMembre.Or => baseAmount * 0.20m, // 20%
                _ => 0
            };
        }

        public int CalculatePoints(decimal totalPaid)
        {
            // 1 point for every 10 euros/units
            return (int)(totalPaid / 10);
        }

        public NiveauMembre UpdateMembershipLevel(int points)
        {
            if (points >= 1000) return NiveauMembre.Or;
            if (points >= 500) return NiveauMembre.Argent;
            return NiveauMembre.Bronze;
        }
    }
}
