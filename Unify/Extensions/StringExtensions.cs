#if NET
using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace Unify.Extensions;

public static class StringExtensions
{
    public static string ToSafeFilename(this string str, string replacement = "_")
    {
		if(string.IsNullOrWhiteSpace(str))
		{
			return string.Empty;
		}
		
        if (str.Trim() != string.Empty)
        {
            str = str.ReplaceSpaceCharacters();
            RemoveInvalidChars.Replace(str, replacement);
        }

        return str;
    }

    public static string RemoveLineBreaks(this string str)
    {
        str = str.Replace(Environment.NewLine, "");
        str = str.Replace("\r", "");
        str = str.Replace("\n", "");
        return str;
    }

    public static string ReplaceNewLineWithBr(this string str)
    {
        return str.Replace(Environment.NewLine, "<br/>").RemoveLineBreaks();
    }

    public static string StripHtml(this string str)
    {
        str = WebUtility.HtmlDecode(str);
        str = Regex.Replace(str, @"\<[^\>]*\>", string.Empty, RegexOptions.Compiled);
        return str;
    }

    public static string StripHTMLFromstr(this string str)
    {
        str = WebUtility.HtmlDecode(str);
        str = str.RemoveLineBreaks();
        str = str.StripHtml();
        str = str.RemoveDoubleSpaceCharacters();
        str = str.Trim();
        str = str.RemoveQuotationMarks();

        return str;
    }

	public static string ReplaceSpaceCharacters(this string str, string replacement = "_")
	{
		return Regex.Replace(str, @"\s+", replacement);
	}

    public static string RemoveDoubleSpaceCharacters(this string str)
    {
        return Regex.Replace(str, "[ ]+", " ");
    }

    public static string CutLongString(this string str, int length, string ellipsis = "...")
    {
        if (str.Trim() == string.Empty) return str;
        if (str.Length > length)
        {
            str = str[..length];
            var positionLastSpace = str.LastIndexOf(" ", StringComparison.Ordinal);
            if (positionLastSpace > -1 && positionLastSpace < length)
            {
                str = str[..positionLastSpace];
            }
        }
        str += ellipsis;
        return str;
    }

    public static string RemoveQuotationMarks(this string str)
    {
        const char singleQuotationMark = (char)39;
        str = str.Replace((char)34, singleQuotationMark);   // "
        str = str.Replace((char)168, singleQuotationMark);  // ¨
        str = str.Replace((char)8220, singleQuotationMark); // “
        str = str.Replace((char)8221, singleQuotationMark); // ”
        str = str.Replace((char)8222, singleQuotationMark); // „

        return str;
    }

    public static int CountChars(this string str, char toFind)
    {
        var count = 0;
        foreach (var c in str.AsSpan())
        {
            if (c == toFind)
                count++;
        }
        return count;
    }

    public static string ToPermaLink(this string str, int length = 100)
    {
        if (string.IsNullOrWhiteSpace(str))
            return str;
        
        // Remove any leading or trailing whitespace
        str = str.Trim();

        // Convert the title to lowercase
        str = str.ToLowerInvariant();

        // Replace spaces and special characters with dashes
        str = Regex.Replace(str, @"[^a-z0-9\s-]", string.Empty);
        str = Regex.Replace(str, @"\s+", "-").Trim();

        // Remove consecutive dashes
        str = Regex.Replace(str, @"-+", "-");

        // Trim the title to a maximum length of 100 characters
        if (str.Length > length)
        {
            str = str[..length].TrimEnd('-');
        }

        // Encode the title using URL encoding
        str = Uri.EscapeDataString(str);

        return str;
    }

    public static string ToNonBreakingSubstring(this string str, int startIndex, int length)
    {
        var endIndex = startIndex + length;
        while (endIndex < str.Length && str[endIndex] != ' ')
        {
            endIndex--;
        }

        return str.Substring(startIndex, endIndex - startIndex);
    }


    private static readonly Regex RemoveInvalidChars = new(
        $"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()))}]",
        RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.CultureInvariant);
}
#endif