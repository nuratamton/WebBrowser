using System.Threading.Tasks;
using System.Net.Http;
using Utility;

namespace Network
{
    public class LoadUrlResult
    {
        public int StatusCode { get; set; }
        public string? ReasonPhrase { get; set; }
        public string? Body { get; set; }
        public string? ErrorMessage { get; set; }
    }
    public class NetworkManager
    {
        private static readonly HttpClient httpClient = new();
        public static async Task<LoadUrlResult> LoadUrl(string url)
        {
            
            var result = new LoadUrlResult();
            try
            {
                HtmlUtility.IsValidUrl(url);
                var response = await httpClient.GetAsync(url);
                
                result.Body = await response.Content.ReadAsStringAsync();
                result.StatusCode = (int)response.StatusCode;
                result.ReasonPhrase = response.ReasonPhrase;

                return result;
                
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request error: {e.Message}");
                result.ErrorMessage = $"Request error: {e.Message}";
                return result;
            }

            catch (UriFormatException)
            {
                result.ErrorMessage = "Please enter a valid URL.";
                return result;
            }


        }
    }
}