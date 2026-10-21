namespace CourseTags;

/// <summary>
/// 预置标签目录：slug、中文显示名、颜色与内置别名。
/// 预置 slug 一经发布即冻结，只能新增不能改名。
/// </summary>
public static class TagCatalog
{
    /// <summary>
    /// 预置标签定义（共享实例；需要修改时先 <see cref="TagDefinition.Clone"/>）。
    /// </summary>
    public static IReadOnlyList<TagDefinition> BuiltIn { get; } = new List<TagDefinition>
    {
        new()
        {
            Slug = TagIds.Outdoor,
            DisplayName = "户外课",
            Color = "#2E7D32",
            IsBuiltIn = true,
            Aliases = new List<string> { " outdoor ", "户外", "室外", "户外课", "体育", "outside", "outdoor-class" },
        },
        new()
        {
            Slug = TagIds.ComputerRoom,
            DisplayName = "上机课",
            Color = "#1565C0",
            IsBuiltIn = true,
            Aliases = new List<string>
            {
                "computer-room", "上机", "机房", "上机课", "计算机房", "电脑室", "computer room", "computerroom",
            },
        },
        new()
        {
            Slug = TagIds.SelfStudy,
            DisplayName = "自习课",
            Color = "#6A1B9A",
            IsBuiltIn = true,
            Aliases = new List<string> { "self-study", "自习", "自习课", "自学", "self study", "selfstudy" },
        },
    };

    /// <summary>
    /// 按 slug 查找预置定义（大小写不敏感），未命中返回 null。
    /// </summary>
    public static TagDefinition? FindBuiltIn(string? slug)
    {
        var canonical = TagNormalizer.Collapse(slug);
        if (canonical.Length == 0)
        {
            return null;
        }
        return BuiltIn.FirstOrDefault(x => string.Equals(x.Slug, canonical, StringComparison.OrdinalIgnoreCase));
    }
}
