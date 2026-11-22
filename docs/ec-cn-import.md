# Eorzea Collection 国服导入

## 原因与边界

EC API 提供英文装备名和染剂 slug，不提供物品或染剂 ID。旧代码通过英文 Item/Stain sheet 查名称。

当前安装的磁盘数据离线重放表明：Item.exh 仅包含 ChineseSimplified；英文原始 sheet 为 null。当前 Dalamud 在英文未加载时会将 English 回退到 ChineseSimplified。已有英文 overlay 包版本为 `2026.09.01`，游戏版本为 `2026.11.03`，加载器会跳过不匹配的包。因此请求英文 sheet 实际得到「学究外套」等中文名称，示例六个英文名精确比较全部失败。这是当前磁盘数据的离线重放证据，未读取运行中的插件服务，不代表所有国服安装均具有相同配置。

本修复仅修改 EC 导入路径，不修改全局 DataCache、语言包、游戏文件或 UI 接口。运行时不访问额外数据库服务，不依赖英文 sheet，也不按当前角色职业筛选。

## 解析规则

- 插件嵌入 `Resources/EcNameIndex.json`，内容仅为英文规范化名称到候选 ID 数组。当前包含 29,026 个装备名、125 个染剂名，共 1,024,383 字节。无需嵌入完整 CSV 或中文 Item 表。
- 生成时只收 `EquipSlotCategory != 0` 且 `SoulCrystal == 0` 的装备；同名不同 ID 保留为数组，不按先后选择。
- 名称使用 invariant 小写，折叠连续空白（含 NBSP），统一常见弯引号和 apostrophe。仅做这些明确转换，不做模糊匹配、不删除标点。
- 宿主以默认语言 Item sheet 按 ID 验证本地行、有效类别、排除魂石，再用现有 `Util.MatchesSlot` 验证目标槽。类别字段必须 `> 0`，例如武士刀的 OffHand 为 `-1`，不能当作可装备。
- 只有一个通过本地验证的候选才导入。候选不存在、错误槽、无效类别或仍有歧义时记录日志并跳过，不选 first。
- 染剂同样通过英文映射得到 ID，再在默认语言 Stain sheet 验证。`none`、缺失、null、空串及空通道均为 0。保留 `opo-opo-brown` → `opo-opo brown` 特例。未知、缺失或歧义染剂记录日志并回落到 0，不丢弃装备。
- 仅处理原有两个染色通道，不新增按 DyeCount 截断的行为，避免兼容性回归。第三个及以后通道维持原有忽略行为。
- 部分成功保留有效装备并记录汇总；全空导入返回 null，不创建空幻化板。

`EcNameResolver.cs` 不依赖 Dalamud/Lumina；`EorzeaCollection.cs` 负责 HTTP、资源加载、默认语言表验证和 SavedPlate 适配。UI 调用接口保持不变。

## 数据来源和权利说明

固定提交与逐文件 SHA256 均记录在 `tools/ec-data/sources.lock.json`：

| 来源 | 固定提交 | 使用内容 |
| --- | --- | --- |
| xivapi/ffxiv-datamining | `a9f3a282a0e6070fdc0469b6d4e3e7ea17e16edf` | `csv/en/Item.csv`、`Stain.csv`、`EquipSlotCategory.csv` |
| thewakingsands/ffxiv-datamining-cn | `ae29be8b21a32329e3356709422427540bf7e364` | Item.csv，仅用于六件中文本地名称交叉验证夹具 |

上游仓库：
- https://github.com/xivapi/ffxiv-datamining
- https://github.com/thewakingsands/ffxiv-datamining-cn

核查时，两仓库的 GitHub license API 均为 null，根目录 LICENSE 返回 404；未找到可确认的标准开源许可证。上游 README 描述了面向 FFXIV 社区的研究与数据分享用途，这不等同于获得 MIT 或其他标准许可证。数据及游戏内容的相关权利仍归原权利人（包括 SQUARE ENIX）所有；不得将本仓库代码许可证套用到游戏数据。发布或再分发前应自行确认数据使用授权。本说明不扩大上游授权范围。

## 生成、检查与显式更新

要求 Python 3.9+，仅使用标准库。命令均从仓库根目录执行。

```sh
# 默认仅使用 manifest 中已固定的提交；下载后逐文件核对 SHA256。
python tools/ec-data/generate.py

# 完全离线：核对已提交 JSON 的结构及 manifest 中的产物 SHA256。
python tools/ec-data/generate.py --check

# 已持有原始快照时：逐文件校验并验证逐字节可复现性。
python tools/ec-data/generate.py --check --source-dir /path/to/verified-csvs

# 显式更新英文上游：可传完整 SHA 或 ref，ref 会先解析成完整 40 位 SHA。
python tools/ec-data/generate.py --revision <SHA-or-ref>

# 更新到当前上游主分支（必须显式执行，不会默认追踪 latest）。
python tools/ec-data/generate.py --revision master
```

`--revision` 仅下载三张英文表，不要求重新下载中文全表；保留中文交叉验证快照。更新前验证必需字段、重复/冲突行 ID、ID 范围、类别标记和引用，再生成排序稳定的候选列表。所有下载、解析和验证成功后，才替换索引及来源 manifest；临时文件在仓库内，普通写入异常会恢复旧内容。操作系统断电等不可恢复中断不属于跨文件原子事务保证。更新后应检查 diff 并运行以下回归测试；本脚本不会提交、推送或部署。

如需刷新中文六行夹具：

```sh
python tools/ec-data/make_fixture.py --source-dir /path/to/verified-csvs
```

此目录需提供 manifest 指定的 `Item.csv`、`Stain.csv`、`EquipSlotCategory.csv`、`cn-Item.csv`。夹具中文名及类别 ID 来自 CN Item，类别的各槽位标记来自固定 EN EquipSlotCategory，不声称是国服实时 sheet 的完整快照。

## 回归测试

```sh
# .NET 9，无 NuGet 包依赖，直接链接生产 resolver 及 PlateSlot 源码。
dotnet run --project tests/EcResolver.Tests/EcResolver.Tests.csproj -- --legacy-repro
dotnet run --project tests/EcResolver.Tests/EcResolver.Tests.csproj

# 离线数据验证。
python tools/ec-data/test_data.py -v

# 加入全量快照校验、重新生成比对及魂石/非装备排除验证。
python tools/ec-data/test_data.py --source-dir /path/to/verified-csvs -v
```

红色复现结果为旧英文名比较 `0/6`。真实 resolver 回归覆盖原始 EC 359114 的六个装备 ID、两通道染色、中文本地名、null/遗漏/空染剂、NBSP、弯引号、同名按槽消歧、歧义拒绝、本地缺失、魂石排除、负数槽位标记和全空返回 null。`ec-six-ten-dyes.json` 是明确的合成变体：仅将原始武器的 null dyes 改为 `none,none`，形成恰好十个显式染剂值，不冒充原始 API 响应。

预期六件结果：body `33988/10/102`，hands `11841/10/22`，legs `32327/6/0`，feet `34021/102/10`，weapon `37755/0/0`，earrings `41081/0/0`（各项依次为 ItemId/Stain1/Stain2）。

独立测试验证离线解析策略，不替代 Dalamud 宿主完整编译或游戏内导入验证。此次已使用 .NET SDK 10.0.404 与本机 CN Dalamud 15.0.4.0 完成 Release 构建：0 个错误、6 个原有警告；原版基线具有同样的 6 个警告。游戏内实际导入仍待验证。

仓库的 `.github/workflows/ec-import-tests.yml` 在推送、拉取请求或手动触发时执行离线索引校验、生成器测试和真实 resolver 回归，不需要游戏数据或 Dalamud SDK。CI 不会更新数据库、推送生成结果或部署插件，也不代替完整宿主构建。

上游新增或改名装备需要显式更新快照；新国际服 ID 在旧国服本地不存在时会安全跳过。
