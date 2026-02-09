using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;
using System.Text.Json.Serialization;

namespace CS2_MapCycle;

public class CS2_MapCycleConfig : BasePluginConfig
{
    [JsonPropertyName("ConfigVersion")] public override int Version { get; set; } = 1;
    [JsonPropertyName("PluginEnabled")] public bool PluginEnabled { get; set; } = true;

    [JsonPropertyName("EnableRandomMaps")] public bool EnableRandomMaps { get; set; } = false;
    [JsonPropertyName("EnableNoDuplicateRandomMaps")] public bool EnableNoDuplicateRandomMaps { get; set; } = true;

    [JsonPropertyName("EnableNextMapMessage")] public bool EnableNextMapMessage { get; set; } = true;

    [JsonPropertyName("MapCycleFile")] public string MapCycleFile { get; set; } = "mapcyclecustom.txt";
}

public class MapEntry
{
    public string IdOrName { get; set; } = "";
    public string DisplayName { get; set; } = "";
}

public class CS2_MapCycle : BasePlugin, IPluginConfig<CS2_MapCycleConfig>
{
    public override string ModuleName => "CS2_MapCycle";
    public override string ModuleVersion => "1.4.2";
    public override string ModuleAuthor => "ddbmaster";
    public override string ModuleDescription => "Mapcycle (mit letzte-Runde-Ansage)";

    public required CS2_MapCycleConfig Config { get; set; } = new();

    private const string DefaultMapCycleFile = "mapcyclecustom.txt";

    private string MapCycleFilePath = "";
    private List<MapEntry> MapCycleList = new();
    private List<MapEntry> MapCycleInUse = new();
    private int MapIndex = 0;
    private readonly Random Rng = new();

    private MapEntry? NextMap = null;
    private bool NextMapAnnounced = false;

    public override void Load(bool hotReload)
    {
        Console.WriteLine($"[{ModuleName}] v{ModuleVersion} wird geladen");

        if (!Config.PluginEnabled)
        {
            Console.WriteLine($"[{ModuleName}] Plugin ist deaktiviert.");
            return;
        }

        MapCycleFilePath = Path.Combine(Server.GameDirectory, "csgo", Config.MapCycleFile);

        // ✅ Auto-Erstellung nur für Default-Dateiname
        if (!File.Exists(MapCycleFilePath))
        {
            if (Config.MapCycleFile.Equals(DefaultMapCycleFile, StringComparison.OrdinalIgnoreCase))
            {
                File.WriteAllLines(MapCycleFilePath, new[]
                {
                    "// CS2_MapCycle – automatisch erstellt",
                    "// Eine Map pro Zeile (oder WorkshopID:Name)",
                    "de_dust2",
                    "de_inferno",
                    "de_mirage",
                    "de_nuke"
                });

                Console.WriteLine($"[{ModuleName}] Standard-Mapcycle erstellt: {MapCycleFilePath}");
            }
            else
            {
                Console.WriteLine($"[{ModuleName}] FEHLER: Mapcycle-Datei fehlt: {MapCycleFilePath}");
                Console.WriteLine($"[{ModuleName}] Kein Auto-Create bei benutzerdefiniertem Dateinamen!");
                return;
            }
        }

        LoadMapcycle();

        RegisterListener<Listeners.OnMapStart>(OnMapStart);
        RegisterListener<Listeners.OnMapEnd>(OnMapEnd);

        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        RegisterEventHandler<EventCsWinPanelMatch>(OnWinPanelMatch);
    }

    public void OnConfigParsed(CS2_MapCycleConfig config) => Config = config;

    // ---------- GameRules Helper (statt Utilities.GetGameRules) ----------
    private static CCSGameRules? TryGetGameRules()
    {
        // DesignerName: "cs_gamerules"
        var proxy = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules")
            .FirstOrDefault();

        return proxy?.GameRules;
    }

    private void LoadMapcycle()
    {
        MapCycleList = File.ReadAllLines(MapCycleFilePath)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith("//"))
            .Select(l =>
            {
                l = l.ToLower();

                if (l.Contains(":"))
                {
                    var p = l.Split(':', 2);
                    return new MapEntry
                    {
                        IdOrName = p[0].Trim(),
                        DisplayName = p[1].Trim()
                    };
                }

                return new MapEntry { IdOrName = l, DisplayName = l };
            })
            .ToList();

        MapCycleInUse = (Config.EnableRandomMaps && Config.EnableNoDuplicateRandomMaps)
            ? DistinctById(MapCycleList)
            : MapCycleList.ToList();

        Console.WriteLine($"[{ModuleName}] Maps geladen: {MapCycleList.Count}");
        foreach (var m in MapCycleList)
            Console.WriteLine($"  - {m.IdOrName} ({m.DisplayName})");

        if (MapCycleInUse.Count == 0)
            Console.WriteLine($"[{ModuleName}] WARNUNG: Keine Maps im Mapcycle!");
    }

    private static List<MapEntry> DistinctById(List<MapEntry> input)
        => input.GroupBy(m => m.IdOrName).Select(g => g.First()).ToList();

    private void OnMapStart(string mapName)
    {
        NextMapAnnounced = false;

        // NextMap pro Map einmal festlegen
        NextMap ??= PickAndConsumeNextMap();

        Console.WriteLine($"[{ModuleName}] OnMapStart: aktuelle Map = {mapName},BetreutesZocken nächste Map = {NextMap?.IdOrName}");
    }

    private void OnMapEnd()
    {
        // nichts erzwingen
    }

    // ✅ „letzte Runde startet“ – variabel, live
    private HookResult OnRoundStart(EventRoundStart ev, GameEventInfo info)
    {
        if (!Config.EnableNextMapMessage || NextMapAnnounced)
            return HookResult.Continue;

        NextMap ??= PickAndConsumeNextMap();
        if (NextMap == null || string.IsNullOrWhiteSpace(NextMap.IdOrName))
            return HookResult.Continue;

        var rules = TryGetGameRules();
        if (rules == null)
            return HookResult.Continue;

        // mp_maxrounds LIVE lesen (kann sich ändern)
        int maxRounds = ConVar.Find("mp_maxrounds")?.GetPrimitiveValue<int>() ?? 0;
        if (maxRounds <= 0)
            return HookResult.Continue;

        // RoundStart: aktuelle Runde läuft schon an, TotalRoundsPlayed zählt meist beendete Runden
        int roundsPlayedIncludingCurrent = rules.TotalRoundsPlayed + 1;
        int roundsLeftAfterThisStarts = maxRounds - roundsPlayedIncludingCurrent;

        // 0 => diese Runde ist die letzte
        if (roundsLeftAfterThisStarts == 0)
        {
            Server.PrintToChatAll($"BetreutesZocken nach dieser Runde kommt: {NextMap.DisplayName}");
            NextMapAnnounced = true;
        }

        return HookResult.Continue;
    }

    private HookResult OnWinPanelMatch(EventCsWinPanelMatch ev, GameEventInfo info)
    {
        NextMap ??= PickAndConsumeNextMap();

        if (NextMap == null || string.IsNullOrWhiteSpace(NextMap.IdOrName))
        {
            Console.WriteLine($"[{ModuleName}] FEHLER: NextMap ist leer – kein Mapwechsel.");
            return HookResult.Continue;
        }

        float winPanel = ConVar.Find("mp_win_panel_display_time")?.GetPrimitiveValue<float>() ?? 1f;
        int restart = ConVar.Find("mp_match_restart_delay")?.GetPrimitiveValue<int>() ?? 1;
        float delay = Math.Max(0, Math.Max(winPanel, restart) - 5);

        if (Config.EnableNextMapMessage)
            Server.PrintToChatAll($"BetreutesZocken Jetzige Map kommt: {NextMap.DisplayName}");

        var mapToGo = NextMap;
        AddTimer(delay, () =>
        {
            Console.WriteLine($"[{ModuleName}] Wechsel zu {mapToGo.IdOrName}");

            if (ulong.TryParse(mapToGo.IdOrName, out _))
                Server.ExecuteCommand($"host_workshop_map {mapToGo.IdOrName}");
            else
                Server.ExecuteCommand($"map {mapToGo.IdOrName}");
        });

        // für die nächste Map neu setzen
        NextMap = null;
        NextMapAnnounced = false;

        return HookResult.Continue;
    }

    // Konsumiert State (keine Doppelmaps)
    private MapEntry? PickAndConsumeNextMap()
    {
        if (MapCycleList.Count == 0)
            return null;

        if (MapCycleInUse.Count == 0)
        {
            MapCycleInUse = (Config.EnableRandomMaps && Config.EnableNoDuplicateRandomMaps)
                ? DistinctById(MapCycleList)
                : MapCycleList.ToList();
        }

        if (MapCycleInUse.Count == 0)
            return null;

        MapEntry next;

        if (Config.EnableRandomMaps)
        {
            int idx = Rng.Next(MapCycleInUse.Count);
            next = MapCycleInUse[idx];

            if (Config.EnableNoDuplicateRandomMaps)
                MapCycleInUse.RemoveAt(idx);

            if (MapCycleInUse.Count == 0)
            {
                MapCycleInUse = Config.EnableNoDuplicateRandomMaps
                    ? DistinctById(MapCycleList)
                    : MapCycleList.ToList();
            }
        }
        else
        {
            next = MapCycleInUse[Math.Clamp(MapIndex, 0, MapCycleInUse.Count - 1)];
            MapIndex = (MapIndex + 1) % MapCycleInUse.Count;
        }

        return next;
    }
}
