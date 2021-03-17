using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialToHttp
{
    class StringUtils
    {
        public static string DecodeHex(string encoded)
        {
            if (encoded.Length % 2 != 0)
            {
                throw new ArgumentException("Hexadecimal encoded string length must be a multiple of 2");
            }
            StringBuilder buffer = new StringBuilder();
            for (int i = 0; i < encoded.Length; i += 2)
            {
                buffer.Append((char) int.Parse(encoded.Substring(i, 2), System.Globalization.NumberStyles.HexNumber));
            }
            return buffer.ToString();
        }
    }
}
