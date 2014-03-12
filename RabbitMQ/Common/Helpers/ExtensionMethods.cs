using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
