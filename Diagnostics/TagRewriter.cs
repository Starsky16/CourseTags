namespace CourseTags.Diagnostics;

/// <summary>
/// 档案标签列表改写工具：只改传入的列表，不触碰档案与磁盘。
/// </summary>
public static class TagRewriter
{
    /// <summary>
    /// 归一化全部标签（别名/大小写/格式 → 主 slug）并去重。
    /// 返回"值被改写"的条目数（去重丢弃的重复项不计入）。
    /// </summary>
    public static int NormalizeAll(List<string> tags, TagRegistry registry)
    {
        var changed = 0;
        var result = new List<string>();
        foreach (var raw in tags)
        {
            var canonical = Canonicalize(raw, registry);
            if (canonical.Length == 0)
            {
                continue;
            }
            if (!string.Equals(raw, canonical, StringComparison.Ordinal))
            {
                changed++;
            }
            if (!result.Contains(canonical, StringComparer.OrdinalIgnoreCase))
            {
                result.Add(canonical);
            }
        }

        tags.Clear();
        tags.AddRange(result);
        return changed;
    }

    /// <summary>
    /// 删除既不是定义 slug、也不能解析为别名的标签，返回删除条数。
    /// </summary>
    public static int RemoveUnknown(List<string> tags, TagRegistry registry)
    {
        var removed = 0;
        for (var i = tags.Count - 1; i >= 0; i--)
        {
            var collapsed = TagNormalizer.Collapse(tags[i]);
            var known = registry.Find(collapsed) != null || registry.ResolveAlias(collapsed) != null;
            if (known)
            {
                continue;
            }
            tags.RemoveAt(i);
            removed++;
        }
        return removed;
    }

    /// <summary>
    /// 把 from 改写为 to 并去重，返回改写条数。
    /// </summary>
    public static int Rename(List<string> tags, string from, string to)
    {
        var changed = 0;
        var oldSlug = TagNormalizer.Collapse(from);
        var newSlug = TagNormalizer.Collapse(to);
        var result = new List<string>();
        foreach (var raw in tags)
        {
            var collapsed = TagNormalizer.Collapse(raw);
            var value = string.Equals(collapsed, oldSlug, StringComparison.OrdinalIgnoreCase) ? newSlug : collapsed;
            if (!string.Equals(raw, value, StringComparison.Ordinal))
            {
                changed++;
            }
            if (value.Length > 0 && !result.Contains(value, StringComparer.OrdinalIgnoreCase))
            {
                result.Add(value);
            }
        }

        tags.Clear();
        tags.AddRange(result);
        return changed;
    }

    private static string Canonicalize(string? raw, TagRegistry registry)
    {
        var collapsed = TagNormalizer.Collapse(raw);
        if (collapsed.Length == 0)
        {
            return "";
        }

        var alias = registry.ResolveAlias(collapsed);
        if (alias != null)
        {
            return alias;
        }

        return registry.Find(collapsed)?.Slug ?? collapsed;
    }
}
