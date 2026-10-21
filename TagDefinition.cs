using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CourseTags;

/// <summary>
/// 标签定义（存于插件本地 tag-definitions.json，不进宿主档案）。
/// </summary>
public class TagDefinition : INotifyPropertyChanged
{
    private string _slug = "";
    private string _displayName = "";
    private string _color = "#2E7D32";
    private List<string> _aliases = new();
    private bool _isBuiltIn;

    /// <summary>
    /// 标签主标识（档案里存的就是它），仅 ASCII 小写字母、数字与连字符。
    /// </summary>
    public string Slug
    {
        get => _slug;
        set
        {
            if (value == _slug) return;
            _slug = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 显示名（可为中文），仅用于 UI 展示。
    /// </summary>
    public string DisplayName
    {
        get => _displayName;
        set
        {
            if (value == _displayName) return;
            _displayName = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 颜色，格式 #RRGGBB。
    /// </summary>
    public string Color
    {
        get => _color;
        set
        {
            if (value == _color) return;
            _color = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 别名（可为中文）；写入档案前会被归一化为主 slug。
    /// </summary>
    public List<string> Aliases
    {
        get => _aliases;
        set
        {
            var next = value ?? new List<string>();
            if (ReferenceEquals(next, _aliases)) return;
            _aliases = next;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 是否为预置标签（预置标签不可删除、不可重命名）。
    /// </summary>
    public bool IsBuiltIn
    {
        get => _isBuiltIn;
        set
        {
            if (value == _isBuiltIn) return;
            _isBuiltIn = value;
            OnPropertyChanged();
        }
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 深拷贝（导出与内置定义重建时使用，避免调用方改动共享实例）。
    /// </summary>
    public TagDefinition Clone() => new()
    {
        Slug = Slug,
        DisplayName = DisplayName,
        Color = Color,
        Aliases = new List<string>(Aliases),
        IsBuiltIn = IsBuiltIn,
    };

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// tag-definitions.json 的根对象。
/// </summary>
public class TagDefinitionFile
{
    /// <summary>
    /// 定义文件结构版本。
    /// </summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>
    /// 标签定义列表。
    /// </summary>
    public List<TagDefinition> Tags { get; set; } = new();
}
