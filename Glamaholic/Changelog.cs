using System;
using System.Collections.Generic;
using System.Linq;

namespace Glamaholic {
    internal static class Changelog {
        internal record ChangelogEntry(Version Version, string[] Changes);

        internal static readonly List<ChangelogEntry> Entries = [
            new ChangelogEntry(new Version(1, 15, 2, 2), [
                "将插件标识调整为独立的 GlamaholicCN，避免自定义插件库被 Dalamud 官方库同名安全策略拦截。",
                "首次启动时若尚无 GlamaholicCN.json，会自动从原有 Glamaholic.json 只读复制配置，完整继承历史幻化板。",
            ]),
            new ChangelogEntry(new Version(1, 15, 2, 1), [
                "通过离线英文名称索引修复 Eorzea Collection 国服导入，按本地物品 ID 和装备槽位校验。",
                "汉化插件界面、帮助、命令说明与更新日志，保留原版配置和分享 JSON 格式。",
                "装备与染剂名称、搜索及染剂统计改为使用客户端默认语言。",
            ]),
            new ChangelogEntry(new Version(1, 12, 0, 0), [
                "现在可以通过右键菜单重命名文件夹。",
                "幻化板和文件夹支持拖动排序，已移除自动排序。",
                "改进第三方插件支持。",
            ]),
            
            new ChangelogEntry(new Version(1, 13, 0, 0), [
                "应用幻化板时，选择装备会考虑贵重染剂。",
                "-原先仅匹配全部条件；现在会优先寻找带有匹配贵重染剂或任一匹配染剂的装备。",
                "不再覆盖当前幻化板装备上已有的相同染色，避免浪费染剂。",
                "改进幻化板首次应用与刷新，使其能完整生效。",
            ]),
            
            new ChangelogEntry(new Version(1, 14, 0, 0), [
                "剪贴板导出加入 '(Dye)' 后缀，改善与其他插件的互通（感谢 @SubaruYashiro）。",
                "创建幻化板时支持选择文件夹（感谢 @firstmagic）。",
                "-“新建幻化板”按钮支持选择目标文件夹。",
                "-从剪贴板和 EC 导入时支持选择目标文件夹。",
                "-试穿和查看装备窗口的下拉菜单支持选择目标文件夹。",
                "-可以通过列表的右键菜单将幻化板移入文件夹。"
            ]),
        ];

        /// <summary>
        /// Gets all changelog entries that are newer than the specified version.
        /// </summary>
        internal static List<ChangelogEntry> GetEntriesSince(Version? lastSeenVersion) {
            if (lastSeenVersion == null) {
                return Entries;
            }

            return Entries
                .Where(e => e.Version > lastSeenVersion)
                .OrderByDescending(e => e.Version)
                .ToList();
        }

        /// <summary>
        /// Gets the latest version from the changelog.
        /// </summary>
        internal static Version? GetLatestVersion() {
            return Entries.Count > 0 
                ? Entries.Max(e => e.Version) 
                : null;
        }
    }
}
