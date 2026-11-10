using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace RCKit.Ads.Main.Editor
{
    /// <summary>
    /// Minimal JSON parser/serializer (object model: Dictionary&lt;string, object&gt;, List&lt;object&gt;,
    /// string, double, bool, null) used to read/write Packages/manifest.json without an extra package dependency.
    /// </summary>
    public static class MiniJson
    {
        public static object Deserialize(string json)
        {
            if (json == null) return null;
            var index = 0;
            var value = ParseValue(json, ref index);
            return value;
        }

        public static string Serialize(object obj)
        {
            var sb = new StringBuilder();
            SerializeValue(obj, sb, 0);
            return sb.ToString();
        }

        private static object ParseValue(string json, ref int index)
        {
            SkipWhitespace(json, ref index);
            var c = json[index];
            switch (c)
            {
                case '{': return ParseObject(json, ref index);
                case '[': return ParseArray(json, ref index);
                case '"': return ParseString(json, ref index);
                case 't':
                    index += 4;
                    return true;
                case 'f':
                    index += 5;
                    return false;
                case 'n':
                    index += 4;
                    return null;
                default: return ParseNumber(json, ref index);
            }
        }

        private static Dictionary<string, object> ParseObject(string json, ref int index)
        {
            var result = new Dictionary<string, object>();
            index++; // {
            while (true)
            {
                SkipWhitespace(json, ref index);
                if (json[index] == '}')
                {
                    index++;
                    break;
                }

                if (json[index] == ',')
                {
                    index++;
                    continue;
                }

                var key = ParseString(json, ref index);
                SkipWhitespace(json, ref index);
                index++; // :
                var value = ParseValue(json, ref index);
                result[key] = value;
            }

            return result;
        }

        private static List<object> ParseArray(string json, ref int index)
        {
            var result = new List<object>();
            index++; // [
            while (true)
            {
                SkipWhitespace(json, ref index);
                if (json[index] == ']')
                {
                    index++;
                    break;
                }

                if (json[index] == ',')
                {
                    index++;
                    continue;
                }

                result.Add(ParseValue(json, ref index));
            }

            return result;
        }

        private static string ParseString(string json, ref int index)
        {
            SkipWhitespace(json, ref index);
            index++; // opening quote
            var sb = new StringBuilder();
            while (json[index] != '"')
            {
                if (json[index] == '\\')
                {
                    index++;
                    switch (json[index])
                    {
                        case 'n': sb.Append('\n'); break;
                        case 't': sb.Append('\t'); break;
                        case 'r': sb.Append('\r'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'u':
                            var hex = json.Substring(index + 1, 4);
                            sb.Append((char)int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                            index += 4;
                            break;
                    }

                    index++;
                }
                else
                {
                    sb.Append(json[index]);
                    index++;
                }
            }

            index++; // closing quote
            return sb.ToString();
        }

        private static double ParseNumber(string json, ref int index)
        {
            var start = index;
            while (index < json.Length && "+-0123456789.eE".IndexOf(json[index]) != -1)
                index++;
            return double.Parse(json.Substring(start, index - start), CultureInfo.InvariantCulture);
        }

        private static void SkipWhitespace(string json, ref int index)
        {
            while (index < json.Length && char.IsWhiteSpace(json[index]))
                index++;
        }

        private static void SerializeValue(object value, StringBuilder sb, int indent)
        {
            switch (value)
            {
                case null:
                    sb.Append("null");
                    break;
                case string s:
                    SerializeString(s, sb);
                    break;
                case bool b:
                    sb.Append(b ? "true" : "false");
                    break;
                case double d:
                    sb.Append(d.ToString(CultureInfo.InvariantCulture));
                    break;
                case Dictionary<string, object> dict:
                    SerializeObject(dict, sb, indent);
                    break;
                case List<object> list:
                    SerializeArray(list, sb, indent);
                    break;
                default:
                    sb.Append(value);
                    break;
            }
        }

        private static void SerializeObject(Dictionary<string, object> dict, StringBuilder sb, int indent)
        {
            if (dict.Count == 0)
            {
                sb.Append("{}");
                return;
            }

            sb.Append("{\n");
            var childIndent = indent + 1;
            var i = 0;
            foreach (var kvp in dict)
            {
                AppendIndent(sb, childIndent);
                SerializeString(kvp.Key, sb);
                sb.Append(": ");
                SerializeValue(kvp.Value, sb, childIndent);
                if (++i < dict.Count) sb.Append(',');
                sb.Append('\n');
            }

            AppendIndent(sb, indent);
            sb.Append('}');
        }

        private static void SerializeArray(List<object> list, StringBuilder sb, int indent)
        {
            if (list.Count == 0)
            {
                sb.Append("[]");
                return;
            }

            sb.Append("[\n");
            var childIndent = indent + 1;
            for (var i = 0; i < list.Count; i++)
            {
                AppendIndent(sb, childIndent);
                SerializeValue(list[i], sb, childIndent);
                if (i < list.Count - 1) sb.Append(',');
                sb.Append('\n');
            }

            AppendIndent(sb, indent);
            sb.Append(']');
        }

        private static void SerializeString(string s, StringBuilder sb)
        {
            sb.Append('"');
            foreach (var c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default: sb.Append(c); break;
                }
            }

            sb.Append('"');
        }

        private static void AppendIndent(StringBuilder sb, int indent)
        {
            sb.Append(' ', indent * 2);
        }
    }
}
