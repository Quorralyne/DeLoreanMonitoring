using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DeLoreanMonitoring
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<DeLoreanDataGeneratorService>();
                    services.AddHttpClient();
                })
                .Build();

            await host.RunAsync();
        }
    }

    public class DeLoreanDataGeneratorService : BackgroundService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<DeLoreanDataGeneratorService> _logger;
        private readonly string _influxUrl;
        private readonly string _influxToken;
        private readonly string _influxOrg;
        private readonly string _influxBucket;
        
        // Simulated data parameters
        private double _currentVelocity = 0;
        private double _powerLevel = 0;
        private double _temperature = 70;
        private double _plutoniumLevel = 85;  // New parameter for Mr. Fusion
        private double _circuitVoltage = 12.0; // New parameter for Time Circuits
        private bool _circuitsActive = true;   // Track if time circuits are active
        private string _currentLocation = "Hill Valley";
        private DateTime _targetDate = new DateTime(1985, 10, 26);
        private readonly Random _random = new Random();
        
        // For cardinality demo
        private readonly string[] _deloreanIds = { "DMC-001", "DMC-002", "DMC-003", "DMC-004", "DMC-005" };
        private readonly string[] _locations = { "Hill Valley", "Twin Pines Mall", "Courthouse Square", "Lyon Estates", "Hilldale" };
        private readonly string[] _timePeriods = { "1955", "1985", "2015", "1885" };
        private readonly string[] _drivers = { "Doc Brown", "Marty McFly", "Jennifer Parker", "Biff Tannen", "George McFly" };

        public DeLoreanDataGeneratorService(
            IHttpClientFactory httpClientFactory,
            ILogger<DeLoreanDataGeneratorService> logger,
            IConfiguration configuration)
        {
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
            
            // Replace with your actual values
            _influxUrl = "https://us-east-1-1.aws.cloud2.influxdata.com/api/v2/write";
            _influxToken = "Csc7reO6J1ozs8JxA2QNjC2drwAaXMBTG9_b2Kl0lNmxIfAyfRcMCjRwf2t83MfHqySHKfz3SFKcmEHMb7aNBQ==";
            _influxOrg = "194b35e60685e18d";
            _influxBucket = "gigawatt_metrics";
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DeLorean Data Generator Service starting at: {time}", DateTimeOffset.Now);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await GenerateAndSendData();
                    await Task.Delay(5000, stoppingToken); // Generate data every 5 seconds
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating or sending data");
                }
            }
        }

        private async Task GenerateAndSendData()
        {
            var timestamp = DateTimeOffset.UtcNow;
            var lines = new List<string>();

            // Generate basic metrics for demo
            UpdateSimulatedValues();
            
            // Basic DeLorean metrics (low cardinality)
            lines.Add($"delorean_stats,device=flux_capacitor power_level={_powerLevel} {timestamp.ToUnixTimeMilliseconds() * 1000000}");
            lines.Add($"delorean_stats,device=speedometer velocity_mph={_currentVelocity} {timestamp.ToUnixTimeMilliseconds() * 1000000}");
            lines.Add($"delorean_stats,device=engine temperature_f={_temperature} {timestamp.ToUnixTimeMilliseconds() * 1000000}");
            lines.Add($"delorean_stats,device=navigation current_location=\"{_currentLocation}\" {timestamp.ToUnixTimeMilliseconds() * 1000000}");
            
            // New metrics for the enhanced dashboard
            lines.Add($"delorean_stats,device=mr_fusion plutonium_level={_plutoniumLevel} {timestamp.ToUnixTimeMilliseconds() * 1000000}");
            lines.Add($"delorean_stats,device=time_circuits voltage={_circuitVoltage} {timestamp.ToUnixTimeMilliseconds() * 1000000}");
            lines.Add($"delorean_stats,device=time_circuits status=\"{(_circuitsActive ? "active" : "inactive")}\" {timestamp.ToUnixTimeMilliseconds() * 1000000}");
            
            // High cardinality data - generate many unique tag combinations
            for (int i = 0; i < 20; i++)
            {
                string deloreanId = _deloreanIds[_random.Next(_deloreanIds.Length)];
                string location = _locations[_random.Next(_locations.Length)];
                string timePeriod = _timePeriods[_random.Next(_timePeriods.Length)];
                string driver = _drivers[_random.Next(_drivers.Length)];
                double fuelLevel = Math.Round(_random.NextDouble() * 100, 2);
                double temporalStability = Math.Round(75 + _random.NextDouble() * 25, 2);
                
                // This creates high cardinality with many tag combinations
                lines.Add($"delorean_detailed,id={deloreanId},location={location},time_period={timePeriod},driver={driver} " +
                          $"fuel_level={fuelLevel},temporal_stability={temporalStability},velocity={Math.Round(_random.NextDouble() * 90, 1)} " +
                          $"{timestamp.AddMilliseconds(i * 100).ToUnixTimeMilliseconds() * 1000000}");
            }
            
            // Join all lines and send to InfluxDB
            string payload = string.Join("\n", lines);
            await SendToInfluxDB(payload);
            
            _logger.LogInformation($"Generated {lines.Count} data points at {DateTimeOffset.Now}");
        }
        
        private void UpdateSimulatedValues()
        {
            // Simulate time travel event if velocity approaching 88 mph
            if (_currentVelocity > 85)
            {
                _currentVelocity = _random.NextDouble() * 30; // Slow down after time travel
                _powerLevel = _random.NextDouble() * 20;      // Power drain after time travel
                _plutoniumLevel = Math.Max(0, _plutoniumLevel - (_random.NextDouble() * 15)); // Consume plutonium during time travel
                
                // Simulate time circuit instability during time travel
                _circuitVoltage = 8.0 + (_random.NextDouble() * 3.0);
                
                // Small chance time circuits might fail after time travel
                _circuitsActive = _random.NextDouble() > 0.1;
                
                _currentLocation = _locations[_random.Next(_locations.Length)];
                _targetDate = _targetDate.AddYears(_random.Next(-100, 100));
                
                _logger.LogInformation("⚡ TIME TRAVEL EVENT DETECTED! ⚡");
            }
            else
            {
                // Otherwise make small adjustments to current values
                _currentVelocity = Math.Min(88, _currentVelocity + (_random.NextDouble() * 10 - 3));
                _powerLevel = Math.Min(100, _powerLevel + (_random.NextDouble() * 10 - 3));
                
                // Gradually decrease plutonium level with normal operation
                _plutoniumLevel = Math.Max(0, _plutoniumLevel - (_random.NextDouble() * 0.5));
                
                // If plutonium gets too low, simulate a refueling
                if (_plutoniumLevel < 10 && _random.NextDouble() > 0.7)
                {
                    _plutoniumLevel = 85 + (_random.NextDouble() * 15);
                    _logger.LogInformation("Mr. Fusion Refueled!");
                }
                
                // Time circuits voltage fluctuations
                if (_circuitsActive)
                {
                    // Normal voltage is around 12-13V with minor fluctuations
                    _circuitVoltage = 12.0 + (_random.NextDouble() * 1.0 - 0.5);
                    
                    // Randomly simulate a voltage spike or drop
                    if (_random.NextDouble() > 0.9)
                    {
                        if (_random.NextDouble() > 0.5)
                        {
                            // Voltage spike
                            _circuitVoltage = 13.5 + (_random.NextDouble() * 2.0);
                        }
                        else
                        {
                            // Voltage drop
                            _circuitVoltage = 10.0 - (_random.NextDouble() * 1.5);
                        }
                    }
                }
                else
                {
                    // Circuits inactive - low voltage with minor noise
                    _circuitVoltage = 1.0 + (_random.NextDouble() * 0.5);
                    
                    // Chance to reactivate circuits
                    if (_random.NextDouble() > 0.8)
                    {
                        _circuitsActive = true;
                        _logger.LogInformation("Time Circuits Activated!");
                    }
                }
                
                // Occasionally spike power when approaching 88 mph
                if (_currentVelocity > 80)
                {
                    _powerLevel = 121.0; // 1.21 gigawatts!
                }
                
                _temperature = Math.Max(60, Math.Min(250, _temperature + (_random.NextDouble() * 8 - 4)));
            }
        }

        private async Task SendToInfluxDB(string lineProtocolData)
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, 
                    $"{_influxUrl}?org={_influxOrg}&bucket={_influxBucket}&precision=ns");
                
                request.Headers.Add("Authorization", $"Token {_influxToken}");
                request.Content = new StringContent(lineProtocolData, Encoding.UTF8, "text/plain");
                
                var response = await _httpClient.SendAsync(request);
                
                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Failed to send data to InfluxDB: {response.StatusCode}, {errorContent}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while sending data to InfluxDB");
            }
        }
    }
}