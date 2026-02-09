# CS2_MapCycle
CS2_MapCycle ist ein leichtgewichtiges Mapcycle-Plugin für CounterStrikeSharp (CS2)
das den nächsten Mapwechsel zuverlässig steuert und Spielern automatisch die nächste Map zur richtigen Zeit ankündigt.
Das Plugin ist speziell für Public-Server ausgelegt und berücksichtigt variable Rundenzahlen, ohne feste Werte oder manuelle Eingriffe.

✨ Features

🔁 Automatischer Mapcycle
  Maps werden aus einer Textdatei geladen (mapcyclecustom.txt oder benutzerdefiniert)
  Unterstützung für Workshop-Maps (WorkshopID:Mapname)
  Reihenfolge oder Random-Rotation
  Optional keine doppelten Maps, bis der Cycle einmal durch ist

📢 NextMap-Anzeige (automatisch)
  Anzeige der nächsten Map am Start der letzten Runde
  Keine manuelle Eingabe nötig (!nextmap o.ä. wird nicht benötigt)
  Funktioniert zuverlässig auch auf Public-Servern

🧠 Stabile Rundenlogik
  Nutzt mp_maxrounds live (kann sich während der Map ändern)
  Berechnung basiert auf echten gespielten Runden
  Die Map wird 1 Runde vor Ende Angezeigt.

🛡️ Keine Doppelmaps
  Die nächste Map wird pro Map nur einmal festgelegt
  Manuelle Mapwechsel verursachen keine kaputte Rotation

🧩 Auto-Create für Standard-Mapcycle
  mapcyclecustom.txt wird automatisch erstellt, wenn sie fehlt
  Bei benutzerdefiniertem Dateinamen wird kein Auto-Create durchgeführt (absichtlich)
