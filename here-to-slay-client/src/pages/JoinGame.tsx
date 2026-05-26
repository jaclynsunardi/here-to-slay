import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { joinGame } from "../api";
import PageLayout from "../components/PageLayout";
import { saveSession } from "../utils/session";
import "../styling/JoinGame.css";

export default function JoinGame() {
  const navigate = useNavigate();
  const [playerName, setPlayerName] = useState("");
  const [roomCode, setRoomCode] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  const handleJoin = async () => {
    if (!playerName.trim()) {
      setError("Please enter a player name.");
      return;
    }
    if (!roomCode.trim()) {
      setError("Please enter a room code.");
      return;
    }

    setLoading(true);
    setError("");
    try {
      const result = await joinGame({
        playerName: playerName.trim(),
        roomCode: roomCode.trim().toUpperCase(),
      });
      saveSession({
        playerId: result.id,
        playerName: result.name,
        roomCode: result.roomCode,
        isHost: false,
      });
      navigate(`/lobby/${result.roomCode}`);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Could not join game");
    } finally {
      setLoading(false);
    }
  };

  return (
    <PageLayout>
      <div className="join-container">
        <h1>Join Game</h1>
        <div className="join-box glass-panel">
          <div className="join-item">
            <label htmlFor="playerName">Player Name</label>
            <input
              type="text"
              id="playerName"
              className="join-input"
              value={playerName}
              onChange={(e) => setPlayerName(e.target.value)}
              placeholder="Enter your name"
            />
            <label htmlFor="roomCode">Room Code</label>
            <input
              type="text"
              id="roomCode"
              className="join-input room-code-input"
              value={roomCode}
              onChange={(e) => setRoomCode(e.target.value.toUpperCase())}
              placeholder="ABCDEF"
              maxLength={8}
            />
            {error && <p className="join-error">{error}</p>}
            <button
              type="button"
              className="btn-join btn btn-primary"
              onClick={handleJoin}
              disabled={loading}
            >
              {loading ? "Joining..." : "Join Game"}
            </button>
            <button
              type="button"
              className="btn-back btn btn-ghost"
              onClick={() => navigate("/")}
            >
              Go Back
            </button>
          </div>
        </div>
      </div>
    </PageLayout>
  );
}
