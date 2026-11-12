<h1>🗺️ CS2_MapCycle – Automatischer Kartenwechsel für CS2</h1>
<p>🇬🇧 <a href="README.md">English</a> | 🇩🇪 Deutsch</p>
<p>
<strong>CS2_MapCycle</strong> ist ein CounterStrikeSharp-Plugin für Counter-Strike 2.<br>
Es wechselt nach Matchende automatisch zur nächsten Karte aus deiner eigenen Rotation, der Reihe nach oder zufällig.
Die eingebaute CS2-Kartenabstimmung wird dabei ignoriert.
</p>
<ul>
  <li>Eigene Mapcycle-Datei mit normalen Maps und Workshop-Maps</li>
  <li>Reihenfolge oder Zufall, optional ohne Wiederholungen</li>
  <li>Chat-Ansage der nächsten Karte zu Beginn der letzten Runde</li>
  <li>Berücksichtigt <code>mp_maxrounds</code> live, auch wenn sich der Wert während des Matches ändert</li>
</ul>
<hr>
<h2>📦 Voraussetzungen</h2>
<ul>
  <li>CS2 Dedicated Server</li>
  <li><a href="https://github.com/roflmuffin/CounterStrikeSharp">CounterStrikeSharp</a> (mit Metamod:Source)</li>
</ul>
<h2>🔧 Installation</h2>
<ol>
  <li>Den Plugin-Ordner nach <code>game/csgo/addons/counterstrikesharp/plugins/CS2_MapCycle/</code> kopieren.</li>
  <li>Server starten. Die Config wird beim ersten Start automatisch angelegt.</li>
  <li>Die Mapcycle-Datei <code>game/csgo/mapcyclecustom.txt</code> anpassen (siehe unten).</li>
</ol>
<hr>
<h2>⚙️ Konfiguration</h2>
<p>Datei: <code>game/csgo/addons/counterstrikesharp/configs/plugins/CS2_MapCycle/CS2_MapCycle.json</code></p>
<pre><code>{
  "ConfigVersion": 1,
  "PluginEnabled": true,
  "EnableRandomMaps": false,
  "EnableNoDuplicateRandomMaps": true,
  "EnableNextMapMessage": true,
  "MapCycleFile": "mapcyclecustom.txt"
}
</code></pre>
<table>
  <thead>
    <tr><th>Option</th><th>Standard</th><th>Bedeutung</th></tr>
  </thead>
  <tbody>
    <tr><td><code>PluginEnabled</code></td><td><code>true</code></td><td>Plugin an/aus</td></tr>
    <tr><td><code>EnableRandomMaps</code></td><td><code>false</code></td><td>Nächste Karte zufällig statt der Reihe nach</td></tr>
    <tr><td><code>EnableNoDuplicateRandomMaps</code></td><td><code>true</code></td><td>Im Zufallsmodus erst alle Karten einmal spielen, bevor sich eine wiederholt</td></tr>
    <tr><td><code>EnableNextMapMessage</code></td><td><code>true</code></td><td>Chat-Ansage der nächsten Karte in der letzten Runde und bei Matchende</td></tr>
    <tr><td><code>MapCycleFile</code></td><td><code>mapcyclecustom.txt</code></td><td>Name der Mapcycle-Datei im Ordner <code>game/csgo/</code></td></tr>
  </tbody>
</table>
<hr>
<h2>🗒️ Mapcycle-Datei</h2>
<p>
Eine Karte pro Zeile. Zeilen mit <code>//</code> sind Kommentare.<br>
Workshop-Maps werden als <code>WorkshopID:Anzeigename</code> eingetragen. Der Anzeigename erscheint dann in der Chat-Ansage.
</p>
<pre><code>// Normale Karten
de_dust2
de_inferno
de_mirage

// Workshop-Karten (ID:Name)
3070923343:fy_pool_day
</code></pre>
<p>
Fehlt die Datei und heißt sie <code>mapcyclecustom.txt</code>, legt das Plugin automatisch eine Beispieldatei an.<br>
Bei einem eigenen Dateinamen wird <strong>nichts</strong> automatisch erstellt. Das Plugin meldet dann einen Fehler in der Konsole.
</p>
<hr>
<h2>⏱️ Zeitsteuerung nach Matchende</h2>
<p>Nach der letzten Runde bestimmen zwei Server-Einstellungen, wann gewechselt wird:</p>
<ul>
  <li><code>mp_win_panel_display_time</code>: Wie lange das Gewinn-/Verlust-Panel angezeigt wird</li>
  <li><code>mp_match_restart_delay</code>: Wie lange CS2 bis zum Neustart bzw. Kartenwechsel wartet</li>
</ul>
<p>
Das Plugin nimmt den <strong>größeren</strong> der beiden Werte und wechselt <strong>5 Sekunden vorher</strong> (mindestens sofort).<br>
Beispiel: <code>mp_win_panel_display_time 3</code> und <code>mp_match_restart_delay 15</code> → Wechsel nach 10 Sekunden.
</p>
<p>
So wechselt das Plugin die Karte, bevor CS2 selbst eingreift.
Wenn CS2 nach Ablauf der Zeit selbst wechseln will und <code>mp_endmatch_votenextmap 0</code> gesetzt ist, versucht das Spiel unter Umständen, eine leere Karte zu laden. Das kann den Server hängen lassen.
</p>
<h3>✅ Empfohlene Einstellungen</h3>
<pre><code>mp_win_panel_display_time 3
mp_match_restart_delay 15      // genug Zeit für die "Nächste Karte"-Ansage
mp_endmatch_votenextmap 1      // als Absicherung aktiv lassen, die Abstimmung wird ignoriert
</code></pre>
<hr>
<h2>🤖 Server nur mit Bots</h2>
<p>
Damit die Karte auch dann wechselt, wenn nur Bots spielen, müssen zwei Dinge stimmen:
</p>
<h3>1. Kein Ruhezustand</h3>
<pre><code>sv_hibernate_when_empty 0
</code></pre>
<p>Im Ruhezustand laufen keine Spiel-Ticks und damit auch keine Plugin-Timer.</p>
<h3>2. Kein Warmup ohne echte Spieler</h3>
<p>
Ohne echten Spieler läuft der Warmup-Timer in CS2 nicht ab, auch wenn <code>mp_warmuptime</code> gesetzt ist.
Die Bots spielen dann endlos Warmup, das Match endet nie, und es wird nie gewechselt.
Erst wenn ein Spieler joint, läuft alles weiter.
</p>
<p>Lösung: Das Warmup für Online-Matches abschalten.</p>
<pre><code>mp_warmup_online_enabled false
</code></pre>
<p>
⚠️ <strong>Wichtig:</strong> Diese Zeile gehört in die <code>gamemode_&lt;modus&gt;_server.cfg</code> (z. B. <code>gamemode_casual_server.cfg</code> bei <code>game_type 0</code> / <code>game_mode 0</code>).
CS2 lädt bei jedem Mapstart die Spielmodus-Config und überschreibt damit Werte aus der normalen Server-Config.
</p>
<p>
Hinweis: <code>mp_do_warmup_period</code> aus CS:GO gibt es in CS2 nicht mehr.
Welche Warmup-Befehle dein Server kennt, zeigt der Konsolenbefehl <code>find warmup</code>.
</p>
<hr>
<h2>📊 Zusammenfassung</h2>
<table>
  <thead>
    <tr><th>Einstellung</th><th>Bedeutung</th><th>Empfehlung</th></tr>
  </thead>
  <tbody>
    <tr><td><code>mp_win_panel_display_time</code></td><td>Dauer der Gewinn-/Verlust-Anzeige</td><td>3</td></tr>
    <tr><td><code>mp_match_restart_delay</code></td><td>Wartezeit nach Matchende</td><td>15</td></tr>
    <tr><td><code>mp_endmatch_votenextmap</code></td><td>CS2-Kartenabstimmung (vom Plugin ignoriert)</td><td>1</td></tr>
    <tr><td><code>sv_hibernate_when_empty</code></td><td>Ruhezustand ohne Spieler</td><td>0</td></tr>
    <tr><td><code>mp_warmup_online_enabled</code></td><td>Warmup bei Online-Matches</td><td>false</td></tr>
  </tbody>
</table>
<hr>
<h2>📝 Lizenz</h2>
<p>Siehe <code>LICENSE</code>.</p>