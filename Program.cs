using System.Text.Json;
using ActivityRag.Models;
using OpenAI.Chat;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using System.Text;
using OpenAI.Graders;
using System.Net;
using OpenAI.Embeddings;
using System.Diagnostics;
using System.Numerics;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;


using HttpClient httpClient = new HttpClient();

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

string? embeddingModel = Environment.GetEnvironmentVariable("OPENAI_EMBEDDING_MODEL");

if (string.IsNullOrWhiteSpace(embeddingModel))
{
    Console.WriteLine("環境変数'OPENAI_EMBEDDING_MODEL'が設定されていません");
    return;
}

ChatClient chatClient = new(model:model,apiKey:apiKey);

EmbeddingClient embeddingClient = new(model:embeddingModel, apiKey:apiKey);


//string wikiUrl = "https://wiki.cas-ru.com/ja/info-tech";

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

string pdfDirectory = Path.Combine("Data", "pdfs");

string[] pdfFiles = Directory.GetFiles(pdfDirectory, "*.pdf");

Console.WriteLine($"PDFファイルを{pdfFiles.Length}件読み込みました。");

List<PdfChunk> pdfChunks = new();
List<EmbeddedPdfChunk> embeddedPdfChunks = new();

foreach(string pdfFile in pdfFiles)
{
   Console.WriteLine();
   Console.WriteLine($"PDF名:{Path.GetFileName(pdfFile)}");

   using PdfDocument document  = PdfDocument.Open(pdfFile);

   Console.WriteLine($"ページ数:{document.NumberOfPages}");


   foreach(var page in document.GetPages())
    {
        string pageText = ContentOrderTextExtractor.GetText(page).Normalize(NormalizationForm.FormKC);

        PdfChunk pdfChunk = new()
        {
            FileName = Path.GetFileName(pdfFile),PageNumber= page.Number, Content = pageText
        };

        pdfChunks.Add(pdfChunk);

        Console.WriteLine($"{page.Number}ページ目:{pageText.Length}文字");
    }

    Console.WriteLine();
    Console.WriteLine($"PDFチャンクを{pdfChunks.Count}件作成しました");


    foreach (PdfChunk chunk in pdfChunks)
    {
        try
        {
            Console.WriteLine($"PDFの{chunk.PageNumber}ページ目をEmbedding化しています...");

            OpenAIEmbedding embedding = await embeddingClient.GenerateEmbeddingAsync(chunk.Content);

            float[] vector = embedding.ToFloats().ToArray();

            EmbeddedPdfChunk embeddedChunk = new()
            {
                Chunk = chunk,Embedding = vector
            };

            embeddedPdfChunks.Add(embeddedChunk);
            Console.WriteLine($"{chunk.PageNumber}ページ目:{vector.Length}次元");

        }
        catch(Exception ex)
        {
            Console.WriteLine($"{chunk.PageNumber}ページ目のEmbedding化に失敗しました。");
            Console.WriteLine($"エラーの内容:{ex.Message}");
        }

    }

    Console.WriteLine();
    Console.WriteLine($"PDFのEmbeddingを{embeddedPdfChunks.Count}件作成しました。");
}
    
/*string? wikiHtml = null;

try
{
    Console.WriteLine("CasるWikiを取得しています...");

    wikiHtml = await httpClient.GetStringAsync(wikiUrl);

    Console.WriteLine("CasるWikiの取得に成功しました。");
    Console.WriteLine($"取得文字数:{wikiHtml.Length}文字");

    HtmlParser parser = new HtmlParser();

    var document = await parser.ParseDocumentAsync(wikiHtml);

    string graphqlUrl = "https://wiki.cas-ru.com/graphql";

    try
    {   
        Console.WriteLine("GraphQLの接続を確認しています...");

        string graphqQuery = """{pages{list (orderBy:TITLE){id path title}}}""";
        string graphqlJson = JsonSerializer.Serialize(new{query = graphqQuery});

        using StringContent graphqqlRequest = new StringContent(graphqlJson, Encoding.UTF8, "application/json");

        HttpResponseMessage graphqlResponse = await httpClient.PostAsync(graphqlUrl,graphqqlRequest);
        string graphqlContent = await graphqlResponse.Content.ReadAsStringAsync();

        Console.WriteLine($"GraphQLステータス:{(int)graphqlResponse.StatusCode}");

        Console.WriteLine($"GraphQL取得文字数:{graphqlContent.Length}文字");

        Console.WriteLine("GraphQLの結果:");
        Console.WriteLine(graphqlContent);

        using JsonDocument graphqlDocument = JsonDocument.Parse(graphqlContent);

        JsonElement pageList = graphqlDocument.RootElement.GetProperty("data").GetProperty("pages").GetProperty("list");

        Console.WriteLine();
        Console.WriteLine("IT班関連ページ");

        foreach(JsonElement page in pageList.EnumerateArray())
        {
            string path = page.GetProperty("path").GetString() ?? "";
            if (!path.StartsWith("info-tech"))
            {
                continue;
            }
            int id = page.GetProperty("id").GetInt32();

            string title = page.GetProperty("title").GetString() ?? "";

            Console.WriteLine($"ID:{id}, タイトル：{title},パス：{path}");
        }

        Console.WriteLine();
        Console.WriteLine("2026年度活動履歴の本文を取得します。");

    string pageQuery = """{pages{singleByPath(path:"info-tech/active-history/2026"locale:"ja"){id path title description content updatedAt}}}""";

    string pageJson = JsonSerializer.Serialize(new{query = pageQuery});

    using StringContent pageRequest = new StringContent(pageJson,Encoding.UTF8,"application/json");
    
    HttpResponseMessage pageResponse = await httpClient.PostAsync(graphqlUrl,pageRequest);

    string pageContent = await pageResponse.Content.ReadAsStringAsync();

    Console.WriteLine($"本文取得ステータス：{(int)pageResponse.StatusCode}");
    Console.WriteLine("本文取得結果：");
    Console.WriteLine(pageContent);

    }
    catch(HttpRequestException ex)
    {
        Console.WriteLine("GraphQLの接続に失敗しました。");
        Console.WriteLine($"エラー内容: {ex.Message}");
    }

}
catch(HttpRequestException ex)
{
    Console.WriteLine("CasるWikiの取得に失敗しました。");
    Console.WriteLine($"エラー内容: {ex.Message}");
}   */

ActivityRecord firstActivity = activities[0];

string activityText = $"""
活動名：{firstActivity.Title}
カテゴリ：{firstActivity.Category}
状態：{firstActivity.Status}
日付：{firstActivity.RecordedAt:yyyy年M月d日}
内容：{firstActivity.Description}
情報源：{firstActivity.Source}
""";

Console.WriteLine();
Console.WriteLine("Embedding対象の文章：");
Console.WriteLine(activityText);

float[]?  activityVector = null;

try
{
    Console.WriteLine("活動データをEmbedding化しています...");

    OpenAIEmbedding embedding = await embeddingClient.GenerateEmbeddingAsync(activityText);

    activityVector = embedding.ToFloats().ToArray();
    
    Console.WriteLine($"{firstActivity.Id}:{activityVector.Length}次元");

}
catch(Exception ex)
{
    Console.WriteLine("Embeddingの生成に失敗しました。");
    Console.WriteLine($"エラーの種類：{ex.GetType().Name}");
    Console.WriteLine($"エラー内容:{ex.Message}");
}


float[] vectorA = {1.0f,2.0f,3.0f};
float[] vectorB ={1.0f,2.0f,3.0f};

while(true)
{ 
    
    Console.WriteLine("質問を入力してください:");
    string? question = Console.ReadLine();




    if(string.IsNullOrWhiteSpace(question))
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

    float[]  questionVector;

    try
    {
        Console.WriteLine("質問をEmbedding化しています...");

        OpenAIEmbedding questionEmbedding = await embeddingClient.GenerateEmbeddingAsync(question);

        questionVector = questionEmbedding.ToFloats().ToArray();

        Console.WriteLine($"質問ベクトル：{questionVector.Length}次元");
    }
    catch(Exception ex)
    {
        Console.WriteLine("質問のEmbedding生成に失敗しました。");
        Console.WriteLine($"エラーの種類：{ex.GetType().Name}");
        Console.WriteLine($"エラー内容：{ex.Message}");

        continue;
    }

    List<(EmbeddedPdfChunk PdfChunk,  double Similarity)> pdfResults = new();

    foreach(EmbeddedPdfChunk embeddedPdfChunk in embeddedPdfChunks)
    {
        double pdfSimilarity = CaculateCosineSimilarity(embeddedPdfChunk.Embedding,questionVector);

        Console.WriteLine($"PDF{embeddedPdfChunk.Chunk.PageNumber}ページ目との類似度:{pdfSimilarity:F4}");

        pdfResults.Add((embeddedPdfChunk, pdfSimilarity));
    }

    var topPdfResults = pdfResults.OrderByDescending(result => result.Similarity).Take(3).ToList();

    Console.WriteLine();
    Console.WriteLine("類似度上位3件のPDFページ：");

    foreach(var result in topPdfResults)
    {
        Console.WriteLine($"PDF{result.PdfChunk.Chunk.PageNumber}ページ目 類似度:{result.Similarity:F4}");
    }

    string pdfContext = "";

    foreach(var result in topPdfResults)
    {
        pdfContext += $"""
===参考資料===
SOURCE_ID:P{result.PdfChunk.Chunk.PageNumber}
ファイル名:{result.PdfChunk.Chunk.FileName}

本文：
{result.PdfChunk.Chunk.Content}
""";
    }

    string pdfRagPrompt = $"""
あなたはPDF資料をもとに質問へ回答するアシスタントです。

次のルールを守ってください。
・参考資料に書かれている情報だけを使って回答してください。
・参考資料にない内容は推測しないでください。
・日本語で簡潔に回答してください。
・回答に使用した参考資料のSOURCE_IDを最後に示してください。
・SOURCE_IDは参考資料に書かれているものをそのまま使用してください。
・SOURCE_IDを新しく作ってはいけません。
・「第○条」などの条文番号をSOURCE_IDと混同しないでください。
・参考資料から答えが確認できない場合は
「PDF資料からは確認できませんでした」と回答してください。

質問：
{question}

参考資料：
{pdfContext}
""";

    try
    {
        Console.WriteLine();
        Console.WriteLine("PDF資料をもとにAIが回答しています...");

        ChatCompletion pdfCompletion = await chatClient.CompleteChatAsync(pdfRagPrompt);

        Console.WriteLine();
        Console.WriteLine("AIの回答:");
        Console.WriteLine(pdfCompletion.Content[0].Text);
        Console.WriteLine();
    }
    catch(Exception ex)
    {
        Console.WriteLine();
        Console.WriteLine("AIの回答生成に失敗しました。");
        Console.WriteLine($"エラー内容:{ex.Message}");
        Console.WriteLine();
    }

    

    continue;
    
    if (activityVector is null)
    {
        Console.WriteLine("活動ベクトルがありません。");
        continue;
    }

    double similarity = CaculateCosineSimilarity(activityVector,questionVector);

    Console.WriteLine($"活動との類似度:{similarity:F4}");

    const double minimumSimilarity = 0.30;

    if (similarity < minimumSimilarity)
    {
        Console.WriteLine();
        Console.WriteLine("AIの回答:");
        Console.WriteLine("登録された活動情報からは確認できませんでした。");
        Console.WriteLine();

        continue;
    }

    string ragPrompt = $"""
あなたは活動情報を案内するアシスタントです。
次のルールを守ってください。
・参考情報だけを使って回答してください。
・参考情報にない内容は推測しないでください。
・日本語で簡潔に回答してください。
・情報が不足している場合は「登録された活動情報からは確認できませんでした」と回答してください。

利用者の質問：
{question}

参考情報：
{activityText}
""";

    try
    {
    Console.WriteLine("AIに質問を送信しています...");

    ChatCompletion completion = await chatClient.CompleteChatAsync(ragPrompt);

    Console.WriteLine();
    Console.WriteLine("AIの回答:");
    Console.WriteLine(completion.Content[0].Text);
    Console.WriteLine();
    } 
    catch(Exception ex)
    {
        Console.WriteLine();
        Console.WriteLine("OpenAI APIへの接続に失敗しました。");
        Console.WriteLine($"エラー種類: {ex.GetType().Name}");
        Console.WriteLine($"エラー内容: {ex.Message}");
        Console.WriteLine();
    }
}
static double CaculateCosineSimilarity(float[] vectorA, float[] vectorB)
{

    if (vectorA.Length != vectorB.Length)
    {
        throw new ArgumentException("2つのベクトルの長さが違います。");
    }

    double dotProduct = 0;
    double magnitudeA = 0;
    double magnitudeB = 0;

    for(int i = 0; i< vectorA.Length; i++)
    {
        dotProduct += vectorA[i]*vectorB[i];

        magnitudeA += vectorA[i]*vectorA[i];

        magnitudeB += vectorB[i]*vectorB[i];
    }

    double denominator = Math.Sqrt(magnitudeA)*Math.Sqrt(magnitudeB);

    if (denominator == 0)
    {
        return 0;
    }

    return dotProduct/denominator;
}
    