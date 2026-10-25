using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Markdig;

using MdContainerBlock = Markdig.Syntax.ContainerBlock;
using MdHeadingBlock = Markdig.Syntax.HeadingBlock;
using MdParagraphBlock = Markdig.Syntax.ParagraphBlock;
using MdCodeBlock = Markdig.Syntax.CodeBlock;
using MdListBlock = Markdig.Syntax.ListBlock;
using MdListItemBlock = Markdig.Syntax.ListItemBlock;
using MdQuoteBlock = Markdig.Syntax.QuoteBlock;
using EmphasisExtraOptions = Markdig.Extensions.EmphasisExtras.EmphasisExtraOptions;
using MdThematicBreakBlock = Markdig.Syntax.ThematicBreakBlock;
using MdTable = Markdig.Extensions.Tables.Table;
using MdTableRow = Markdig.Extensions.Tables.TableRow;
using MdTableCell = Markdig.Extensions.Tables.TableCell;
using MdTableColumnAlign = Markdig.Extensions.Tables.TableColumnAlign;
using MdContainerInline = Markdig.Syntax.Inlines.ContainerInline;
using MdLiteralInline = Markdig.Syntax.Inlines.LiteralInline;
using MdCodeInline = Markdig.Syntax.Inlines.CodeInline;
using MdEmphasisInline = Markdig.Syntax.Inlines.EmphasisInline;
using MdLinkInline = Markdig.Syntax.Inlines.LinkInline;
using MdAutolinkInline = Markdig.Syntax.Inlines.AutolinkInline;
using MdLineBreakInline = Markdig.Syntax.Inlines.LineBreakInline;
using MdHtmlInline = Markdig.Syntax.Inlines.HtmlInline;
using MdHtmlEntityInline = Markdig.Syntax.Inlines.HtmlEntityInline;
using MdTaskMarker = Markdig.Extensions.TaskLists.TaskList;

namespace MdView;

public sealed record TocEntry(int Level, string Text, Paragraph Block, Thickness Indent);

public sealed record ConversionResult(FlowDocument Document, IReadOnlyList<TocEntry> Toc);

/// <summary>Конвертация Markdown (AST Markdig) в стандартный WPF FlowDocument.</summary>
public static class MarkdownConverter
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UsePipeTables()
        .UseTaskLists()
        .UseAutoLinks()
        .UseEmphasisExtras(EmphasisExtraOptions.Strikethrough
                         | EmphasisExtraOptions.Superscript
                         | EmphasisExtraOptions.Subscript)
        .Build();

    private static readonly FontFamily MonoFont = new("Consolas, Courier New");
    private static readonly double[] HeadingSizes = { 26, 22, 18, 15.5, 14, 13.5 };

    private sealed class Ctx
    {
        public required string BasePath { get; init; }
        public Action<string>? OpenFile { get; init; }
        public bool SyntaxHighlight { get; init; } = true;
        public List<TocEntry> Toc { get; } = new();
    }

    public static ConversionResult Convert(string markdown, string basePath, Action<string>? openFile = null, bool syntaxHighlight = true)
    {
        var ctx = new Ctx { BasePath = basePath, OpenFile = openFile, SyntaxHighlight = syntaxHighlight };
        var ast = Markdown.Parse(markdown, Pipeline);

        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 14.5,
            PagePadding = new Thickness(35, 15, 45, 30),
            Background = Brushes.Transparent,
        };
        doc.SetResourceReference(TextElement.ForegroundProperty, "FgBrush");

        AddBlocks(doc.Blocks, ast, ctx);
        return new ConversionResult(doc, ctx.Toc);
    }

    // ---------- Блоки ----------

    private static void AddBlocks(BlockCollection target, MdContainerBlock container, Ctx ctx)
    {
        foreach (var block in container)
        {
            var converted = ConvertBlock(block, ctx);
            if (converted is not null)
                target.Add(converted);
        }
    }

    private static Block? ConvertBlock(Markdig.Syntax.Block block, Ctx ctx)
    {
        switch (block)
        {
            case MdHeadingBlock heading:
                return ConvertHeading(heading, ctx);

            case MdParagraphBlock paragraph:
            {
                var para = new Paragraph { Margin = new Thickness(0, 0, 0, 9) };
                AddInlines(para.Inlines, paragraph.Inline, ctx);
                return para;
            }

            case MdCodeBlock code: // покрывает и FencedCodeBlock, и indented code
                return ConvertCode(code, ctx);

            case MdListBlock list:
                return ConvertList(list, ctx);

            case MdQuoteBlock quote:
                return ConvertQuote(quote, ctx);

            case MdTable table:
                return ConvertTable(table, ctx);

            case MdThematicBreakBlock:
                return HorizontalRule();

            default: // HtmlBlock и прочее не рендерим
                return null;
        }
    }

    private static Block ConvertHeading(MdHeadingBlock heading, Ctx ctx)
    {
        var level = Math.Clamp(heading.Level, 1, 6);
        var para = new Paragraph
        {
            FontSize = HeadingSizes[level - 1],
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, level == 1 ? 4 : 16, 0, 8),
        };
        if (level <= 2)
        {
            para.SetResourceReference(Block.BorderBrushProperty, "BorderBrush");
            para.BorderThickness = new Thickness(0, 0, 0, 1);
            para.Padding = new Thickness(0, 0, 0, 5);
        }
        AddInlines(para.Inlines, heading.Inline, ctx);
        ctx.Toc.Add(new TocEntry(level, GetInlineText(heading.Inline), para,
            new Thickness((level - 1) * 14, 0, 0, 0)));
        return para;
    }

    private static Block ConvertCode(MdCodeBlock code, Ctx ctx)
    {
        var codeText = code.Lines.ToString().TrimEnd();
        var lang = (code as Markdig.Syntax.FencedCodeBlock)?.Info;

        // TextBlock с TextWrapping=NoWrap: длинные строки не переносятся,
        // а уходят в горизонтальный скроллбар (как в обычных редакторах кода)
        var textBlock = new TextBlock
        {
            FontFamily = MonoFont,
            FontSize = 13,
            TextWrapping = TextWrapping.NoWrap,
            Background = Brushes.Transparent,
        };
        textBlock.SetResourceReference(TextElement.ForegroundProperty, "FgBrush");

        // подсветка (если включена и язык известен)
        var profile = ctx.SyntaxHighlight ? SyntaxHighlighter.GetProfile(lang) : null;
        var lines = codeText.Split('\n');
        for (var li = 0; li < lines.Length; li++)
        {
            if (li > 0)
                textBlock.Inlines.Add(new LineBreak());
            if (profile is not null)
            {
                foreach (var (text, brushKey) in SyntaxHighlighter.Tokenize(lines[li], profile))
                {
                    var run = new Run(text);
                    run.SetResourceReference(TextElement.ForegroundProperty, brushKey);
                    textBlock.Inlines.Add(run);
                }
            }
            else
            {
                textBlock.Inlines.Add(new Run(lines[li]));
            }
        }

        var box = new ScrollViewer
        {
            Content = textBlock,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Padding = new Thickness(0),
        };

        // кнопка «Копировать» — иконка, как принято в вебе, поверх блока справа сверху
        var grid = new Grid();
        grid.Children.Add(box);

        var copyIcon = CreateCopyIcon();
        var copyButton = new Border
        {
            Child = copyIcon,
            Width = 28,
            Height = 28,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Cursor = System.Windows.Input.Cursors.Hand,
            ToolTip = L10n.L("Lang.Copy"),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
        };
        copyButton.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");
        copyButton.SetResourceReference(Border.BackgroundProperty, "MenuBgBrush");
        copyButton.MouseLeftButtonDown += (_, _) =>
        {
            try
            {
                System.Windows.Clipboard.SetText(codeText);
                copyIcon.Data = Geometry.Parse("M 2.5 7 L 5.5 10 L 11.5 3.5");
                copyIcon.StrokeStartLineCap = PenLineCap.Round;
                copyIcon.StrokeEndLineCap = PenLineCap.Round;
                copyIcon.StrokeLineJoin = PenLineJoin.Round;
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(1.2),
                };
                timer.Tick += (_, _) =>
                {
                    timer.Stop();
                    copyIcon.Data = CreateCopyIconData();
                    copyIcon.StrokeStartLineCap = PenLineCap.Flat;
                    copyIcon.StrokeEndLineCap = PenLineCap.Flat;
                    copyIcon.StrokeLineJoin = PenLineJoin.Miter;
                };
                timer.Start();
            }
            catch { /* clipboard недоступен */ }
        };
        grid.Children.Add(copyButton);

        // язык блока — мелкая плашка слева от кнопки (тоже поверх кода)
        if (!string.IsNullOrWhiteSpace(lang))
        {
            var langBadge = new Border
            {
                Child = new TextBlock
                {
                    Text = lang,
                    FontFamily = MonoFont,
                    FontSize = 11,
                    Margin = new Thickness(7, 1, 7, 2),
                },
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 0, 36, 0),
            };
            langBadge.SetResourceReference(TextElement.ForegroundProperty, "MutedBrush");
            langBadge.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");
            langBadge.SetResourceReference(Border.BackgroundProperty, "MenuBgBrush");
            grid.Children.Add(langBadge);
        }

        var border = new Border
        {
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(11, 8, 9, 8),
            Margin = new Thickness(0, 2, 0, 12),
            BorderThickness = new Thickness(1),
            Child = grid,
        };
        border.SetResourceReference(Border.BackgroundProperty, "CodeBgBrush");
        border.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");
        return new BlockUIContainer(border);
    }

    private static Block ConvertList(MdListBlock list, Ctx ctx)
    {
        var wpfList = new List
        {
            MarkerStyle = list.IsOrdered ? TextMarkerStyle.Decimal : TextMarkerStyle.Disc,
            Margin = new Thickness(0, 0, 0, 9),
            Padding = new Thickness(24, 0, 0, 0),
        };
        foreach (var child in list)
        {
            if (child is not MdListItemBlock item)
                continue;
            var listItem = new ListItem();
            AddBlocks(listItem.Blocks, item, ctx);
            wpfList.ListItems.Add(listItem);
        }
        return wpfList;
    }

    private static Block ConvertQuote(MdQuoteBlock quote, Ctx ctx)
    {
        var section = new Section
        {
            Margin = new Thickness(4, 2, 0, 10),
            Padding = new Thickness(10, 2, 0, 2),
            BorderThickness = new Thickness(3, 0, 0, 0),
        };
        section.SetResourceReference(Block.BorderBrushProperty, "AccentBrush");
        section.SetResourceReference(TextElement.ForegroundProperty, "MutedBrush");
        AddBlocks(section.Blocks, quote, ctx);
        return section;
    }

    private static Block HorizontalRule()
    {
        var line = new Border { Height = 1, Margin = new Thickness(0, 10, 0, 12) };
        line.SetResourceReference(Border.BackgroundProperty, "BorderBrush");
        return new BlockUIContainer(line);
    }

    private static Block ConvertTable(MdTable table, Ctx ctx)
    {
        var alignments = table.ColumnDefinitions;
        var wpfTable = new Table { CellSpacing = 0, Margin = new Thickness(0, 2, 0, 12) };
        foreach (var _ in alignments)
            wpfTable.Columns.Add(new TableColumn());

        var group = new TableRowGroup();
        wpfTable.RowGroups.Add(group);

        foreach (var child in table)
        {
            if (child is not MdTableRow mdRow)
                continue;

            var row = new TableRow();
            if (mdRow.IsHeader)
            {
                row.SetResourceReference(TextElement.BackgroundProperty, "TableHeaderBrush");
                row.FontWeight = FontWeights.Bold;
            }

            foreach (var cellChild in mdRow)
            {
                if (cellChild is not MdTableCell mdCell)
                    continue;

                var cell = new TableCell
                {
                    Padding = new Thickness(9, 5, 9, 5),
                    BorderThickness = new Thickness(0, 0, 1, 1),
                };
                cell.SetResourceReference(TableCell.BorderBrushProperty, "BorderBrush");
                if (mdCell.ColumnSpan > 1)
                    cell.ColumnSpan = mdCell.ColumnSpan;

                var alignment = mdCell.ColumnIndex >= 0 && mdCell.ColumnIndex < alignments.Count
                    ? alignments[mdCell.ColumnIndex].Alignment
                    : null;
                AddBlocks(cell.Blocks, mdCell, ctx);
                foreach (var para in cell.Blocks.OfType<Paragraph>())
                    para.TextAlignment = ToTextAlignment(alignment);

                row.Cells.Add(cell);
            }
            group.Rows.Add(row);
        }
        return wpfTable;
    }

    private static TextAlignment ToTextAlignment(MdTableColumnAlign? alignment) => alignment switch
    {
        MdTableColumnAlign.Center => TextAlignment.Center,
        MdTableColumnAlign.Right => TextAlignment.Right,
        _ => TextAlignment.Left,
    };

    // ---------- Инлайны ----------

    private static void AddInlines(InlineCollection target, MdContainerInline? container, Ctx ctx)
    {
        if (container is null)
            return;
        foreach (var node in container)
        {
            var inline = ConvertInline(node, ctx);
            if (inline is not null)
                target.Add(inline);
        }
    }

    private static Inline? ConvertInline(Markdig.Syntax.Inlines.Inline node, Ctx ctx)
    {
        switch (node)
        {
            case MdLiteralInline literal:
                return new Run(literal.Content.ToString());

            case MdCodeInline code:
            {
                var run = new Run(code.Content) { FontFamily = MonoFont, FontSize = 13 };
                run.SetResourceReference(TextElement.BackgroundProperty, "CodeBgBrush");
                return run;
            }

            case MdEmphasisInline emphasis:
            {
                var span = new Span();
                AddInlines(span.Inlines, emphasis, ctx);
                ApplyEmphasis(span, emphasis);
                return span;
            }

            case MdLinkInline link when link.IsImage:
                return ConvertImage(link, ctx);

            case MdLinkInline link:
                return ConvertLink(link, ctx);

            case MdAutolinkInline autolink:
                return ConvertAutolink(autolink, ctx);

            case MdLineBreakInline:
                return new LineBreak();

            case MdHtmlInline html when IsBreakTag(html.Tag):
                return new LineBreak();

            case MdHtmlInline:
                return null;

            case MdHtmlEntityInline entity:
            {
                var text = entity.Transcoded.IsEmpty
                    ? WebUtility.HtmlDecode(entity.Original.ToString())
                    : entity.Transcoded.ToString();
                return new Run(text);
            }

            case MdTaskMarker marker:
                return TaskCheckbox(marker.Checked);

            default:
                // неизвестный контейнер — сохраняем содержимое
                if (node is MdContainerInline generic)
                {
                    var span = new Span();
                    AddInlines(span.Inlines, generic, ctx);
                    return span.Inlines.Count > 0 ? span : null;
                }
                return null;
        }
    }

    private static void ApplyEmphasis(Span span, MdEmphasisInline emphasis)
    {
        switch (char.ToLowerInvariant(emphasis.DelimiterChar))
        {
            case '*' or '_':
                if (emphasis.DelimiterCount == 1)
                    span.FontStyle = FontStyles.Italic;
                if (emphasis.DelimiterCount >= 2)
                    span.FontWeight = FontWeights.Bold;
                if (emphasis.DelimiterCount >= 3)
                    span.FontStyle = FontStyles.Italic;
                break;

            case '~' when emphasis.DelimiterCount >= 2:
                span.TextDecorations = TextDecorations.Strikethrough;
                break;

            case '~': // одиночная ~ — нижний индекс
                span.Typography.Variants = FontVariants.Subscript;
                break;

            case '^':
                span.Typography.Variants = FontVariants.Superscript;
                break;
        }
    }

    private static Inline ConvertLink(MdLinkInline link, Ctx ctx)
    {
        var url = link.Url ?? "";
        var hyperlink = new Hyperlink();
        AddInlines(hyperlink.Inlines, link, ctx);
        if (hyperlink.Inlines.Count == 0)
            hyperlink.Inlines.Add(new Run(url));

        hyperlink.ToolTip = url;
        hyperlink.SetResourceReference(TextElement.ForegroundProperty, "AccentBrush");
        hyperlink.NavigateUri = SafeUri(url);
        hyperlink.RequestNavigate += (_, e) =>
        {
            e.Handled = true;
            OpenUri(url, ctx);
        };
        return hyperlink;
    }

    private static Inline ConvertAutolink(MdAutolinkInline autolink, Ctx ctx)
    {
        var url = autolink.IsEmail ? "mailto:" + autolink.Url : autolink.Url;
        var hyperlink = new Hyperlink(new Run(autolink.Url))
        {
            NavigateUri = SafeUri(url),
            ToolTip = autolink.Url,
        };
        hyperlink.SetResourceReference(TextElement.ForegroundProperty, "AccentBrush");
        hyperlink.RequestNavigate += (_, e) =>
        {
            e.Handled = true;
            OpenUri(url, ctx);
        };
        return hyperlink;
    }

    private static Inline ConvertImage(MdLinkInline image, Ctx ctx)
    {
        var alt = GetInlineText(image);
        var url = image.Url ?? "";
        if (url.Length == 0)
            return AltTextRun(alt);

        try
        {
            var uri = ResolveUri(url, ctx.BasePath);
            if (uri.IsFile && !File.Exists(uri.LocalPath))
                return AltTextRun(alt);

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.UriSource = uri;
            bmp.EndInit();

            var imageControl = new Image
            {
                Source = bmp,
                Stretch = Stretch.Uniform,
                StretchDirection = StretchDirection.DownOnly,
                MaxWidth = 900,
                MaxHeight = 700,
                Margin = new Thickness(0, 2, 0, 2),
            };
            var container = new InlineUIContainer(imageControl) { BaselineAlignment = BaselineAlignment.Bottom };
            bmp.DownloadFailed += (_, _) => container.Child = AltTextBlock(alt);
            return container;
        }
        catch
        {
            return AltTextRun(alt);
        }
    }

    // ---------- Вспомогательные ----------

    /// <summary>Иконка «копировать» (два квадратика) — стандартный веб-глиф.</summary>
    private static Geometry CreateCopyIconData()
    {
        var group = new GeometryGroup();
        group.Children.Add(new RectangleGeometry(new Rect(1.5, 1.5, 8.5, 8.5), 1.5, 1.5));
        group.Children.Add(new RectangleGeometry(new Rect(5, 5, 8.5, 8.5), 1.5, 1.5));
        return group;
    }

    private static System.Windows.Shapes.Path CreateCopyIcon()
    {
        var path = new System.Windows.Shapes.Path
        {
            Data = CreateCopyIconData(),
            StrokeThickness = 1.4,
            Stretch = Stretch.Fill,
        };
        path.SetResourceReference(System.Windows.Shapes.Shape.StrokeProperty, "FgBrush");
        return path;
    }

    /// <summary>Единый чекбокс: одинаковая рамка в обоих состояниях, галочка внутри.</summary>
    private static Inline TaskCheckbox(bool isChecked)
    {
        var box = new Border
        {
            Width = 14,
            Height = 14,
            CornerRadius = new CornerRadius(3),
            BorderThickness = new Thickness(1.2),
            Margin = new Thickness(0, 2, 5, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };
        box.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");

        if (isChecked)
        {
            var check = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse("M 2.5 7 L 5.5 10 L 11.5 3.5"),
                StrokeThickness = 1.8,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
                Stretch = Stretch.Fill,
            };
            check.SetResourceReference(System.Windows.Shapes.Shape.StrokeProperty, "AccentBrush");
            box.Child = check;
        }

        return new InlineUIContainer(box) { BaselineAlignment = BaselineAlignment.Center };
    }

    private static void OpenUri(string url, Ctx ctx)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;
        try
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var absolute))
            {
                if (absolute.Scheme is "http" or "https" or "mailto")
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                else if (absolute.IsFile && File.Exists(absolute.LocalPath))
                {
                    ctx.OpenFile?.Invoke(absolute.LocalPath);
                }
            }
            else
            {
                // относительная ссылка на файл рядом с текущим .md (без якоря)
                var pathPart = url.Split('#')[0];
                if (pathPart.Length == 0)
                    return;
                var full = Path.GetFullPath(Path.Combine(ctx.BasePath, Uri.UnescapeDataString(pathPart)));
                if (File.Exists(full))
                    ctx.OpenFile?.Invoke(full);
            }
        }
        catch
        {
            // не смогли открыть — молча игнорируем
        }
    }

    private static Uri ResolveUri(string url, string basePath)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var absolute))
            return absolute;
        var local = Path.GetFullPath(Path.Combine(basePath, Uri.UnescapeDataString(url.Split('#')[0])));
        return new Uri(local, UriKind.Absolute);
    }

    private static Uri? SafeUri(string url)
    {
        try { return new Uri(url, UriKind.RelativeOrAbsolute); }
        catch { return null; }
    }

    private static Run AltTextRun(string alt)
    {
        var run = new Run("\uD83D\uDDBC " + (alt.Length > 0 ? alt : "изображение"))
        {
            FontStyle = FontStyles.Italic,
        };
        run.SetResourceReference(TextElement.ForegroundProperty, "MutedBrush");
        return run;
    }

    private static FrameworkElement AltTextBlock(string alt)
    {
        var textBlock = new TextBlock
        {
            Text = "\uD83D\uDDBC " + (alt.Length > 0 ? alt : "изображение"),
            FontStyle = FontStyles.Italic,
        };
        textBlock.SetResourceReference(TextElement.ForegroundProperty, "MutedBrush");
        return textBlock;
    }

    private static bool IsBreakTag(string? tag) =>
        tag is not null && tag.StartsWith("<br", StringComparison.OrdinalIgnoreCase);

    private static string GetInlineText(MdContainerInline? container)
    {
        if (container is null)
            return string.Empty;
        var sb = new StringBuilder();
        CollectText(container, sb);
        return sb.ToString();
    }

    private static void CollectText(MdContainerInline container, StringBuilder sb)
    {
        foreach (var node in container)
        {
            switch (node)
            {
                case MdLiteralInline literal:
                    sb.Append(literal.Content.ToString());
                    break;
                case MdCodeInline code:
                    sb.Append(code.Content);
                    break;
                case MdContainerInline nested:
                    CollectText(nested, sb);
                    break;
            }
        }
    }
}
