using CourseTags;

namespace CourseTags.Tests;

/// <summary>
/// 标签匹配测试：大小写不敏感、IsOutDoor 只读兼容、空标签不匹配。
/// </summary>
public class CourseTagMatcherTests
{
    [Fact]
    public void Matches_IsCaseInsensitive()
    {
        Assert.True(CourseTagMatcher.Matches(new[] { "OutDoor" }, false, "outdoor"));
    }

    [Fact]
    public void Matches_TreatsIsOutDoorAsOutdoorTag()
    {
        Assert.True(CourseTagMatcher.Matches(Array.Empty<string>(), true, TagIds.Outdoor));
        Assert.False(CourseTagMatcher.Matches(Array.Empty<string>(), true, TagIds.ComputerRoom));
    }

    [Fact]
    public void Matches_EmptyTagIsFalse()
    {
        Assert.False(CourseTagMatcher.Matches(new[] { TagIds.Outdoor }, true, ""));
        Assert.False(CourseTagMatcher.Matches(new[] { TagIds.Outdoor }, true, null));
    }
}
