using System;

namespace Amenone.CSharpExtension
{
    public static class EnumExtension
    {
        public static bool TryParse<TEnum>(string enumName, out TEnum enumInstance) where TEnum : struct
        {
            return Enum.TryParse(enumName, out enumInstance) && Enum.IsDefined(typeof(TEnum), enumInstance);
        }
    }
}