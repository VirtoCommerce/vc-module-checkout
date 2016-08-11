﻿using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace VirtoCommerce.CheckoutModule.Data.Model
{
    public static class StringExtensions
    {
        /// <summary>
        /// http://stackoverflow.com/questions/484085/an-algorithm-to-spacify-camelcased-strings
        /// </summary>
        /// <param name="str"></param>
        /// <param name="spacer"></param>
        /// <returns></returns>
        public static string Decamelize(this string str, char spacer = '_')
        {
            if (string.IsNullOrEmpty(str))
                return str;

            /* Note that the .ToString() is required, otherwise the char is implicitly
             * converted to an integer and the wrong overloaded ctor is used */
            var sb = new StringBuilder(str[0].ToString());
            for (var i = 1; i < str.Length; i++)
            {
                if (char.IsUpper(str, i))
                    sb.Append(spacer);
                sb.Append(str[i]);
            }
            return sb.ToString().ToLowerInvariant();
        }

        /// <summary>
        /// Equals invariant
        /// </summary>
        /// <param name="str1">The STR1.</param>
        /// <param name="str2">The STR2.</param>
        /// <returns></returns>
        public static bool EqualsInvariant(this string str1, string str2)
        {
            return string.Equals(str1, str2, StringComparison.OrdinalIgnoreCase);
        }

        public static bool FitsMask(this string fileName, string fileMask)
        {
            var mask = new Regex("^" + fileMask.Replace(".", "[.]").Replace("*", ".*").Replace("?", ".") + "$", RegexOptions.IgnoreCase);
            return mask.IsMatch(fileName);
        }

        public static int? ToNullableInt(this string str)
        {
            int retVal;
            if (int.TryParse(str, out retVal))
            {
                return retVal;
            }
            return null;
        }

        public static Tuple<string, string> SplitIntoTuple(this string input, char separator)
        {
            if(input == null)
            {
                throw new ArgumentNullException("input");
            }

            var pieces = input.Split(separator);
            return Tuple.Create(pieces.FirstOrDefault(), pieces.Skip(1).FirstOrDefault());
        }
    }
}
