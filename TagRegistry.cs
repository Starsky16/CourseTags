using System.Text.Json;

namespace CourseTags;

/// <summary>
/// 标签定义注册表：内置标签 + 用户自定义标签，持久化到插件配置目录的 tag-definitions.json。
/// 预置标签不可删除、slug 不可重命名；显示名、颜色与别名可覆盖并持久化。
/// 定义文件缺失/损坏/不可读时静默退回内置定义，绝不把异常抛给宿主。
/// </summary>
public class TagRegistry
{
    /// <summary>
    /// 当前定义文件结构版本。
    /// </summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>
    /// 预设色板（定义文件里的颜色一律为 #RRGGBB）。
    /// </summary>
    public static IReadOnlyList<string> Palette { get; } = new[]
    {
        "#2E7D32", "#1565C0", "#6A1B9A", "#EF6C00", "#C62828", "#00838F", "#4E342E", "#37474F",
    };

    private const string FileName = "tag-definitions.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    private readonly string? _configFolder;
    private readonly List<TagDefinition> _tags = new();
    private readonly Dictionary<string, string> _aliasIndex = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 创建注册表并立即尝试加载定义文件。configFolder 为空时只在内存中使用内置定义。
    /// </summary>
    public TagRegistry(string? configFolder)
    {
        _configFolder = string.IsNullOrWhiteSpace(configFolder) ? null : configFolder;
        Load();
    }

    /// <summary>
    /// 全部标签定义（预置在前，其后按 slug 升序）。
    /// </summary>
    public IReadOnlyList<TagDefinition> Tags => _tags;

    private string? ConfigFilePath => _configFolder == null ? null : Path.Combine(_configFolder, FileName);

    /// <summary>
    /// 按 slug 查找定义（大小写不敏感），未命中返回 null。别名请用 <see cref="ResolveAlias"/>。
    /// </summary>
    public TagDefinition? Find(string? slug)
    {
        var canonical = TagNormalizer.Collapse(slug);
        if (canonical.Length == 0)
        {
            return null;
        }
        return _tags.FirstOrDefault(t => string.Equals(t.Slug, canonical, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 解析别名/slug 到主 slug（大小写不敏感），未命中返回 null。
    /// </summary>
    public string? ResolveAlias(string? raw)
    {
        var alias = TagNormalizer.Collapse(raw);
        if (alias.Length == 0)
        {
            return null;
        }
        return _aliasIndex.TryGetValue(alias, out var slug) ? slug : null;
    }

    /// <summary>
    /// 确保 slug 有定义（输入即登记）：不存在时用默认显示名与色板下一色新建并落盘。
    /// slug 不合法（如历史遗留中文）时返回未登记的临时定义，便于 UI 展示原始文本。
    /// </summary>
    public TagDefinition EnsureCustom(string slug)
    {
        var canonical = TagNormalizer.Collapse(slug);
        if (!TagNormalizer.TryValidateSlug(canonical, out _, out _))
        {
            return new TagDefinition
            {
                Slug = canonical,
                DisplayName = canonical,
                Color = NextColor(),
                IsBuiltIn = false,
            };
        }

        var existing = Find(canonical);
        if (existing != null)
        {
            return existing;
        }

        var definition = new TagDefinition
        {
            Slug = canonical,
            DisplayName = canonical,
            Color = NextColor(),
            IsBuiltIn = false,
        };
        _tags.Add(definition);
        SortTags();
        RebuildAliasIndex();
        TrySave(out _);
        return definition;
    }

    /// <summary>
    /// 新增自定义标签。
    /// </summary>
    public bool TryAdd(string slug, string displayName, string color, IEnumerable<string>? aliases, out string? error)
    {
        if (!TagNormalizer.TryValidateSlug(slug, out var canonical, out error))
        {
            return false;
        }
        if (Find(canonical) != null)
        {
            error = $"标签 {canonical} 已存在。";
            return false;
        }
        if (!TagNormalizer.IsValidColor(color))
        {
            error = "颜色格式应为 #RRGGBB。";
            return false;
        }
        if (!TryBuildAliases(canonical, aliases, out var prepared, out error))
        {
            return false;
        }

        var definition = new TagDefinition
        {
            Slug = canonical,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? canonical : displayName.Trim(),
            Color = color.ToUpperInvariant(),
            IsBuiltIn = false,
            Aliases = prepared,
        };
        _tags.Add(definition);
        SortTags();
        RebuildAliasIndex();
        return TrySave(out error);
    }

    /// <summary>
    /// 更新定义（不改 slug）：显示名、颜色、别名。内置标签同样可覆盖。
    /// </summary>
    public bool TryUpdate(string slug, string displayName, string color, IEnumerable<string>? aliases, out string? error)
    {
        var definition = Find(slug);
        if (definition == null)
        {
            error = $"标签 {TagNormalizer.Collapse(slug)} 不存在。";
            return false;
        }
        if (!TagNormalizer.IsValidColor(color))
        {
            error = "颜色格式应为 #RRGGBB。";
            return false;
        }
        if (!TryBuildAliases(definition.Slug, aliases, out var prepared, out error))
        {
            return false;
        }

        definition.DisplayName = string.IsNullOrWhiteSpace(displayName) ? definition.Slug : displayName.Trim();
        definition.Color = color.ToUpperInvariant();
        definition.Aliases = prepared;
        RebuildAliasIndex();
        return TrySave(out error);
    }

    /// <summary>
    /// 重命名自定义标签的 slug（预置标签不可重命名）。
    /// </summary>
    public bool TryRename(string oldSlug, string newSlug, out string? error)
    {
        var definition = Find(oldSlug);
        if (definition == null)
        {
            error = $"标签 {TagNormalizer.Collapse(oldSlug)} 不存在。";
            return false;
        }
        if (definition.IsBuiltIn)
        {
            error = "预置标签不可重命名。";
            return false;
        }
        if (!TagNormalizer.TryValidateSlug(newSlug, out var canonical, out error))
        {
            return false;
        }

        var oldCanonical = definition.Slug;
        if (string.Equals(oldCanonical, canonical, StringComparison.OrdinalIgnoreCase))
        {
            error = null;
            return true;
        }
        if (Find(canonical) != null)
        {
            error = $"标签 {canonical} 已存在。";
            return false;
        }
        // 传 previousSlug：本标签自己的旧别名仍指向旧 slug，不能被判成"他人占用"
        if (!TryBuildAliases(canonical, definition.Aliases, out var prepared, out error, definition.Slug))
        {
            return false;
        }

        definition.Slug = canonical;
        definition.Aliases = prepared;
        if (string.Equals(definition.DisplayName, oldCanonical, StringComparison.OrdinalIgnoreCase))
        {
            definition.DisplayName = canonical;
        }
        SortTags();
        RebuildAliasIndex();
        return TrySave(out error);
    }

    /// <summary>
    /// 删除自定义标签定义（预置标签不可删除）。不会修改宿主档案。
    /// </summary>
    public bool TryRemove(string slug, out string? error)
    {
        var definition = Find(slug);
        if (definition == null)
        {
            error = $"标签 {TagNormalizer.Collapse(slug)} 不存在。";
            return false;
        }
        if (definition.IsBuiltIn)
        {
            error = "预置标签不可删除。";
            return false;
        }

        _tags.Remove(definition);
        RebuildAliasIndex();
        return TrySave(out error);
    }

    /// <summary>
    /// 导出全部定义（深拷贝，供设置页写出 JSON）。
    /// </summary>
    public TagDefinitionFile Export() => new()
    {
        SchemaVersion = CurrentSchemaVersion,
        Tags = _tags.Select(t => t.Clone()).ToList(),
    };

    /// <summary>
    /// 导入定义（合并语义）：同名 slug 覆盖显示名/颜色/别名，新 slug 追加；不删除现有标签。
    /// 返回成功应用的条目数，格式错误的条目写入 errors。
    /// 别名冲突按"内置优先、先到先得"重建索引，不做回滚。
    /// </summary>
    public int Import(TagDefinitionFile file, out List<string> errors)
    {
        errors = new List<string>();
        if (file?.Tags == null)
        {
            errors.Add("导入文件不包含标签定义。");
            return 0;
        }

        var applied = ApplyFile(file.Tags, errors);
        if (!TrySave(out var saveError) && saveError != null)
        {
            errors.Add(saveError);
        }
        return applied;
    }

    /// <summary>
    /// 保存定义文件（先写 .tmp 再原子替换）。
    /// </summary>
    public bool TrySave(out string? error)
    {
        error = null;
        var path = ConfigFilePath;
        if (path == null || _configFolder == null)
        {
            error = "插件配置目录不可用，标签定义未保存。";
            return false;
        }

        try
        {
            Directory.CreateDirectory(_configFolder);
            var json = JsonSerializer.Serialize(new TagDefinitionFile
            {
                SchemaVersion = CurrentSchemaVersion,
                Tags = _tags,
            }, JsonOptions);
            var temp = path + ".tmp";
            File.WriteAllText(temp, json);
            File.Move(temp, path, true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            error = ex.Message;
            return false;
        }
    }

    private void Load()
    {
        ResetToBuiltIn();

        var path = ConfigFilePath;
        if (path == null || !File.Exists(path))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(path);
            var file = JsonSerializer.Deserialize<TagDefinitionFile>(json, JsonOptions);
            if (file?.Tags != null)
            {
                ApplyFile(file.Tags, null);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or NotSupportedException or ArgumentException)
        {
            // 定义文件损坏或不可读：退回内置定义，绝不把异常抛给宿主
            ResetToBuiltIn();
        }
    }

    private int ApplyFile(IEnumerable<TagDefinition> tags, List<string>? errors)
    {
        var applied = 0;
        foreach (var item in tags)
        {
            if (item == null)
            {
                continue;
            }
            if (!TagNormalizer.TryValidateSlug(item.Slug, out var slug, out var slugError))
            {
                errors?.Add($"{item.Slug}: {slugError}");
                continue;
            }
            if (!TagNormalizer.IsValidColor(item.Color))
            {
                errors?.Add($"{slug}: 颜色格式应为 #RRGGBB。");
                continue;
            }

            var definition = Find(slug);
            if (definition == null)
            {
                definition = new TagDefinition { Slug = slug, IsBuiltIn = false };
                _tags.Add(definition);
            }

            definition.DisplayName = string.IsNullOrWhiteSpace(item.DisplayName) ? slug : item.DisplayName.Trim();
            definition.Color = item.Color.ToUpperInvariant();
            definition.Aliases = item.Aliases?
                .Select(TagNormalizer.Collapse)
                .Where(x => x.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();
            applied++;
        }

        SortTags();
        RebuildAliasIndex();
        return applied;
    }

    /// <summary>
    /// 校验并整理别名：折叠、去空、去自引用、去重；不得与其他标签的 slug 或别名冲突。
    /// <paramref name="previousSlug"/> 用于重命名场景：旧 slug 及其别名索引仍指向本标签，视为自引用。
    /// </summary>
    private bool TryBuildAliases(
        string slug,
        IEnumerable<string>? aliases,
        out List<string> prepared,
        out string? error,
        string? previousSlug = null)
    {
        prepared = new List<string>();
        error = null;
        if (aliases == null)
        {
            return true;
        }

        foreach (var raw in aliases)
        {
            var alias = TagNormalizer.Collapse(raw);
            if (alias.Length == 0)
            {
                continue;
            }
            if (string.Equals(alias, slug, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            if (prepared.Contains(alias, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            var owner = ResolveAlias(alias);
            var isSelf = owner != null &&
                         (string.Equals(owner, slug, StringComparison.OrdinalIgnoreCase) ||
                          (previousSlug != null && string.Equals(owner, previousSlug, StringComparison.OrdinalIgnoreCase)));
            if (owner != null && !isSelf)
            {
                error = $"别名“{alias}”已被标签 {owner} 使用。";
                return false;
            }

            var slugOwner = Find(alias);
            var slugIsSelf = slugOwner != null &&
                             (string.Equals(slugOwner.Slug, slug, StringComparison.OrdinalIgnoreCase) ||
                              (previousSlug != null && string.Equals(slugOwner.Slug, previousSlug, StringComparison.OrdinalIgnoreCase)));
            if (slugOwner != null && !slugIsSelf)
            {
                error = $"别名“{alias}”与标签 {slugOwner.Slug} 的 slug 冲突。";
                return false;
            }

            prepared.Add(alias);
        }

        return true;
    }

    private void RebuildAliasIndex()
    {
        _aliasIndex.Clear();

        // 先登记所有 slug，再登记别名：别名冲突时以 slug 与内置定义的先到者为准
        foreach (var tag in _tags)
        {
            _aliasIndex[tag.Slug] = tag.Slug;
        }
        foreach (var tag in _tags)
        {
            foreach (var raw in tag.Aliases)
            {
                var alias = TagNormalizer.Collapse(raw);
                if (alias.Length == 0 || string.Equals(alias, tag.Slug, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                _aliasIndex.TryAdd(alias, tag.Slug);
            }
        }
    }

    private void ResetToBuiltIn()
    {
        _tags.Clear();
        foreach (var definition in TagCatalog.BuiltIn)
        {
            _tags.Add(definition.Clone());
        }
        SortTags();
        RebuildAliasIndex();
    }

    private void SortTags()
    {
        _tags.Sort((a, b) =>
        {
            var builtInCompare = b.IsBuiltIn.CompareTo(a.IsBuiltIn);
            return builtInCompare != 0 ? builtInCompare : string.CompareOrdinal(a.Slug, b.Slug);
        });
    }

    private string NextColor()
    {
        var minIndex = 0;
        var minCount = int.MaxValue;
        for (var i = 0; i < Palette.Count; i++)
        {
            var count = _tags.Count(t => string.Equals(t.Color, Palette[i], StringComparison.OrdinalIgnoreCase));
            if (count >= minCount)
            {
                continue;
            }
            minCount = count;
            minIndex = i;
        }
        return Palette[minIndex];
    }
}
