using CourseTags;

namespace CourseTags.Tests;

/// <summary>
/// 标签定义注册表测试：内置定义、别名索引、持久化、CRUD 约束与导出导入。
/// 每个测试使用独立临时目录，测试结束删除。
/// </summary>
public sealed class TagRegistryTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "coursetags-tests-" + Guid.NewGuid().ToString("N"));

    public TagRegistryTests()
    {
        Directory.CreateDirectory(_folder);
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, true);
        }
    }

    [Fact]
    public void NewRegistry_ContainsThreeBuiltIns()
    {
        var registry = new TagRegistry(_folder);

        Assert.Equal(3, registry.Tags.Count);
        Assert.All(registry.Tags, tag => Assert.True(tag.IsBuiltIn));
        Assert.Equal(
            new[] { TagIds.ComputerRoom, TagIds.Outdoor, TagIds.SelfStudy },
            registry.Tags.Select(t => t.Slug).ToArray());
    }

    [Theory]
    [InlineData("机房", TagIds.ComputerRoom)]
    [InlineData("户外", TagIds.Outdoor)]
    [InlineData("自习课", TagIds.SelfStudy)]
    [InlineData("computer room", TagIds.ComputerRoom)]
    public void ResolveAlias_ResolvesBuiltInAliases(string alias, string expected)
    {
        var registry = new TagRegistry(_folder);

        Assert.Equal(expected, registry.ResolveAlias(alias));
    }

    [Fact]
    public void AddCustom_PersistsAcrossInstances()
    {
        var registry = new TagRegistry(_folder);
        Assert.True(registry.TryAdd("lab-room", "实验课", "#EF6C00", new[] { "实验" }, out var error), error);

        var reloaded = new TagRegistry(_folder);
        var tag = reloaded.Find("lab-room");

        Assert.NotNull(tag);
        Assert.Equal("实验课", tag!.DisplayName);
        Assert.Equal("#EF6C00", tag.Color);
        Assert.Contains("实验", tag.Aliases);
        Assert.False(tag.IsBuiltIn);
    }

    [Fact]
    public void TryAdd_RejectsDuplicateSlugAndAliasConflict()
    {
        var registry = new TagRegistry(_folder);
        Assert.True(registry.TryAdd("lab-room", "实验课", "#EF6C00", new[] { "实验" }, out _));

        Assert.False(registry.TryAdd("lab-room", "实验课2", "#EF6C00", Array.Empty<string>(), out var duplicateError));
        Assert.Contains("已存在", duplicateError);

        Assert.False(registry.TryAdd("lab-room-2", "实验课2", "#EF6C00", new[] { "实验" }, out var aliasError));
        Assert.Contains("别名", aliasError);
        Assert.Null(registry.Find("lab-room-2"));
    }

    [Fact]
    public void BuiltIn_CannotBeRemovedOrRenamed()
    {
        var registry = new TagRegistry(_folder);

        Assert.False(registry.TryRemove(TagIds.Outdoor, out var removeError));
        Assert.Contains("预置", removeError);

        Assert.False(registry.TryRename(TagIds.Outdoor, "outdoor-2", out var renameError));
        Assert.Contains("预置", renameError);
        Assert.NotNull(registry.Find(TagIds.Outdoor));
    }

    [Fact]
    public void Rename_UpdatesSlugAndAliasIndex()
    {
        var registry = new TagRegistry(_folder);
        Assert.True(registry.TryAdd("lab-room", "实验课", "#EF6C00", new[] { "实验" }, out _));

        Assert.True(registry.TryRename("lab-room", "science-lab", out var error), error);

        Assert.Null(registry.Find("lab-room"));
        Assert.NotNull(registry.Find("science-lab"));
        Assert.Equal("science-lab", registry.ResolveAlias("实验"));

        var reloaded = new TagRegistry(_folder);
        Assert.Null(reloaded.Find("lab-room"));
        Assert.NotNull(reloaded.Find("science-lab"));
    }

    [Fact]
    public void CorruptedFile_FallsBackToBuiltIns()
    {
        File.WriteAllText(Path.Combine(_folder, "tag-definitions.json"), "{ 这不是 JSON");

        var registry = new TagRegistry(_folder);

        Assert.Equal(3, registry.Tags.Count);
        Assert.All(registry.Tags, tag => Assert.True(tag.IsBuiltIn));
    }

    [Fact]
    public void ExportImport_RoundTripsCustomAndOverridesBuiltIn()
    {
        var source = new TagRegistry(_folder);
        Assert.True(source.TryAdd("lab-room", "实验课", "#EF6C00", new[] { "实验" }, out _));
        Assert.True(source.TryUpdate(TagIds.Outdoor, "户外运动", "#2E7D32", new[] { "体育课" }, out var updateError), updateError);

        var exported = source.Export();
        Assert.Equal(4, exported.Tags.Count);

        var targetFolder = Path.Combine(Path.GetTempPath(), "coursetags-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(targetFolder);
        try
        {
            var target = new TagRegistry(targetFolder);
            var applied = target.Import(exported, out var errors);

            Assert.Empty(errors);
            Assert.Equal(4, applied);
            Assert.Equal(4, target.Tags.Count);
            Assert.Equal("户外运动", target.Find(TagIds.Outdoor)!.DisplayName);
            Assert.NotNull(target.Find("lab-room"));
            Assert.Equal(TagIds.ComputerRoom, target.ResolveAlias("机房"));
        }
        finally
        {
            Directory.Delete(targetFolder, true);
        }
    }
}
