using Dalamud.Game.Command;
using System;

namespace Glamaholic {
    internal class Commands : IDisposable {
        private Plugin Plugin { get; }

        internal Commands(Plugin plugin) {
            this.Plugin = plugin;

            Service.CommandManager.AddHandler("/glamaholic", new CommandInfo(this.OnCommand) {
                HelpMessage = $"显示或隐藏 {Plugin.Name} 窗口",
            });
        }

        public void Dispose() {
            Service.CommandManager.RemoveHandler("/glamaholic");
        }

        private void OnCommand(string command, string arguments) {
            this.Plugin.Ui.ToggleMainInterface();
        }
    }
}
