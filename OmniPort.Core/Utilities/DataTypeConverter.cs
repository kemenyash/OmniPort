using OmniPort.Core.Enums;
using OmniPort.Core.Models;
using System.Globalization;
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

            string normalString = NormalizeToString(value);
            if (string.IsNullOrWhiteSpace(normalString)) return null;

            try
            {
                switch (mapping.TargetType)
                {
                    case FieldDataType.String:
                        return normalString;

                    case FieldDataType.Integer:
                        {
                            if (TryCoerceToDouble(value, out double doubleValue)) return Convert.ToInt32(Math.Round(doubleValue, MidpointRounding.AwayFromZero));
                            if (TryParseDecimalFromString(normalString, out decimal decimalValue)) return Convert.ToInt32(Math.Round(decimalValue, MidpointRounding.AwayFromZero));

                            throw new FormatException($"Cannot parse integer from '{value}'.");
                        }

                    case FieldDataType.Decimal:
                        {
                            if (TryCoerceToDecimal(value, out decimal decimalValue)) return decimalValue;
                            if (TryParseDecimalFromString(normalString, out decimal decimalValueFromString)) return decimalValueFromString;

                            throw new FormatException($"Cannot parse decimal from '{value}'.");
                        }

                    case FieldDataType.Boolean:
                        {
                            if (value is bool b) return b;

                            if (TryCoerceToDouble(value, out double doubleValue)) return Math.Abs(doubleValue) > double.Epsilon;

                            if (TrueVals.Contains(normalString)) return true;
                            if (FalseVals.Contains(normalString)) return false;

                            if (TryParseDecimalFromString(normalString, out decimal decimalValue)) return decimalValue != 0m;

                            throw new FormatException($"Cannot parse boolean from '{value}'.");
                        }

                    case FieldDataType.DateTime:
                        {
                            if (value is DateTime dateTime) return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc).ToUniversalTime();

                            if (TryCoerceToDouble(value, out double doubleValue))
                            {
                                if (doubleValue > 0 && doubleValue < 600000) return DateTime.FromOADate(doubleValue);
                            }

                            if (!string.IsNullOrWhiteSpace(mapping.DateFormat))
                            {
                                if (DateTime.TryParseExact(normalString, mapping.DateFormat, CultureInfo.InvariantCulture,
                                                           DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces, out DateTime parsedDateTime))
                                {
                                    return parsedDateTime;
                                }
                            }

                            if (DateTime.TryParseExact(normalString, CommonDateFormats, CultureInfo.InvariantCulture,
                                                       DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces, out DateTime parsedExactDateTime))
                            {
                                return parsedExactDateTime;
                            }

                            if (DateTime.TryParse(normalString, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out DateTime parsedInvariantDateTime)) return parsedInvariantDateTime;
                            if (DateTime.TryParse(normalString, new CultureInfo("uk-UA"), DateTimeStyles.AssumeLocal, out DateTime parsedUkUaDateTime)) return parsedUkUaDateTime;
                            if (DateTime.TryParse(normalString, new CultureInfo("en-US"), DateTimeStyles.AssumeLocal, out DateTime parsedEnUsDateTime)) return parsedEnUsDateTime;

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
            if (value is string stringValue) return stringValue.Trim();

            if (value is DateTime dateTimeValue) return dateTimeValue.ToString("s", CultureInfo.InvariantCulture);

            if (value is IFormattable formattableValue) return formattableValue.ToString(null, CultureInfo.InvariantCulture);

            return value.ToString()?.Trim() ?? string.Empty;
        }

        private static bool TryCoerceToDouble(object objectParam, out double doubleValue)
        {
            switch (objectParam)
            {
                case double doubleParam: doubleValue = doubleParam; return true;
                case float floatParam: doubleValue = floatParam; return true;
                case decimal decimalParam: doubleValue = (double)decimalParam; return true;
                case long longParam: doubleValue = longParam; return true;
                case int intParam: doubleValue = intParam; return true;
                case short shortParam: doubleValue = shortParam; return true;
                case byte byteParam: doubleValue = byteParam; return true;
                case string stringParam when TryParseDecimalFromString(stringParam, out decimal decimalParam):
                    doubleValue = (double)decimalParam;
                    return true;
                default:
                    doubleValue = default;
                    return false;
            }
        }

        private static bool TryCoerceToDecimal(object objectParam, out decimal decimalValue)
        {
            switch (objectParam)
            {
                case decimal decimalParam: decimalValue = decimalParam; return true;
                case double doubleParam: decimalValue = (decimal)doubleParam; return true;
                case float floatParam: decimalValue = (decimal)floatParam; return true;
                case long longParam: decimalValue = longParam; return true;
                case int intParam: decimalValue = intParam; return true;
                case short shortParam: decimalValue = shortParam; return true;
                case byte byteParam: decimalValue = byteParam; return true;
                case string stringParam: return TryParseDecimalFromString(stringParam, out decimalValue);
                default: decimalValue = default; return false;
            }
        }

        private static bool TryParseDecimalFromString(string raw, out decimal result)
        {
            string preparationString = NumericKeepRegex.Replace(raw.Trim(), string.Empty);

            if (string.IsNullOrWhiteSpace(preparationString))
            {
                result = 0m;
                return false;
            }

            preparationString = preparationString.Replace(" ", "").Replace("\u00A0", "");

            if (preparationString.Contains(',') && preparationString.Contains('.'))
            {
                int lastComma = preparationString.LastIndexOf(',');
                int lastDot = preparationString.LastIndexOf('.');

                if (lastComma > lastDot)
                {
                    preparationString = preparationString.Replace(".", "");
                    preparationString = preparationString.Replace(',', '.');
                }
                else
                {
                    preparationString = preparationString.Replace(",", "");
                }
            }
            else if (preparationString.Contains(',') && !preparationString.Contains('.'))
            {
                preparationString = preparationString.Replace(',', '.');
            }

            return decimal.TryParse(preparationString, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out result);
        }
    }
}
