# Sternenfeuer - Kugelhagel im All
## Multiplayer Bullet-Hell

## Was ist das für ein Spiel?

Sternenfeuer ist ein Top-Down Multiplayer-Bullethell. 
Man kann mit mehreren Leuten zusammen spielen und muss gegen verschiedene Gegner kämpfen. 
Ziel ist es möglichst viele Punkte zu sammeln bevor man stirbt.\

Was kann man machen:\
    • Zusammen mit anderen Spielern online spielen\
    • Mit 4 verschiedenen Schussmustern schießen (normal, verteilt, spiral, welle)  
    • Gegen normale Gegner und große Boss-Gegner kämpfen\
    • Punkte sammeln wenn man Gegner besiegt\
    • Seinen Highscore in einer Datenbank speichern\
    • In der Bestenliste die Highscores nachschauen\
    • Spielen, bis alle Spieler k.o sind oder alle Gegner besiegt wurden

## Wie starte ich das Spiel?
Was man braucht:\
    • Unity 6000.0.62f1 - das haben wir benutzt\
    • FishNet muss installiert sein\
    • Einen PHP-Server mit MySQL Datenbank (XAMPP) 
    
So startet man als Host (Server):\
    1. XAMPP starten – MySQL und Apache starten\
    2. MySQL Admin – Datenbank importieren\
    3. Unity Projekt aufmachen\
    4. Play drücken (im Unity Editor)\
    5. StartServer drücken\
    6. StartClient drücken\
    7. Spielernamen eingeben\
    8. ggf. auf weitere Spieler warten\
    9. Den „Start-Button“ drücken und das Spiel geht los 
    
So kann ein weiterer Spieler mitspielen:\
    1. Nochmal Unity aufmachen\
    2. Im Inspector, bei NetworkManager -Tugboat, IPV4 den Host eingeben und Port anpassen, falls erforderlich\
    3. Play drücken  (im Unity Editor)\
    4. StartClient drücken\
    5. Spielernamen eingeben\
    6. Warten, bis der Host das Spiel startet

Steuerung:\
    • WASD: Bewegen\
    • Maus: Zielen, wohin man schießt/ Rotieren\
    • Linke Maustaste drücken: Schießen\
    • 1, 2, 3, 4: Verschiedene Schussmuster wählen


## Wie funktioniert das alles auf technischer Ebene?

### RPCs (Remote Procedure Calls)

RPCs sind Funktionsaufrufe, die vom Client zum Server (oder umgekehrt) geschickt werden. 
So synchronisiert man Aktionen zwischen Server und Clients. 

ServerRpc = Client schickt zum Server:\
    • MoveServerRPC(Vector2 input) - Damit sage ich dem Server wo ich hin laufen will\
    • RotateServerRPC(Vector3 mouseWorldPosition) - Sagt dem Server wo meine Maus ist\
    • ShootServerRPC() - Sagt dem Server dass ich schießen will\
    • ChangePatternServerRPC(ShootPattern newPattern) - Ändert welches Schussmuster ich benutze\
    • SetPlayerNameServerRPC(string name) - Gibt dem Server meinen Spielernamen\
    • ServerSaveScore(...) - Speichert meinen Score in die Datenbank\
    • ServerGetScores(...) - Holt die Highscore Liste\
    • ServerGetMyBestScore(...) - Guckt was mein bester Score war
    
ObserversRpc = Server schickt an ALLE:\
    • ToggleNameField(bool active) - Zeigt oder versteckt das Namensfeld bei allen\
    • DeactivateGOVscreens() - Versteckt Victory/ GameOver Screen bei allen\
    • ShowVictoryMessage(bool isVictory) - Zeigt Victory oder GameOver Screen bei allen\
    • ToogleForAll(bool setActive) - Zeigt/ Versteckt Highscore Liste bei allen
    
TargetRpc = Server schickt nur an EINEN bestimmten Client:\
    • TargetReturnScores(...) - Schickt die Highscore Liste zurück\
    • TargetReturnMyBestScore(...) - Schickt meinen besten Score zurück 
    
Warum braucht man das?\
Damit alle Spieler das gleiche sehen und niemand schummeln kann. 
Der Server entscheidet alles wichtige und teilt es dann den Clients mit.


### SyncVars (Synchronisierte Variablen)

SyncVars sind Variablen die automatisch bei allen Spielern gleich sind.
Wenn der Server den Wert ändert, ändert er sich auch bei allen Clients.

In PlayerController.cs:\
    • SyncVar<string> playerName - Der Name vom Spieler\
    • SyncVar<Color> playerColor - Die Farbe vom Spieler (damit man sie auseinander halten kann)\
    • SyncVar<int> playerHP - Wie viel Leben der Spieler noch hat 
    
In Enemy1.cs:\
    • SyncVar<int> health - Leben vom Gegner

In EnemySpawner.cs:\
    • SyncVar<bool> isGameOver - Ob das Spiel vorbei ist
    
In ScoreManager.cs:\
    • SyncDictionary<int, int> playerScores - Eine Liste mit allen Spielern und ihren Punkten\
    • SyncDictionary<int, bool> playerAliveStatus - Ob jeder Spieler noch lebt oder schon tot ist\
    • SyncDictionary<int, float> playerSpawnTimes - Wann jeder Spieler gespawnt ist (für Bonus-Punkte) 
    
Vorteil: Man muss nicht selbst daran denken die Werte zu synchronisieren, das macht FishNet automatisch. 


## Wie funktionieren die Bullets?

Datei: Bullet.cs

Die Bullets werden mit Object Pooling gemacht. Das heißt man erstellt sie nicht neu sondern benutzt sie immer wieder. 
Das ist besser für die Performance.

Was passiert wenn man schießt:\
    1. Spawnen: Bullet wird aus dem Pool geholt und am Gegner/ Player positioniert\
    2. Initialisierung: ShootBullet() wird aufgerufen mit Schaden, Geschwindigkeit, Lebenszeit und wer geschossen hat\
    3. Bewegung: Jeden Netzwerk-Tick bewegt sich die Bullet vorwärts\
    4. Kollision: Wenn sie was trifft wird geprüft, OB sie überhaupt treffen darf:\
      ◦ Spieler-Bullets treffen nur Gegner (Tag "Enemy")\
      ◦ Gegner-Bullets treffen nur Spieler (Tag "Player")\
    5. Schaden: Wenn es ein gültiges Ziel ist wird Damage() aufgerufen\ 
    6. Despawn: Bullet geht zurück in den Pool

Warum speichere ich den Shooter? 
Damit ich später weiß WER den Gegner getötet hat und kann ihm dann die Punkte geben.


## Wie funktionieren die Gegner?

Datei: Enemy1.cs

Die Gegner spawnen in einem Ring um die Spieler herum und laufen dann auf den nächsten lebenden Spieler zu.

Was machen die Gegner:
1. Spawnen: Werden aus dem Pool geholt und vom EnemySpawner an zufälliger Position platziert  
2. Ziel finden: Suchen den nächsten lebenden Spieler mit GetClosestPlayers() 
                    (Spieler, die k.o gegangen sind werden übersprungen)
3. Bewegen: Laufen auf den Spieler zu und verfolgen ihn 
4. Schießen: Schießen automatisch alle paar Sekunden 
5. Sterben: Wenn Leben auf 0 ist geben sie Punkte und gehen zurück in den Pool
   
Boss-Gegner sind anders:\
Wenn isBoss = true ist, dann:\
    • 10x mehr Leben\
    • Halb so schnell\ 
    • 3x größer\ 
    • Geben 300 Punkte statt 100\
    
Raid-Events:\
Alle 60 Sekunden spawnen 5 normale Gegner + 1 Boss auf einmal.


## Wie werden die Highscores gespeichert?

MySQL Datenbank mit PHP um die Scores zu speichern
Die Datenbank-Tabelle:

sql
CREATE TABLE highscores (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(50) NOT NULL,
    score INT NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

Also ganz einfach: ID, Name, Punkte und wann das war.


Die PHP-Dateien:

Es gibt 3 PHP-Dateien:
1. save_score.php
Speichert einen neuen Score in die Datenbank.
2. get_scores.php
Holt die Top 10 Scores aus der Datenbank.
3. get_my_best.php
Ermittelt, was der persönlicher Rekord ist.

## Wie läuft das ab?

1. Spieler stirbt
2. Unity ruft RequestSaveScore() auf
3. Das wird als ServerRpc zum Server geschickt
4. Server macht einen HTTP-Request an die PHP-Datei
5. PHP speichert es in der MySQL-DatenBank
6. Server bekommt Antwort zurück
7. Server sagt dem Client ob es geklappt hat

Wichtig: Nur der Server redet mit der Datenbank! Sonst könnte jemand schummeln.

## Score-Berechnung:

Endpunkte = Gegner-Punkte + (Überlebenszeit in Sekunden × 10)

Also wenn ich 1000 Punkte durch Gegner hab und 50 Sekunden überlebt hab:
Endpunkte = 1000 + (50 × 10) = 1500 Punkte


## Was haben wir implementiert?

### 1. Verschiedene Schussmuster
Nicht nur gerade aus schießen sondern auch:
- **Spread**: 3 Bullets auf einmal (Mitte, Links, Rechts)
- **Spiral**: 12 Bullets die sich im Kreis drehen
- **Wave**: 12 Bullets in einer Wellen-Form

### 2. Object Pooling
Bullets und Gegner werden nicht immer neu erstellt sondern wiederverwendet.
Das ist viel schneller!
- 500 Bullets werden vorgeladen
- 50 normale Gegner werden vorgeladen
- 5 Boss-Gegner werden vorgeladen

### 3. Datenbank für Highscores
Mit MySQL und PHP. Man kann die Top 10 sehen und seinen eigenen Rekord nachschauen.

### 4. Jeder Spieler hat eigene Punkte
Nicht ein gemeinsamer Score sondern jeder hat seinen eigenen. 
Am Ende sieht man wer am besten war.

### 5. UI
- Spielername unter dem Player
- Gesundheitsbalken
- Live Punkteanzeige
- Highscore Anzeige
- Victory/GameOver Screens

### 6. Spieler haben verschiedene Farben
Automatisch bekommt jeder Spieler eine andere Farbe aus einer Liste (Gelb, Orange, Lila, Pink, Cyan). 
So kann man sie auseinander halten.

### 7. Boss-Gegner
Größere Gegner die mehr Leben haben und mehr Punkte geben.

### 8. Raid-Events
Alle 60 Sekunden kommen auf einmal viele Gegner + ein Boss.

### 9. Game Over Mechanik
- Spiel endet wenn alle Spieler tot sind (Game Over Screen)
- Spiel endet wenn alle Gegner besiegt wurden (Victory Screen)
- Nach Spielende werden alle Spieler geheilt und Scores zurückgesetzt
- Highscores werden automatisch gespeichert

### 10. Tote Spieler Logik
- Tote Spieler können keine Punkte mehr sammeln
- Tote Spieler können nicht mehr schießen
- Gegner verfolgen keine toten Spieler mehr

## Was funktioniert noch nicht richtig? (Bugs)

Uns ist nichts aufgefallen.
Nicht erschrecken, wenn der Server gestartet ist aber noch kein Host/ Client anwesend ist, wird nach 
einem Player gesucht - Es erscheint mehrmals der gleiche Debug.Log und zählt die Versuche hoch.


## Wie ist das Projekt strukturiert?

Scripts\

  Player\
    PlayerController.cs       (Spieler Steuerung)\
    PlayerHealthBar.cs        (Lebensbalken)\
    NameDisplay.cs            (Name unter Player)
    
  Enemies\
     Enemy1.cs                 (Gegner)\
     EnemySpawner.cs           (Spawnt die Gegner)
     
  Weapons\
     Bullet.cs                 (Geschosse)
     
  Managers\
     ScoreManager.cs           (Verwaltet Punkte)\
     ScoreNetworkManager.cs    (Redet mit Datenbank)\
     NewObjectPoolManager.cs   (Object Pooling)\
     HubManager.cs             (Spiel starten/stoppen)
     
  UI\
     ScoreUI.cs                (Zeigt Punkte an)\
     HighScoreMenu.cs          (Highscore Liste)\
     ShowHideScoreList.cs      (Ein/Ausblenden)
     
  Interfaces\
     IDamagable.cs             (Interface für Dinge die Schaden nehmen)

  Gameplay\
     TestDamage.cs             (Damage-Trigger für Umgebung)
     

Database\

  save_score.php                (Score speichern)\
  get_scores.php                (Top 10 holen)\
  get_my_best.php               (Mein Rekord holen)\   


## Wichtige Learnings

### Server-Authority:
Alles wichtige passiert auf dem Server. Die Clients schicken nur Inputs und bekommen dann gesagt was passiert ist. 
So kann niemand schummeln.

### SyncVars sind cool:
Man muss nicht selbst Sachen synchronisieren. FishNet macht das automatisch. 

### Object Pooling ist wichtig:
Wenn man 500 Bullets pro Sekunde erstellen und löschen würde wäre das mega langsam. Mit Pooling ist es viel schneller.



## Wer hat das Spiel gemacht?

**Spiel:** Osman Sengül & Naomi Zellhofer\
**Networking:** FishNet (von FirstGearGames)\
**Unity Version:** 6000.0.62f1
