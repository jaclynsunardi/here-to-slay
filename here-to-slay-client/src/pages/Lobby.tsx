import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { startGame } from "../api";
import PageLayout from "../components/PageLayout";
import { useGamePolling } from "../hooks/useGamePolling";
import { loadSession } from "../utils/session";
import "../styling/Lobby.css";

export default function Lobby() {
  const navigate = useNavigate();
  const { roomCode } = useParams();
  const session = loadSession();
  const [copied, setCopied] = useState(false);
  const [error, setError] = useState("");
  const [starting, setStarting] = useState(false);

  const { game, loading } = useGamePolling(
    roomCode,
    session?.playerId,
    Boolean(roomCode && session?.playerId)
  );

  useEffect(() => {
    if (game?.state === "inProgress" && roomCode) {
      navigate(`/game/${roomCode}`);
    }
  }, [game?.state, navigate, roomCode]);

  const handleCopy = async () => {
    await navigator.clipboard.writeText(roomCode || "");
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  const handleStart = async () => {
    if (!session?.playerId || !roomCode) return;
    setStarting(true);
    setError("");
    try {
      await startGame(roomCode, session.playerId);
      navigate(`/game/${roomCode}`);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Could not start game");
    } finally {
      setStarting(false);
    }
  };

  if (!session) {
    return (
      <PageLayout>
        <div className="lobby-container">
          <div className="lobby-card glass-panel">
            <h1 className="lobby-title">No session</h1>
            <p>Please create or join a game first.</p>
            <button type="button" className="btn btn-primary" onClick={() => navigate("/")}>
              Home
            </button>
          </div>
        </div>
      </PageLayout>
    );
  }

  const slots = Array.from({ length: game?.maxPlayers ?? 2 }, (_, i) => {
    const player = game?.players[i];
    return player ?? null;
  });

  const canStart =
    session.isHost &&
    (game?.players.length ?? 0) >= 2 &&
    game?.state === "waiting";

  return (
    <PageLayout>
      <div className="lobby-container">
        <div className="lobby-card glass-panel">
          <img
            src="/images/edited/logo.png"
            alt=""
            className="lobby-logo"
            aria-hidden
          />
          <h1 className="lobby-title">Game Lobby</h1>

          <div className="room-code-box">
            <span className="room-label">Room Code</span>
            <div className="room-code">{roomCode}</div>
          </div>

          <div className="players-section">
            <h2>
              Players ({game?.players.length ?? 0}/{game?.maxPlayers ?? 2})
            </h2>
            {loading && !game && <p className="lobby-loading">Loading lobby...</p>}
            <div className="players-list">
              {slots.map((player, index) => (
                <div
                  key={player?.id ?? `slot-${index}`}
                  className={`player-card ${player ? "joined" : "waiting"}`}
                >
                  {player ? (
                    <>
                      <span className="player-card__name">{player.name}</span>
                      <span className="player-card__role">
                        {player.type === "Host" ? "Host" : "Guest"}
                        {player.id === session.playerId ? " · You" : ""}
                      </span>
                    </>
                  ) : (
                    "Waiting for player..."
                  )}
                </div>
              ))}
            </div>
          </div>

          {error && <p className="lobby-error">{error}</p>}

          <div className="lobby-actions">
            <button type="button" className="btn-copy btn btn-secondary" onClick={handleCopy}>
              {copied ? "Copied!" : "Copy Code"}
            </button>
            {session.isHost ? (
              <button
                type="button"
                className="btn-start btn btn-primary"
                disabled={!canStart || starting}
                onClick={handleStart}
              >
                {starting ? "Starting..." : "Start Game"}
              </button>
            ) : (
              <button
                type="button"
                className="btn-start btn btn-primary"
                onClick={() => navigate(`/game/${roomCode}`)}
              >
                Enter Game
              </button>
            )}
          </div>

          <p className="lobby-hint">
            {session.isHost
              ? "Share the room code with a friend. Start when everyone has joined."
              : "Waiting for the host to start the match..."}
          </p>
        </div>
      </div>
    </PageLayout>
  );
}
