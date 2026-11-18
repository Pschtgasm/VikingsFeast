VikingFoodPlus 0.7 – Quellcode und 30 eingebettete Icons

1. Build-Windows.bat starten, Valheim- und Jotunn-Pfad angeben.
2. Die DLL aus bin\Release\netstandard2.1\VikingFoodPlus.dll ins passende BepInEx-Plugin-Verzeichnis kopieren.
3. Bei Multiplayer Mod auf Client und Server installieren.

Änderungen:
- 30 eigenständige PNG-Icons (256 x 256, RGBA), eingebettet als Assembly-Ressourcen.
- Vulkangierbraten statt Asksvin-Feuerbraten; Geierfleisch statt Asksvinfleisch.
- 3 zusätzliche Gerichte des Hohen Nordens, je 2 Portionen.
- Bestehende interne ID AsksvinFireRoast beibehalten, um Config/Save-Kompatibilität zu schonen.

ACHTUNG – noch im Spiel zu prüfen:
Die Deep-North-Prefabnamen MooseMeat, Poteitr, Kale, Lingonberry, Oats, OatMilk und SealBlubber sind derzeit NICHT gegen die tatsächlichen Game-Assets verifiziert. Wenn ein Prefab nicht existiert, protokolliert die Mod eine Warnung und überspringt das jeweilige Rezept. Die tatsächlichen Namen können über BepInEx-Log und Valheim-Assets geprüft werden.
Auch Fiddlehead, SmokePuff und VoltureMeat müssen in deiner Spielversion vorhanden sein. Das Projekt wurde hier mangels Valheim-/Jotunn-DLLs nicht kompiliert oder im Spiel getestet.

Die PNGs sind aus einer Sprite-Übersicht gewonnen; echte Alpha-Transparenz ist vorhanden, aber eine pixelgenaue manuelle Freistellprüfung wurde nicht durchgeführt.
