using System.Text.RegularExpressions;

namespace ABSystem.Editor
{
    /// <summary>
    /// AB 代码生成共用工具。
    /// </summary>
    public static class ABCodeGenUtil
    {
        /// <summary>
        /// 将任意字符串转换为合法 C# 标识符。
        /// </summary>
        public static string ToIdentifier(string raw, string fallback)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return fallback;

            var cleaned = Regex.Replace(raw.Trim(), @"[^\w]", "_");
            cleaned = Regex.Replace(cleaned, @"_+", "_").Trim('_');
            if (string.IsNullOrEmpty(cleaned))
                return fallback;

            if (char.IsDigit(cleaned[0]))
                cleaned = "_" + cleaned;

            return cleaned;
        }

        /// <summary>
        /// 转义写入 C# 字符串字面量的内容。
        /// </summary>
        public static string EscapeCSharp(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"");
        }

        /// <summary>
        /// 转义写入 XML 文档注释的内容。
        /// </summary>
        public static string EscapeXml(string value)
        {
            return (value ?? string.Empty)
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
        }
    }
}
