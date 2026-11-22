using Lumina.Excel.Sheets;
using System;
using System.Text.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace Glamaholic.Interop {
    internal class EorzeaCollection {
        private const string BASE_URL = "https://ffxiv.eorzeacollection.com/api/glamour/";

        // The default HttpClient UA is blocked by CloudFlare
        private const string USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36";

        private static readonly Lazy<EcNameIndex> NameIndex = new(() => {
            using var stream = typeof(EorzeaCollection).Assembly.GetManifestResourceStream("Glamaholic.EcNameIndex.json")
                ?? throw new InvalidOperationException("Embedded EC name index missing");
            return JsonSerializer.Deserialize<EcNameIndex>(stream)
                ?? throw new InvalidOperationException("Embedded EC name index invalid");
        });

        public static async Task<SavedPlate?> ImportFromURL(string userFacingURL) {
            string? url = ConvertURLForAPI(userFacingURL);
            if (url == null) {
                Service.Log.Error($"EorzeaCollection Import: Invalid URL '{userFacingURL}'");
                return null;
            }

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", USER_AGENT);

            HttpResponseMessage? resp = null;
            try {
                resp = await httpClient.GetAsync(url);
                if (!resp.IsSuccessStatusCode) {
                    Service.Log.Warning($"EorzeaCollection Import returned status code {resp.StatusCode}:\n{await resp.Content.ReadAsStringAsync()}");
                    return null;
                }
            } catch (Exception e) {
                Service.Log.Warning(e, $"EorzeaCollection Import: Request failed with Exception");
                return null;
            }

            if (resp == null) {
                Service.Log.Warning($"EorzeaCollection Import: Request failed with no response");
                return null;
            }

            try {
                // No English sheet is requested: CN installations can validate IDs in their default language.
                var items = Service.DataManager.GetExcelSheet<Item>();
                var stains = Service.DataManager.GetExcelSheet<Stain>();
                bool ValidItem(uint id, PlateSlot slot) {
                    var item = items.GetRowOrDefault(id);
                    if (!item.HasValue || !item.Value.EquipSlotCategory.IsValid)
                        return false;
                    var category = item.Value.EquipSlotCategory.Value;
                    return item.Value.EquipSlotCategory.RowId != 0 && category.SoulCrystal == 0
                        && Util.MatchesSlot(category, slot);
                }
                var resolver = new EcNameResolver(NameIndex.Value, ValidItem,
                    id => stains.GetRowOrDefault(id).HasValue,
                    message => Service.Log.Warning($"EorzeaCollection Import: {message}"));
                var resolved = resolver.Import(await resp.Content.ReadAsStringAsync());
                if (resolved == null) return null;
                SavedPlate plate = new(resolved.Name);
                foreach (var entry in resolved.Items) {
                    plate.Items.Add(entry.Key, new SavedGlamourItem {
                        ItemId = entry.Value.ItemId, Stain1 = entry.Value.Stain1, Stain2 = entry.Value.Stain2,
                    });
                }

                return plate;
            } catch (Exception e) {
                Service.Log.Warning($"EorzeaCollection Import: Failed to parse response: {e.Message}");
                return null;
            }
        }

        public static bool IsEorzeaCollectionURL(string url) {
            return ConvertURLForAPI(url) != null;
        }

        private static string? ConvertURLForAPI(string userFacingURL) {
            if (!Uri.TryCreate(userFacingURL, UriKind.Absolute, out var uri))
                return null;

            if (uri.Host != "ffxiv.eorzeacollection.com")
                return null;

            var path = uri.AbsolutePath;
            if (!path.StartsWith("/glamour/"))
                return null;

            var parts = path.Split("/");
            // [0] is empty, [1] is "glamour", [2] is the ID

            if (!Int64.TryParse(parts[2], out _))
                return null;

            return BASE_URL + parts[2];
        }

    }
}
