using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Glamaholic.Interop {
    // No Dalamud/Lumina dependency: candidates are accepted only by the host's local-sheet validator.
    internal sealed class EcNameIndex {
        public Dictionary<string, uint[]> Items { get; set; } = new();
        public Dictionary<string, uint[]> Stains { get; set; } = new();
    }

    internal sealed class EcGear {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("dyes")] public string? Dyes { get; set; }
    }

    internal sealed class EcGlamour {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("gear")] public Dictionary<string, EcGear?>? Gear { get; set; }
    }

    internal sealed class EcResolvedItem {
        public uint ItemId { get; init; }
        public byte Stain1 { get; init; }
        public byte Stain2 { get; init; }
    }

    internal sealed class EcResolvedPlate {
        public string Name { get; init; } = "Eorzea Collection";
        public Dictionary<PlateSlot, EcResolvedItem> Items { get; } = new();
    }

    internal sealed class EcNameResolver {
        private readonly EcNameIndex index;
        private readonly Func<uint, PlateSlot, bool> validItem;
        private readonly Func<uint, bool> validStain;
        private readonly Action<string> log;

        public EcNameResolver(EcNameIndex index, Func<uint, PlateSlot, bool> validItem,
            Func<uint, bool> validStain, Action<string> log) {
            this.index = index;
            this.validItem = validItem;
            this.validStain = validStain;
            this.log = log;
        }

        public static string Normalize(string? value) {
            var result = new StringBuilder();
            bool space = false;
            foreach (char c in value ?? "") {
                if (char.IsWhiteSpace(c)) { space = result.Length > 0; continue; }
                if (space) result.Append(' ');
                space = false;
                result.Append(c is '\u2018' or '\u2019' or '\u02bc' or '\uff07' ? '\'' : char.ToLowerInvariant(c));
            }
            return result.ToString();
        }

        public static string NormalizeDye(string? value) {
            // The only hyphen inside an English stain name; other EC hyphens represent spaces.
            var name = Normalize(value);
            return name == "opo-opo-brown" ? "opo-opo brown" : name.Replace('-', ' ');
        }

        private uint? Resolve(Dictionary<string, uint[]> names, string name, Func<uint, bool> valid, string kind) {
            if (!names.TryGetValue(name, out var candidates)) {
                log($"{kind} '{name}': no English mapping");
                return null;
            }
            var accepted = new List<uint>();
            foreach (uint id in candidates.Distinct()) {
                if (id != 0 && valid(id)) accepted.Add(id);
                else log($"{kind} '{name}': candidate {id} absent locally, wrong slot, or invalid equipment category");
            }
            if (accepted.Count == 1) return accepted[0];
            log($"{kind} '{name}': {accepted.Count} valid candidates (ambiguous or unavailable); skipped");
            return null;
        }

        public EcResolvedPlate? Import(string json) {
            var glam = JsonSerializer.Deserialize<EcGlamour>(json);
            var plate = new EcResolvedPlate { Name = glam?.Name ?? "Eorzea Collection" };
            int attempted = 0, failedItems = 0, failedDyes = 0;
            foreach (var entry in glam?.Gear ?? new Dictionary<string, EcGear?>()) {
                if (entry.Key is "fashion" or "facewear" || entry.Value == null) continue;
                if (!TrySlot(entry.Key, out var slot)) {
                    log($"Unknown slot '{entry.Key}'; skipped");
                    failedItems++;
                    continue;
                }
                attempted++;
                var name = Normalize(entry.Value.Name);
                var id = Resolve(index.Items, name, candidate => validItem(candidate, slot), $"Item ({entry.Key})");
                if (id == null) { failedItems++; continue; }
                var dyes = entry.Value.Dyes?.Split(',') ?? Array.Empty<string>();
                byte Dye(int channel) {
                    if (channel >= dyes.Length) return 0;
                    var dye = NormalizeDye(dyes[channel]);
                    if (dye.Length == 0 || dye == "none") return 0;
                    var stain = Resolve(index.Stains, dye, candidate => candidate <= byte.MaxValue && validStain(candidate), "Stain");
                    if (stain != null) return (byte)stain.Value;
                    failedDyes++;
                    return 0;
                }
                plate.Items.Add(slot, new EcResolvedItem { ItemId = id.Value, Stain1 = Dye(0), Stain2 = Dye(1) });
            }
            log($"Import summary: {plate.Items.Count}/{attempted} items imported, {failedItems} item failures, {failedDyes} dye failures");
            if (plate.Items.Count != 0) return plate;
            log("No equipment resolved; no plate created");
            return null;
        }

        private static bool TrySlot(string name, out PlateSlot slot) {
            PlateSlot? parsed = name switch {
                "weapon" => PlateSlot.MainHand, "offhand" => PlateSlot.OffHand,
                "head" => PlateSlot.Head, "body" => PlateSlot.Body, "hands" => PlateSlot.Hands,
                "legs" => PlateSlot.Legs, "feet" => PlateSlot.Feet, "earrings" => PlateSlot.Ears,
                "necklace" => PlateSlot.Neck, "bracelets" => PlateSlot.Wrists,
                "left_ring" => PlateSlot.LeftRing, "right_ring" => PlateSlot.RightRing, _ => null,
            };
            slot = parsed ?? default;
            return parsed.HasValue;
        }
    }
}
