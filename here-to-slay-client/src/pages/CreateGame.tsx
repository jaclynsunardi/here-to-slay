import {useState} from "react";
import {useNavigate} from 'react-router-dom';
import "../styling/CreateGame.css";
import HomeScreen from "./HomeScreen.tsx";
import GameSetting from "../interactive-components/GameSetting.tsx";


function CreateGame() {

    const navigate = useNavigate();
    
    return (
        <div className="create-container">
            <h1>Create Game</h1>
            <GameSetting />
            <button className="go-back-button" onClick={() => navigate('/')}>Go Back</button>

        </div>
    );
}

export default CreateGame;