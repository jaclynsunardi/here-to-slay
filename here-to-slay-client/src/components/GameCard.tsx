import type { CardView } from "../types/game";
import "../styling/GameCard.css";

interface GameCardProps {
  card: CardView;
  selected?: boolean;
  disabled?: boolean;
  small?: boolean;
  onClick?: () => void;
}

export default function GameCard({
  card,
  selected = false,
  disabled = false,
  small = false,
  onClick,
}: GameCardProps) {
  const classNames = [
    "game-card",
    small ? "game-card--small" : "",
    selected ? "game-card--selected" : "",
    disabled ? "game-card--disabled" : "",
    onClick && !disabled ? "game-card--clickable" : "",
    card.isPartyLeader ? "game-card--leader" : "",
  ]
    .filter(Boolean)
    .join(" ");

  return (
    <button
      type="button"
      className={classNames}
      onClick={disabled ? undefined : onClick}
      title={`${card.name} — ${card.effectText}`}
    >
      <img src={card.imagePath} alt={card.name} className="game-card__art" />
      <div className="game-card__badge">{card.type}</div>
      {card.type === "Monster" && (
        <div className="game-card__difficulty">★ {card.rollThreshold}</div>
      )}
      {card.isPartyLeader && <div className="game-card__leader-tag">Leader</div>}
      {card.attachedItems.length > 0 && (
        <div className="game-card__items">{card.attachedItems.length} item(s)</div>
      )}
    </button>
  );
}
