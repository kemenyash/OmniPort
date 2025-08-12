using OmniPort.Core.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace OmniPort.Core.Utilities
{
    public static class DataTypeConverter
    {
        private static readonly Regex NumericKeepRegex = new Regex(@"[^0-9\-\+\,\.]", RegexOptions.Compiled);

        private static readonly HashSet<string> TrueVals = new(StringComparer.OrdinalIgnoreCase) { "true", "1", "yes", "y", "on", "t" };
        private static readonly HashSet<string> FalseVals = new(StringComparer.OrdinalIgnoreCase) { "false", "0", "no", "n", "off", "f" };

        private static readonly string[] CommonDateFormats = new[]
        {
            "dd.MM.yyyy", "dd.MM.yyyy HH:mm", "dd.MM.yyyy HH:mm:ss",
            "yyyy-MM-dd", "yyyy-MM-dd HH:mm", "yyyy-MM-dd HH:mm:ss",
            "MM/dd/yyyy", "MM/dd/yyyy HH:mm", "MM/dd/yyyy HH:mm:ss",
            "dd/MM/yyyy", "dd/MM/yyyy HH:mm", "dd/MM/yyyy HH:mm:ss"
        };

        public static object? ConvertToType(object? value, FieldMapping mapping)
        {
            if (value is null || value is DBNull) return null;

            string s = NormalizeToString(value);
            if (string.IsNullOrWhiteSpace(s)) return null;

            try
            {
                switch (mapping.TargetType)
                {
                    case FieldDataType.String:
                        return s;

                    case FieldDataType.Integer:
                        {
                            if (TryCoerceToDouble(value, out var d)) return Convert.ToInt32(Math.Round(d, MidpointRounding.AwayFromZero));
                            if (TryParseDecimalFromString(s, out var dec)) return Convert.ToInt32(Math.Round(dec, MidpointRounding.AwayFromZero));

                            throw new FormatException($"Cannot parse integer from '{value}'.");
                        }

                    case FieldDataType.Decimal:
                        {
                            if (TryCoerceToDecimal(value, out var dec)) return dec;

                            if (TryParseDecimalFromString(s, out var dec2)) return dec2;

                            throw new FormatException($"Cannot parse decimal from '{value}'.");
                        }

                    case FieldDataType.Boolean:
                        {
                            if (value is bool b) return b;

                            if (TryCoerceToDouble(value, out var d))
                                return Math.Abs(d) > double.Epsilon;

                            if (TrueVals.Contains(s)) return true;
                            if (FalseVals.Contains(s)) return false;
                            if (TryParseDecimalFromString(s, out var dec))
                                return dec != 0m;

                            throw new FormatException($"Cannot parse boolean from '{value}'.");
                        }

                    case FieldDataType.DateTime:
                        {
                            if (value is DateTime dt) return DateTime.SpecifyKind(dt, DateTimeKind.Utc).ToUniversalTime();

                            if (TryCoerceToDouble(value, out var d))
                            {
                                if (d > 0 && d < 600000)
                                    return DateTime.FromOADate(d);
                            }

                            if (!string.IsNullOrWhiteSpace(mapping.DateFormat))
                            {
                                if (DateTime.TryParseExact(s, mapping.DateFormat, CultureInfo.InvariantCulture,
                                                           DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces, out var p))
                                    return p;
                            }

                            if (DateTime.TryParseExact(s, CommonDateFormats, CultureInfo.InvariantCulture,
                                                       DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces, out var p2))
                                return p2;

                            if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var p3)) return p3;
                            if (DateTime.TryParse(s, new CultureInfo("uk-UA"), DateTimeStyles.AssumeLocal, out var p4)) return p4;
                            if (DateTime.TryParse(s, new CultureInfo("en-US"), DateTimeStyles.AssumeLocal, out var p5)) return p5;

                            throw new FormatException($"Cannot parse DateTime from '{value}'.");
                        }

                    default:
                        return value;
                }
            }
            catch (Exception ex)
            {
                throw new FormatException($"Cannot convert value '{value}' to {mapping.TargetType}. Error: {ex.Message}");
            }
        }

        private static string NormalizeToString(object value)
        {
            if (value is string s) return s.Trim();

            if (value is DateTime dt) return dt.ToString("s", CultureInfo.InvariantCulture);

            if (value is IFormattable f) return f.ToString(null, CultureInfo.InvariantCulture);

            return value.ToString()?.Trim() ?? string.Empty;
        }

        private static bool TryCoerceToDouble(object value, out double d)
        {
            switch (value)
            {
                case double dv: d = dv; return true;
                case float fv: d = fv; return true;
                case decimal m: d = (double)m; return true;
                case long l: d = l; return true;
                case int i: d = i; return true;
                case short sh: d = sh; return true;
                case byte b: d = b; return true;
                case string s when TryParseDecimalFromString(s, out var dec): d = (double)dec; return true;
                default: d = default; return false;
            }
        }

        private static bool TryCoerceToDecimal(object value, out decimal dec)
        {
            switch (value)
            {
                case decimal m: dec = m; return true;
                case double d: dec = (decimal)d; return true;
                case float f: dec = (decimal)f; return true;
                case long l: dec = l; return true;
                case int i: dec = i; return true;
                case short sh: dec = sh; return true;
                case byte b: dec = b; return true;
                case string s: return TryParseDecimalFromString(s, out dec);
                default: dec = default; return false;
            }
        }

        private static bool TryParseDecimalFromString(string raw, out decimal result)
        {
            var s = NumericKeepRegex.Replace(raw.Trim(), string.Empty);

            if (string.IsNullOrWhiteSpace(s))
            {
                result = 0m;
                return false;
            }

            s = s.Replace(" ", "").Replace("\u00A0", "");

            if (s.Contains(',') && s.Contains('.'))
            {
                int lastComma = s.LastIndexOf(',');
                int lastDot = s.LastIndexOf('.');

                if (lastComma > lastDot)
                {
                    s = s.Replace(".", "");
                    s = s.Replace(',', '.'); 
                }
                else
                {
                    s = s.Replace(",", "");
                }
            }
            else if (s.Contains(',') && !s.Contains('.'))
            {
                s = s.Replace(',', '.');
            }

            return decimal.TryParse(s, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out result);
        }
    }
}
