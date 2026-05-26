import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { createGame } from "../api";
import { saveSession } from "../utils/session";
import "../styling/GameSetting.css";

const roomCodeLength = 6;

function generateRoomCode() {
  const chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
  let code = "";
  for (let i = 0; i < roomCodeLength; i++) {
    code += chars.charAt(Math.floor(Math.random() * chars.length));
  }
  return code;
}

export default function GameSetting() {
  const navigate = useNavigate();
  const [hostName, setHostName] = useState("");
  const [gameType, setGameType] = useState("classic");
  const [numPlayers, setNumPlayers] = useState(2);
  const [roomCode, setRoomCode] = useState(generateRoomCode);
  const [lobbyVisibility, setLobbyVisibility] = useState("public");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  const handleCreate = async () => {
    const name = hostName.trim() || "Host";
    setError("");
    setLoading(true);
    try {
      const result = await createGame({
        hostName: name,
        roomCode: roomCode.trim().toUpperCase(),
        numPlayers,
        gameType,
      });
      saveSession({
        playerId: result.id,
        playerName: result.name,
        roomCode: result.roomCode,
        isHost: true,
      });
      navigate(`/lobby/${result.roomCode}`);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Could not create game");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="game-setting-container glass-panel">
      <h3>Game Settings</h3>
      <div className="setting-box">
        <div className="setting-item">
          <label htmlFor="hostName">Your Name</label>
          <input
            type="text"
            id="hostName"
            className="setting-input"
            value={hostName}
            onChange={(e) => setHostName(e.target.value)}
            placeholder="Enter your name"
          />

          <label htmlFor="numPlayers">Number of Players</label>
          <select
            id="numPlayers"
            className="setting-dropbox"
            value={numPlayers}
            onChange={(e) => setNumPlayers(Number(e.target.value))}
          >
            <option value={2}>2</option>
            <option value={3}>3</option>
            <option value={4}>4</option>
            <option value={5}>5</option>
            <option value={6}>6</option>
          </select>

          <label htmlFor="gameType">Game Type</label>
          <select
            id="gameType"
            className="setting-dropbox"
            value={gameType}
            onChange={(e) => setGameType(e.target.value)}
          >
            <option value="classic">Classic (Base Deck)</option>
            <option value="expansions">With Expansions</option>
          </select>

          {gameType === "expansions" && (
            <>
              <label htmlFor="expansionAddOn">Expansion Add-On</label>
              <select id="expansionAddOn" className="setting-dropbox" disabled>
                <option value="comingSoon">Coming Soon</option>
              </select>
            </>
          )}

          <label htmlFor="roomCode">Room Code</label>
          <input
            type="text"
            id="roomCode"
            className="setting-input room-code-input"
            value={roomCode}
            onChange={(e) => setRoomCode(e.target.value.toUpperCase())}
            maxLength={8}
          />

          <label htmlFor="lobbyVisibility">Lobby Visibility</label>
          <select
            id="lobbyVisibility"
            className="setting-dropbox"
            value={lobbyVisibility}
            onChange={(e) => setLobbyVisibility(e.target.value)}
          >
            <option value="public">Public</option>
            <option value="private">Private</option>
          </select>

          {error && <p className="setting-error">{error}</p>}

          <button
            type="button"
            className="btn-create btn btn-primary"
            onClick={handleCreate}
            disabled={loading}
          >
            {loading ? "Creating..." : "Create Game"}
          </button>
        </div>
      </div>
    </div>
  );
}
