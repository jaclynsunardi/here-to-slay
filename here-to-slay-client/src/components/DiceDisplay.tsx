import "../styling/DiceDisplay.css";

interface DiceDisplayProps {
  die1: number | null;
  die2: number | null;
  modifier: number | null;
  total: number | null;
}

export default function DiceDisplay({ die1, die2, modifier, total }: DiceDisplayProps) {
  if (die1 == null || die2 == null) return null;

  return (
    <div className="dice-display">
      <div className="dice-display__die">{die1}</div>
      <span className="dice-display__plus">+</span>
      <div className="dice-display__die">{die2}</div>
      {modifier != null && modifier > 0 && (
        <>
          <span className="dice-display__plus">+</span>
          <div className="dice-display__mod">{modifier}</div>
        </>
      )}
      {total != null && (
        <>
          <span className="dice-display__equals">=</span>
          <div className="dice-display__total">{total}</div>
        </>
      )}
    </div>
  );
}
