using ClassIsland.Core.Abstractions;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Extensions.Registry;
using CourseTags.Controls;
using CourseTags.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CourseTags;

[PluginEntrance]
public class Plugin : PluginBase
{
    public override void Initialize(HostBuilderContext context, IServiceCollection services)
    {
        // 标签定义存插件配置目录（PluginConfigFolder 由宿主在 Initialize 之前赋值并创建）
        var configFolder = PluginConfigFolder;
        services.AddSingleton(sp => new CourseTagService(configFolder, sp.GetRequiredService<IProfileService>()));

        // 科目编辑页附加设置区出现"课程标签"卡片
        services.AddAttachedSettingsControl<CourseTagAttachedSettingsControl>();
    }
}
