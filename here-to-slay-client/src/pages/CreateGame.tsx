import { useNavigate } from "react-router-dom";
import PageLayout from "../components/PageLayout";
import GameSetting from "../interactive-components/GameSetting";
import "../styling/CreateGame.css";

export default function CreateGame() {
  const navigate = useNavigate();

  return (
    <PageLayout>
      <div className="create-container">
        <h1>Create Game</h1>
        <GameSetting />
        <button
          type="button"
          className="go-back-button btn btn-ghost"
          onClick={() => navigate("/")}
        >
          Go Back
        </button>
      </div>
    </PageLayout>
  );
}
