#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;

namespace lilToon
{
    internal static class JsonDictionaryParser
    {
        public static Dictionary<string,string> Deserialize(string json)
        {
            var dic = new Dictionary<string,string>();
            var sr = new StringReader(json);
            if(sr.ReadToNonSpaseChar() != '{') throw new FormatException();
            while(true)
            {
                var k = sr.ReadString();
                if(sr.ReadToNonSpaseChar() != ':') throw new FormatException();
                dic.Add(k, sr.ReadString());
                int c = sr.ReadToNonSpaseChar();
                if(c == ',') continue;
                else if(c == '}') break;
                else throw new FormatException();
            }
            return dic;
        }

        private static string ReadString(this StringReader sr)
        {
            var chars = new List<char>();
            int v;
            if(sr.ReadToNonSpaseChar() != '"') throw new FormatException();
            while(true)
            {
                switch(v = sr.Read())
                {
                    case -1: throw new FormatException();
                    case '"': return new string(chars.ToArray());
                    case '\\':
                        switch(sr.Read())
                        {
                            case '"': chars.Add('"'); break;
                            case '\\': chars.Add('\\'); break;
                            case '/': chars.Add('/'); break;
                            case 'b': chars.Add('\b'); break;
                            case 'f': chars.Add('\f'); break;
                            case 'n': chars.Add('\n'); break;
                            case 'r': chars.Add('\r'); break;
                            case 't': chars.Add('\t'); break;
                            case 'u': chars.Add(sr.ParseCode()); break;
                            default: throw new FormatException();
                        }
                        break;
                    default: chars.Add((char)v); break;
                }
            }
        }

        private static int ReadToNonSpaseChar(this StringReader sr)
        {
            int v;
            while((v = sr.Read()) != -1 && char.IsWhiteSpace((char)v)){}
            return v;
        }

        private static char ParseCode(this StringReader sr)
        {
            char v;
            int code = 0;
            for(int i = 3; i >= 0; i--)
            {
                if(!Uri.IsHexDigit(v = (char)sr.Read())) throw new FormatException();
                code += Uri.FromHex(v) << (4*i);
            }
            return Convert.ToChar(code);
        }
    }
}
#endif
