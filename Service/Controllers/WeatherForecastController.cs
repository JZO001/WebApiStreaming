using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace Service.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
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

            List<WeatherForecast> list = Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToList();

            StreamWriter sw;
            await using ((sw = new StreamWriter(Response.Body)).ConfigureAwait(false))
            {
                foreach (WeatherForecast item in list)
                {
                    // Thread.Sleep simulates a long running process, which generates some kind of output
                    Thread.Sleep(1000);

                    await sw.WriteLineAsync(item.ToString()).ConfigureAwait(false);
                    await sw.FlushAsync().ConfigureAwait(false);
                }
                await sw.WriteLineAsync("[DONE]").ConfigureAwait(false);
            };

        }

    }
}