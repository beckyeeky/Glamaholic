using System.Reflection;
using System.Text.Json;
using Glamaholic;
using Glamaholic.Interop;

var root = Path.Combine(AppContext.BaseDirectory, "Fixtures");
var json = File.ReadAllText(Path.Combine(root, "ec-359114.json"));
var local = JsonSerializer.Deserialize<LocalItem[]>(File.ReadAllText(Path.Combine(root, "cn-local-items.json")))!;
using var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("Glamaholic.EcNameIndex.json")!;
var index = JsonSerializer.Deserialize<EcNameIndex>(resource)!;
var logs = new List<string>();
var stainIds = new HashSet<uint> { 6, 10, 22, 24, 102 };
bool Valid(uint id, PlateSlot slot) => local.Any(x => x.Id == id && !x.SoulCrystal && x.Slots.Contains((int)slot));
EcNameResolver Resolver(EcNameIndex? data = null, Func<uint, PlateSlot, bool>? items = null, Func<uint, bool>? stains = null) =>
    new(data ?? index, items ?? Valid, stains ?? stainIds.Contains, logs.Add);
int checks = 0;
void Check(bool condition, string name) {
    if (!condition) throw new Exception("FAIL: " + name);
    checks++;
    Console.WriteLine("PASS: " + name);
}
string One(string? name = "Academic's Coat", string? dyes = null, string slot = "body") =>
    JsonSerializer.Serialize(new { gear = new Dictionary<string, object?> { [slot] = new { name, dyes } } });

// Reproduce the old name comparison against CN local names, before running the actual new resolver.
var glam = JsonSerializer.Deserialize<EcGlamour>(json)!;
int oldMatches = glam.Gear!.Values.Count(g => g != null && local.Any(i => i.Name.ToLowerInvariant() == g.Name!.ToLowerInvariant()));
Console.WriteLine($"RED evidence: legacy English-name comparison vs CN local names = {oldMatches}/6 (expected failure)");
Check(oldMatches == 0, "legacy comparison reproduces six misses");
if (args.Contains("--legacy-repro")) return;

var plate = Resolver().Import(json)!;
Check(plate.Items.Count == 6, "real embedded mapping imports all six with CN-only local names");
var expected = new (PlateSlot Slot, uint Id, byte Dye1, byte Dye2)[] {
    (PlateSlot.Body,33988,10,102), (PlateSlot.Hands,11841,10,22),
    (PlateSlot.Legs,32327,6,0), (PlateSlot.Feet,34021,102,10),
    (PlateSlot.MainHand,37755,0,0), (PlateSlot.Ears,41081,0,0),
};
foreach (var e in expected) {
    var actual = plate.Items[e.Slot];
    Check(actual.ItemId == e.Id && actual.Stain1 == e.Dye1 && actual.Stain2 == e.Dye2, $"{e.Slot}: ID={e.Id}, dyes={e.Dye1},{e.Dye2}");
}
Check(plate.Items.Values.Sum(x => x.Stain1 != 0 ? 1 : 0) + plate.Items.Values.Sum(x => x.Stain2 != 0 ? 1 : 0) == 7,
    "original fixture: seven nonzero colors and safe null/omitted channels");
// A deliberate ten-value variant: add two explicit none values to the original weapon.
var ten = File.ReadAllText(Path.Combine(root, "ec-six-ten-dyes.json"));
var tenGlam = JsonSerializer.Deserialize<EcGlamour>(ten)!;
Check(tenGlam.Gear!.Values.Where(x => x?.Dyes != null).Sum(x => x!.Dyes!.Split(',').Length) == 10,
    "synthetic fixture contains exactly ten supplied dye values");
var tenPlate = Resolver().Import(ten)!;
Check(tenPlate.Items.Count == 6 && expected.All(e => tenPlate.Items[e.Slot].ItemId == e.Id
    && tenPlate.Items[e.Slot].Stain1 == e.Dye1 && tenPlate.Items[e.Slot].Stain2 == e.Dye2),
    "all six IDs and ten supplied dye values resolved exactly");
foreach (var dyes in new string?[] { null, "", " ", ",", "none", "none,none" }) {
    var item = Resolver().Import(One(dyes: dyes))!.Items[PlateSlot.Body];
    Check(item.Stain1 == 0 && item.Stain2 == 0, "null/empty/none dyes safe: " + (dyes ?? "null"));
}
Check(Resolver().Import(One("  ACADEMIC’S\u00a0\t Coat  ", " DALAMUD-RED , JET-BLACK "))!.Items[PlateSlot.Body].Stain2 == 102,
    "invariant case, NBSP, whitespace and curly apostrophe");
Check(Resolver().Import(One(dyes: "opo-opo-brown"))!.Items[PlateSlot.Body].Stain1 == 24, "opo-opo stain exception");
Check(Resolver().Import(One("Academic Coat")) == null, "no fuzzy matching");
Check(Resolver().Import(One(slot: "feet")) == null, "wrong slot rejected");
Check(Resolver(items: (_, _) => false).Import(json) == null, "all local IDs absent produces null");
Check(Resolver().Import("null") == null && Resolver().Import("{}") == null && Resolver().Import("{\"gear\":null}") == null,
    "null/missing gear produces null");
var collision = new EcNameIndex {
    Items = new() { ["test"] = new uint[] { 1, 2, 3, 4 } },
    Stains = new() { ["ambiguous"] = new uint[] { 1, 2 }, ["absent"] = new uint[] { 3 }, ["overflow"] = new uint[] { 256 } },
};
var mock = new[] {
    new LocalItem { Id = 1, Name = "中文头", Slots = new[] { 2 } },
    new LocalItem { Id = 2, Name = "中文衣", Slots = new[] { 3 } },
    new LocalItem { Id = 3, Name = "魂石", Slots = new[] { 3 }, SoulCrystal = true },
};
bool MockValid(uint id, PlateSlot slot) => mock.Any(x => x.Id == id && !x.SoulCrystal && x.Slots.Contains((int)slot));
Check(Resolver(collision, MockValid).Import(One("test"))!.Items[PlateSlot.Body].ItemId == 2,
    "same name: slot filters, missing local row and soul crystal excluded");
Check(Resolver(collision, (_, _) => true).Import(One("test")) == null, "multiple local candidates are not first-selected");
foreach (var dye in new[] { "ambiguous", "absent", "overflow" }) {
    var item = Resolver(collision, MockValid, id => id is 1 or 2).Import(One("test", dye))!.Items[PlateSlot.Body];
    Check(item.Stain1 == 0, "invalid dye remains zero: " + dye);
}
Check(Resolver(stains: _ => false).Import(json)!.Items.Count == 6, "unavailable local stains preserve equipment");
var partial = Resolver(items: (id, slot) => id != 33988 && Valid(id, slot)).Import(json)!;
Check(partial.Items.Count == 5, "partial item failure keeps other five");
Check(logs.Any(x => x.Contains("1 item failures")) && logs.Any(x => x.Contains("dye failures")) && logs.Any(x => x.Contains("No equipment resolved")),
    "failure and empty summaries logged");
Check(Resolver().Import(One(dyes: "jet-black,dalamud-red,soot-black"))!.Items[PlateSlot.Body].Stain2 == 10,
    "existing two-channel behavior, no DyeCount truncation");
Check(Resolver().Import(One("Rinascita Samurai Blade", slot: "offhand")) == null,
    "negative Samurai OffHand category is not equip-compatible");
Console.WriteLine($"GREEN: {checks} checks passed");

sealed class LocalItem {
    public uint Id { get; set; }
    public string Name { get; set; } = "";
    public int[] Slots { get; set; } = Array.Empty<int>();
    public bool SoulCrystal { get; set; }
}
