bool shouldFail = true;
string functionUrl = "http://localhost:7071/api/HttpStart";
var client = new HttpClient();

Console.WriteLine("Press ENTER to start...");
Console.ReadLine();

Console.WriteLine("Sending request...");
HttpResponseMessage response = await client.PostAsync(functionUrl, new StringContent(shouldFail.ToString()));
Console.WriteLine("Request sent.");

if (!response.IsSuccessStatusCode)
{
    Console.WriteLine($"Request failed with HTTP {response.StatusCode} {response.ReasonPhrase}.");
}

bool shouldPollForResult;
string statusUrl = response.Headers.Location!.ToString();
do
{
    Console.WriteLine("Polling for response...");
    response = await client.GetAsync(statusUrl);

    shouldPollForResult = response.StatusCode == System.Net.HttpStatusCode.Accepted;
    if (shouldPollForResult)
    {
        Console.WriteLine("Still running. Another polling scheduled.");
        Thread.Sleep(5000);
    }
} while (shouldPollForResult);

if (response.StatusCode == System.Net.HttpStatusCode.OK)
{
    Console.WriteLine($"Successfully finished with result {await response.Content.ReadAsStringAsync()}.");
}
else
{
    Console.WriteLine($"Failed with HTTP {(int)response.StatusCode} {response.ReasonPhrase}.");
}