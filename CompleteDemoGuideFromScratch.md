# **Demo Script: DeLorean Time Series Monitoring System**

## **Introduction to Demo**

"Now I'd like to show you our DeLorean monitoring system in action. This demo brings together everything we've been discussing about time series data \- high cardinality management, efficient writing with Line Protocol, and scalable querying with SQL.

Let me walk you through both the code generating our time series data and the dashboard visualizing it."

## **Part 1: Program.cs Walkthrough**

"Let's first look at the data generator behind our dashboard \- the Program.cs file in our .NET application."

### **Key Time Series Components**

#### Service Setup

// Point out these lines in the code

| public class DeLoreanDataGeneratorService : BackgroundService{    private readonly HttpClient \_httpClient;    private readonly ILogger\<DeLoreanDataGeneratorService\> \_logger;    private readonly string \_influxUrl;    private readonly string \_influxToken;    private readonly string \_influxOrg;    private readonly string \_influxBucket; |
| :---- |

"Our DeLorean monitoring system uses a background service that continuously generates and sends time series data to InfluxDB. Notice how we set up the connection parameters including the InfluxDB URL, auth token, organization, and bucket name. These are essential for any time series database connection."

#### Simulated Data Parameters

// Direct attention to this section

| private double \_currentVelocity \= 0;private double \_powerLevel \= 0;private double \_temperature \= 70;private double \_plutoniumLevel \= 85;  *// New parameter for Mr. Fusion*private double \_circuitVoltage \= 12.0; *// New parameter for Time Circuits*private bool \_circuitsActive \= true;   *// Track if time circuits are active*private string \_currentLocation \= "Hill Valley";private DateTime \_targetDate \= new DateTime(1985, 10, 26);private readonly Random \_random \= new Random(); |
| :---- |

"For our demo, we're simulating several key metrics of our DeLorean time machine \- velocity, power levels, temperature, and more. In a real-world application, these would be actual sensor readings or application metrics. Each of these represents a distinct time series that we'll track over time."

#### Cardinality Management

// Highlight these arrays

| private readonly string\[\] \_deloreanIds \= { "DMC-001", "DMC-002", "DMC-003", "DMC-004", "DMC-005" };private readonly string\[\] \_locations \= { "Hill Valley", "Twin Pines Mall", "Courthouse Square", "Lyon Estates", "Hilldale" };private readonly string\[\] \_timePeriods \= { "1955", "1985", "2015", "1885" };private readonly string\[\] \_drivers \= { "Doc Brown", "Marty McFly", "Jennifer Parker", "Biff Tannen", "George McFly" }; |
| :---- |

"Here's where cardinality comes into play. We're defining several dimensions that could combine to create high cardinality. With 5 DeLorean IDs, 5 locations, 4 time periods, and 5 drivers, we could generate up to 500 unique combinations. This demonstrates how quickly cardinality can explode with multiple tag dimensions."

#### Data Generation Logic

// Point to the execution loop

| protected override async Task ExecuteAsync(CancellationToken stoppingToken){    \_logger.LogInformation("DeLorean Data Generator Service starting at: {time}", DateTimeOffset.Now);    while (\!stoppingToken.IsCancellationRequested)    {        try        {            await GenerateAndSendData();            await Task.Delay(5000, stoppingToken); *// Generate data every 5 seconds*        }        catch (Exception ex)        {            \_logger.LogError(ex, "Error generating or sending data");        }    }} |
| :---- |

"This is our main execution loop that runs every 5 seconds. For time series data, the collection interval is crucial \- too frequent and you may overwhelm storage; too infrequent and you might miss important patterns. For our DeLorean metrics, 5-second intervals provide a good balance."

#### Line Protocol Formation

// Highlight this section of GenerateAndSendData()  
// Basic DeLorean metrics (low cardinality)

| lines.Add($"delorean\_stats,device=flux\_capacitor power\_level={\_powerLevel} {timestamp.ToUnixTimeMilliseconds() \* 1000000}");lines.Add($"delorean\_stats,device=speedometer velocity\_mph={\_currentVelocity} {timestamp.ToUnixTimeMilliseconds() \* 1000000}");lines.Add($"delorean\_stats,device=engine temperature\_f={\_temperature} {timestamp.ToUnixTimeMilliseconds() \* 1000000}");lines.Add($"delorean\_stats,device=navigation current\_location=\\"{\_currentLocation}\\" {timestamp.ToUnixTimeMilliseconds() \* 1000000}"); |
| :---- |

"This is one of the most important parts of our code \- creating the Line Protocol format we discussed earlier. Each line follows the pattern: `measurement,tag_key=tag_value field_key=field_value timestamp`

Notice how we're structuring our data with a consistent measurement name 'delorean\_stats' and using 'device' as a tag key to differentiate metrics. This is a best practice that makes querying much more efficient, as we can filter by this tag.

The timestamp at the end is in nanoseconds since Unix epoch, which is InfluxDB's highest precision. We're generating this by converting milliseconds and multiplying by 1,000,000."

#### High Cardinality Data Generation

// Show the high-cardinality data generation code

| for (int i \= 0; i \< 20; i++){    string deloreanId \= \_deloreanIds\[\_random.Next(\_deloreanIds.Length)\];    string location \= \_locations\[\_random.Next(\_locations.Length)\];    string timePeriod \= \_timePeriods\[\_random.Next(\_timePeriods.Length)\];    string driver \= \_drivers\[\_random.Next(\_drivers.Length)\];    double fuelLevel \= Math.Round(\_random.NextDouble() \* 100, 2);    double temporalStability \= Math.Round(75 \+ \_random.NextDouble() \* 25, 2);        *// This creates high cardinality with many tag combinations*    lines.Add($"delorean\_detailed,id={deloreanId},location={location},time\_period={timePeriod},driver={driver} " \+              $"fuel\_level={fuelLevel},temporal\_stability={temporalStability},velocity={Math.Round(\_random.NextDouble() \* 90, 1)} " \+              $"{timestamp.AddMilliseconds(i \* 100).ToUnixTimeMilliseconds() \* 1000000}");} |
| :---- |

"Here's where we deliberately create high cardinality data. For each data generation cycle, we create 20 data points with randomly selected combinations of DeLorean ID, location, time period, and driver as tags.

This results in many unique tag combinations \- exactly the high cardinality challenge we discussed earlier. Notice that we're using a separate measurement name 'delorean\_detailed' to isolate this high-cardinality data from our primary metrics.

Also, look at how we're slightly offsetting each timestamp by adding milliseconds. This creates a more realistic pattern of data points arriving with small time differences, rather than all at exactly the same moment."

#### Batch Writing Implementation

// Highlight the batch writing code

| string payload \= string.Join("\\n", lines);await SendToInfluxDB(payload); |
| :---- |

"Here's our batch writing implementation. Instead of sending each line separately, we join them with newlines and send them as a single HTTP request. This batch approach significantly reduces network overhead and is essential for high-throughput time series data collection."

#### InfluxDB Write API Call

// Show the SendToInfluxDB method

| private async Task SendToInfluxDB(string lineProtocolData){    try    {        HttpRequestMessage request \= new HttpRequestMessage(HttpMethod.Post,             $"{\_influxUrl}?org={\_influxOrg}\&bucket={\_influxBucket}\&precision=ns");                request.Headers.Add("Authorization", $"Token {\_influxToken}");        request.Content \= new StringContent(lineProtocolData, Encoding.UTF8, "text/plain");                var response \= await \_httpClient.SendAsync(request);                if (\!response.IsSuccessStatusCode)        {            string errorContent \= await response.Content.ReadAsStringAsync();            \_logger.LogError($"Failed to send data to InfluxDB: {response.StatusCode}, {errorContent}");        }    }    catch (Exception ex)    {        \_logger.LogError(ex, "Exception while sending data to InfluxDB");    }} |
| :---- |

"Finally, here's how we send the data to InfluxDB using the HTTP API. Note a few important details:

1. We're specifying 'ns' (nanosecond) precision in the URL  
2. Authentication is handled with a token in the Authorization header  
3. The content type is 'text/plain' for Line Protocol data  
4. We have error handling to catch and log any failures

This robust implementation ensures our time series data reliably makes it into InfluxDB, even at high volumes."

## **Part 2: Grafana Dashboard and SQL Queries**

"Now let's look at the Grafana dashboard and the SQL queries behind each panel. This demonstrates the 'reading with SQL' aspect of our key takeaways."

### **Flux Capacitor Power Panel**

"This gauge shows the current power level of the flux capacitor \- vital for time travel. Here's the SQL query powering it:"

| SELECT time, power\_level FROM "delorean\_stats" WHERE device \= 'flux\_capacitor' AND time \>= now() \- interval '15 minutes' |
| :---- |

"This query demonstrates several best practices for time series SQL:

1. We're selecting only the specific fields we need (time and power\_level)  
2. We're filtering by the 'device' tag to target only flux capacitor data  
3. Most importantly, we're using a time filter to limit the query to the last 15 minutes

That time filter is crucial for performance \- without it, the database would need to scan all historical data, which could be enormous in a real-world application."

### **DeLorean Velocity Chart**

"This time series chart shows our velocity over time, with the critical 88 mph threshold marked. The SQL query is:"

| SELECT time, velocity\_mph FROM "delorean\_stats" WHERE device \= 'speedometer' AND time \>= now() \- interval '15 minutes' |
| :---- |

"Again, notice the tight time filter of 15 minutes. For a real-time dashboard, we typically only need recent data. In Grafana, this time range can be adjusted by the user, but having a default filter in the query provides a performance safety net."

### **Time Circuits Power Stability**

"This chart tracks voltage fluctuations in our time circuits \- essential for accurate temporal navigation. The query is:"

| SELECT time, voltage FROM "delorean\_stats" WHERE device \= 'time\_circuits' AND time \>= now() \- interval '15 minutes' |
| :---- |

"The consistency across these queries shows the power of our data model. By using 'device' as a tag, we can easily filter for specific components of our DeLorean. This is much more efficient than using separate measurements for each device type."

### **Mr. Fusion Fuel Level**

"This gauge shows the current plutonium level in our Mr. Fusion reactor:"

| SELECT time, plutonium\_level FROM "delorean\_stats" WHERE device \= 'mr\_fusion' AND time \>= now() \- interval '15 minutes' |
| :---- |

"Again, we see the same pattern of targeted field selection, tag filtering, and time bounding."

## **High Cardinality Query Example**

"Let me show you how we could query our high-cardinality data. This isn't on our dashboard, but demonstrates important concepts:"

| SELECT time, fuel\_level, temporal\_stability FROM "delorean\_detailed" WHERE id \= 'DMC-001'   AND time \>= now() \- interval '1 hour'GROUP BY time(5m), driver |
| :---- |

"This query demonstrates how to handle high cardinality data efficiently:

1. We're filtering on a specific ID to narrow the scope dramatically  
2. We're using a tight time window of 1 hour  
3. We're grouping by 5-minute intervals to reduce the data points returned  
4. We're including 'driver' in the GROUP BY to maintain that dimension in our results

Without these strategies, querying high-cardinality data could overwhelm both the database and visualization."

## **Time Series Patterns in the Dashboard**

"Looking at our dashboard as a whole, we can see several time series patterns in action:

1. **Current State** \- Gauges showing present values like power level and fuel level  
2. **Trends** \- The velocity and voltage charts showing how values change over time  
3. **Thresholds** \- The critical 88 mph line in our velocity chart marking the time travel threshold  
4. **Anomalies** \- Spikes in power when we approach 88 mph  
5. **Correlations** \- How engine temperature relates to velocity increases

These patterns demonstrate the multi-dimensional nature of time series analysis \- it's not just about individual values, but how they change, relate, and indicate events over time."

## **Closing the Demo**

"Our DeLorean monitoring system brings together all the key concepts we've discussed:

1. Efficient writing with Line Protocol in our .NET application  
2. Reading with SQL in our Grafana dashboard  
3. Managing high cardinality with smart data modeling and query practices  
4. Scaling to handle thousands or millions of data points with consistent performance

The same architecture and techniques we're using for our fictional DeLorean can be applied to real-world scenarios like application monitoring, IoT device tracking, financial analysis, and of course, agricultural automation."

