using System.Text.Json;
using ActivityRag.Models;
using OpenAI.Chat;

string filePath = Path.Combine("Data", "activities.json");

if (!File.Exists(filePath))
{
    Console.WriteLine($"activities.jsonが見つかりません");
    return;
}
string jsonText = File.ReadAllText(filePath);

JsonSerializerOptions options = new JsonSerializerOptions()
{
    PropertyNameCaseInsensitive = true
};

List<ActivityRecord>? activities = JsonSerializer.Deserialize<List<ActivityRecord>>(jsonText, options);

if (activities is null || activities.Count == 0)
{
    Console.WriteLine("活動データがありません。");
    return;
}

foreach(ActivityRecord activity in activities)
{
    Console.WriteLine($"ID: {activity.Id}");
    Console.WriteLine($"活動名: {activity.Title}");
    Console.WriteLine($"内容: {activity.Description}");
    Console.WriteLine($"状態: {activity.Status}");
    Console.WriteLine();
}

Console.WriteLine($"活動データを{activities.Count}件読み込みました。");

string? apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.WriteLine("環境変数 'OPENAI_API_KEY' が設定されていません。");
    return;
}

string? model = Environment.GetEnvironmentVariable("OPENAI_CHAT_MODEL");

if(string.IsNullOrWhiteSpace(model))
{
    Console.WriteLine("環境変数 'OPENAI_CHAT_MODEL' が設定されていません。");
    return;
}

ChatClient chatClient = new(model:model,apiKey:apiKey);



while(true)
{ 
    
    Console.WriteLine("質問を入力してください:");
    string? question = Console.ReadLine();




    if(string .IsNullOrWhiteSpace(question))
    {
        Console.WriteLine("質問が入力できていません。もう一度やり直してください。");
        continue;
    }

     if (question.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("プログラムを終了します。");
        break;
    }
    
    
    Console.WriteLine("入力された質問：");
    Console.WriteLine(question);
    
    Console.WriteLine("AIに質問を送信しています...");

    ChatCompletion completion = await chatClient.CompleteChatAsync(question);

    Console.WriteLine();
    Console.WriteLine("AIの回答:");
    Console.WriteLine(completion.Content[0].Text);
    Console.WriteLine();
}

