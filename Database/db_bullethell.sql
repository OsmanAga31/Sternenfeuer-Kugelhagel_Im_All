-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- Host: 127.0.0.1
-- Erstellungszeit: 23. Jan 2026 um 16:23
-- Server-Version: 10.4.32-MariaDB
-- PHP-Version: 8.2.12

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Datenbank: `db_bullethell`
--

-- --------------------------------------------------------

--
-- Tabellenstruktur für Tabelle `highscores`
--

CREATE TABLE `highscores` (
  `id` int(11) NOT NULL,
  `player_name` varchar(50) NOT NULL,
  `score` int(11) NOT NULL,
  `created_at` timestamp NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Daten für Tabelle `highscores`
--

INSERT INTO `highscores` (`id`, `player_name`, `score`, `created_at`) VALUES
(1, 'World!', 506, '2026-01-22 14:48:45'),
(2, 'Hello!', 824, '2026-01-22 14:48:47'),
(3, 'NootNoot', 500, '2026-01-23 13:04:02'),
(4, 'Melon Husk', 200, '2026-01-23 13:04:02'),
(19, 'Mac', 200, '2026-01-23 13:14:25'),
(20, 'Cheese', 400, '2026-01-23 13:14:25'),
(23, 'Ready Player Two', 300, '2026-01-23 13:19:36'),
(24, 'Ready Player One', 200, '2026-01-23 13:19:36'),
(35, 'Ready Player 3', 400, '2026-01-23 13:26:54'),
(36, 'Ready Player 4', 300, '2026-01-23 13:26:54'),
(37, 'Melon Husk', 600, '2026-01-23 13:30:56'),
(41, 'Wer noch?', 240, '2026-01-23 13:49:01'),
(42, 'Bin da!', 573, '2026-01-23 13:49:01'),
(43, 'Bei Technik Fragen,', 1141, '2026-01-23 13:52:57'),
(44, 'Tech Nik fragen!', 2848, '2026-01-23 13:52:57'),
(45, 'Steve', 686, '2026-01-23 14:06:53'),
(46, 'Alex', 596, '2026-01-23 14:06:53'),
(47, '2', 0, '2026-01-23 14:10:47'),
(48, '1', 0, '2026-01-23 14:10:47'),
(49, 'Roblox Noob 1', 0, '2026-01-23 14:15:26'),
(50, 'Roblox Noob 2', 0, '2026-01-23 14:15:26'),
(51, 'Roblox Noob 1', 916, '2026-01-23 14:16:16'),
(52, 'Roblox Noob 2', 1048, '2026-01-23 14:16:16'),
(53, 'Mario', 0, '2026-01-23 14:23:17'),
(54, 'Luigi', 0, '2026-01-23 14:23:17'),
(55, 'Mario', 1023, '2026-01-23 14:23:59'),
(56, 'Luigi', 999, '2026-01-23 14:23:59'),
(57, 'Sonic', 1270, '2026-01-23 14:30:44'),
(58, 'Tails', 1753, '2026-01-23 14:30:44'),
(63, 'Clyde', 4357, '2026-01-23 14:48:28'),
(64, 'Bonnie', 1528, '2026-01-23 14:48:28'),
(65, 'Batman', 2847, '2026-01-23 15:02:02'),
(66, 'Robin', 705, '2026-01-23 15:02:02'),
(71, 'SpiderMan (Tobey Maquire)', 1930, '2026-01-23 15:11:42'),
(72, 'IronMan', 3085, '2026-01-23 15:11:42'),
(73, 'Schweiß', 539, '2026-01-23 15:17:06'),
(74, 'Axel', 1829, '2026-01-23 15:17:06');

--
-- Indizes der exportierten Tabellen
--

--
-- Indizes für die Tabelle `highscores`
--
ALTER TABLE `highscores`
  ADD PRIMARY KEY (`id`);

--
-- AUTO_INCREMENT für exportierte Tabellen
--

--
-- AUTO_INCREMENT für Tabelle `highscores`
--
ALTER TABLE `highscores`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=75;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
