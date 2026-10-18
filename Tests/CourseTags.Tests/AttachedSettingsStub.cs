// ⚠️ 与宿主 ClassIsland.Shared IAttachedSettings 保持签名同步（当前仅一个属性）。
// 漂移后果：主工程编译仍通过，测试不再反映真实契约。
namespace ClassIsland.Shared.Interfaces;

/// <summary>
/// 附加设置接口（测试工程 stub，签名镜像宿主 IAttachedSettings）。
/// </summary>
public interface IAttachedSettings
{
    /// <summary>
    /// 此附加设置是否启用。
    /// </summary>
    public bool IsAttachSettingsEnabled
    {
        get;
        set;
    }
}
