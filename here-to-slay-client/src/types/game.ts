export type GameState = "waiting" | "inProgress" | "finished";

export interface PartyRequirement {
  heroClass: string | null;
  count: number;
  genericHero: boolean;
}

export interface CardView {
  instanceId: string;
  definitionId: string;
  name: string;
  type: string;
  heroClass: string | null;
  rollThreshold: number;
  failIfRollAtOrBelow: number | null;
  failPenalty: string | null;
  imagePath: string;
  effectText: string;
  partyRequirements: PartyRequirement[];
  attachedItems: CardView[];
  isPartyLeader: boolean;
}

export interface PlayerView {
  id: string;
  name: string;
  type: string;
  partyLeader: CardView | null;
  hand: CardView[];
  party: CardView[];
  slainMonsters: CardView[];
  handCount: number;
  heroesRolledThisTurn: string[];
}

export interface GameView {
  roomCode: string;
  state: GameState;
  gameType: string;
  maxPlayers: number;
  currentPlayerIndex: number;
  actionPointsRemaining: number;
  winnerId: string | null;
  winnerName: string | null;
  lastMessage: string | null;
  lastRollDie1: number | null;
  lastRollDie2: number | null;
  lastRollModifier: number | null;
  lastRollTotal: number | null;
  deckCount: number;
  monsterDeckCount: number;
  discardCount: number;
  monsterRow: CardView[];
  players: PlayerView[];
  viewingPlayerId: string;
}

export interface SessionInfo {
  playerId: string;
  playerName: string;
  roomCode: string;
  isHost: boolean;
}

export const AP = {
  draw: 1,
  play: 1,
  heroRoll: 1,
  attack: 2,
  discardRedraw: 3,
} as const;
