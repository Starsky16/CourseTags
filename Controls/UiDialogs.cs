using ClassIsland.Core;
using ClassIsland.Core.Controls;
using FluentAvalonia.UI.Controls;

namespace CourseTags.Controls;

/// <summary>
/// 插件内统一的提示与确认对话框。
/// </summary>
public static class UiDialogs
{
    /// <summary>
    /// 只有“确定”按钮的信息框。
    /// </summary>
    public static async Task InfoAsync(string header, string content)
    {
        await CommonTaskDialogs.ShowDialog(header, content);
    }

    /// <summary>
    /// 确定/取消确认框，确定返回 true。
    /// </summary>
    public static async Task<bool> ConfirmAsync(string header, string content)
    {
        var dialog = new TaskDialog
        {
            Header = header,
            Content = content,
            XamlRoot = AppBase.Current.GetRootWindow(),
            Buttons =
            {
                new TaskDialogButton("取消", false),
                new TaskDialogButton("确定", true) { IsDefault = true },
            },
        };
        var result = await dialog.ShowAsync();
        return Equals(result, true);
    }
}
