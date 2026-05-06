using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SmartToolbox.Models;

namespace SmartToolbox.Services;

public class KnowledgeDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = "text";
    public DateTime AddedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public int ChunkCount { get; set; }
    public int TotalTokens { get; set; }
    public List<string> Tags { get; set; } = new();
    public string? Summary { get; set; }
}

public class DocumentChunk
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    public string Content { get; set; } = string.Empty;
    public int ChunkIndex { get; set; }
    public int StartPosition { get; set; }
    public int TokenCount { get; set; }
    public float[]? Embedding { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public class SearchResult
{
    public DocumentChunk Chunk { get; set; } = new();
    public KnowledgeDocument Document { get; set; } = new();
    public double Similarity { get; set; }
    public int Rank { get; set; }
}

public class KnowledgeAnswerSource
{
    public int Rank { get; set; }
    public string DocumentTitle { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int ChunkIndex { get; set; }
    public int StartPosition { get; set; }
    public int TokenCount { get; set; }
    public double Similarity { get; set; }
    public string Preview { get; set; } = string.Empty;
}

public class KnowledgeAnswerResult
{
    public string Answer { get; set; } = string.Empty;
    public List<KnowledgeAnswerSource> Sources { get; set; } = new();
}

public sealed class KnowledgeBaseService
{
    private static readonly Lazy<KnowledgeBaseService> _instance = new(() => new KnowledgeBaseService());
    public static KnowledgeBaseService Instance => _instance.Value;

    private readonly string _knowledgeBasePath;
    private readonly string _documentsIndexFile;
    private readonly string _chunksPath;
    private readonly string _embeddingsPath;

    private Dictionary<Guid, KnowledgeDocument> _documents = new();
    private List<DocumentChunk> _allChunks = new();
    private readonly TokenCounterService _tokenCounter;
    private readonly AIService _aiService;

    private int _chunkSize = 500;
    private int _chunkOverlap = 50;

    public event Action<KnowledgeDocument>? OnDocumentAdded;
    public event Action<Guid>? OnDocumentRemoved;

    private KnowledgeBaseService()
    {
        _knowledgeBasePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SmartToolbox",
            "knowledge_base");

        _documentsIndexFile = Path.Combine(_knowledgeBasePath, "documents.json");
        _chunksPath = Path.Combine(_knowledgeBasePath, "chunks");
        _embeddingsPath = Path.Combine(_knowledgeBasePath, "embeddings");

        Directory.CreateDirectory(_knowledgeBasePath);
        Directory.CreateDirectory(_chunksPath);
        Directory.CreateDirectory(_embeddingsPath);

        _tokenCounter = TokenCounterService.Instance;
        _aiService = AIService.Instance;

        LoadDocumentsIndex();
    }

    public void SetChunkSettings(int chunkSize, int chunkOverlap)
    {
        _chunkSize = Math.Max(100, chunkSize);
        _chunkOverlap = Math.Max(0, Math.Min(chunkOverlap, chunkSize / 2));
    }

    public async Task<KnowledgeDocument> AddDocumentAsync(string filePath, List<string>? tags = null)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"文件不存在: {filePath}");
        }

        if (!IsTextSupportedFile(filePath))
        {
            throw new NotSupportedException("当前仅支持导入文本类型文件（txt/md/json/cs/java/py/js/ts/log/csv/xml/yml/yaml）。");
        }

        var content = await File.ReadAllTextAsync(filePath);
        var fileName = Path.GetFileName(filePath);

        var document = new KnowledgeDocument
        {
            Title = fileName,
            FilePath = filePath,
            ContentType = GetContentType(filePath),
            Tags = tags ?? new List<string>()
        };

        var chunks = SplitIntoChunks(content, document.Id);
        await PopulateEmbeddingsAsync(chunks);
        document.ChunkCount = chunks.Count;
        document.TotalTokens = chunks.Sum(c => c.TokenCount);

        document.Summary = await GenerateSummaryAsync(content);

        _documents[document.Id] = document;
        _allChunks.AddRange(chunks);

        await SaveChunksAsync(document.Id, chunks);
        await SaveEmbeddingsAsync(document.Id, chunks);
        SaveDocumentsIndex();

        OnDocumentAdded?.Invoke(document);
        return document;
    }

    public async Task<KnowledgeDocument> AddTextDocumentAsync(string title, string content, List<string>? tags = null)
    {
        var document = new KnowledgeDocument
        {
            Title = title,
            FilePath = "",
            ContentType = "text",
            Tags = tags ?? new List<string>()
        };

        var chunks = SplitIntoChunks(content, document.Id);
        await PopulateEmbeddingsAsync(chunks);
        document.ChunkCount = chunks.Count;
        document.TotalTokens = chunks.Sum(c => c.TokenCount);

        document.Summary = await GenerateSummaryAsync(content);

        _documents[document.Id] = document;
        _allChunks.AddRange(chunks);

        await SaveChunksAsync(document.Id, chunks);
        await SaveEmbeddingsAsync(document.Id, chunks);
        SaveDocumentsIndex();

        OnDocumentAdded?.Invoke(document);
        return document;
    }

    public void RemoveDocument(Guid documentId)
    {
        if (_documents.Remove(documentId))
        {
            _allChunks.RemoveAll(c => c.DocumentId == documentId);

            var chunkFile = Path.Combine(_chunksPath, $"{documentId}.json");
            if (File.Exists(chunkFile))
            {
                File.Delete(chunkFile);
            }

            var embeddingFile = Path.Combine(_embeddingsPath, $"{documentId}.json");
            if (File.Exists(embeddingFile))
            {
                File.Delete(embeddingFile);
            }

            SaveDocumentsIndex();
            OnDocumentRemoved?.Invoke(documentId);
        }
    }

    public List<KnowledgeDocument> GetAllDocuments()
    {
        return _documents.Values.OrderByDescending(d => d.AddedAt).ToList();
    }

    public List<KnowledgeDocument> SearchDocuments(string query)
    {
        var lowerQuery = query.ToLower();
        return _documents.Values
            .Where(d => d.Title.ToLower().Contains(lowerQuery) ||
                        d.Tags.Any(t => t.ToLower().Contains(lowerQuery)) ||
                        (d.Summary?.ToLower().Contains(lowerQuery) ?? false))
            .OrderByDescending(d => d.UpdatedAt)
            .ToList();
    }

    public async Task<List<SearchResult>> SearchChunksAsync(string query, int topK = 5)
    {
        var results = new List<SearchResult>();
        var queryLower = query.ToLower();
        var queryKeywords = queryLower.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        var queryEmbedding = await TryGenerateQueryEmbedding(query);

        // 当前检索走“关键词分数 + 向量相似度”的混合排序，向量不可用时自动退化为关键词检索。
        foreach (var chunk in _allChunks)
        {
            var contentLower = chunk.Content.ToLower();
            var keywordScore = 0.0;

            foreach (var keyword in queryKeywords)
            {
                if (contentLower.Contains(keyword))
                {
                    keywordScore += 1.0;
                    var count = contentLower.Split(new[] { keyword }, StringSplitOptions.None).Length - 1;
                    keywordScore += count * 0.1;
                }
            }

            var semanticScore = queryEmbedding != null && chunk.Embedding != null
                ? Math.Max(0, CosineSimilarity(queryEmbedding, chunk.Embedding))
                : 0;
            var score = keywordScore + semanticScore * 2.0;

            if (score > 0)
            {
                if (_documents.TryGetValue(chunk.DocumentId, out var document))
                {
                    results.Add(new SearchResult
                    {
                        Chunk = chunk,
                        Document = document,
                        Similarity = score
                    });
                }
            }
        }

        return results
            .OrderByDescending(r => r.Similarity)
            .Take(topK)
            .Select((r, i) =>
            {
                r.Rank = i + 1;
                return r;
            })
            .ToList();
    }

    public async Task<string> QueryWithContextAsync(string query, int maxChunks = 3, int maxTokens = 2000)
    {
        var searchResults = await SearchChunksAsync(query, maxChunks * 2);

        if (searchResults.Count == 0)
        {
            return "未找到相关文档内容。";
        }

        var contextBuilder = new StringBuilder();
        contextBuilder.AppendLine("相关文档内容：");
        contextBuilder.AppendLine();

        int currentTokens = 0;

        foreach (var result in searchResults)
        {
            if (currentTokens + result.Chunk.TokenCount > maxTokens)
            {
                break;
            }

            contextBuilder.AppendLine($"【{result.Document.Title}】");
            contextBuilder.AppendLine(result.Chunk.Content);
            contextBuilder.AppendLine();

            currentTokens += result.Chunk.TokenCount;
        }

        return contextBuilder.ToString();
    }

    public async Task<string> AskQuestionAsync(string question, int maxChunks = 3)
    {
        var result = await AskQuestionWithSourcesAsync(question, maxChunks);
        return result.Answer;
    }

    public async Task<KnowledgeAnswerResult> AskQuestionWithSourcesAsync(string question, int maxChunks = 3)
    {
        var searchResults = await SearchChunksAsync(question, maxChunks * 2);
        if (searchResults.Count == 0)
        {
            return new KnowledgeAnswerResult
            {
                Answer = "未找到相关文档内容。",
                Sources = new List<KnowledgeAnswerSource>()
            };
        }

        var context = await QueryWithContextAsync(question, maxChunks);

        var prompt = $@"基于以下文档内容回答问题。如果文档中没有相关信息，请说明。

{context}

问题：{question}

请提供准确、详细的回答：";

        var response = await _aiService.SendMessageAsync(prompt, "你是一个专业的知识库问答助手，擅长根据提供的文档内容准确回答问题。");
        return new KnowledgeAnswerResult
        {
            Answer = response.Content,
            Sources = searchResults
                .Take(maxChunks)
                .Select(r => new KnowledgeAnswerSource
                {
                    Rank = r.Rank,
                    DocumentTitle = r.Document.Title,
                    ContentType = r.Document.ContentType,
                    FilePath = r.Document.FilePath,
                    ChunkIndex = r.Chunk.ChunkIndex,
                    StartPosition = r.Chunk.StartPosition,
                    TokenCount = r.Chunk.TokenCount,
                    Similarity = r.Similarity,
                    Preview = r.Chunk.Content.Length > 180
                        ? r.Chunk.Content.Substring(0, 180) + "..."
                        : r.Chunk.Content
                })
                .ToList()
        };
    }

    private List<DocumentChunk> SplitIntoChunks(string content, Guid documentId)
    {
        var chunks = new List<DocumentChunk>();
        var paragraphs = content.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);

        var currentChunk = new StringBuilder();
        int chunkIndex = 0;
        int position = 0;

        foreach (var paragraph in paragraphs)
        {
            if (currentChunk.Length + paragraph.Length > _chunkSize && currentChunk.Length > 0)
            {
                var chunkContent = currentChunk.ToString().Trim();
                if (!string.IsNullOrEmpty(chunkContent))
                {
                    chunks.Add(new DocumentChunk
                    {
                        DocumentId = documentId,
                        Content = chunkContent,
                        ChunkIndex = chunkIndex++,
                        StartPosition = position,
                        TokenCount = _tokenCounter.EstimateTokens(chunkContent)
                    });
                }

                currentChunk.Clear();

                if (_chunkOverlap > 0 && chunks.Count > 0)
                {
                    var lastChunk = chunks.Last();
                    var overlapText = lastChunk.Content.Length > _chunkOverlap
                        ? lastChunk.Content.Substring(lastChunk.Content.Length - _chunkOverlap)
                        : lastChunk.Content;
                    currentChunk.Append(overlapText).Append("\n\n");
                }
            }

            currentChunk.Append(paragraph).Append("\n\n");
            position += paragraph.Length + 2;
        }

        if (currentChunk.Length > 0)
        {
            var chunkContent = currentChunk.ToString().Trim();
            if (!string.IsNullOrEmpty(chunkContent))
            {
                chunks.Add(new DocumentChunk
                {
                    DocumentId = documentId,
                    Content = chunkContent,
                    ChunkIndex = chunkIndex,
                    StartPosition = position,
                    TokenCount = _tokenCounter.EstimateTokens(chunkContent)
                });
            }
        }

        return chunks;
    }

    private async Task<string> GenerateSummaryAsync(string content)
    {
        var preview = content.Length > 2000 ? content.Substring(0, 2000) + "..." : content;

        var prompt = $"请用1-2句话总结以下文档的主要内容：\n\n{preview}";

        try
        {
            var response = await _aiService.SendMessageAsync(prompt, "你是一个文档摘要助手。");
            return response.Content;
        }
        catch
        {
            return "";
        }
    }

    private string GetContentType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLower();
        return extension switch
        {
            ".pdf" => "pdf",
            ".doc" or ".docx" => "word",
            ".md" => "markdown",
            ".txt" => "text",
            ".json" => "json",
            ".cs" or ".java" or ".py" or ".js" or ".ts" => "code",
            _ => "text"
        };
    }

    private static bool IsTextSupportedFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".txt" or ".md" or ".json" or ".cs" or ".java" or ".py" or ".js" or ".ts" or ".log" or ".csv" or ".xml" or ".yml" or ".yaml" => true,
            _ => false
        };
    }

    private void LoadDocumentsIndex()
    {
        try
        {
            if (File.Exists(_documentsIndexFile))
            {
                var json = File.ReadAllText(_documentsIndexFile);
                var documents = JsonSerializer.Deserialize<List<KnowledgeDocument>>(json);
                if (documents != null)
                {
                    foreach (var doc in documents)
                    {
                        _documents[doc.Id] = doc;
                        LoadChunksForDocument(doc.Id);
                    }
                }
            }
        }
        catch { }
    }

    private void LoadChunksForDocument(Guid documentId)
    {
        try
        {
            var chunkFile = Path.Combine(_chunksPath, $"{documentId}.json");
            if (File.Exists(chunkFile))
            {
                var json = File.ReadAllText(chunkFile);
                var chunks = JsonSerializer.Deserialize<List<DocumentChunk>>(json);
                if (chunks != null)
                {
                    LoadEmbeddingsForChunks(documentId, chunks);
                    _allChunks.AddRange(chunks);
                }
            }
        }
        catch { }
    }

    private void SaveDocumentsIndex()
    {
        try
        {
            var json = JsonSerializer.Serialize(_documents.Values.ToList(), new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_documentsIndexFile, json);
        }
        catch { }
    }

    private async Task SaveChunksAsync(Guid documentId, List<DocumentChunk> chunks)
    {
        try
        {
            var chunkFile = Path.Combine(_chunksPath, $"{documentId}.json");
            var json = JsonSerializer.Serialize(chunks, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(chunkFile, json);
        }
        catch { }
    }

    private async Task PopulateEmbeddingsAsync(List<DocumentChunk> chunks)
    {
        var contents = chunks
            .Select(c => c.Content)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .ToList();

        if (contents.Count == 0)
        {
            return;
        }

        var response = await _aiService.GenerateEmbeddingsAsync(contents);
        if (!response.IsSuccess || response.Embeddings.Count == 0)
        {
            return;
        }

        for (int i = 0; i < chunks.Count && i < response.Embeddings.Count; i++)
        {
            chunks[i].Embedding = response.Embeddings[i].Vector;
        }
    }

    private async Task SaveEmbeddingsAsync(Guid documentId, List<DocumentChunk> chunks)
    {
        try
        {
            var embeddingFile = Path.Combine(_embeddingsPath, $"{documentId}.json");
            var payload = chunks.Select(c => c.Embedding ?? Array.Empty<float>()).ToList();
            var json = JsonSerializer.Serialize(payload);
            await File.WriteAllTextAsync(embeddingFile, json);
        }
        catch { }
    }

    private void LoadEmbeddingsForChunks(Guid documentId, List<DocumentChunk> chunks)
    {
        try
        {
            var embeddingFile = Path.Combine(_embeddingsPath, $"{documentId}.json");
            if (!File.Exists(embeddingFile))
            {
                return;
            }

            var json = File.ReadAllText(embeddingFile);
            var embeddings = JsonSerializer.Deserialize<List<float[]>>(json);
            if (embeddings == null)
            {
                return;
            }

            for (int i = 0; i < chunks.Count && i < embeddings.Count; i++)
            {
                chunks[i].Embedding = embeddings[i];
            }
        }
        catch { }
    }

    private async Task<float[]?> TryGenerateQueryEmbedding(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        var response = await _aiService.GenerateEmbeddingsAsync(new List<string> { query });
        if (!response.IsSuccess || response.Embeddings.Count == 0)
        {
            return null;
        }

        return response.Embeddings[0].Vector;
    }

    private static double CosineSimilarity(float[] left, float[] right)
    {
        if (left.Length == 0 || right.Length == 0 || left.Length != right.Length)
        {
            return 0;
        }

        double dot = 0;
        double leftNorm = 0;
        double rightNorm = 0;

        for (int i = 0; i < left.Length; i++)
        {
            dot += left[i] * right[i];
            leftNorm += left[i] * left[i];
            rightNorm += right[i] * right[i];
        }

        if (leftNorm == 0 || rightNorm == 0)
        {
            return 0;
        }

        return dot / (Math.Sqrt(leftNorm) * Math.Sqrt(rightNorm));
    }

    public void ClearAll()
    {
        _documents.Clear();
        _allChunks.Clear();

        try
        {
            foreach (var file in Directory.GetFiles(_chunksPath))
            {
                File.Delete(file);
            }
            foreach (var file in Directory.GetFiles(_embeddingsPath))
            {
                File.Delete(file);
            }
        }
        catch { }

        SaveDocumentsIndex();
    }

    public KnowledgeBaseStats GetStats()
    {
        return new KnowledgeBaseStats
        {
            DocumentCount = _documents.Count,
            TotalChunks = _allChunks.Count,
            TotalTokens = _allChunks.Sum(c => c.TokenCount),
            TotalSizeBytes = _allChunks.Sum(c => c.Content.Length)
        };
    }
}

public class KnowledgeBaseStats
{
    public int DocumentCount { get; set; }
    public int TotalChunks { get; set; }
    public int TotalTokens { get; set; }
    public long TotalSizeBytes { get; set; }
}
