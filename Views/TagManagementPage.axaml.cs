using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using CourseTags.Controls;
using CourseTags.Diagnostics;
using CourseTags.Services;

namespace CourseTags.Views;

/// <summary>
/// 标签管理设置页：定义列表 + 详情编辑 + 导出/导入；重命名会同步改写档案里的旧 slug。
/// </summary>
[SettingsPageInfo("coursetags.management", "课程标签")]
public partial class TagManagementPage : SettingsPageBase
{
    private static readonly JsonSerializerOptions ImportJsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly CourseTagService _tagService;
    private readonly ObservableCollection<TagEditRow> _rows = new();

    public TagManagementPage(CourseTagService tagService)
    {
        _tagService = tagService;
        InitializeComponent();
        TagListBox.ItemsSource = _rows;
        ColorComboBox.ItemsSource = TagRegistry.Palette;
        Loaded += OnLoaded;
    }

    private TagRegistry Registry => _tagService.Registry;

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        RefreshList();
    }

    private void RefreshList(string? selectSlug = null)
    {
        var current = TagListBox.SelectedItem as TagEditRow;
        var targetSlug = selectSlug ?? current?.OriginalSlug;
        _rows.Clear();
        foreach (var definition in Registry.Tags)
        {
            _rows.Add(TagEditRow.FromDefinition(definition));
        }

        var target = _rows.FirstOrDefault(r => string.Equals(r.OriginalSlug, targetSlug, StringComparison.OrdinalIgnoreCase));
        TagListBox.SelectedItem = target ?? (_rows.Count > 0 ? _rows[0] : null);
        UpdateDetail();
    }

    private void TagListBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdateDetail();
    }

    private void UpdateDetail()
    {
        if (TagListBox.SelectedItem is not TagEditRow row)
        {
            SlugTextBox.Text = "";
            DisplayNameTextBox.Text = "";
            AliasesTextBox.Text = "";
            ColorComboBox.SelectedIndex = 0;
            SlugTextBox.IsEnabled = false;
            SaveButton.IsEnabled = false;
            DeleteButton.IsEnabled = false;
            HintTextBlock.Text = "在左侧选择标签，或点击“新增”。";
            return;
        }

        SlugTextBox.Text = row.Slug;
        SlugTextBox.IsEnabled = !row.IsBuiltIn;
        DisplayNameTextBox.Text = row.DisplayName;
        AliasesTextBox.Text = row.AliasesText;
        ColorComboBox.SelectedItem = TagRegistry.Palette
            .FirstOrDefault(c => string.Equals(c, row.Color, StringComparison.OrdinalIgnoreCase))
            ?? TagRegistry.Palette[0];
        SaveButton.IsEnabled = true;
        DeleteButton.IsEnabled = !row.IsBuiltIn;
        HintTextBlock.Text = row.IsBuiltIn
            ? "预置标签：slug 不可修改、不可删除，显示名/颜色/别名可覆盖。"
            : "自定义标签：保存时可修改 slug（会同步改写所有科目里的旧 slug）。";
    }

    private void AddButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var row = new TagEditRow
        {
            OriginalSlug = "",
            IsBuiltIn = false,
            Slug = "",
            DisplayName = "",
            Color = TagRegistry.Palette[0],
            AliasesText = "",
        };
        _rows.Add(row);
        TagListBox.SelectedItem = row;
        UpdateDetail();
        SlugTextBox.Focus();
    }

    private async void SaveButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (TagListBox.SelectedItem is not TagEditRow row)
        {
            return;
        }

        var displayName = DisplayNameTextBox.Text?.Trim() ?? "";
        var color = ColorComboBox.SelectedItem as string ?? TagRegistry.Palette[0];
        var aliases = TagNormalizer.SplitInput(AliasesTextBox.Text).ToList();
        if (!TagNormalizer.TryValidateSlug(SlugTextBox.Text, out var slug, out var error))
        {
            ShowError(error);
            return;
        }

        if (string.IsNullOrEmpty(row.OriginalSlug))
        {
            if (!Registry.TryAdd(slug, displayName, color, aliases, out error))
            {
                ShowError(error);
                return;
            }
            ShowError(null);
            HintTextBlock.Text = "已新增标签定义。";
            RefreshList(slug);
            return;
        }

        var renameFrom = string.Equals(row.OriginalSlug, slug, StringComparison.OrdinalIgnoreCase)
            ? null
            : row.OriginalSlug;
        if (renameFrom != null)
        {
            var confirmed = await UiDialogs.ConfirmAsync(
                "重命名标签",
                $"确定要把 {renameFrom} 重命名为 {slug} 吗？\r\n所有科目里的旧 slug 会被同步改写。");
            if (!confirmed)
            {
                return;
            }
            if (!Registry.TryRename(renameFrom, slug, out error))
            {
                ShowError(error);
                return;
            }
        }

        if (!Registry.TryUpdate(slug, displayName, color, aliases, out error))
        {
            if (renameFrom != null)
            {
                // 回滚改名，避免半完成状态
                Registry.TryRename(slug, renameFrom, out _);
            }
            ShowError(error);
            return;
        }

        var rewritten = 0;
        if (renameFrom != null)
        {
            rewritten = RewriteProfile(renameFrom, slug);
        }
        ShowError(null);
        HintTextBlock.Text = renameFrom == null
            ? "已保存。"
            : $"已保存，并改写了 {rewritten} 处科目标签。";
        RefreshList(slug);
    }

    private async void DeleteButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (TagListBox.SelectedItem is not TagEditRow row)
        {
            return;
        }

        if (string.IsNullOrEmpty(row.OriginalSlug))
        {
            // 尚未保存的新增行：直接从列表移除
            _rows.Remove(row);
            RefreshList();
            return;
        }

        if (!await UiDialogs.ConfirmAsync(
                "删除标签定义",
                $"确定要删除标签定义 {row.Slug} 吗？\r\n注意：只删除定义，不会修改档案里已有的标签字符串；这些科目会显示为悬空标签，可在“课程标签体检”页清理。"))
        {
            return;
        }

        if (!Registry.TryRemove(row.Slug, out var error))
        {
            ShowError(error);
            return;
        }
        ShowError(null);
        HintTextBlock.Text = "已删除定义（档案未改动）。";
        RefreshList();
    }

    private async void ExportButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
        {
            ShowError("无法获取窗口，导出失败。");
            return;
        }

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "导出标签定义",
            SuggestedFileName = "tag-definitions.json",
            DefaultExtension = "json",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("JSON 文件") { Patterns = new[] { "*.json" } },
            },
        });
        if (file == null)
        {
            return;
        }

        var json = JsonSerializer.Serialize(Registry.Export(), new JsonSerializerOptions { WriteIndented = true });
        await using var stream = await file.OpenWriteAsync();
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(json);
        HintTextBlock.Text = "已导出标签定义。";
    }

    private async void ImportButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
        {
            ShowError("无法获取窗口，导入失败。");
            return;
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "导入标签定义",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("JSON 文件") { Patterns = new[] { "*.json" } },
            },
        });
        if (files.Count == 0)
        {
            return;
        }

        TagDefinitionFile? file;
        try
        {
            await using var stream = await files[0].OpenReadAsync();
            file = await JsonSerializer.DeserializeAsync<TagDefinitionFile>(stream, ImportJsonOptions);
        }
        catch (Exception ex) when (ex is JsonException or IOException or NotSupportedException)
        {
            ShowError($"导入失败：{ex.Message}");
            return;
        }

        if (file == null)
        {
            ShowError("导入失败：文件内容为空。");
            return;
        }

        var confirmed = await UiDialogs.ConfirmAsync(
            "导入标签定义",
            $"文件包含 {file.Tags?.Count ?? 0} 个标签定义。\r\n导入会覆盖同名预置标签的显示名/颜色/别名，并新增自定义标签；不会删除现有标签。继续吗？");
        if (!confirmed)
        {
            return;
        }

        var applied = Registry.Import(file, out var errors);
        ShowError(null);
        HintTextBlock.Text = errors.Count == 0
            ? $"已导入 {applied} 个标签定义。"
            : $"已导入 {applied} 个标签定义，{errors.Count} 个被跳过：{string.Join("；", errors)}";
        RefreshList();
    }

    /// <summary>
    /// 把档案里所有科目的 from 改写为 to；返回改写条数。改完必须回写附加设置对象再保存档案。
    /// </summary>
    private int RewriteProfile(string from, string to)
    {
        var changed = 0;
        foreach (var subject in _tagService.Subjects)
        {
            var data = CourseTagService.GetData(subject);
            if (data?.Tags == null || data.Tags.Count == 0)
            {
                continue;
            }

            var tags = new List<string>(data.Tags);
            var count = TagRewriter.Rename(tags, from, to);
            if (count == 0)
            {
                continue;
            }

            data.Tags = tags;
            subject.WriteAttachedObject(TagIds.CourseTags, data);
            changed += count;
        }

        if (changed > 0)
        {
            _tagService.SaveProfile();
        }
        return changed;
    }

    private void ShowError(string? message)
    {
        ErrorTextBlock.IsVisible = !string.IsNullOrEmpty(message);
        ErrorTextBlock.Text = message ?? "";
    }
}

/// <summary>
/// 标签管理页的列表行模型。
/// </summary>
public class TagEditRow : INotifyPropertyChanged
{
    private string _slug = "";
    private string _displayName = "";
    private string _color = TagRegistry.Palette[0];
    private string _aliasesText = "";

    /// <summary>
    /// 保存时的原始 slug（空串表示尚未保存的新增行）。
    /// </summary>
    public string OriginalSlug { get; set; } = "";

    /// <summary>
    /// 是否预置标签。
    /// </summary>
    public bool IsBuiltIn { get; set; }

    /// <summary>
    /// 当前编辑中的 slug。
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
    /// 当前编辑中的显示名。
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
    /// 当前编辑中的颜色。
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
    /// 当前编辑中的别名文本（逗号分隔）。
    /// </summary>
    public string AliasesText
    {
        get => _aliasesText;
        set
        {
            if (value == _aliasesText) return;
            _aliasesText = value;
            OnPropertyChanged();
        }
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 由定义创建行模型。
    /// </summary>
    public static TagEditRow FromDefinition(TagDefinition definition) => new()
    {
        OriginalSlug = definition.Slug,
        IsBuiltIn = definition.IsBuiltIn,
        Slug = definition.Slug,
        DisplayName = definition.DisplayName,
        Color = definition.Color,
        AliasesText = string.Join(", ", definition.Aliases),
    };

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
