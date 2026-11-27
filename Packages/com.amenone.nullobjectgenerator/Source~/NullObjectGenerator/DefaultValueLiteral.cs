using System;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Amenone.NullObjectGenerator
{
    internal static class DefaultValueLiteral
    {
        public static string Of(IParameterSymbol parameter)
        {
            var value = parameter.ExplicitDefaultValue;
            if (value is null)
            {
                return "default";
            }

            var valueType = UnderlyingValueType(parameter.Type);
            return valueType.TypeKind == TypeKind.Enum
                ? $"({TypeNames.Of(valueType)})({Primitive(value)})"
                : Primitive(value);
        }

        private static ITypeSymbol UnderlyingValueType(ITypeSymbol type) =>
            type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable
                ? nullable.TypeArguments[0]
                : type;

        private static string Primitive(object value) =>
            value switch
            {
                string text => SymbolDisplay.FormatLiteral(text, quote: true),
                char character => SymbolDisplay.FormatLiteral(character, quote: true),
                bool flag => flag ? "true" : "false",
                float number => Single(number),
                double number => Double(number),
                decimal number => number.ToString(CultureInfo.InvariantCulture) + "M",
                uint number => number.ToString(CultureInfo.InvariantCulture) + "U",
                long number => number.ToString(CultureInfo.InvariantCulture) + "L",
                ulong number => number.ToString(CultureInfo.InvariantCulture) + "UL",
                IFormattable number => number.ToString(null, CultureInfo.InvariantCulture),
                _ => "default",
            };

        private static string Single(float number)
        {
            if (float.IsNaN(number))
            {
                return "float.NaN";
            }

            if (float.IsPositiveInfinity(number))
            {
                return "float.PositiveInfinity";
            }

            return float.IsNegativeInfinity(number)
                ? "float.NegativeInfinity"
                : number.ToString("R", CultureInfo.InvariantCulture) + "F";
        }

        private static string Double(double number)
        {
            if (double.IsNaN(number))
            {
                return "double.NaN";
            }

            if (double.IsPositiveInfinity(number))
            {
                return "double.PositiveInfinity";
            }

            return double.IsNegativeInfinity(number)
                ? "double.NegativeInfinity"
                : number.ToString("R", CultureInfo.InvariantCulture) + "D";
        }
    }
}
