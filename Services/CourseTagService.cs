using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Shared.Models.Profile;

namespace CourseTags.Services;

/// <summary>
/// 课程标签服务：持有标签定义注册表，并提供科目标签的读写辅助。
/// 注意：<see cref="AttachableSettingsObject.GetAttachedObject{T}"/> 对档案重载后的 JsonElement
/// 会反序列化出新对象——调用方修改后必须用 WriteAttachedObject 回写，再 SaveProfile。
/// </summary>
public class CourseTagService
{
    private readonly IProfileService _profileService;

    /// <summary>
    /// 创建服务。configFolder 为宿主分配的插件配置目录。
    /// </summary>
    public CourseTagService(string configFolder, IProfileService profileService)
    {
        _profileService = profileService;
        Registry = new TagRegistry(configFolder);
    }

    /// <summary>
    /// 标签定义注册表。
    /// </summary>
    public TagRegistry Registry { get; }

    /// <summary>
    /// 当前档案里的全部科目。
    /// </summary>
    public IEnumerable<Subject> Subjects => _profileService.Profile.Subjects.Values;

    /// <summary>
    /// 读取科目的课程标签附加设置对象（可能是新反序列化出的对象），不存在返回 null。
    /// </summary>
    public static CourseTagData? GetData(Subject subject)
    {
        return subject.GetAttachedObject<CourseTagData>(TagIds.CourseTags);
    }

    /// <summary>
    /// 读取科目实际生效的标签：未启用附加设置时返回空表。
    /// </summary>
    public static List<string> GetTags(Subject? subject)
    {
        if (subject == null)
        {
            return new List<string>();
        }

        var data = GetData(subject);
        if (data is not { IsAttachSettingsEnabled: true })
        {
            return new List<string>();
        }

        return data.Tags ?? new List<string>();
    }

    /// <summary>
    /// 判断科目是否包含指定标签（大小写不敏感，IsOutDoor=true 视为含 outdoor）。
    /// </summary>
    public static bool HasTag(Subject? subject, string tag)
    {
        if (subject == null)
        {
            return false;
        }
        return CourseTagMatcher.Matches(GetTags(subject), subject.IsOutDoor, tag);
    }

    /// <summary>
    /// 收集当前档案里出现过的全部标签字符串（未归一化）。
    /// </summary>
    public IEnumerable<string> CollectUsedTags()
    {
        var result = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var subject in Subjects)
        {
            foreach (var tag in GetTags(subject))
            {
                result.Add(tag);
            }
        }
        return result;
    }

    /// <summary>
    /// 保存当前档案。
    /// </summary>
    public void SaveProfile()
    {
        _profileService.SaveProfile();
    }
}
