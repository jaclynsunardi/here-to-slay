import { useCallback, useEffect, useState } from "react";
import { fetchGame } from "../api";
import type { GameView } from "../types/game";

export function useGamePolling(
  roomCode: string | undefined,
  playerId: string | undefined,
  enabled = true
) {
  const [game, setGame] = useState<GameView | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  const refresh = useCallback(async () => {
    if (!roomCode || !playerId) return;
    try {
      const data = await fetchGame(roomCode, playerId);
      setGame(data);
      setError(null);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to load game");
    } finally {
      setLoading(false);
    }
  }, [roomCode, playerId]);

  useEffect(() => {
    if (!enabled || !roomCode || !playerId) return;
    refresh();
    const id = setInterval(refresh, 2000);
    return () => clearInterval(id);
  }, [enabled, refresh, roomCode, playerId]);

  return { game, error, loading, refresh, setGame };
}
