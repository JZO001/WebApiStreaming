# WebApi Streaming
How to get data as a stream from a WebAPI (.NET)

A remote web API can be consumed as a stream, which can take longer,
depends an other server process, and the necessary time to generate
the requested content.

It is a real scenario, if we get event, data pieces from a services, etc.


In this project I will demonstrate, how we can use a streaming API,
how we can iterate through on the incoming dataset with the IAsyncEnumerable interface.


## The server side, using a controller

This controller generates a set of weather data, than send the dataset
back to the client as a stream. Every data item serialized into Json format,
and finally it sends '[DONE]' string to indicate, the streaming is over.


```c#
[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", 
        "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;

    public WeatherForecastController(ILogger<WeatherForecastController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public async Task GetAsync()
    {
        Response.StatusCode = 200;
        Response.ContentType = "text/html";

        List<WeatherForecast> list = Enumerable.Range(1, 5)
            .Select(index => new WeatherForecast
        {
            Date = DateTime.Now.AddDays(index),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })
        .ToList();

        StreamWriter sw;
        await using ((sw = new StreamWriter(Response.Body))
            .ConfigureAwait(false))
        {
            foreach (WeatherForecast item in list)
            {
                // Thread.Sleep simulates a long running process, 
                // which generates some kind of output
                Thread.Sleep(1000);

                await sw.WriteLineAsync(item.ToString()).ConfigureAwait(false);
                await sw.FlushAsync().ConfigureAwait(false);
            }
            await sw.WriteLineAsync("[DONE]").ConfigureAwait(false);
        };

    }

}
```


## The client side #1, implementing the caller API

On the client side, we need a caller code, which can read and handle the incoming data.
Check out the 'yield' instruction when passing every incoming data item towards the API caller.
If the code detects the '[DONE]' maker, the reading ends.


```c#
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
```


## The client side #2, using the client side API

Finally, a code can use the Api class to acquire the data, using IAsyncEnumerable<string> on a proper way.

This code iterates on the incoming data asynchronously and display the given data as a text, 
one by one, after a data item arrived. This approch does not wait to arrive all of the data,
it immediatelly displays what we got from the server.


```c#
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
```
