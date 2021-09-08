using System;

namespace NextBus.Common.Extensions
{
    public static class NextBusExtensions
    {
        public static string Slugify(this string str)
        {
            return str.ToLower().Replace(" ", "-").Replace(".", "").Replace("/", "-").Replace("\\", "-").Replace("@", "-").Replace("!", "").Replace(",", "").Replace("~", "").Replace("`", "").Replace("'", "").Replace("\"", "").Replace("#", "").Replace("&", "") + Guid.NewGuid().ToString().Substring(0, 12);
        }

        public static string GenerateRef(this string str)
        {
            return $"{str}{Guid.NewGuid().ToString().Replace("-", "").ToUpper().Substring(0, 10)}";
        }

        public static string GenerateReferralCode(this string str)
        {
            return $"{str}{Guid.NewGuid().ToString().Replace("-", "").ToUpper().Substring(0, 7)}";
        }

        public static string CorrectPhoneNumber(this string str)
        {
            if (str.StartsWith('+') && str.Length == 14)
            {
                return str;
            }

            return "+234" + str.Substring(1);
        }
    }
}
