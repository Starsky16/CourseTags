namespace CourseTags;

/// <summary>
/// 标签匹配工具：本插件触发器与 UI 的统一语义（大小写不敏感 + 宿主 IsOutDoor 只读兼容）。
/// </summary>
public static class CourseTagMatcher
{
    /// <summary>
    /// 判断标签集合是否包含指定标签。
    /// <paramref name="isOutDoor"/> 为 true 时视为包含 outdoor，但不写回宿主字段。
    /// </summary>
    public static bool Matches(IEnumerable<string>? tags, bool isOutDoor, string? tag)
    {
        var target = TagNormalizer.Collapse(tag);
        if (target.Length == 0)
        {
            return false;
        }

        if (isOutDoor && string.Equals(target, TagIds.Outdoor, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (tags == null)
        {
            return false;
        }

        foreach (var item in tags)
        {
            if (string.Equals(TagNormalizer.Collapse(item), target, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
