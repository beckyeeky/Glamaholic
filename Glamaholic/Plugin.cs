using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using System;
using System.IO;

namespace Glamaholic {
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Plugin : IDalamudPlugin {
        internal static string Name => "Glamaholic";

        internal Configuration Config { get; }
        internal GameFunctions Functions { get; }
        internal PluginUi Ui { get; }
        private Commands Commands { get; }

        private DateTime LastInteropCheckTime { get; set; } = DateTime.Now;

#pragma warning disable 8618
        public Plugin(IDalamudPluginInterface pluginInterface) {
            pluginInterface.Create<Service>();

            TryMigrateLegacyConfig(Service.Interface!.ConfigFile);
            this.Config = Configuration.LoadAndMigrate(Service.Interface!.ConfigFile);

            this.Functions = new GameFunctions(this);
            this.Ui = new PluginUi(this);
            this.Commands = new Commands(this);

            Interop.Glamourer.Initialize(Service.Interface);
            Service.Framework.Update += OnFrameworkUpdate;
        }

        private static void TryMigrateLegacyConfig(FileInfo configFile) {
            if (configFile.Exists || configFile.DirectoryName == null)
                return;

            var legacyPath = Path.Combine(configFile.DirectoryName, "Glamaholic.json");
            if (!File.Exists(legacyPath))
                return;

            try {
                File.Copy(legacyPath, configFile.FullName, overwrite: false);
                configFile.Refresh();
                Service.Log.Info($"Migrated legacy Glamaholic configuration to {configFile.Name}");
            } catch (Exception e) {
                Service.Log.Warning(e, $"Failed to migrate legacy Glamaholic configuration from {legacyPath}");
            }
        }

        private void OnFrameworkUpdate(IFramework framework) {
            var now = DateTime.Now;
            if (now.Subtract(LastInteropCheckTime).TotalSeconds < 5)
                return;

            Interop.Glamourer.RefreshStatus(Service.Interface);

            LastInteropCheckTime = now;
        }
#pragma warning restore 8618

        public void Dispose() {
            this.Commands.Dispose();
            this.Ui.Dispose();
            this.Functions.Dispose();

            Service.Framework.Update -= OnFrameworkUpdate;
        }

        public void LogTroubleshooting(string message) {
            if (!Config.TroubleshootingMode)
                return;

            Service.Log.Info($"[Troubleshooting] {message}");
        }

        internal void SaveConfig() {
            Service.Interface.SavePluginConfig(this.Config);
        }
    }
}
