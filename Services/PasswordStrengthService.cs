using System.Linq;

namespace HackHelper.Services
{
    public enum PasswordStrength
    {
        VeryWeak,
        Weak,
        Medium,
        Strong,
        VeryStrong
    }

    public class PasswordStrengthResult
    {
        public PasswordStrength Strength { get; set; }
        public string StrengthText { get; set; }
        public string Color { get; set; }
        public double ProgressPercentage { get; set; }
    }

    public static class PasswordStrengthService
    {
        public static PasswordStrengthResult CalculateStrength(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return new PasswordStrengthResult
                {
                    Strength = PasswordStrength.VeryWeak,
                    StrengthText = "No Password",
                    Color = "#6B7280",
                    ProgressPercentage = 0
                };
            }

            int score = 0;
            int length = password.Length;

            // Length scoring
            if (length >= 8) score += 1;
            if (length >= 12) score += 1;
            if (length >= 16) score += 1;

            // Character variety scoring
            bool hasLower = password.Any(char.IsLower);
            bool hasUpper = password.Any(char.IsUpper);
            bool hasDigit = password.Any(char.IsDigit);
            bool hasSpecial = password.Any(c => !char.IsLetterOrDigit(c));

            if (hasLower) score += 1;
            if (hasUpper) score += 1;
            if (hasDigit) score += 1;
            if (hasSpecial) score += 1;

            // Bonus for multiple character types
            int charTypeCount = (hasLower ? 1 : 0) + (hasUpper ? 1 : 0) + (hasDigit ? 1 : 0) + (hasSpecial ? 1 : 0);
            if (charTypeCount >= 3) score += 1;
            if (charTypeCount == 4) score += 1;

            // Determine strength based on score
            PasswordStrength strength;
            string strengthText;
            string color;
            double percentage;

            if (score <= 2)
            {
                strength = PasswordStrength.VeryWeak;
                strengthText = "Very Weak";
                color = "#EF4444"; // Red
                percentage = 20;
            }
            else if (score <= 4)
            {
                strength = PasswordStrength.Weak;
                strengthText = "Weak";
                color = "#F59E0B"; // Orange
                percentage = 40;
            }
            else if (score <= 6)
            {
                strength = PasswordStrength.Medium;
                strengthText = "Medium";
                color = "#EAB308"; // Yellow
                percentage = 60;
            }
            else if (score <= 8)
            {
                strength = PasswordStrength.Strong;
                strengthText = "Strong";
                color = "#22C55E"; // Green
                percentage = 80;
            }
            else
            {
                strength = PasswordStrength.VeryStrong;
                strengthText = "Very Strong";
                color = "#3B82F6"; // Blue
                percentage = 100;
            }

            return new PasswordStrengthResult
            {
                Strength = strength,
                StrengthText = strengthText,
                Color = color,
                ProgressPercentage = percentage
            };
        }
    }
}