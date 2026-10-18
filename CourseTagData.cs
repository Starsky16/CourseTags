using ClassIsland.Shared.Interfaces;

namespace CourseTags;

/// <summary>
/// 课程标签数据契约（存于科目的 AttachedObjects[TagIds.CourseTags]）。
/// 属性名即 JSON 契约：宿主档案序列化为 PascalCase 且大小写敏感，发布后不可重命名。
/// </summary>
public class CourseTagData : IAttachedSettings
{
    private bool _isAttachSettingsEnabled;

    /// <summary>
    /// 附加设置启用位（宿主科目编辑页的开关；与宿主自带附加设置一致，默认 false）。
    /// </summary>
    public bool IsAttachSettingsEnabled
    {
        get => _isAttachSettingsEnabled;
        set
        {
            if (value == _isAttachSettingsEnabled) return;
            _isAttachSettingsEnabled = value;
        }
    }

    /// <summary>
    /// 标签集合。预置标签名见 <see cref="TagIds"/> 常量，亦允许自定义字符串。
    /// </summary>
    public List<string> Tags { get; set; } = new();
}
