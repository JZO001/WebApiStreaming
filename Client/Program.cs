using System.Text.Json;
using System.Text;

namespace Client
{

    internal class Program
    {

        static async Task Main(string[] args)
        {
            Console.WriteLine("Press a key to start");
            Console.ReadKey();

            Api api = new Api();
            await foreach (string line in api.StreamedAsync(CancellationToken.None))
            {
                Console.WriteLine(line);
            }

            Console.WriteLine("[DONE] press a key to exit");
            Console.ReadKey();
        }

    }

    public class Api
    {

        public async IAsyncEnumerable<string> StreamedAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://localhost:7176/weatherforecast"))
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    //httpClient.Timeout = Timeout.InfiniteTimeSpan;
                    using (HttpResponseMessage response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            using (Stream contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                            {
                                using (StreamReader reader = new StreamReader(contentStream))
                                {
                                    string? line = null;

                                    while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null &&
                                        !cancellationToken.IsCancellationRequested)
                                    {
                                        if ("[DONE]".Equals(line.Trim())) break;

                                        if (!string.IsNullOrWhiteSpace(line))
                                        {
                                            yield return line;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            string? jsonResult = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                            yield return jsonResult;
                        }
                    }
                }
            }
        }

    }

}