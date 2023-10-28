using System.Text;
using Utility;

namespace Network
{
    // class for storing result when fetching URL
    public struct LoadUrlResult
    {
        public int StatusCode { get; set; }
        public string? ReasonPhrase { get; set; }
        public string? Body { get; set; }
        public string? ErrorMessage { get; set; }

        public string? Url { get; set; }
        public readonly int ByteCount => Body != null ? Encoding.UTF8.GetByteCount(Body) : 0;
    }

    public class NetworkManager
    {
        private static readonly HttpClient httpClient = new();

        // Takes a URL and stores the response in the form of LoadUrlResult class defined above
        public static async Task<LoadUrlResult> FetchUrlContent(string url)
        {
            // send a GET request and stores it in response
            var response = await httpClient.GetAsync(url);
            // create an instance of LoadUrlResult to store the content
            var result = new LoadUrlResult
            {
                // stores fetched URL
                Url = url,
                // html content is stored in Body
                Body = await response.Content.ReadAsStringAsync(),
                // the status code
                StatusCode = (int)response.StatusCode,
                // ReasonPhrase stores the response messages like "OK", "Not Found"
                ReasonPhrase = response.ReasonPhrase
            };

            return result;
        }

        public static async Task<LoadUrlResult> LoadUrl(string url)
        {

            var result = new LoadUrlResult { Url = url };
            // if URL not valid stores error message
            if (!HtmlUtility.IsValidUrl(url))
            {
                result.ErrorMessage = "Please enter a valid URL";
            }
            try
            {
                // fetches the content
                return await FetchUrlContent(url);
            }
            catch (HttpRequestException e)
            {
                result.ErrorMessage = "Request error" + e.Message;
                return result;
            }
        }
    }
}