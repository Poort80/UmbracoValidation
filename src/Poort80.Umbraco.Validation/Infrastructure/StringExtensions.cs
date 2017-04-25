using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poort80.Umbraco.Validation.Infrastructure
{
    internal static class StringExtensions
    {
        public static string UppercaseFirst(this string s)
        {
            // Check for empty string.
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            // Return char and concat substring.
            return char.ToUpper(s[0]) + s.Substring(1);
        }


        public static decimal? ToNullableDecimal(this string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return null;
            }

            decimal result;
            if (decimal.TryParse(s, out result))
            {
                return result;
            }
            return null;

        }

    }
}
