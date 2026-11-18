Viking's Feast - eigene 3D-Modelle
==================================

Neu: Runenkessel, Gaerfass und Runen-Trockengestell bestehen nicht mehr aus Wuerfeln, sondern aus echten Meshes
(je Stufe Sumpf / Nebelland / Hoher Norden).
Die Eisdestillerie (3. Erweiterung) ist ab v1.4 registriert (siehe unten).

Dateien
  Models/*.obj, *.mtl      Modelle (werden in die DLL eingebettet, Blender-kompatibel)
  src/ModelLoader.cs       OBJ-Loader + CauldronStageVisuals (schaltet die Optik je Ausbaustufe)
  tools/make_models.py     Generator (Python, numpy + pillow) -> schreibt Models/ und preview_*.png
  tools/preview_*.png      Vorschau (Software-Renderer, ohne Valheim-Beleuchtung)

Runenkessel-Stufen
  Objekte mit "B_"  immer sichtbar
  Objekte "S1_".."S4_"  nur auf der jeweiligen Stufe (1 Standard, 2 Sumpf, 3 Nebelland, 4 Hoher Norden)
  Die Stufe liest CraftingStation.GetLevel() alle 0,5 s.
  Gaerfass, Trockengestell und Destillerie (S2_..S4_) uebernehmen die Stufe des naechsten Kessels (ExtensionStageVisuals):
  Gaerfass allein = Sumpf, sobald das Trockengestell steht = Nebelland, mit Destillerie = Hoher Norden.
  Der Vanilla-Kessel (Topf, Feuer, Dampf) bleibt erhalten; die Modelle bauen das Gestell darum.

Aendern
  1. tools/make_models.py anpassen  (python make_models.py)  ODER eigene OBJ aus Blender mit gleichen Objektnamen/Materialnamen
  2. Build-Windows.bat ausfuehren
Materialfarben stehen in der .mtl (Kd = Farbe, Ke = Leuchten).

Nicht getestet im Spiel (kein Valheim/Compiler in meiner Umgebung).

v1.4 - Eisdestillerie (3. Erweiterung) + Gerichte des Nordens
=============================================================
Neu registriert: VFP_IceDistillery (Hammer > Crafting), hebt den Runenkessel auf Stufe 4.
Baukosten (BepInEx-Config, Abschnitt "Piece - IceDistillery", Zeile Requirements):
    Crystal:20,DragonTear:5,BlackMetalScrap:10,YggdrasilWood:2
  Das sind Ersatz-Prefabs fuer Eissplitter / Frostdrachenextrakt / Schwarzeisenerz / Yggdrasil-Kern aus dem Konzeptbild.
  Passende Deep-North-Namen im Spiel per F5-Konsole ('spawn ' + Autovervollstaendigung) nachschlagen und eintragen.
  Unbekannte Prefabs werden uebersprungen und im BepInEx-Log gemeldet.
3 neue Rezepte (Stufe 4, nur mit Destillerie): NordicWildRoast, IcefjordFishPot, FrostberryBake
  Werte aus dem Konzeptbild (Leben/Ausdauer/Eitr 110/60/120, 85/90/140, 70/40/120).
  Zutaten (Config je Rezept) sind provisorisch wie bei den anderen Deep-North-Gerichten.
  Diese 3 Gerichte haben noch keine eigenen Icons (Icon des Basis-Items wird verwendet).
Nicht umgesetzt: Freischaltbedingung "Biom Hoher Norden erreicht" aus dem Bild.
