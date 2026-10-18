# CourseTags

ClassIsland 课程标签提供者插件：为科目附加课程标签（户外、上机、自习等），供其他插件按标签决定运行状态。

> 本仓库由 AI（Trae）辅助开发。

## 机制

- 标签数据存储于宿主档案的科目附加设置（`Subject.AttachedObjects`），随档案导出导入与同步。
- 附加设置 GUID：`d722a5b9-d62a-472b-96a2-3abdfc4d3518`（协议本体，发布后永不更改）。
- 其他插件查询本插件数据**无需引用本程序集**：自定义同形 DTO 后按 GUID 调用
  `subject.GetAttachedObject<T>(guid)` 即可，未安装本插件时查询返回 null（自动降级）。

（消费端接入指南见下文"消费端接入"。）
