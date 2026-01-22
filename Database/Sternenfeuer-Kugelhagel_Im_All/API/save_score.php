<?php
require_once "db.php";

// Eingabedaten lesen
$raw = file_get_contents("php://input");
$data = json_decode($raw, true);

// Prüfen, ob name UND score vorhanden sind
if (!is_array($data) || !isset($data["name"]) || !isset($data["score"])) {
    http_response_code(400);
    echo json_encode(["ok" => false, "error" => "Missing name or score"]);
    exit;
}

$name = trim($data["name"]);
$score = intval($data["score"]); // Sicherstellen, dass es eine Zahl ist

// Validierung des Namens
if ($name === "" || mb_strlen($name) > 50) {
    http_response_code(400);
    echo json_encode(["ok" => false, "error" => "Invalid name"]);
    exit;
}

try {
    // SQL Insert: Wir schreiben in player_name und score
    // Tabelle heißt 'highscores' passend zu deinem get-Script
    $stmt = $pdo->prepare("INSERT INTO highscores (player_name, score) VALUES (?, ?)");
    $stmt->execute([$name, $score]);

    echo json_encode(["ok" => true, "id" => $pdo->lastInsertId()]);
} catch (PDOException $e) {
    http_response_code(500);
    // Fehler zurückgeben (für Debugging, im Live-Betrieb evtl. ausblenden)
    echo json_encode(["ok" => false, "error" => "Database error: " . $e->getMessage()]);
}
?>