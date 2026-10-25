namespace MdView;

/// <summary>Описание языка для лёгкой подсветки синтаксиса.</summary>
public sealed class LangProfile
{
    public required string[] Keywords { get; init; }
    public string[] LineComments { get; init; } = [];
    public (string Start, string End)[] BlockComments { get; init; } = [];
    public bool SingleQuotedStrings { get; init; } = true;
    public bool DoubleQuotedStrings { get; init; } = true;
    public bool MarkupTags { get; init; } = false;
}

/// <summary>Минималистичная подсветка (комментарии, строки, числа, ключевые слова,
/// вызовы функций) без внешних зависимостей. Цвета берутся из темы по ключам
/// Syntax*Brush (по умолчанию — схема VS Code Light+/Dark+).</summary>
public static class SyntaxHighlighter
{
    public const string Comment = "SyntaxCommentBrush";
    public const string String = "SyntaxStringBrush";
    public const string Keyword = "SyntaxKeywordBrush";
    public const string Number = "SyntaxNumberBrush";
    public const string Function = "SyntaxFunctionBrush";
    public const string Plain = "FgBrush";

    private static readonly LangProfile CSharp = new()
    {
        Keywords = ["abstract", "as", "base", "bool", "break", "byte", "case", "catch",
            "char", "checked", "class", "const", "continue", "decimal", "default", "delegate",
            "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally",
            "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int",
            "interface", "internal", "is", "lock", "long", "namespace", "new", "null", "object",
            "operator", "out", "override", "params", "private", "protected", "public", "readonly",
            "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static",
            "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint",
            "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile",
            "while", "async", "await", "record", "required", "init", "var", "yield", "get",
            "set", "partial", "when", "where", "and", "or", "not"],
        LineComments = ["//"],
        BlockComments = [("/*", "*/")],
    };

    private static readonly LangProfile Cpp = new()
    {
        Keywords = ["alignas", "alignof", "and", "and_eq", "asm", "auto", "bitand", "bitor",
            "bool", "break", "case", "catch", "char", "char8_t", "char16_t", "char32_t",
            "class", "compl", "concept", "const", "consteval", "constexpr", "constinit",
            "const_cast", "continue", "co_await", "co_return", "co_yield", "decltype", "default",
            "delete", "do", "double", "dynamic_cast", "else", "enum", "explicit", "export",
            "extern", "false", "float", "for", "friend", "goto", "if", "inline", "int", "long",
            "mutable", "namespace", "new", "noexcept", "not", "not_eq", "nullptr", "operator",
            "or", "or_eq", "private", "protected", "public", "register", "reinterpret_cast",
            "requires", "return", "short", "signed", "sizeof", "static", "static_assert",
            "static_cast", "struct", "switch", "template", "this", "thread_local", "throw",
            "true", "try", "typedef", "typeid", "typename", "union", "unsigned", "using",
            "virtual", "void", "volatile", "wchar_t", "while", "xor", "xor_eq", "final",
            "override", "import", "module"],
        LineComments = ["//"],
        BlockComments = [("/*", "*/")],
    };

    private static readonly LangProfile Java = new()
    {
        Keywords = ["abstract", "assert", "boolean", "break", "byte", "case", "catch", "char",
            "class", "const", "continue", "default", "do", "double", "else", "enum", "extends",
            "final", "finally", "float", "for", "goto", "if", "implements", "import", "instanceof",
            "int", "interface", "long", "native", "new", "package", "private", "protected",
            "public", "return", "short", "static", "strictfp", "super", "switch", "synchronized",
            "this", "throw", "throws", "transient", "try", "void", "volatile", "while", "true",
            "false", "null", "var", "record", "sealed", "permits", "yield"],
        LineComments = ["//"],
        BlockComments = [("/*", "*/")],
    };

    private static readonly LangProfile JavaScript = new()
    {
        Keywords = ["var", "let", "const", "function", "return", "if", "else", "for", "while",
            "do", "switch", "case", "break", "continue", "new", "class", "extends", "super",
            "this", "typeof", "instanceof", "in", "of", "delete", "void", "yield", "async",
            "await", "import", "export", "default", "try", "catch", "finally", "throw", "static",
            "get", "set", "public", "private", "protected", "readonly", "implements", "interface",
            "type", "enum", "namespace", "declare", "global", "any", "unknown", "never", "string",
            "number", "boolean", "object", "symbol", "undefined", "null", "true", "false", "NaN",
            "Infinity", "from", "as", "is", "keyof", "infer", "module", "require"],
        LineComments = ["//"],
        BlockComments = [("/*", "*/")],
    };

    private static readonly LangProfile Python = new()
    {
        Keywords = ["def", "return", "if", "elif", "else", "for", "while", "in", "not", "and",
            "or", "pass", "break", "continue", "import", "from", "as", "with", "try", "except",
            "finally", "raise", "lambda", "yield", "class", "global", "nonlocal", "del", "assert",
            "async", "await", "None", "True", "False", "is", "match", "case", "type", "self"],
        LineComments = ["#"],
    };

    private static readonly LangProfile Shell = new()
    {
        Keywords = ["if", "then", "elif", "else", "fi", "for", "while", "do", "done", "case",
            "esac", "in", "function", "select", "until", "time", "coproc", "declare", "typeset",
            "local", "export", "readonly", "unset", "shift", "set", "trap", "exec", "exit",
            "return", "source", ".", "read", "eval", "alias", "bg", "fg", "jobs", "kill", "wait",
            "ulimit", "umask", "true", "false", "[[", "]]"],
        LineComments = ["#"],
    };

    private static readonly LangProfile PowerShell = new()
    {
        Keywords = ["begin", "break", "catch", "class", "continue", "data", "define", "do",
            "dynamicparam", "else", "elseif", "end", "enum", "exit", "filter", "finally", "for",
            "foreach", "from", "function", "if", "in", "param", "process", "return", "switch",
            "throw", "trap", "try", "until", "using", "var", "while", "workflow", "parallel",
            "sequence", "inlinescript", "configuration", "hidden", "static", "this", "$true",
            "$false", "$null"],
        LineComments = ["#"],
        BlockComments = [("<#", "#>")],
    };

    private static readonly LangProfile Sql = new()
    {
        Keywords = ["select", "insert", "update", "delete", "create", "drop", "alter", "table",
            "index", "view", "procedure", "function", "trigger", "from", "where", "group", "by",
            "order", "having", "join", "inner", "left", "right", "full", "outer", "on", "as",
            "and", "or", "not", "null", "is", "in", "exists", "between", "like", "distinct",
            "top", "limit", "offset", "union", "all", "into", "values", "set", "case", "when",
            "then", "else", "end", "begin", "commit", "rollback", "truncate", "grant", "revoke",
            "default", "primary", "key", "foreign", "references", "unique", "check", "cascade",
            "constraint", "database", "schema", "with", "over", "partition", "row_number", "count",
            "sum", "avg", "min", "max", "cast", "convert", "date", "datetime", "varchar", "int",
            "bigint", "decimal", "numeric", "char", "nvarchar", "bit", "boolean", "text", "blob",
            "true", "false", "asc", "desc", "if", "else", "while", "declare", "exec", "return",
            "current_date", "current_timestamp", "interval", "extract", "coalesce",
            "nullif", "len", "substring", "upper", "lower", "trim", "replace", "round", "floor",
            "ceiling", "year", "month", "day"],
        LineComments = ["--"],
        BlockComments = [("/*", "*/")],
    };

    private static readonly LangProfile Json = new()
    {
        Keywords = ["true", "false", "null"],
        DoubleQuotedStrings = true,
        SingleQuotedStrings = false,
    };

    private static readonly LangProfile Xml = new()
    {
        Keywords = [],
        LineComments = [],
        BlockComments = [("<!--", "-->")],
        DoubleQuotedStrings = true,
        SingleQuotedStrings = true,
        MarkupTags = true,
    };

    private static readonly LangProfile Yaml = new()
    {
        Keywords = ["true", "false", "null", "yes", "no", "on", "off"],
        LineComments = ["#"],
    };

    private static readonly Dictionary<string, LangProfile> Profiles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["csharp"] = CSharp,
        ["cs"] = CSharp,
        ["c#"] = CSharp,
        ["cpp"] = Cpp,
        ["c++"] = Cpp,
        ["cc"] = Cpp,
        ["h"] = Cpp,
        ["hpp"] = Cpp,
        ["java"] = Java,
        ["javascript"] = JavaScript,
        ["js"] = JavaScript,
        ["typescript"] = JavaScript,
        ["ts"] = JavaScript,
        ["jsx"] = JavaScript,
        ["tsx"] = JavaScript,
        ["python"] = Python,
        ["py"] = Python,
        ["py3"] = Python,
        ["bash"] = Shell,
        ["sh"] = Shell,
        ["shell"] = Shell,
        ["zsh"] = Shell,
        ["powershell"] = PowerShell,
        ["ps1"] = PowerShell,
        ["pwsh"] = PowerShell,
        ["sql"] = Sql,
        ["json"] = Json,
        ["xml"] = Xml,
        ["html"] = Xml,
        ["xaml"] = Xml,
        ["svg"] = Xml,
        ["csproj"] = Xml,
        ["config"] = Xml,
        ["yaml"] = Yaml,
        ["yml"] = Yaml,
    };

    public static LangProfile? GetProfile(string? lang)
    {
        if (string.IsNullOrWhiteSpace(lang))
            return null;
        var name = lang.Trim().Split(' ', '\t')[0].ToLowerInvariant();
        return Profiles.TryGetValue(name, out var profile) ? profile : null;
    }

    public static IReadOnlyList<(string Text, string BrushKey)> Tokenize(string code, LangProfile profile)
    {
        var result = new List<(string, string)>();
        var n = code.Length;
        var i = 0;

        while (i < n)
        {
            var c = code[i];

            // построчные комментарии
            var lineComment = MatchPrefix(code, i, profile.LineComments);
            if (lineComment >= 0)
            {
                var end = code.IndexOf('\n', i + lineComment);
                if (end < 0) end = n;
                Add(result, code[i..end], Comment);
                i = end;
                continue;
            }

            // блочные комментарии
            var blockStart = MatchPrefix(code, i, profile.BlockComments.Select(b => b.Start).ToArray());
            if (blockStart >= 0)
            {
                var startLen = profile.BlockComments[blockStart].Start.Length;
                var close = code.IndexOf(profile.BlockComments[blockStart].End, i + startLen);
                if (close < 0) close = n;
                else close += profile.BlockComments[blockStart].End.Length;
                Add(result, code[i..close], Comment);
                i = close;
                continue;
            }

            // строки
            if ((c == '"' && profile.DoubleQuotedStrings) || (c == '\'' && profile.SingleQuotedStrings))
            {
                var quote = c;
                var j = i + 1;
                while (j < n)
                {
                    if (code[j] == '\\' && j + 1 < n) { j += 2; continue; }
                    if (code[j] == quote) { j++; break; }
                    j++;
                }
                Add(result, code[i..j], String);
                i = j;
                continue;
            }

            // числа
            if (char.IsAsciiDigit(c) || (c == '.' && i + 1 < n && char.IsAsciiDigit(code[i + 1])))
            {
                var j = ScanNumber(code, i);
                Add(result, code[i..j], Number);
                i = j;
                continue;
            }

            // слова (ключевые, вызовы функций или обычные)
            if (char.IsLetter(c) || c == '_' || c == '$')
            {
                var j = i;
                while (j < n && (char.IsLetterOrDigit(code[j]) || code[j] == '_' || code[j] == '$'))
                    j++;
                var word = code[i..j];
                if (Array.IndexOf(profile.Keywords, word) >= 0)
                {
                    Add(result, word, Keyword);
                }
                else
                {
                    // вызов функции: слово, за которым (после пробелов) следует «(»
                    var k = j;
                    while (k < n && char.IsWhiteSpace(code[k])) k++;
                    Add(result, word, k < n && code[k] == '(' ? Function : Plain);
                }
                i = j;
                continue;
            }

            // теги разметки <...>
            if (c == '<' && profile.MarkupTags)
            {
                var close = code.IndexOf('>', i + 1);
                if (close >= 0)
                {
                    Add(result, code[i..(close + 1)], Keyword);
                    i = close + 1;
                    continue;
                }
            }

            // обычный символ
            var start = i;
            while (i < n)
            {
                var ch = code[i];
                if (IsSpecialStart(ch, profile)) break;
                i++;
            }
            Add(result, code[start..i], Plain);
        }

        return result;
    }

    private static bool IsSpecialStart(char c, LangProfile profile) =>
        c == '"' && profile.DoubleQuotedStrings ||
        c == '\'' && profile.SingleQuotedStrings ||
        char.IsAsciiDigit(c) ||
        char.IsLetter(c) || c == '_' || c == '$' ||
        (c == '<' && profile.MarkupTags);

    private static int MatchPrefix(string code, int pos, string[] prefixes)
    {
        for (var k = 0; k < prefixes.Length; k++)
        {
            var p = prefixes[k];
            if (p.Length > 0 && pos + p.Length <= code.Length && code.AsSpan(pos, p.Length).SequenceEqual(p))
                return k;
        }
        return -1;
    }

    private static int ScanNumber(string code, int start)
    {
        var i = start;
        var n = code.Length;
        if (i + 1 < n && code[i] == '0' && (code[i + 1] == 'x' || code[i + 1] == 'X'))
        {
            i += 2;
            while (i < n && Uri.IsHexDigit(code[i])) i++;
            return i;
        }
        while (i < n && (char.IsAsciiDigit(code[i]) || code[i] == '_')) i++;
        if (i < n && code[i] == '.')
        {
            i++;
            while (i < n && (char.IsAsciiDigit(code[i]) || code[i] == '_')) i++;
        }
        if (i < n && (code[i] == 'e' || code[i] == 'E'))
        {
            var j = i + 1;
            if (j < n && (code[j] == '+' || code[j] == '-')) j++;
            if (j < n && char.IsAsciiDigit(code[j]))
            {
                while (j < n && char.IsAsciiDigit(code[j])) j++;
                i = j;
            }
        }
        return i;
    }

    private static void Add(List<(string, string)> result, string text, string brushKey)
    {
        if (text.Length == 0)
            return;
        if (result.Count > 0 && result[^1].Item2 == brushKey)
        {
            var last = result[^1];
            result[^1] = (last.Item1 + text, brushKey);
        }
        else
        {
            result.Add((text, brushKey));
        }
    }
}
