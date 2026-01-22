<?php
require_once "db.php";

// Eingabedaten lesen
$raw = file_get_contents("php://input");
$data = json_decode($raw, true);

if (!is_array($data) || !isset($data["name"])) {
    http_response_code(400);
    echo json_encode(["ok" => false, "error" => "Missing name"]);
    exit;
}

$name = trim($data["name"]);

try {
    // Hole den HÖCHSTEN Score (ORDER BY score DESC) für diesen Namen (WHERE player_name = ?)
    // LIMIT 1 sorgt dafür, dass wir nur einen (den besten) Wert bekommen.
    $stmt = $pdo->prepare("SELECT score FROM highscores WHERE player_name = ? ORDER BY score DESC LIMIT 1");
    $stmt->execute([$name]);
    
    $row = $stmt->fetch(PDO::FETCH_ASSOC);

    if ($row) {
        // Score gefunden
        echo json_encode(["ok" => true, "score" => (int)$row['score']]);
    } else {
        // Spieler hat noch nie gespielt -> Score ist 0
        echo json_encode(["ok" => true, "score" => 0]);
    }

} catch (PDOException $e) {
    http_response_code(500);
    echo json_encode(["ok" => false, "error" => "Database error"]);
}
?>