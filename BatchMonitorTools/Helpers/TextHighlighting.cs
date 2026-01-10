using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfBrush = System.Windows.Media.Brush;
using WpfBrushes = System.Windows.Media.Brushes;

namespace BatchMonitorTools.Helpers;

// Builds inline runs to highlight matched substrings inside a TextBlock.
public static class TextHighlighting
{
    public static readonly DependencyProperty SourceTextProperty =
        DependencyProperty.RegisterAttached(
            "SourceText",
            typeof(string),
            typeof(TextHighlighting),
            new PropertyMetadata(string.Empty, OnHighlightChanged));

    public static readonly DependencyProperty HighlightTextProperty =
        DependencyProperty.RegisterAttached(
            "HighlightText",
            typeof(string),
            typeof(TextHighlighting),
            new PropertyMetadata(string.Empty, OnHighlightChanged));

    public static readonly DependencyProperty HighlightBrushProperty =
        DependencyProperty.RegisterAttached(
            "HighlightBrush",
            typeof(WpfBrush),
            typeof(TextHighlighting),
            new PropertyMetadata(WpfBrushes.LightGoldenrodYellow, OnHighlightChanged));

    public static string GetSourceText(DependencyObject obj) => (string)obj.GetValue(SourceTextProperty);

    public static void SetSourceText(DependencyObject obj, string value) => obj.SetValue(SourceTextProperty, value);

    public static string GetHighlightText(DependencyObject obj) => (string)obj.GetValue(HighlightTextProperty);

    public static void SetHighlightText(DependencyObject obj, string value) => obj.SetValue(HighlightTextProperty, value);

    public static WpfBrush GetHighlightBrush(DependencyObject obj) => (WpfBrush)obj.GetValue(HighlightBrushProperty);

    public static void SetHighlightBrush(DependencyObject obj, WpfBrush value) => obj.SetValue(HighlightBrushProperty, value);

    private static void OnHighlightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not WpfTextBlock textBlock)
        {
            return;
        }

        UpdateInlines(textBlock);
    }

    private static void UpdateInlines(WpfTextBlock textBlock)
    {
        var text = GetSourceText(textBlock) ?? string.Empty;
        var query = GetHighlightText(textBlock);

        textBlock.Inlines.Clear();

        if (string.IsNullOrEmpty(text) || string.IsNullOrWhiteSpace(query))
        {
            textBlock.Inlines.Add(new Run(text));
            return;
        }

        var comparison = StringComparison.OrdinalIgnoreCase;
        var index = 0;
        var brush = GetHighlightBrush(textBlock);

        while (index < text.Length)
        {
            var matchIndex = text.IndexOf(query, index, comparison);
            if (matchIndex < 0)
            {
                textBlock.Inlines.Add(new Run(text.Substring(index)));
                break;
            }

            if (matchIndex > index)
            {
                textBlock.Inlines.Add(new Run(text.Substring(index, matchIndex - index)));
            }

            var highlightRun = new Run(text.Substring(matchIndex, query.Length))
            {
                Background = brush
            };
            textBlock.Inlines.Add(highlightRun);
            index = matchIndex + query.Length;
        }
    }
}
