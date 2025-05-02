using GPTProject.Core.ChatBot.LLMMemory;
using GPTProject.Providers.Dialogs.Implementations;
using QdrantExpansion.Models;
using QdrantExpansion.Repository;
using QdrantExpansion.Services;

//var repo = new QdrantRepository();

//await repo.CreateCollectionAsync("test", 2, DistanceType.Cosine);

/*var points = new List<VectorPoint>
{
	new()
	{
		Id = 1,
		Vector = [1f, 1f],
		Payload = {["test"] = "Test payload1"}
	},
	new()
	{
		Id = 2,
		Vector = [1f, 0f],
		Payload = {["test"] = "Test payload2"}
	},
	new()
	{
		Id = 3,
		Vector = [0f, 1f],
		Payload = {["test"] = "Test payload3"}
	},
	new()
	{
		Id = 4,
		Vector = [0.5f, 1f],
		Payload = { ["test"] = "Test payload4" }
	}
};
await repo.UpsertPointsAsync("test", points);*/

/*var results = await repo.SearchAsync("test", [1f, 1f], 0.7f, 10);

Console.WriteLine(results.Count());

foreach (var result in results)
{
	Console.WriteLine(result.Payload["test"]);
}*/

//await repo.DeletePayloadAsync("test", "test", 4);

//await repo.DeletePointAsync("test", 4);

/*var vect = new DefaultVectorizer();

var result = await vect.GetEmbeddingAsync(request);*/

//var a = await repo.GetAllCollectionsInfoAsync();

/*var request = new VectorizerRequest() { Key = "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", Url = "https://api.openai.com/v1/embeddings", Encoding_format = "float", Model = "text-embedding-3-small" };
var service = new DefaultQdrantService("realTestCollection", request);

var payloads = new List<Payload>()
{
	new()
	{
		Text = "the first!",
		Importance = 0
	},

	new()
	{
		Text = "the second!",
		Importance = 0
	},

	new()
	{
		Text = "the third!",
		Importance = 0
	},

	new()
	{
		Text = "dog!",
		Importance = 0
	},

	new()
	{
		Text = "cat",
		Importance = 0
	},

	new()
	{
		Text = "black sea",
		Importance = 0
	}
};

await service.DeleteCollectionAsync();

await service.CreateCollectionAsync(1536);

await service.UpsertStringsAsync(payloads);

var results = await service.FindClosestAsync("animal that loves bones and treats", 0.4f);

foreach (var item in results)
{
	Console.WriteLine(item["text"]);
}*/

var dialogueAgent = new DialogueAgent(new ChatGPTDialog());
dialogueAgent.Run(() => Task.FromResult(Console.ReadLine()));

