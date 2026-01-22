<?php
require_once "db.php";

try {
    // WICHTIG: 'player_name AS name'. 
    // Unity erwartet im JSON den Key "name" (weil die Variable im C# Script so heißt),
    // aber die Datenbank hat "player_name". Das "AS" löst das Mapping-Problem.
    $sql = "SELECT id, player_name AS name, score, created_at 
            FROM highscores 
            ORDER BY score DESC 
            LIMIT 10"; // Limit auf 10 erhöht, kannst du anpassen

    $stmt = $pdo->query($sql);
    $rows = $stmt->fetchAll(PDO::FETCH_ASSOC);

    echo json_encode(["ok" => true, "players" => $rows]);

} catch (PDOException $e) {
    http_response_code(500);
    echo json_encode(["ok" => false, "error" => "Database error", "players" => []]);
}
?>