﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using static KitchenSink.Operators;

namespace KitchenSink.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// A Regex that identifies contiguous sequences of whitespace.
        /// </summary>
        public static readonly Regex WhiteSpaceRegex = new Regex("\\s+");

        /// <summary>
        /// Trims string and replaces sequences of whitespace with a single space.
        /// </summary>
        public static string CollapseSpace(this string s)
        {
            return WhiteSpaceRegex.Replace(s.Trim(), " ");
        }

        /// <summary>
        /// Returns string with words captialized.
        /// </summary>
        public static string ToTitleCase(this string x)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(x);
        }

        /// <summary>
        /// Formats decimal value as currency.
        /// </summary>
        public static string ToCurrencyString(this decimal x)
        {
            return $"{x:c}";
        }

        public static Maybe<int> ToInt(this string s)
        {
            int i;
            return int.TryParse(s, out i) ? Some(i) : None<int>();
        }

        public static Maybe<double> ToDouble(this string s)
        {
            double d;
            return double.TryParse(s, out d) ? Some(d) : None<double>();
        }

        /// <summary>
        /// Converts items in sequence to string and concats them
        /// separated by <c>sep</c>, which defaults to empty string.
        /// </summary>
        public static string MakeString<A>(this IEnumerable<A> seq, string sep = "")
        {
            return string.Join(sep, seq);
        }

        /// <summary>
        /// Converts sequence to character-separated string, using quotes
        /// to escape values containing the separator (comma by default).
        /// </summary>
        public static string ToCsv(this IEnumerable<object> seq, string sep = ",")
        {
            return seq
                .Select(Str)
                .Select(s => s.Contains(sep)
                    ? $"\"{s.Replace("\"", "\"\"")}\""
                    : s)
                .MakeString(sep);
        }

        /// <summary>
        /// Converts string from Windows-style CRLF to Unix-style LF.
        /// </summary>
        public static string ToLF(this string s)
        {
            return s.Replace("\r\n", "\n");
        }

        /// <summary>
        /// Converts string from Unix-style LF to Windows-style CRLF.
        /// </summary>
        public static string ToCRLF(this string s)
        {
            return s.Replace("\r\n", "\n").Replace("\n", "\r\n");
        }

        /// <summary>
        /// Returns true if two strings are equal ignoring case.
        /// </summary>
        public static bool IsSimilar(this string x, string y)
        {
            return string.Equals(x, y, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Splits a string according to given Regex.
        /// </summary>
        public static IEnumerable<string> Split(this string s, Regex r)
        {
            var m = r.Match(s);

            while (m.Success)
            {
                yield return m.Value;
                m = m.NextMatch();
            }
        }

        /// <summary>
        /// Splits a string according to given separator and optional
        /// StringComparison method.
        /// </summary>
        public static IEnumerable<string> Split(
            this string s,
            string sep,
            StringComparison comparison = StringComparison.InvariantCulture)
        {
            var i = 0;

            for (int j; (j = s.IndexOf(sep, i, comparison)) >= 0; i = j + sep.Length)
            {
                yield return s.Substring(i, j - i);
            }

            yield return s.Substring(i, s.Length - i);
        }
    }
}
