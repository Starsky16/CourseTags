namespace CourseTags.Diagnostics;

/// <summary>
/// 单个科目标签快照（体检输入，避免体检逻辑依赖宿主 Subject）。
/// </summary>
public record SubjectTagSnapshot(string SubjectName, IReadOnlyList<string> Tags);

/// <summary>
/// 标签使用统计。
/// </summary>
public record TagUsage(string Slug, string DisplayName, int Count);

/// <summary>
/// 悬空标签（档案里有、定义里没有）。
/// </summary>
public record SubjectTagIssue(string SubjectName, string Tag);

/// <summary>
/// 可归一化的标签（别名/大小写/格式可改写为规范 slug）。
/// </summary>
public record TagSuggestion(string SubjectName, string Stored, string Canonical);

/// <summary>
/// 体检结果。
/// </summary>
public class TagDiagnosticsReport
{
    /// <summary>
    /// 未打任何标签的科目名。
    /// </summary>
    public IReadOnlyList<string> UntaggedSubjects { get; init; } = Array.Empty<string>();

    /// <summary>
    /// 标签使用统计（按使用次数降序，其次按 slug 升序）。
    /// </summary>
    public IReadOnlyList<TagUsage> Usage { get; init; } = Array.Empty<TagUsage>();

    /// <summary>
    /// 悬空标签列表。
    /// </summary>
    public IReadOnlyList<SubjectTagIssue> Dangling { get; init; } = Array.Empty<SubjectTagIssue>();

    /// <summary>
    /// 可归一化列表。
    /// </summary>
    public IReadOnlyList<TagSuggestion> Suggestions { get; init; } = Array.Empty<TagSuggestion>();

    /// <summary>
    /// 没有未打标、悬空或可归一化项时为 true。
    /// </summary>
    public bool IsClean => UntaggedSubjects.Count == 0 && Dangling.Count == 0 && Suggestions.Count == 0;
}

/// <summary>
/// 标签体检：只读分析，不修改任何数据。
/// </summary>
public static class TagDiagnostics
{
    /// <summary>
    /// 分析科目标签快照。
    /// </summary>
    public static TagDiagnosticsReport Analyze(IEnumerable<SubjectTagSnapshot> subjects, TagRegistry registry)
    {
        var untagged = new List<string>();
        var usage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var dangling = new List<SubjectTagIssue>();
        var suggestions = new List<TagSuggestion>();

        foreach (var subject in subjects)
        {
            if (subject.Tags.Count == 0)
            {
                untagged.Add(subject.SubjectName);
                continue;
            }

            foreach (var raw in subject.Tags)
            {
                var stored = TagNormalizer.Collapse(raw);
                if (stored.Length == 0)
                {
                    continue;
                }

                var canonical = registry.ResolveAlias(stored);
                if (canonical == null && registry.Find(stored) == null)
                {
                    dangling.Add(new SubjectTagIssue(subject.SubjectName, raw));
                    continue;
                }

                var slug = canonical ?? stored;
                if (!string.Equals(stored, slug, StringComparison.OrdinalIgnoreCase))
                {
                    suggestions.Add(new TagSuggestion(subject.SubjectName, raw, slug));
                }

                usage[slug] = usage.TryGetValue(slug, out var count) ? count + 1 : 1;
            }
        }

        var usageList = usage
            .Select(pair => new TagUsage(pair.Key, registry.Find(pair.Key)?.DisplayName ?? pair.Key, pair.Value))
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Slug, StringComparer.Ordinal)
            .ToList();

        return new TagDiagnosticsReport
        {
            UntaggedSubjects = untagged,
            Usage = usageList,
            Dangling = dangling,
            Suggestions = suggestions,
        };
    }
}
