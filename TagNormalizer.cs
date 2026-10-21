using System.Text;
using System.Text.RegularExpressions;

namespace CourseTags;

/// <summary>
/// 标签输入归一化与校验。
/// 规则：slug 仅 ASCII 小写字母、数字、连字符，长度 ≤ 32；中文只能作为显示名或别名。
/// </summary>
public static class TagNormalizer
{
    /// <summary>
    /// slug 长度上限。
    /// </summary>
    public const int MaxSlugLength = 32;

    private static readonly Regex ValidSlugRegex = new("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.Compiled);

    private static readonly Regex ValidColorRegex = new("^#[0-9A-Fa-f]{6}$", RegexOptions.Compiled);

    /// <summary>
    /// 折叠输入：去首尾空白、转小写，空格/制表/换行/下划线/连字符/全角空格统一为单个连字符，去首尾连字符。
    /// 中文原样保留。
    /// </summary>
    public static string Collapse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return "";
        }

        var builder = new StringBuilder(raw.Length);
        var lastWasSeparator = false;
        foreach (var ch in raw.Trim())
        {
            if (ch is ' ' or '\t' or '\n' or '\r' or '_' or '-' or '\u3000')
            {
                if (builder.Length > 0 && !lastWasSeparator)
                {
                    builder.Append('-');
                    lastWasSeparator = true;
                }
                continue;
            }

            builder.Append(char.ToLowerInvariant(ch));
            lastWasSeparator = false;
        }

        while (builder.Length > 0 && builder[^1] == '-')
        {
            builder.Length--;
        }
        return builder.ToString();
    }

    /// <summary>
    /// 按英文逗号、中文逗号、顿号拆分输入，去空项与首尾空白。
    /// </summary>
    public static IReadOnlyList<string> SplitInput(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<string>();
        }

        return text
            .Split(new[] { ',', '，', '、' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => x.Length > 0)
            .ToList();
    }

    /// <summary>
    /// 校验并归一化 slug。失败时 <paramref name="error"/> 给出中文原因。
    /// </summary>
    public static bool TryValidateSlug(string? raw, out string slug, out string? error)
    {
        slug = Collapse(raw);
        if (slug.Length == 0)
        {
            error = "标签不能为空。";
            return false;
        }
        if (slug.Length > MaxSlugLength)
        {
            error = $"标签 slug 最多 {MaxSlugLength} 个字符，当前 {slug.Length} 个。";
            return false;
        }
        if (!ValidSlugRegex.IsMatch(slug))
        {
            error = "标签 slug 只能包含小写字母、数字与连字符；中文请填到显示名（或作为别名）。";
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>
    /// 别名优先归一化：先查别名（否则中文别名会被 ASCII 校验误杀），未命中再按 slug 校验。
    /// </summary>
    public static bool TryNormalize(string? raw, Func<string, string?> resolveAlias, out string slug, out string? error)
    {
        var collapsed = Collapse(raw);
        if (collapsed.Length == 0)
        {
            slug = "";
            error = "标签不能为空。";
            return false;
        }

        var resolved = resolveAlias(collapsed);
        if (!string.IsNullOrEmpty(resolved))
        {
            slug = resolved;
            error = null;
            return true;
        }

        if (!TryValidateSlug(raw, out slug, out error))
        {
            // 失败时不把非法输入当作结果返回（调用方约定：失败时 slug 为空）
            slug = "";
            return false;
        }

        return true;
    }

    /// <summary>
    /// 校验颜色是否为 #RRGGBB。
    /// </summary>
    public static bool IsValidColor(string? color)
    {
        return !string.IsNullOrEmpty(color) && ValidColorRegex.IsMatch(color);
    }
}
