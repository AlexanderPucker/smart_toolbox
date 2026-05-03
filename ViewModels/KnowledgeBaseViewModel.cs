using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartToolbox.Services;

namespace SmartToolbox.ViewModels;

public partial class KnowledgeBaseViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _statusMessage = "准备就绪";

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _documentCount;

    [ObservableProperty]
    private int _totalChunks;

    [ObservableProperty]
    private int _totalTokens;

    [ObservableProperty]
    private string _questionInput = string.Empty;

    [ObservableProperty]
    private string _answerOutput = string.Empty;

    [ObservableProperty]
    private KnowledgeDocumentItem? _selectedDocument;

    public ObservableCollection<KnowledgeDocumentItem> Documents { get; } = new();
    public ObservableCollection<SearchResultItem> SearchResults { get; } = new();

    private readonly KnowledgeBaseService _knowledgeBase;

    public KnowledgeBaseViewModel()
    {
        _knowledgeBase = KnowledgeBaseService.Instance;
        _knowledgeBase.OnDocumentAdded += OnDocumentAdded;
        _knowledgeBase.OnDocumentRemoved += OnDocumentRemoved;
        LoadDocuments();
    }

    private void OnDocumentAdded(KnowledgeDocument doc)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            Documents.Add(new KnowledgeDocumentItem
            {
                Id = doc.Id,
                Title = doc.Title,
                ChunkCount = doc.ChunkCount,
                TotalTokens = doc.TotalTokens,
                AddedAt = doc.AddedAt,
                Summary = doc.Summary ?? ""
            });
            UpdateStats();
        });
    }

    private void OnDocumentRemoved(Guid id)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            var doc = Documents.FirstOrDefault(d => d.Id == id);
            if (doc != null)
            {
                Documents.Remove(doc);
            }
            UpdateStats();
        });
    }

    private void LoadDocuments()
    {
        IsLoading = true;
        try
        {
            var docs = _knowledgeBase.GetAllDocuments();
            Documents.Clear();

            foreach (var doc in docs)
            {
                Documents.Add(new KnowledgeDocumentItem
                {
                    Id = doc.Id,
                    Title = doc.Title,
                    ChunkCount = doc.ChunkCount,
                    TotalTokens = doc.TotalTokens,
                    AddedAt = doc.AddedAt,
                    Summary = doc.Summary ?? ""
                });
            }

            UpdateStats();
            StatusMessage = $"已加载 {Documents.Count} 个文档";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpdateStats()
    {
        var stats = _knowledgeBase.GetStats();
        DocumentCount = stats.DocumentCount;
        TotalChunks = stats.TotalChunks;
        TotalTokens = stats.TotalTokens;
    }

    [RelayCommand]
    private async Task AddDocumentAsync()
    {
        StatusMessage = "请选择要添加的文档...";
    }

    public async Task AddDocumentFromFileAsync(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            StatusMessage = "文件不存在";
            return;
        }

        IsLoading = true;
        StatusMessage = "正在处理文档...";

        try
        {
            await _knowledgeBase.AddDocumentAsync(filePath);
            StatusMessage = "文档添加成功";
        }
        catch (Exception ex)
        {
            StatusMessage = $"添加失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task AddTextDocumentAsync(string title, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            StatusMessage = "内容不能为空";
            return;
        }

        IsLoading = true;
        StatusMessage = "正在处理文档...";

        try
        {
            await _knowledgeBase.AddTextDocumentAsync(title, content);
            StatusMessage = "文档添加成功";
        }
        catch (Exception ex)
        {
            StatusMessage = $"添加失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void RemoveDocument()
    {
        if (SelectedDocument != null)
        {
            _knowledgeBase.RemoveDocument(SelectedDocument.Id);
            StatusMessage = "文档已删除";
        }
    }

    [RelayCommand]
    private void Search()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            SearchResults.Clear();
            return;
        }

        IsLoading = true;
        try
        {
            var results = _knowledgeBase.SearchChunks(SearchQuery, 10);
            SearchResults.Clear();

            foreach (var result in results)
            {
                SearchResults.Add(new SearchResultItem
                {
                    DocumentTitle = result.Document.Title,
                    Content = result.Chunk.Content.Length > 200
                        ? result.Chunk.Content.Substring(0, 200) + "..."
                        : result.Chunk.Content,
                    Similarity = result.Similarity,
                    Rank = result.Rank
                });
            }

            StatusMessage = $"找到 {SearchResults.Count} 个相关片段";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AskQuestionAsync()
    {
        if (string.IsNullOrWhiteSpace(QuestionInput))
        {
            StatusMessage = "请输入问题";
            return;
        }

        IsLoading = true;
        StatusMessage = "正在思考...";
        AnswerOutput = string.Empty;

        try
        {
            var answer = await _knowledgeBase.AskQuestionAsync(QuestionInput);
            AnswerOutput = answer;
            StatusMessage = "回答完成";
        }
        catch (Exception ex)
        {
            StatusMessage = $"回答失败: {ex.Message}";
            AnswerOutput = $"错误: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ClearAll()
    {
        _knowledgeBase.ClearAll();
        Documents.Clear();
        SearchResults.Clear();
        UpdateStats();
        StatusMessage = "已清空知识库";
    }
}

public class KnowledgeDocumentItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int ChunkCount { get; set; }
    public int TotalTokens { get; set; }
    public DateTime AddedAt { get; set; }
    public string Summary { get; set; } = string.Empty;
}

public class SearchResultItem
{
    public string DocumentTitle { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public double Similarity { get; set; }
    public int Rank { get; set; }
}
