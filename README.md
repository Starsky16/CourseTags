# CourseTags

ClassIsland 课程标签提供者插件：为科目附加课程标签（户外、上机、自习等），供其他插件按标签决定运行状态。

> 本仓库由 AI（Trae）辅助开发。

## 机制

- 标签数据存储于宿主档案的科目附加设置（`Subject.AttachedObjects`），随档案导出导入与同步。
- 附加设置 GUID：`d722a5b9-d62a-472b-96a2-3abdfc4d3518`（协议本体，发布后永不更改）。
- 其他插件查询本插件数据**无需引用本程序集**：自定义同形 DTO 后按 GUID 调用
  `subject.GetAttachedObject<T>(guid)` 即可，未安装本插件时查询返回 null（自动降级）。

（消费端接入指南见下文"消费端接入"。）

## 兼容性

- 宿主：ClassIsland **2.1.0.1 及以上**（插件 API 契约 `2.0.0.0`）。
- 构建目标：`net8.0`，不依赖 Windows 专属 API，设计上支持 Windows / macOS / Linux（Linux 需 X11，不支持 Wayland）。
- 验证状态：Windows 与 Linux（X11，arm64）已在 ClassIsland 2.1.0.1 实机验证；macOS 未验证（暂不在验证范围）。

## 消费端接入（其他插件）

消费端**不引用本插件程序集**，自定义同形 DTO 后按 GUID 直读宿主档案对象：

```csharp
// 1. 定义同形 DTO（属性名 PascalCase，与档案 JSON 大小写敏感匹配；只列出需要的属性即可）
public class CourseTagData
{
    public bool IsAttachSettingsEnabled { get; set; }
    public List<string> Tags { get; set; } = new();
}

public static class TagIds
{
    public static readonly Guid CourseTags = new("d722a5b9-d62a-472b-96a2-3abdfc4d3518");
}

// 2. 查询当前科目标签（subject 为 ClassIsland.Shared.Models.Profile.Subject）
var data = subject.GetAttachedObject<CourseTagData>(TagIds.CourseTags);
var enabled = data?.IsAttachSettingsEnabled == true;
var tags = data?.Tags ?? new List<string>();

// 3. 决策示例
var allow = enabled && tags.Contains("computer-room");
```

注意事项：

- **必须订阅而非快照**：监听 `ILessonsService` 的 `PropertyChanged`（`CurrentSubject`），换课时重查。
- 未安装本插件 / 该科目未启用标签时 `GetAttachedObject` 返回 null，按无标签降级。
- GUID 与预置标签名（`outdoor`、`computer-room`、`self-study`）一经发布永不更改；新增标签只增不改。

