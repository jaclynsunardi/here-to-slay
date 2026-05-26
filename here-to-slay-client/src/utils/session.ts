import type { SessionInfo } from "../types/game";

const SESSION_KEY = "hts_session";

export function saveSession(session: SessionInfo) {
  sessionStorage.setItem(SESSION_KEY, JSON.stringify(session));
}

export function loadSession(): SessionInfo | null {
  const raw = sessionStorage.getItem(SESSION_KEY);
  if (!raw) return null;
  try {
    return JSON.parse(raw) as SessionInfo;
  } catch {
    return null;
  }
}

export function clearSession() {
  sessionStorage.removeItem(SESSION_KEY);
}
