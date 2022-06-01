using System.Collections.Generic;
using System.Linq;

namespace StellarisPatcher
{
    public static class StringExtension
    {
        public static string GetLast(this string source, int tail_length)
        {
            if(tail_length >= source.Length)
                return source;
            return source.Substring(source.Length - tail_length);
        }

        public static string StrJoin(this IEnumerable<string> strings, string separator = " ")
        {
            return string.Join(separator, strings);
        }
        
        public static string MakeBytesStr(this byte[] arr, int start, int len)
        {
            return arr.Skip(start).Take(len).Select(x => x.ToString("X").PadLeft(2, '0')).StrJoin();
        }
    }
}