using System.ComponentModel;
using System.Runtime.CompilerServices;
using ClassIsland.Shared.Interfaces;

namespace CourseTags;

/// <summary>
/// 课程标签数据契约（存于科目的 AttachedObjects[TagIds.CourseTags]）。
/// 属性名即 JSON 契约：宿主档案序列化为 PascalCase 且大小写敏感，发布后不可重命名。
/// 实现 <see cref="INotifyPropertyChanged"/> 与宿主自带附加设置一致：宿主编辑页的
/// 展开状态与内容区可用性绑定本对象属性，不通知会导致开关打开后输入区仍禁用。
/// </summary>
public class CourseTagData : IAttachedSettings, INotifyPropertyChanged
{
    private bool _isAttachSettingsEnabled;
    private List<string> _tags = new();

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
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 标签集合。预置标签名见 <see cref="TagIds"/> 常量，亦允许自定义字符串。
    /// </summary>
    public List<string> Tags
    {
        get => _tags;
        set
        {
            if (ReferenceEquals(value, _tags)) return;
            _tags = value ?? new List<string>();
            OnPropertyChanged();
        }
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
