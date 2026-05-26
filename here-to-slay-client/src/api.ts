import type { GameView } from "./types/game";

export const BACKEND_URL = "http://localhost:5262";

async function parseJson<T>(response: Response): Promise<T> {
  const text = await response.text();
  let data: unknown;
  try {
    data = text ? JSON.parse(text) : {};
  } catch {
    throw new Error(
      response.ok
        ? "Server returned invalid JSON"
        : `Server error (${response.status}): ${text.slice(0, 120)}`
    );
  }
  if (!response.ok) {
    const message = (data as { message?: string }).message ?? "Request failed";
    throw new Error(message);
  }
  return data as T;
}

export async function createGame(body: {
  hostName: string;
  roomCode: string;
  numPlayers: number;
  gameType: string;
}) {
  return parseJson<{ roomCode: string; id: string; name: string }>(
    await fetch(`${BACKEND_URL}/game/create`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    })
  );
}

export async function joinGame(body: { playerName: string; roomCode: string }) {
  return parseJson<{ roomCode: string; id: string; name: string }>(
    await fetch(`${BACKEND_URL}/game/join`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    })
  );
}

export async function fetchGame(roomCode: string, playerId: string) {
  return parseJson<GameView>(
    await fetch(
      `${BACKEND_URL}/game/${encodeURIComponent(roomCode)}?playerId=${encodeURIComponent(playerId)}`
    )
  );
}

export async function startGame(roomCode: string, playerId: string) {
  return postAction(roomCode, "start", { playerId });
}

export async function drawCard(roomCode: string, playerId: string) {
  return postAction(roomCode, "draw", { playerId });
}

export async function playCard(
  roomCode: string,
  body: {
    playerId: string;
    cardInstanceId: string;
    targetHeroInstanceId?: string;
    targetPlayerId?: string;
    modifierCardInstanceId?: string;
    rollHeroOnPlay?: boolean;
  }
) {
  return postAction(roomCode, "play", body);
}

export async function playChallenge(
  roomCode: string,
  playerId: string,
  cardInstanceId: string,
  targetPlayerId: string
) {
  return postAction(roomCode, "challenge", {
    playerId,
    cardInstanceId,
    targetPlayerId,
  });
}

export async function rollHero(
  roomCode: string,
  playerId: string,
  heroInstanceId: string,
  modifierCardInstanceId?: string
) {
  return postAction(roomCode, "roll-hero", {
    playerId,
    heroInstanceId,
    modifierCardInstanceId,
  });
}

export async function attackMonster(
  roomCode: string,
  playerId: string,
  monsterInstanceId: string,
  modifierCardInstanceId?: string
) {
  return postAction(roomCode, "attack", {
    playerId,
    monsterInstanceId,
    modifierCardInstanceId,
  });
}

export async function discardHandRedraw(roomCode: string, playerId: string) {
  return postAction(roomCode, "discard-hand", { playerId });
}

export async function endTurn(roomCode: string, playerId: string) {
  return postAction(roomCode, "end-turn", { playerId });
}

async function postAction(roomCode: string, action: string, body: object) {
  return parseJson<GameView>(
    await fetch(`${BACKEND_URL}/game/${encodeURIComponent(roomCode)}/${action}`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    })
  );
}
