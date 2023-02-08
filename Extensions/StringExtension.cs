using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DirectorySearcherTestApp.Extensions
{
    public static class StringExtension
    {
        public static bool IsFileNameMatchMask(string fileName, string mask)
        {
            mask = mask.Replace(".", @"\.").Replace("?", ".").Replace("*", ".*");
            // Указываем, что требуется найти точное соответствие маске
            mask = "^" + mask + "$";
            Regex regMask = new Regex(mask, RegexOptions.IgnoreCase);
            return regMask.IsMatch(fileName);
        }

        public static string AddWhereExpression(this string curent, string add_value)
           => curent.AddWithDelimeter(add_value, " and ");

        public static string AddWithDelimeter(this string cur, string add_value, char delimeter = ',')
            => cur.AddWithDelimeter(add_value, delimeter.ToString());

        public static string AddWithDelimeter(this string cur, string add_value, string delimeter = ",")
        {
            if (cur.IsNullOrWhiteSpace())
                return add_value;
            else if (add_value.IsNullOrWhiteSpace())
                return cur;
            else
                return cur.AppendInBuilder(delimeter).AppendInBuilder(add_value);
        }

        public static bool IsNullOrWhiteSpace(this string str)
            => string.IsNullOrWhiteSpace(str);

        public static string ReplaceCharsForCompare(this string current)
            => Regex.Replace(current.RemoveDiacritics() ?? "", @"\W", "");

        public static string NewLine(this string current)
            => current + Environment.NewLine;

        public static string NewLineBefore(this string current)
            => Environment.NewLine + current;

        public static string RemoveDiacritics(this string text)
        {
            if (text.IsNullOrWhiteSpace())
                return text;
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();
            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }
            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        public static bool RegexMatchIgnoreCase(this string current, string pattern)
            => Regex.IsMatch(current, pattern, RegexOptions.IgnoreCase);
        public static MatchCollection RegexMatchesIgnoreCase(this string current, string pattern)
          => Regex.Matches(current, pattern, RegexOptions.IgnoreCase);
        public static string RegexReplaceIgnoreCase(this string current, string pattern, string new_value)
           => Regex.Replace(current, pattern, new_value, RegexOptions.IgnoreCase);

        public static bool Contains(this string source, string toCheck, StringComparison comp)
        => source?.IndexOf(toCheck, comp) >= 0;

        public static bool ContainsIgnoreCase(this string source, string toCheck)
            => source.Contains(toCheck, StringComparison.OrdinalIgnoreCase);

        public static string AppendInBuilder(this string source, string[] add_parts)
        {
            var result = source;
            for (int i = 0; i < (add_parts?.Length ?? 0); i++)
                result = result.AppendInBuilder(add_parts?[i]);
            return result;
        }

        public static string AppendInBuilder(this string source, char addChar)
            => source.AppendInBuilder(addChar.ToString());


        public static string AppendInBuilder(this string source, string add_part, string separator = null)
        {
            if (add_part.IsNullOrWhiteSpace())
                return source;
            if (source.IsNullOrWhiteSpace())
                return add_part;
            StringBuilder bld = new StringBuilder();
            for (int i = 0; i < source.Length; ++i)
                bld.Append(source[i]);
            for (int i = 0; i < (separator?.Length ?? 0); ++i)
                bld.Append(separator[i]);
            for (int i = 0; i < add_part.Length; ++i)
                bld.Append(add_part[i]);
            return bld.ToString();
        }

        public static string ConvertToUTF8(this string from)
        {
            byte[] bytes = Encoding.Default.GetBytes(from);
            return Encoding.UTF8.GetString(bytes);
        }

        public static string GetPostgresSQLName(string for_name)
        {
            StringBuilder bld = new StringBuilder();
            bool is_first = true;
            for (int i = 0; i < for_name.Length; ++i)
            {
                if (Char.IsUpper(for_name[i]))
                {
                    if (is_first)
                        is_first = false;
                    else
                        bld.Append('_');
                    bld.Append(Char.ToLower(for_name[i]));
                }
                else
                    bld.Append(for_name[i]);
            }
            return bld.ToString();
        }

        public static string Left(this string from, int count)
          => from?.Substring(0, Math.Min(count, from.Length)) ?? string.Empty;

        public static string Right(this string from, int count)
        {
            var min = Math.Min(count, from.Length);
            return from?.Substring(from.Length - min, min) ?? string.Empty;
        }

        public static readonly char[] VowelChars = { 'а', 'я', 'у', 'ю', 'о', 'е', 'ё', 'э', 'и', 'ы' };
        public static bool IsVowel(this char c)
          => VowelChars.Any(x => x == char.ToLower(c));
    }
}
