using Newtonsoft.Json;
using System.Text;

static async Task<float[]?> EncodeText(string text)
{
	var key = Environment.GetEnvironmentVariable("OPENAIKEY");
	var client = new HttpClient();
	var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/embeddings");
	request.Headers.Add("Authorization", "Bearer " + key);
	request.Content = new StringContent("{\"input\": \"" + text + "\", \"model\": \"text-embedding-ada-002\"}", Encoding.UTF8, "application/json");
	var response = await client.SendAsync(request);
	var responseString = await response.Content.ReadAsStringAsync();
	var responseJson = JsonConvert.DeserializeObject<dynamic>(responseString);
	var encodedText = responseJson?.data[0].embedding.ToArray<float>();
	return encodedText;
}

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");
var encoding = await EncodeText("Hello, World!");
