using System;

namespace Common
{
    public static class ExtensionMethods
    {
        public static string ToPrintable(this Guid guid)
        {
            return guid.ToString().Substring(30);
        }
    }
}