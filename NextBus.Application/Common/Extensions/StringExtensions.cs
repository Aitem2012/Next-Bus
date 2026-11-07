using System;

namespace NextBus.Application.Common.Extensions
{
    public static class StringExtensions
    {
        public static string GenerateRef(this string prefix)
        {
            return $"{prefix}_{Guid.NewGuid():N}"[..16].ToUpperInvariant();
        }

        public static string NormalizePhoneNumber(this string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return string.Empty;
            var trimmed = phone.Trim().Replace(" ", "").Replace("-", "");

            if (trimmed.StartsWith("+234") && trimmed.Length == 14)
                return trimmed;

            if (trimmed.StartsWith("234") && trimmed.Length == 13)
                return $"+{trimmed}";

            if (trimmed.StartsWith('0') && trimmed.Length == 11)
                return $"+234{trimmed[1..]}";

            return trimmed;
        }
    }
}

