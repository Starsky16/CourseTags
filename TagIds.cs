namespace CourseTags;

/// <summary>
/// 课程标签协议常量。GUID 与预置标签名一经发布即为契约，不可更改/重命名。
/// </summary>
public static class TagIds
{
    /// <summary>
    /// 课程标签附加设置的 GUID（协议本体，发布后永不更改）。
    /// </summary>
    public static readonly Guid CourseTags = new("d722a5b9-d62a-472b-96a2-3abdfc4d3518");

    /// <summary>
    /// 预置标签：户外课程。
    /// </summary>
    public const string Outdoor = "outdoor";

    /// <summary>
    /// 预置标签：机房/上机课程。
    /// </summary>
    public const string ComputerRoom = "computer-room";

    /// <summary>
    /// 预置标签：自习课程。
    /// </summary>
    public const string SelfStudy = "self-study";
}
