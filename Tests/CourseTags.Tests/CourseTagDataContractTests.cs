using System.Text.Json;
using ClassIsland.Shared.Interfaces;
using CourseTags;

namespace CourseTags.Tests;

/// <summary>
/// 课程标签数据契约测试：
/// 覆盖 GUID 冻结、JSON 往返与 JsonElement 反序列化（档案重载后消费端读取的真实路径）。
/// 档案序列化为 PascalCase 且大小写敏感（宿主 ConfigureFileHelper 覆盖全局默认 options），
/// 因此本文件的 JSON 字面量即契约文档。
/// </summary>
public class CourseTagDataContractTests
{
    [Fact]
    public void GuidConstant_IsFrozen()
    {
        // 契约冻结校验：该 GUID 已随数据发布，更改即丢失所有用户档案数据。
        Assert.Equal("d722a5b9-d62a-472b-96a2-3abdfc4d3518", TagIds.CourseTags.ToString());
    }

    [Fact]
    public void IsAttachSettingsEnabled_RaisesPropertyChanged()
    {
        // 宿主科目编辑页的 Expander.IsExpanded 与内容区 IsEnabled 均绑定该属性，
        // 不发出通知会导致开启开关后同会话内输入框仍为禁用态（实机验证发现的缺陷）。
        var data = new CourseTagData();
        var raised = new List<string?>();
        data.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        data.IsAttachSettingsEnabled = true;

        Assert.Contains(nameof(CourseTagData.IsAttachSettingsEnabled), raised);
    }

    [Fact]
    public void NewInstance_DefaultsMatchHostConvention()
    {
        // 与宿主自带附加设置一致：IsAttachSettingsEnabled 默认 false（未启用不回写，档案干净）。
        var data = new CourseTagData();
        Assert.False(data.IsAttachSettingsEnabled);
        Assert.Empty(data.Tags);
    }

    [Fact]
    public void JsonRoundTrip_PreservesTags()
    {
        var data = new CourseTagData { IsAttachSettingsEnabled = true, Tags = { TagIds.Outdoor, "custom-tag" } };
        var json = JsonSerializer.Serialize(data);

        // PascalCase 属性名即契约（大小写敏感）
        Assert.Contains("\"Tags\"", json);
        Assert.Contains("\"IsAttachSettingsEnabled\"", json);

        var restored = JsonSerializer.Deserialize<CourseTagData>(json);
        Assert.NotNull(restored);
        Assert.True(restored.IsAttachSettingsEnabled);
        Assert.Equal(new[] { TagIds.Outdoor, "custom-tag" }, restored.Tags);
    }

    [Fact]
    public void JsonElementDeserialize_MatchesProfileReloadPath()
    {
        // 模拟档案重载：宿主把 AttachedObjects 的值反序列化为 JsonElement 存回内存对象，
        // 消费端 GetAttachedObject<T> 内部走 JsonElement.Deserialize<T>()。
        var data = new CourseTagData { IsAttachSettingsEnabled = true, Tags = { TagIds.ComputerRoom } };
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(data));

        var restored = doc.RootElement.Deserialize<CourseTagData>();
        Assert.NotNull(restored);
        Assert.Equal(new[] { TagIds.ComputerRoom }, restored.Tags);
    }

    [Fact]
    public void JsonElement_DeserializesIntoStandaloneDto()
    {
        // 模拟消费端零依赖读取：消费端自定义同形 DTO（不引用本程序集），须能正确反序列化。
        var data = new CourseTagData { IsAttachSettingsEnabled = true, Tags = { TagIds.SelfStudy } };
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(data));

        var consumerDto = doc.RootElement.Deserialize<ConsumerSideDto>();
        Assert.NotNull(consumerDto);
        Assert.True(consumerDto.IsAttachSettingsEnabled);
        Assert.Equal(new[] { TagIds.SelfStudy }, consumerDto.Tags);
    }

    /// <summary>模拟消费端插件自定义的同形 DTO（PascalCase 属性名）。</summary>
    private class ConsumerSideDto
    {
        public bool IsAttachSettingsEnabled { get; set; }
        public List<string> Tags { get; set; } = new();
    }
}
