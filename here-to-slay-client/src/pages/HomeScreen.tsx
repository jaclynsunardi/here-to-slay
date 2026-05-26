import { useNavigate } from "react-router-dom";
import "../styling/HomeScreen.css";

export default function HomeScreen() {
  const navigate = useNavigate();

  return (
    <div className="title-screen">
      <div className="title-screen__bg" aria-hidden />
      <div className="title-screen__vignette" aria-hidden />

      <main className="title-screen__main">
        <img
          src="/images/edited/logo.png"
          className="title-screen__logo"
          alt="Here to Slay"
        />

        <p className="title-screen__tagline">
          Assemble six classes or slay three monsters to win.
        </p>

        <div className="title-screen__rules-hint">
          <span>3 action points per turn</span>
          <span className="title-screen__dot">·</span>
          <span>Official rules</span>
        </div>

        <div className="title-screen__actions">
          <button
            type="button"
            className="btn btn-primary title-screen__btn"
            onClick={() => navigate("/creategame")}
          >
            Create Game
          </button>
          <button
            type="button"
            className="btn btn-secondary title-screen__btn"
            onClick={() => navigate("/joingame")}
          >
            Join Game
          </button>
        </div>
      </main>

      <div className="title-screen__art title-screen__art--left" aria-hidden>
        <img src="/images/edited/backdrop.png" alt="" />
      </div>
      <div className="title-screen__art title-screen__art--right" aria-hidden>
        <img src="/images/edited/backdrop.png" alt="" />
      </div>
    </div>
  );
}
