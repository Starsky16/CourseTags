using CourseTags;

namespace CourseTags.Tests;

/// <summary>
/// 标签归一化与校验测试：覆盖折叠、拆分、别名优先解析与颜色校验。
/// </summary>
public class TagNormalizerTests
{
    [Theory]
    [InlineData("  Computer  Room ", "computer-room")]
    [InlineData("self_study", "self-study")]
    [InlineData("A--B", "a-b")]
    [InlineData("机房", "机房")]
    [InlineData("   ", "")]
    public void Collapse_NormalizesSeparators(string raw, string expected)
    {
        Assert.Equal(expected, TagNormalizer.Collapse(raw));
    }

    [Fact]
    public void SplitInput_SplitsChineseAndEnglishSeparators()
    {
        var result = TagNormalizer.SplitInput("outdoor, 机房，自习、实验");

        Assert.Equal(new[] { "outdoor", "机房", "自习", "实验" }, result);
    }

    [Theory]
    [InlineData("机房", "computer-room")]
    [InlineData("computer room", "computer-room")]
    [InlineData("Lab-Room", "lab-room")]
    public void TryNormalize_ResolvesAliasBeforeValidation(string raw, string expected)
    {
        Assert.True(TagNormalizer.TryNormalize(raw, ResolveTestAlias, out var slug, out var error), error);
        Assert.Equal(expected, slug);
        Assert.Null(error);
    }

    [Fact]
    public void TryNormalize_RejectsChineseSlugWithDisplayNameHint()
    {
        // 中文不是别名时必须失败，并明确提示“中文请填到显示名”
        Assert.False(TagNormalizer.TryNormalize("实验课", ResolveTestAlias, out var slug, out var error));
        Assert.Equal("", slug);
        Assert.NotNull(error);
        Assert.Contains("显示名", error);
    }

    [Fact]
    public void TryNormalize_RejectsBlankInput()
    {
        Assert.False(TagNormalizer.TryNormalize("   ", ResolveTestAlias, out _, out var error));
        Assert.Contains("不能为空", error);
    }

    [Theory]
    [InlineData("#2E7D32", true)]
    [InlineData("#2e7d32", true)]
    [InlineData("2E7D32", false)]
    [InlineData("#2E7D3", false)]
    public void IsValidColor_AcceptsOnlySixDigitHex(string color, bool expected)
    {
        Assert.Equal(expected, TagNormalizer.IsValidColor(color));
    }

    /// <summary>测试用别名解析：模拟"机房/computer room 指向 computer-room"。</summary>
    private static string? ResolveTestAlias(string collapsed) => collapsed switch
    {
        "机房" => TagIds.ComputerRoom,
        "computer-room" => TagIds.ComputerRoom,
        _ => null,
    };
}
