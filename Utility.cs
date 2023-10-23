using System;
namespace Utility
{
    public class HtmlUtility
    {
        public static string ExtractTitle(string? html)
        {
            if (html == null)
            {
                return "";
            }
            // constants for the starting and ending tags for the html file
            const string startTag = "<title>";
            const string endTag = "</title>";

            // finding the starting and ending position of title tag
            // StringComparison.OrdinalIgnoreCase ensures that the search is case-insensitive
            int startTitleIndex = html.IndexOf(startTag, StringComparison.OrdinalIgnoreCase);
            int endTitleIndex = html.IndexOf(endTag, StringComparison.OrdinalIgnoreCase);

            // if starting or ending tag is not found then return empty string
            if (startTitleIndex == -1 || endTitleIndex == -1)
                return "";

            // starting position of the title will be after the start tag
            startTitleIndex += startTag.Length;

            // returns from stating position upto the ending index position 
            return html[startTitleIndex..endTitleIndex];
        }

        public static bool IsValidUrl(string urlString)
        {
            bool result = Uri.TryCreate(urlString, UriKind.Absolute, out Uri? uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            return result;
        }

    }
}