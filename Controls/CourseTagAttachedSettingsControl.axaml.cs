using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums;
using CourseTags.Diagnostics;
using CourseTags.Services;

namespace CourseTags.Controls;

/// <summary>
/// 课程标签附加设置控件：可选标签 chip + 输入框（回车/失焦提交，写入前归一化并登记）。
/// </summary>
[AttachedSettingsUsage(AttachedSettingsTargets.Subject)]
[AttachedSettingsControlInfo("d722a5b9-d62a-472b-96a2-3abdfc4d3518", "课程标签")]
public partial class CourseTagAttachedSettingsControl : AttachedSettingsControlBase<CourseTagData>
{
    private readonly CourseTagService _tagService;

    public CourseTagAttachedSettingsControl(CourseTagService tagService)
    {
        _tagService = tagService;
        InitializeComponent();
        Loaded += OnLoaded;
        TagInputBox.KeyDown += OnInputKeyDown;
        TagInputBox.LostFocus += OnInputLostFocus;
        AddButton.Click += OnAddButtonClick;
    }

    /// <summary>
    /// 宿主挂载后注入 Settings；把存量标签归一化为规范 slug 后刷新 chip。
    /// 注意：这里**不做**输入即登记——否则打开科目页会复活用户在管理页删掉的悬空定义；
    /// 登记只发生在用户主动输入时。
    /// </summary>
    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (Settings == null)
        {
            return;
        }

        var stored = Settings.Tags ?? new List<string>();
        var normalized = new List<string>(stored);
        TagRewriter.NormalizeAll(normalized, _tagService.Registry);
        if (!normalized.SequenceEqual(stored, StringComparer.OrdinalIgnoreCase))
        {
            Settings.Tags = normalized;
        }

        RefreshChips();
    }

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }
        e.Handled = true;
        CommitInput();
    }

    private void OnInputLostFocus(object? sender, RoutedEventArgs e)
    {
        CommitInput();
    }

    private void OnAddButtonClick(object? sender, RoutedEventArgs e)
    {
        CommitInput();
    }

    /// <summary>
    /// 提交输入框内容：逐个归一化（别名优先），合法未定义的 slug 自动登记，失败的汇总提示。
    /// </summary>
    private void CommitInput()
    {
        if (Settings == null)
        {
            return;
        }

        var parts = TagNormalizer.SplitInput(TagInputBox.Text);
        if (parts.Count == 0)
        {
            return;
        }

        var tags = new List<string>(Settings.Tags ?? new List<string>());
        var errors = new List<string>();
        foreach (var part in parts)
        {
            if (!TagNormalizer.TryNormalize(part, _tagService.Registry.ResolveAlias, out var slug, out var error))
            {
                errors.Add($"“{part}”：{error}");
                continue;
            }

            // 输入即登记：合法但未定义的 slug 自动创建定义（显示名默认同 slug）
            _tagService.Registry.EnsureCustom(slug);
            if (!tags.Contains(slug, StringComparer.OrdinalIgnoreCase))
            {
                tags.Add(slug);
            }
        }

        Settings.Tags = tags;
        TagInputBox.Text = "";
        RefreshChips();
        ShowError(errors.Count > 0 ? string.Join(Environment.NewLine, errors) : null);
    }

    private void OnChipClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton { Tag: string slug } || Settings == null)
        {
            return;
        }

        var tags = new List<string>(Settings.Tags ?? new List<string>());
        if (tags.RemoveAll(x => string.Equals(x, slug, StringComparison.OrdinalIgnoreCase)) == 0)
        {
            tags.Add(slug);
        }
        Settings.Tags = tags;
        ShowError(null);
    }

    private void RefreshChips()
    {
        ChipsPanel.Children.Clear();
        if (Settings == null)
        {
            return;
        }

        var current = new HashSet<string>(Settings.Tags ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
        foreach (var definition in _tagService.Registry.Tags)
        {
            ChipsPanel.Children.Add(CreateChip(definition.DisplayName, definition.Slug, definition.Color,
                current.Contains(definition.Slug)));
        }

        // 历史遗留且未登记的标签也显示出来，便于用户移除（不静默丢弃）
        foreach (var raw in Settings.Tags ?? new List<string>())
        {
            if (_tagService.Registry.Find(raw) != null)
            {
                continue;
            }
            ChipsPanel.Children.Add(CreateChip(raw, raw, "#607D8B", true));
        }
    }

    private ToggleButton CreateChip(string displayName, string slug, string color, bool isChecked)
    {
        var chip = new ToggleButton
        {
            Content = displayName,
            Tag = slug,
            IsChecked = isChecked,
            Background = Brush.Parse(color),
            Foreground = Brushes.White,
            Margin = new Thickness(0, 0, 6, 6),
            Padding = new Thickness(8, 2),
        };
        ToolTip.SetTip(chip, string.Equals(displayName, slug, StringComparison.Ordinal) ? slug : $"{displayName}（{slug}）");
        chip.Click += OnChipClick;
        return chip;
    }

    private void ShowError(string? message)
    {
        ErrorTextBlock.IsVisible = !string.IsNullOrEmpty(message);
        ErrorTextBlock.Text = message ?? "";
    }
}
