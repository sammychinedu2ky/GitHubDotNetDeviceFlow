using Octokit;
using Octokit.Internal;
using System.Text.Json;
using System.Diagnostics;
using System.Text;

var token = String.Empty;
var clientId = "Your client ID gotten from GitHub";
var scope = "repo";

var flow = new GitHubDeviceFlow();
var getCodes = await flow.RequestCodes(clientId, scope);

//setting inputs to request for access token
var RequestAccessTokenInput = new[]{
     new KeyValuePair<string, string>("client_id", clientId),
     new KeyValuePair<string, string>("device_code",  Convert.ToString(getCodes["device_code"])),
     new KeyValuePair<string, string>("grant_type",  "urn:ietf:params:oauth:grant-type:device_code")
}; 

//save user_code to clipboard
Process.Start(new ProcessStartInfo
{
    FileName = "Powershell",
    ArgumentList = { $"Set-Clipboard -Value \"{Convert.ToString(getCodes["user_code"])}\"" },
    RedirectStandardOutput = true
});

Console.WriteLine($"Your Code {Convert.ToString(getCodes["user_code"])} has been Copied to Clipboard, So paste it in your browser");

//wait for three seconds before opening browser
await Task.Delay(TimeSpan.FromSeconds(3));

//open browser
Process.Start(new ProcessStartInfo
{
    FileName = "Powershell",
    ArgumentList = { "Start-Process", "msedge", Convert.ToString(getCodes["verification_uri"]) },
});
while (true)
{
    var response = await flow.RequestAccessToken(RequestAccessTokenInput);
    if (response.ContainsKey("error"))
    {
        var delay = Convert.ToDouble(Convert.ToString(getCodes["interval"]));
        await Task.Delay(TimeSpan.FromSeconds(delay));

    }
    else
    {
        Console.WriteLine("Access Token retrieved");
        token = Convert.ToString(response["access_token"]);
        var creds = new InMemoryCredentialStore(new Credentials(token));
        var github = new GitHubClient(new ProductHeaderValue("MyDeviceFlowApp"), creds);
        var createRepo = await github.Repository.Create(new NewRepository(flow.GenerateRandomName()));
        Console.WriteLine(createRepo.CloneUrl);
        break;
    }
}


public class GitHubDeviceFlow
{
    //request for user_code and device_code
    public async Task<IDictionary<string, object>> RequestCodes(string clientId, string scope)
    {
        var requestCodeInput = new[] { new KeyValuePair<string, string>("client_id", clientId), new KeyValuePair<string, string>("scope", scope) };
        var uri = new Uri("https://github.com/login/device/code");
        var response = await MakePostRequest(requestCodeInput, uri);
        return response;
    }

    // request for access token 
    public async Task<IDictionary<string, object>> RequestAccessToken(KeyValuePair<string, string>[] requestInput)
    {
        var uri = new Uri("https://github.com/login/oauth/access_token");
        var response = await MakePostRequest(requestInput, uri);
        return response;
    }

    //helper function to make post request
    private async Task<IDictionary<string, object>> MakePostRequest(KeyValuePair<string, string>[] input, Uri uri)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        var formContent = new FormUrlEncodedContent(input);
        var response = await client.PostAsync(uri, formContent);
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<IDictionary<string, object>>(content)!;
    }

    //Generate a random 6 letter word
    public string GenerateRandomName()
    {
        var stringBuilder = new StringBuilder();
        var random = new Random();
        for (var i = 0; i < 6; i++)
        {
            var randomNumber = random.NextDouble();
            var constraint = Convert.ToInt16(Math.Floor(randomNumber * 25));
            var randomLetter = Convert.ToChar(constraint + 65);
            stringBuilder.Append(randomLetter);
        }
        return stringBuilder.ToString();
    }
}
