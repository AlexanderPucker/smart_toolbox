using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Globalization;
using System.Linq;

namespace PersonalToolbox.Views;

public partial class TextToolView : UserControl
{
    public TextToolView()
    {
        InitializeComponent();
        
        // 监听输入文本变化
        var inputTextBox = this.FindControl<TextBox>("InputTextBox");
        if (inputTextBox != null)
        {
            inputTextBox.TextChanged += OnInputTextChanged;
        }
    }

    private void OnInputTextChanged(object? sender, TextChangedEventArgs e)
    {
        UpdateStatistics();
    }

    private void ToUpperCase(object? sender, RoutedEventArgs e)
    {
        var inputTextBox = this.FindControl<TextBox>("InputTextBox");
        var outputTextBox = this.FindControl<TextBox>("OutputTextBox");
        
        if (inputTextBox?.Text != null && outputTextBox != null)
        {
            outputTextBox.Text = inputTextBox.Text.ToUpper();
        }
    }

    private void ToLowerCase(object? sender, RoutedEventArgs e)
    {
        var inputTextBox = this.FindControl<TextBox>("InputTextBox");
        var outputTextBox = this.FindControl<TextBox>("OutputTextBox");
        
        if (inputTextBox?.Text != null && outputTextBox != null)
        {
            outputTextBox.Text = inputTextBox.Text.ToLower();
        }
    }

    private void ToTitleCase(object? sender, RoutedEventArgs e)
    {
        var inputTextBox = this.FindControl<TextBox>("InputTextBox");
        var outputTextBox = this.FindControl<TextBox>("OutputTextBox");
        
        if (inputTextBox?.Text != null && outputTextBox != null)
        {
            var textInfo = CultureInfo.CurrentCulture.TextInfo;
            outputTextBox.Text = textInfo.ToTitleCase(inputTextBox.Text.ToLower());
        }
    }

    private void TrimSpaces(object? sender, RoutedEventArgs e)
    {
        var inputTextBox = this.FindControl<TextBox>("InputTextBox");
        var outputTextBox = this.FindControl<TextBox>("OutputTextBox");
        
        if (inputTextBox?.Text != null && outputTextBox != null)
        {
            // 去除行首行尾空格，并将多个连续空格替换为单个空格
            var lines = inputTextBox.Text.Split('\n')
                .Select(line => System.Text.RegularExpressions.Regex.Replace(line.Trim(), @"\s+", " "))
                .Where(line => !string.IsNullOrEmpty(line));
            
            outputTextBox.Text = string.Join('\n', lines);
        }
    }

    private void CountWords(object? sender, RoutedEventArgs e)
    {
        UpdateStatistics();
        
        var inputTextBox = this.FindControl<TextBox>("InputTextBox");
        var outputTextBox = this.FindControl<TextBox>("OutputTextBox");
        
        if (inputTextBox?.Text != null && outputTextBox != null)
        {
            var text = inputTextBox.Text;
            var charCount = text.Length;
            var wordCount = string.IsNullOrWhiteSpace(text) ? 0 : 
                text.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
            var lineCount = text.Split('\n').Length;
            
            outputTextBox.Text = $"文本统计结果:\n\n" +
                               $"字符数: {charCount}\n" +
                               $"单词数: {wordCount}\n" +
                               $"行数: {lineCount}\n" +
                               $"非空行数: {text.Split('\n').Count(line => !string.IsNullOrWhiteSpace(line))}";
        }
    }

    private void ClearText(object? sender, RoutedEventArgs e)
    {
        var inputTextBox = this.FindControl<TextBox>("InputTextBox");
        var outputTextBox = this.FindControl<TextBox>("OutputTextBox");
        
        if (inputTextBox != null)
            inputTextBox.Text = "";
        
        if (outputTextBox != null)
            outputTextBox.Text = "";
            
        UpdateStatistics();
    }

    private void UpdateStatistics()
    {
        var inputTextBox = this.FindControl<TextBox>("InputTextBox");
        var charCountText = this.FindControl<TextBlock>("CharCountText");
        var wordCountText = this.FindControl<TextBlock>("WordCountText");
        var lineCountText = this.FindControl<TextBlock>("LineCountText");
        
        if (inputTextBox?.Text != null)
        {
            var text = inputTextBox.Text;
            var charCount = text.Length;
            var wordCount = string.IsNullOrWhiteSpace(text) ? 0 : 
                text.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
            var lineCount = Math.Max(1, text.Split('\n').Length);
            
            if (charCountText != null)
                charCountText.Text = $"字符数: {charCount}";
            
            if (wordCountText != null)
                wordCountText.Text = $"单词数: {wordCount}";
            
            if (lineCountText != null)
                lineCountText.Text = $"行数: {lineCount}";
        }
        else
        {
            if (charCountText != null)
                charCountText.Text = "字符数: 0";
            
            if (wordCountText != null)
                wordCountText.Text = "单词数: 0";
            
            if (lineCountText != null)
                lineCountText.Text = "行数: 0";
        }
    }
} 