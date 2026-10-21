using CourseTags;
using CourseTags.Diagnostics;

namespace CourseTags.Tests;

/// <summary>
/// 标签体检与档案改写测试：未打标/悬空/可归一化/统计，以及归一化、清理、改名。
/// </summary>
public class TagDiagnosticsTests
{
    [Fact]
    public void Analyze_ReportsUntaggedDanglingSuggestionsAndUsage()
    {
        var registry = new TagRegistry(null);
        var snapshots = new[]
        {
            new SubjectTagSnapshot("数学", Array.Empty<string>()),
            new SubjectTagSnapshot("体育", new[] { TagIds.Outdoor }),
            new SubjectTagSnapshot("上机", new[] { "机房" }),
            new SubjectTagSnapshot("化学", new[] { "mystery" }),
            new SubjectTagSnapshot("物理", new[] { TagIds.Outdoor }),
        };

        var report = TagDiagnostics.Analyze(snapshots, registry);

        Assert.Equal(new[] { "数学" }, report.UntaggedSubjects);
        Assert.False(report.IsClean);
        Assert.Single(report.Dangling);
        Assert.Equal("mystery", report.Dangling[0].Tag);

        var suggestion = Assert.Single(report.Suggestions);
        Assert.Equal("机房", suggestion.Stored);
        Assert.Equal(TagIds.ComputerRoom, suggestion.Canonical);

        var outdoor = report.Usage.Single(x => x.Slug == TagIds.Outdoor);
        Assert.Equal(2, outdoor.Count);
        Assert.Equal("户外课", outdoor.DisplayName);

        var computerRoom = report.Usage.Single(x => x.Slug == TagIds.ComputerRoom);
        Assert.Equal(1, computerRoom.Count);
    }
}

/// <summary>
/// 档案标签列表改写测试。
/// </summary>
public class TagRewriterTests
{
    [Fact]
    public void NormalizeAll_RewritesAliasesAndDeduplicates()
    {
        var registry = new TagRegistry(null);
        var tags = new List<string> { "机房", "Outdoor", "computer-room" };

        var changed = TagRewriter.NormalizeAll(tags, registry);

        // “机房”“Outdoor”各算一处改写；“computer-room”是重复项，去重不计入改写数
        Assert.Equal(2, changed);
        Assert.Equal(new[] { TagIds.ComputerRoom, TagIds.Outdoor }, tags);
    }

    [Fact]
    public void RemoveUnknown_KeepsAliasesAndRemovesUndefined()
    {
        var registry = new TagRegistry(null);
        var tags = new List<string> { "机房", "mystery", TagIds.Outdoor };

        var removed = TagRewriter.RemoveUnknown(tags, registry);

        Assert.Equal(1, removed);
        Assert.Equal(new[] { "机房", TagIds.Outdoor }, tags);
    }

    [Fact]
    public void Rename_ReplacesAndDeduplicates()
    {
        var registry = new TagRegistry(null);
        var tags = new List<string> { "lab-room", TagIds.Outdoor, "lab-room" };

        var changed = TagRewriter.Rename(tags, "lab-room", "science-lab");

        Assert.Equal(2, changed);
        Assert.Equal(new[] { "science-lab", TagIds.Outdoor }, tags);
    }
}
