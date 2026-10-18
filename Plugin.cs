using ClassIsland.Core.Abstractions;
using ClassIsland.Core.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CourseTags;

[PluginEntrance]
public class Plugin : PluginBase
{
    public override void Initialize(HostBuilderContext context, IServiceCollection services)
    {
        // Task 3 在此注册附加设置控件：
        // services.AddAttachedSettingsControl<Controls.CourseTagAttachedSettingsControl>();
    }
}
