using OmniPort.Core.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.Core.Utilities
{
    public static class DataTypeConverter
    {
        public static object? ConvertToType(object? value, FieldMapping mapping)
        {
            if (value == null || value is DBNull)
                return null;

            try
            {
                return mapping.TargetType switch
                {
                    FieldDataType.String => value.ToString(),
                    FieldDataType.Integer => Convert.ToInt32(value),
                    FieldDataType.Decimal => Convert.ToDecimal(value, CultureInfo.InvariantCulture),
                    FieldDataType.Boolean => Convert.ToBoolean(value),
                    FieldDataType.DateTime => mapping.DateFormat != null
                        ? DateTime.ParseExact(value.ToString(), mapping.DateFormat, CultureInfo.InvariantCulture)
                        : Convert.ToDateTime(value, CultureInfo.InvariantCulture),
                    _ => value
                };
            }
            catch(Exception error)
            {
                throw new FormatException($"Cannot convert value '{value}' to {mapping.TargetType}\r\nError: {error.Message}");
            }
        }
    }
}
