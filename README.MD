# Glamaholic (GlamaholicCN)

Glamaholic 是用于 FINAL FANTASY XIV 的 Dalamud 幻化模板管理插件。本分支以独立插件标识 `GlamaholicCN` 发布，在上游基础上提供中文界面、中文帮助和 Eorzea Collection（EC）国服导入兼容修复，同时保留原版配置格式并在首次启动时自动迁移已有配置。

## 功能与适用范围

- 保存不限数量的幻化模板，使用文件夹、标签和搜索整理搭配。
- 在幻化投影台编辑幻化板时，一键启用最多 15 套模板；插件保存数量不等于游戏内幻化板槽位数量。
- 从幻化板编辑、查看装备和试穿窗口保存搭配；支持 JSON 导入、导出和分享。
- 提供中文界面、帮助、命令说明与更新日志。
- 使用内置的英文名称 → 物品／染剂 ID 离线索引修复 EC 国服导入，不依赖客户端英文物品表。下载 EC 搭配仍需联网，“离线”仅指名称解析不调用额外数据库服务。
- 装备列表、装备名称搜索和染剂统计使用客户端默认语言：国服使用中文，英文客户端使用英文。
- 支持同模装备查询；安装并加载 Glamourer 后可使用其试穿功能。

导入时会验证本地是否存在对应 ID 及其装备槽位，不会猜测歧义名称。国际服新增但国服尚未实装的装备会安全跳过；全部装备均无法解析时不创建空幻化板。离线回归测试不能替代游戏内验证。

## 原作者、上游与许可证

- 原作者 **Anna**：[原始仓库](https://git.anna.lgbt/anna/Glamaholic)，[Ko-fi 支持页面](https://ko-fi.com/lojewalo)。
- 上游维护者 **Caitlyn**：[caitlyn-gg/Glamaholic](https://github.com/caitlyn-gg/Glamaholic)。
- 本分支：[beckyeeky/Glamaholic](https://github.com/beckyeeky/Glamaholic)，提供中文适配与国服 EC 导入兼容修改，不替代或变更原作者署名。

插件元数据中的作者保持为 `Anna, Caitlyn`。代码沿用 **EUPL v1.2** 许可证，具体条款见 [LICENCE](LICENCE)。第三方依赖及游戏数据的权利边界见下文，不能将代码许可证套用到游戏数据。

## 通过自定义插件库安装

订阅链接：

```text
https://raw.githubusercontent.com/beckyeeky/Glamaholic/main/repo.json
```

1. 打开 **Dalamud 设置 → 实验性 → 自定义插件库**。
2. 添加上述链接并保存，然后刷新插件列表。
3. 查找 `GlamaholicCN` 并安装。

本分支使用独立的 `InternalName: GlamaholicCN`，不会被 Dalamud 禁止第三方库覆盖官方插件的安全策略拦截。首次启动时，若尚未生成 `GlamaholicCN.json`，插件会自动从原版 `Glamaholic.json` 只读复制配置（不会修改或删除原文件），完整继承已有幻化板与文件夹。确认迁移无误后，建议停用或卸载官方库版 `Glamaholic`，以免两个插件同时响应 `/glamaholic` 命令或重复显示辅助菜单。

## 与原版的兼容性

1. **配置文件格式 100% 通用，且首次启动自动迁移。** 未修改配置属性名、JSON 序列化字段、`NodeType` 的 `Folder`／`Plate` 值或装备槽位枚举值。由于独立标识使用 `GlamaholicCN.json`，首次加载会自动复制原有 `Glamaholic.json`（只读复制，不改动原文件）。原有幻化板名称、文件夹名称和标签不会自动翻译；仅新建默认名称改为中文。
2. **幻化板 JSON 双向互通。** `SharedPlate.cs` 与分享 JSON 格式保持不变，可在本分支与原版之间导入、导出同一分享字符串。分享 JSON 应通过“分享”按钮取得。
3. **纯文本导出发生显示变化。** “导出为纯文本”使用中文槽位名称，装备与染剂名称跟随客户端默认语言，未知项提示改为中文。这是供人阅读的文本，不是 JSON 分享格式；依赖英文槽位名称或英文物品名的第三方文本解析器可能需要调整。
4. **Glamourer IPC 保持互通。** 未修改 IPC 调用、槽位转换或 `Glamourer` 插件识别名；功能要求 Glamourer 已安装、加载且 IPC 可用。
5. **命令和搜索语法不变。** `/glamaholic` 显示或隐藏主窗口；`/tryonglamourer` 通过 Glamourer 试穿 EC 搭配，链接可来自命令参数或剪贴板。后者仅在 Glamourer 已加载时注册。高级搜索前缀 `j:`、`lvl:`、`t:`、`!t:`、`id:`、`i:` 全部保留。
6. **DataCache 改用默认语言。** 全部装备列表、按名称搜索及染剂物品用量改为读取客户端默认语言表；英文客户端仍显示英文名称。国服名称查询应使用中文，不再依赖英文表回退。职业属性反射所需的 `ClassJob` 英文表保持不变，以匹配 `PLD`／`WAR` 等代码属性。EC 仍使用独立的离线英文名称索引，不受 UI 名称语言影响。
7. **窗口与游戏接口保持稳定。** 主窗口保留原名，已有 `##`／`###` ID 后缀保留；批量导入与更新日志窗口使用原英文名作为 `###` 锚点。游戏 Addon 名称未修改。

## 使用指南

### 添加、保存与导入幻化板

- 输入 `/glamaholic` 打开主窗口，选择“幻化板 → 新建”，然后编辑装备与染剂。列表底部的“新建幻化板”按钮可选择目标文件夹。
- 复制他人分享的幻化板 JSON，选择“幻化板 → 从剪贴板导入”，再选择根目录或目标文件夹。
- 在幻化投影台打开幻化板编辑窗口，通过左上角 Glamaholic 菜单选择“创建幻化板”，输入名称后按 Enter 保存。也可从“查看装备”或“试穿”窗口的同名菜单保存搭配。
- 复制 EC 幻化页面链接，在“Eorzea Collection → 从链接导入”中选择新建幻化板或试穿；“批量导入”窗口会在复制新的有效链接时导入。
- 应用模板前，先在幻化投影台打开游戏内幻化板编辑窗口。收藏柜物品不可用时，先打开一次收藏柜以加载数据。

### 查询同模装备

在装备名称或图标上单击鼠标中键，或按住 Control 并单击鼠标左键，即可查看同模装备。列表中左键单击可发送装备链接到聊天，右键单击可试穿。

### 高级搜索

在幻化板搜索框输入条件，可用空格组合多个条件。含空格的标签或装备名称使用英文双引号。

| 语法 | 用途 | 示例 |
| --- | --- | --- |
| `j:` | 按可穿戴职业缩写筛选 | `j:pld`（骑士）、`j:war`（战士） |
| `lvl:` | 所需穿戴等级不超过指定值 | `lvl:1` |
| `t:` | 包含标签 | `t:日常`、`t:"日常 搭配"` |
| `!t:` | 排除标签 | `!t:活动` |
| `id:` | 包含指定物品 ID | `id:33988` |
| `i:` | 装备名称匹配客户端默认语言 | 国服 `i:学究`；英文客户端 `i:skallic` |

## 上游依赖与数据更新

### 插件代码与框架依赖

- **上游插件代码**：[caitlyn-gg/Glamaholic](https://github.com/caitlyn-gg/Glamaholic)；原作者仓库为 [Anna/Glamaholic](https://git.anna.lgbt/anna/Glamaholic)。合并上游时应复核 API 适配、配置迁移和界面文本，不能覆盖已验证的 EC 解析边界。
- **Git 子模块**：[Ottermandias/Glamourer.Api](https://github.com/Ottermandias/Glamourer.Api)，路径为 `thirdparty/Glamourer.Api`；IPC 类型和接口随子模块固定提交维护。
- **Dalamud.NET.Sdk**：当前项目使用 `15.0.0`，面向 Dalamud API 15 与 `.NET 10`（`net10.0-windows7.0`）。构建需要匹配的 Dalamud 宿主程序集。
- **Lumina**：使用 Dalamud 提供的 Excel 数据接口读取本地物品、染剂、装备槽位和职业数据，须与宿主版本匹配。
- **Fody / Resourcer.Fody**：当前版本分别为 `6.8.0` / `1.8.1`，用于构建期资源嵌入。NuGet 锁文件为 `Glamaholic/packages.lock.json`。

### EC 名称索引的来源

固定提交、下载地址及逐文件 SHA-256 记录在 [tools/ec-data/sources.lock.json](tools/ec-data/sources.lock.json)。运行时只加载嵌入的精简名称索引，不加载完整 CSV 或中文全表。

| 数据仓库 | 固定提交 | 用途 |
| --- | --- | --- |
| [xivapi/ffxiv-datamining](https://github.com/xivapi/ffxiv-datamining) | `a9f3a282a0e6070fdc0469b6d4e3e7ea17e16edf` | 英文 `Item.csv`、`Stain.csv`、`EquipSlotCategory.csv`；生成英文名称到候选 ID 的索引并过滤非装备、魂石 |
| [thewakingsands/ffxiv-datamining-cn](https://github.com/thewakingsands/ffxiv-datamining-cn) | `ae29be8b21a32329e3356709422427540bf7e364` | 中文 `Item.csv`，仅用于六件装备的测试夹具交叉验证，不作为运行时中文名称数据库 |

核查时，两个数据仓库均未找到可确认的标准开源许可证：GitHub license API 为 null，根目录 LICENSE 返回 404。社区研究与数据分享说明不等于 MIT 或其他标准许可证授权。游戏数据及相关内容的权利仍归原权利人（包括 **SQUARE ENIX**）所有；再分发前应自行确认授权，不能依据本项目 EUPL v1.2 推定游戏数据可自由使用。

详细解析规则、来源证据和限制见 [docs/ec-cn-import.md](docs/ec-cn-import.md)。该文档记录 EC 修复阶段的范围与验证结果；本分支进一步增加 UI 汉化和 DataCache 默认语言显示。

### 显式更新与校验

从仓库根目录运行，要求 Python 3.9+，工具仅依赖标准库。数据库不会自动追踪上游最新版本。

```sh
# 显式更新英文数据库；将 master 解析并固定为完整提交 SHA。
python tools/ec-data/generate.py --revision master

# 离线核对已提交索引的结构及产物 SHA-256。
python tools/ec-data/generate.py --check

# 回归校验生成器和已提交数据。
python tools/ec-data/test_data.py -v
```

更新完成后检查索引与来源清单的差异，并运行真实解析器测试。新增或改名装备需要更新快照；旧国服不存在的国际服 ID 仍会跳过，不应通过模糊匹配或强行选择首个候选绕过校验。

## 本地构建与测试

需要 Git（含子模块）、Python 3.9+、.NET 10 SDK、匹配 API 15 的 Dalamud 开发程序集；独立解析器测试目标为 `net9.0`，还需要 .NET 9 运行时。

```sh
git submodule update --init --recursive
python tools/ec-data/generate.py --check
python tools/ec-data/test_data.py -v
dotnet run --project tests/EcResolver.Tests/EcResolver.Tests.csproj
dotnet build Glamaholic/Glamaholic.csproj -c Release
git diff --check
```

Windows 使用开发环境配置的 Dalamud 路径；在非默认目录中构建时可通过 MSBuild 的 `DalamudLibPath` 指定对应程序集目录，不要将本机绝对路径写入项目文件。测试覆盖 EC 物品与染剂解析、中文本地行验证、歧义拒绝、槽位校验及空结果处理。CI 的离线测试不替代完整宿主构建，也不证明游戏内导入已经验证。

## 生成插件库索引与发布

Release 构建产生 `Glamaholic/bin/Release/GlamaholicCN/GlamaholicCN.json`。生成工具从该清单读取作者、版本、API 等元数据，输出标准 Dalamud 自定义插件库 JSON 数组；不会修改原作者或插件标识。

```sh
# 默认读取 Release 构建清单，在仓库根目录生成 repo.json。
python tools/generate_repo.py v1.15.2.2

# 也可显式指定标签、清单和输出路径。
python tools/generate_repo.py --tag v1.15.2.2 --manifest Glamaholic/bin/Release/GlamaholicCN/GlamaholicCN.json --output repo.json
```

`LastUpdate` 默认使用生成时的 Unix 秒级整数时间戳，可用 `--last-update` 显式指定以便复现。三个下载字段均指向 `https://github.com/beckyeeky/Glamaholic/releases/download/{tag}/latest.zip`。

发布前应先完成真实 Release 构建，再运行生成工具刷新 `repo.json`，确认 `InternalName` 为 `GlamaholicCN`、原作者为 `Anna, Caitlyn`、版本号及 API 等字段与构建一致，随后将构建产物 `latest.zip` 上传到对应 GitHub Release，并将刷新后的 `repo.json` 推送到 `main`。生成工具不上传、不提交、不推送、不部署到游戏目录。
