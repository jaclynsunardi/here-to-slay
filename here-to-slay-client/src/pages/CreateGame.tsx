import {useState} from "react";
import "../styling/CreateGame.css";
import HomeScreen from "./HomeScreen.tsx";
import GameSetting from "../interactive-components/GameSetting.tsx";


function CreateGame() {

    const [goBack, setGoBack] = useState(false);

    if (goBack) {
        return <HomeScreen />;
    }
    
    return (
        <div className="create-container">
            <h1>Create Game</h1>
            <GameSetting />
            <button className="go-back-button" onClick={() => setGoBack(true)}>Go Back</button>

        </div>
    );
}

export default CreateGame;