import {useState} from "react";
import {useNavigate} from 'react-router-dom';
import {useParams} from 'react-router-dom';
import "../styling/Lobby.css";

function Lobby() {

    const navigate = useNavigate();
    const {roomCode} = useParams();

    const players = [
        "Player 1",
        "Waiting for player..."
    ];

    return (
        <div className="lobby-container">
            <div className="lobby-card">
                <h1 className="lobby-title">Game Lobby</h1>
                <div className="room-code-box">
                    <span className="room-label">Room Code:</span>
                    <div className="room-code">{roomCode}</div>
                </div>
                <div className="players-section">
                    <h2>Players</h2>
                    <div className="players-list">
                        {players.map((player, index) => (
                            <div key={index} className={`player-card ${player.includes("Waiting")? "waiting" : "joined"}`}>{player}</div>
                        ))}
                    </div>
                    <div className="lobby-actions">
                        <button className="btn-copy" onClick={() => navigator.clipboard.writeText(roomCode || "")}>Copy Code</button>
                        <button className="btn-start">Start Game</button>
                    </div>
                </div>
            </div>
        </div>
    );
}

export default Lobby;