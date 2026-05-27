import { useState } from "react";
import type { PendingChoiceView } from "../types/game";
import GameCard from "./GameCard";
import "../styling/PendingChoicePanel.css";

interface PendingChoicePanelProps {
  choice: PendingChoiceView;
  busy: boolean;
  onConfirm: (selectedCardInstanceIds: string[], selectedOption?: string) => void;
}

export default function PendingChoicePanel({
  choice,
  busy,
  onConfirm,
}: PendingChoicePanelProps) {
  const [selected, setSelected] = useState<string[]>([]);
  const [option, setOption] = useState<string | null>(null);

  if (!choice.isYourChoice) {
    return (
      <div className="pending-choice pending-choice--waiting">
        <p>{choice.prompt}</p>
      </div>
    );
  }

  const isOption = choice.kind === "SelectOption";
  const canConfirmOption = isOption && option != null;
  const canConfirmCards =
    !isOption &&
    selected.length >= choice.minSelections &&
    selected.length <= choice.maxSelections;

  const toggleCard = (instanceId: string) => {
    setSelected((prev) => {
      if (prev.includes(instanceId)) return prev.filter((id) => id !== instanceId);
      if (prev.length >= choice.maxSelections) {
        if (choice.maxSelections === 1) return [instanceId];
        return prev;
      }
      return [...prev, instanceId];
    });
  };

  return (
    <div className="pending-choice">
      <h3 className="pending-choice__title">Your choice</h3>
      <p className="pending-choice__prompt">{choice.prompt}</p>
      {!isOption && (
        <p className="pending-choice__hint">
          Select {choice.minSelections === choice.maxSelections
            ? `${choice.minSelections}`
            : `${choice.minSelections}–${choice.maxSelections}`}{" "}
          card{choice.maxSelections !== 1 ? "s" : ""} ({selected.length} selected)
        </p>
      )}

      {isOption ? (
        <div className="pending-choice__options">
          {choice.options.map((opt) => (
            <button
              key={opt}
              type="button"
              className={`btn btn-secondary ${option === opt ? "btn--active" : ""}`}
              disabled={busy}
              onClick={() => setOption(opt)}
            >
              {opt === "yes" ? "Yes" : opt === "no" ? "No" : opt}
            </button>
          ))}
        </div>
      ) : (
        <div className="pending-choice__cards">
          {choice.selectableCards.map((card) => (
            <GameCard
              key={card.instanceId}
              card={card}
              small
              selected={selected.includes(card.instanceId)}
              onClick={() => toggleCard(card.instanceId)}
            />
          ))}
        </div>
      )}

      <div className="pending-choice__actions">
        <button
          type="button"
          className="btn btn-primary"
          disabled={busy || (!canConfirmCards && !canConfirmOption)}
          onClick={() =>
            onConfirm(
              selected,
              option ?? undefined
            )
          }
        >
          Confirm
        </button>
        {!isOption && choice.minSelections === 0 && (
          <button
            type="button"
            className="btn btn-ghost"
            disabled={busy}
            onClick={() => onConfirm([])}
          >
            Skip (discard none)
          </button>
        )}
      </div>
    </div>
  );
}
