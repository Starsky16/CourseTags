using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums;

namespace CourseTags.Controls;

/// <summary>
/// 课程标签附加设置控件：以逗号分隔文本编辑科目的标签集合。
/// </summary>
[AttachedSettingsUsage(AttachedSettingsTargets.Subject)]
[AttachedSettingsControlInfo("d722a5b9-d62a-472b-96a2-3abdfc4d3518", "课程标签")]
public partial class CourseTagAttachedSettingsControl : AttachedSettingsControlBase<CourseTagData>
{
    public CourseTagAttachedSettingsControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        TagsTextBox.TextChanged += OnTextChanged;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // 宿主在挂载后注入 SettingsInternal；已启用且有存量标签时回填文本。
        if (Settings is { IsAttachSettingsEnabled: true } && string.IsNullOrEmpty(TagsTextBox.Text))
        {
            TagsTextBox.Text = string.Join(", ", Settings.Tags);
        }
    }

    private void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (Settings is null)
        {
            return;
        }

        Settings.Tags = TagsTextBox.Text?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct()
            .ToList() ?? new List<string>();
    }
}
