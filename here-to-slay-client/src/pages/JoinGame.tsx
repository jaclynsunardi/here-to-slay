import {useState} from "react";
import {useNavigate} from 'react-router-dom';
import "../styling/JoinGame.css";

function JoinGame() {

    const navigate = useNavigate();
    const [playerName, setPlayerName] = useState("");
    const [roomCode, setRoomCode] = useState("");
    const [error, setError] = useState("");

    const handleJoin = async () => {
        if (!playerName.trim()) {
            setError("Please enter a player name.");
            return;
        }
        if (!roomCode.trim()) {
            setError("Please enter a room code.");
            return;
        }

        navigate(`/lobby/${roomCode}`);
    };

    return (
        <div className="join-container">
            <h1> Join Game </h1>
            <div className="join-box">
                <div className="join-item">
                    <label htmlFor="playerName">Player Name:</label>
                    <input type="text" id="playerName" className="join-input" value={playerName} onChange={(e) => setPlayerName(e.target.value)} placeholder="Enter your player name" />
                    <label htmlFor="roomCode">RoomCode:</label>
                    <input type="text" id="roomCode" className="join-input" value={roomCode} onChange={(e) => setRoomCode(e.target.value.toUpperCase())} placeholder="Enter room code" />
                    {error && <p className="join-error">{error}</p>}
                    <button className="btn-join" onClick={handleJoin}>Join Game</button>
                    <button className="btn-back" onClick={() => navigate("/")}>Go Back</button>
                </div>
            </div>
        </div>
    );
}

export default JoinGame;