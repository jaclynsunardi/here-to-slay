import { useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import {
  attackMonster,
  discardHandRedraw,
  drawCard,
  endTurn,
  playCard,
  playChallenge,
  rollHero,
  startGame,
} from "../api";
import { AP } from "../types/game";
import DiceDisplay from "../components/DiceDisplay";
import GameCard from "../components/GameCard";
import PageLayout from "../components/PageLayout";
import { useGamePolling } from "../hooks/useGamePolling";
import type { CardView, SessionInfo } from "../types/game";
import { loadSession } from "../utils/session";
import "../styling/GameBoard.css";

function countClasses(player: {
  partyLeader: CardView | null;
  party: CardView[];
}) {
  const classes = new Set<string>();
  if (player.partyLeader?.heroClass) classes.add(player.partyLeader.heroClass);
  player.party.forEach((h) => {
    if (h.heroClass) classes.add(h.heroClass);
  });
  return classes.size;
}

export default function GameBoard() {
  const { roomCode } = useParams();
  const navigate = useNavigate();
  const session = loadSession() as SessionInfo | null;

  const [selectedCard, setSelectedCard] = useState<string | null>(null);
  const [selectedHero, setSelectedHero] = useState<string | null>(null);
  const [selectedMonster, setSelectedMonster] = useState<string | null>(null);
  const [selectedModifier, setSelectedModifier] = useState<string | null>(null);
  const [challengeTargetId, setChallengeTargetId] = useState<string | null>(null);
  const [rollOnPlay, setRollOnPlay] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const playerId = session?.playerId;
  const { game, error, loading, setGame } = useGamePolling(
    roomCode,
    playerId,
    Boolean(roomCode && playerId)
  );

  const me = game?.players.find((p) => p.id === playerId);
  const opponents = game?.players.filter((p) => p.id !== playerId) ?? [];
  const isMyTurn =
    game != null &&
    game.players[game.currentPlayerIndex]?.id === playerId &&
    game.state === "inProgress";

  const ap = game?.actionPointsRemaining ?? 0;
  const uniqueClasses = useMemo(() => (me ? countClasses(me) : 0), [me]);

  const selectedCardData = me?.hand.find((c) => c.instanceId === selectedCard);
  const canAfford = (cost: number) => isMyTurn && ap >= cost;

  if (!session || !playerId) {
    return (
      <PageLayout>
        <div className="game-board__message-panel">
          <h2>Session expired</h2>
          <button type="button" className="btn btn-primary" onClick={() => navigate("/")}>
            Home
          </button>
        </div>
      </PageLayout>
    );
  }

  const runAction = async (action: () => Promise<unknown>) => {
    setActionError(null);
    setBusy(true);
    try {
      const updated = await action();
      setGame(updated as typeof game);
      setSelectedCard(null);
      setSelectedHero(null);
      setSelectedMonster(null);
      setSelectedModifier(null);
      setChallengeTargetId(null);
    } catch (e) {
      setActionError(e instanceof Error ? e.message : "Action failed");
    } finally {
      setBusy(false);
    }
  };

  if (loading && !game) {
    return (
      <PageLayout>
        <div className="game-board__loading">Summoning heroes...</div>
      </PageLayout>
    );
  }

  if (error || !game) {
    return (
      <PageLayout>
        <div className="game-board__message-panel">
          <h2>Could not load game</h2>
          <p>{error}</p>
          <button type="button" className="btn btn-secondary" onClick={() => navigate("/")}>
            Home
          </button>
        </div>
      </PageLayout>
    );
  }

  if (game.state === "waiting") {
    return (
      <PageLayout>
        <div className="game-board__message-panel">
          <h2>Waiting in lobby</h2>
          {session.isHost && (
            <button
              type="button"
              className="btn btn-primary"
              disabled={busy || game.players.length < 2}
              onClick={() => runAction(() => startGame(roomCode!, playerId))}
            >
              Start Game
            </button>
          )}
          <button type="button" className="btn btn-ghost" onClick={() => navigate(`/lobby/${roomCode}`)}>
            Back to Lobby
          </button>
        </div>
      </PageLayout>
    );
  }

  return (
    <div className="game-board">
      <header className="game-board__header">
        <img src="/images/edited/logo.png" alt="" className="game-board__logo" />
        <div className="ap-meter" title="Action points remaining this turn">
          {[1, 2, 3].map((n) => (
            <span key={n} className={`ap-meter__pip ${ap >= n ? "ap-meter__pip--on" : ""}`} />
          ))}
          <span className="ap-meter__label">{ap} AP</span>
        </div>
        <div className="game-board__status">
          <span className={`turn-pill ${isMyTurn ? "turn-pill--active" : ""}`}>
            {isMyTurn ? "Your turn" : `${game.players[game.currentPlayerIndex]?.name}'s turn`}
          </span>
        </div>
        <div className="game-board__stats">
          <div className="stat-chip">
            <span className="stat-chip__label">Classes</span>
            <span className="stat-chip__value">{uniqueClasses}/6</span>
          </div>
          <div className="stat-chip">
            <span className="stat-chip__label">Slain</span>
            <span className="stat-chip__value">{me?.slainMonsters.length ?? 0}/3</span>
          </div>
        </div>
      </header>

      {game.state === "finished" && (
        <div className="win-overlay">
          <div className="win-overlay__card">
            <h2>{game.winnerId === playerId ? "Victory!" : "Defeat"}</h2>
            <p>{game.lastMessage}</p>
            <button type="button" className="btn btn-primary" onClick={() => navigate("/")}>
              Return Home
            </button>
          </div>
        </div>
      )}

      {opponents.map((opp) => (
        <section key={opp.id} className="game-board__opponent zone-panel">
          <div className="zone-panel__title">
            <h3>{opp.name}</h3>
            <span>{opp.handCount} cards</span>
          </div>
          {opp.partyLeader && (
            <div className="zone-panel__row">
              <div className="zone-label">Leader</div>
              <div className="card-row">
                <GameCard card={opp.partyLeader} small />
              </div>
            </div>
          )}
          <div className="zone-panel__row">
            <div className="zone-label">Party</div>
            <div className="card-row">
              {opp.party.map((c) => (
                <GameCard key={c.instanceId} card={c} small />
              ))}
            </div>
          </div>
        </section>
      ))}

      <section className="game-board__center">
        <div className="message-banner">
          <p>{game.lastMessage}</p>
          <DiceDisplay
            die1={game.lastRollDie1}
            die2={game.lastRollDie2}
            modifier={game.lastRollModifier}
            total={game.lastRollTotal}
          />
        </div>
        <div className="monster-lane">
          <h3>Monster Row — Attack costs 2 AP</h3>
          <div className="monster-lane__cards">
            {game.monsterRow.map((monster) => (
              <div key={monster.instanceId} className="monster-wrap">
                <GameCard
                  card={monster}
                  selected={selectedMonster === monster.instanceId}
                  disabled={!canAfford(AP.attack)}
                  onClick={() =>
                    setSelectedMonster((p) =>
                      p === monster.instanceId ? null : monster.instanceId
                    )
                  }
                />
                <p className="monster-wrap__req">
                  {monster.partyRequirements
                    .map((r) =>
                      r.genericHero
                        ? `${r.count}× Hero`
                        : `${r.count}× ${r.heroClass}`
                    )
                    .join(", ") || "No party req."}
                  {monster.rollThreshold > 0 && <> · Slay {monster.rollThreshold}+</>}
                  {monster.failIfRollAtOrBelow != null && (
                    <> · ≤{monster.failIfRollAtOrBelow}: {monster.failPenalty ?? "penalty"}</>
                  )}
                </p>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="game-board__player zone-panel">
        <div className="zone-panel__title">
          <h3>{me?.name}</h3>
        </div>
        {me?.partyLeader && (
          <div className="zone-panel__row">
            <div className="zone-label">Party Leader</div>
            <div className="card-row">
              <GameCard card={me.partyLeader} small />
            </div>
          </div>
        )}
        <div className="zone-panel__row">
          <div className="zone-label">Party — click hero to roll ability (1 AP)</div>
          <div className="card-row">
            {me?.party.map((hero) => (
              <GameCard
                key={hero.instanceId}
                card={hero}
                small
                selected={selectedHero === hero.instanceId}
                disabled={
                  !canAfford(AP.heroRoll) ||
                  me.heroesRolledThisTurn.includes(hero.instanceId)
                }
                onClick={() =>
                  setSelectedHero((p) => (p === hero.instanceId ? null : hero.instanceId))
                }
              />
            ))}
          </div>
        </div>

        <div className="hand-zone">
          <h3>Hand — Modifiers apply to your next roll</h3>
          <div className="hand-zone__cards">
            {me?.hand.map((card) => (
              <GameCard
                key={card.instanceId}
                card={card}
                selected={
                  selectedCard === card.instanceId || selectedModifier === card.instanceId
                }
                disabled={!isMyTurn}
                onClick={() => {
                  if (card.type === "Modifier") {
                    setSelectedModifier((p) =>
                      p === card.instanceId ? null : card.instanceId
                    );
                    setSelectedCard(null);
                  } else {
                    setSelectedCard((p) =>
                      p === card.instanceId ? null : card.instanceId
                    );
                    setSelectedModifier(null);
                  }
                }}
              />
            ))}
          </div>
          {selectedCardData && (
            <p className="card-hint">{selectedCardData.effectText}</p>
          )}
        </div>

        {selectedCardData?.type === "Challenge" && (
          <div className="target-row">
            <span>Challenge target:</span>
            {opponents.map((o) => (
              <button
                key={o.id}
                type="button"
                className={`btn btn-ghost ${challengeTargetId === o.id ? "btn--active" : ""}`}
                onClick={() => setChallengeTargetId(o.id)}
              >
                {o.name}
              </button>
            ))}
          </div>
        )}

        {selectedCardData?.type === "Item" && (
          <div className="target-row">
            <span>Equip on:</span>
            {me?.party.map((h) => (
              <button
                key={h.instanceId}
                type="button"
                className={`btn btn-ghost ${selectedHero === h.instanceId ? "btn--active" : ""}`}
                onClick={() => setSelectedHero(h.instanceId)}
              >
                {h.name}
              </button>
            ))}
          </div>
        )}

        {selectedCardData?.type === "Hero" && (
          <label className="roll-on-play">
            <input
              type="checkbox"
              checked={rollOnPlay}
              onChange={(e) => setRollOnPlay(e.target.checked)}
            />
            Roll hero ability immediately on play (free roll)
          </label>
        )}
      </section>

      <footer className="game-board__actions">
        {actionError && <p className="action-error">{actionError}</p>}
        <div className="action-bar">
          <button
            type="button"
            className="btn btn-primary"
            disabled={!canAfford(AP.draw) || busy}
            onClick={() => runAction(() => drawCard(roomCode!, playerId))}
          >
            Draw (1 AP)
          </button>
          <button
            type="button"
            className="btn btn-secondary"
            disabled={
              !selectedCard ||
              !selectedCardData ||
              selectedCardData.type === "Modifier" ||
              selectedCardData.type === "Challenge" ||
              !canAfford(AP.play) ||
              busy
            }
            onClick={() =>
              runAction(() =>
                playCard(roomCode!, {
                  playerId,
                  cardInstanceId: selectedCard!,
                  targetHeroInstanceId: selectedHero ?? undefined,
                  modifierCardInstanceId: selectedModifier ?? undefined,
                  rollHeroOnPlay: rollOnPlay,
                })
              )
            }
          >
            Play (1 AP)
          </button>
          <button
            type="button"
            className="btn btn-secondary"
            disabled={
              !selectedCard ||
              selectedCardData?.type !== "Challenge" ||
              !challengeTargetId ||
              !canAfford(AP.play) ||
              busy
            }
            onClick={() =>
              runAction(() =>
                playChallenge(roomCode!, playerId, selectedCard!, challengeTargetId!)
              )
            }
          >
            Challenge (1 AP)
          </button>
          <button
            type="button"
            className="btn btn-secondary"
            disabled={!selectedHero || !canAfford(AP.heroRoll) || busy}
            onClick={() =>
              runAction(() =>
                rollHero(roomCode!, playerId, selectedHero!, selectedModifier ?? undefined)
              )
            }
          >
            Roll Hero (1 AP)
          </button>
          <button
            type="button"
            className="btn btn-accent"
            disabled={!selectedMonster || !canAfford(AP.attack) || busy}
            onClick={() =>
              runAction(() =>
                attackMonster(
                  roomCode!,
                  playerId,
                  selectedMonster!,
                  selectedModifier ?? undefined
                )
              )
            }
          >
            Attack (2 AP)
          </button>
          <button
            type="button"
            className="btn btn-secondary"
            disabled={!canAfford(AP.discardRedraw) || busy}
            onClick={() => runAction(() => discardHandRedraw(roomCode!, playerId))}
          >
            Discard Hand (3 AP)
          </button>
          <button
            type="button"
            className="btn btn-ghost"
            disabled={!isMyTurn || busy}
            onClick={() => runAction(() => endTurn(roomCode!, playerId))}
          >
            End Turn
          </button>
        </div>
      </footer>
    </div>
  );
}
