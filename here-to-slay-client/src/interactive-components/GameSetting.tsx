import {useState} from "react";
import {useNavigate} from 'react-router-dom';
import "../styling/GameSetting.css";

const BACKEND_URL = "http://localhost:5262";
const roomCodeLength = 6;

function generateRoomCode() {
    const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';
    let code = '';

    for (let i = 0; i < roomCodeLength; i++) {
        code += chars.charAt(Math.floor(Math.random() * chars.length));
    }

    return code;
};

function GameSetting() {

    const navigate = useNavigate();

    const [gameType, setGameType] = useState("classic");
    const [numPlayers, setNumPlayers] = useState(2);
    const [roomCode, setRoomCode] = useState(generateRoomCode());
    const [lobbyVisibility, setLobbyVisibility] = useState("public");

    const handleCreate = async () => {
        const response = await fetch(`${BACKEND_URL}/game/create`, {
            method: "POST",
            headers: {"Content-Type": "application/json"},
            body: JSON.stringify({
                hostName: "Player1",
                roomCode: roomCode,
                numPlayers: numPlayers,
                gameType: gameType
            })
        });

        const game = await response.json();
        console.log("Game created:", game);
        navigate(`/lobby/${roomCode}`);
    };
    
    return (
        <div className="game-setting-container">
            <h3>Game Settings</h3>
            <div className="setting-box">
                <div className="setting-item">
                    <label htmlFor="numPlayers">Number of Players:</label>
                    <select id="numPlayers" name="numPlayers" className="setting-dropbox" value={numPlayers} onChange={(e) => setNumPlayers(Number(e.target.value))}>
                        <option value={2}>2</option>
                    </select>
                    <label htmlFor="gameType">Game Type:</label>
                    <select id="gameType" name="gameType" className="setting-dropbox" value={gameType} onChange={(e) => setGameType(e.target.value)}>
                        <option value="classic">Classic</option>
                        <option value="expansions">With Expansions</option>   
                    </select>
                    {gameType === "expansions" && (
                        <>
                            <label htmlFor="expansionAddOn">Expansion Add-On:</label>
                            <select id="expansionAddOn" className="setting-dropbox">
                                <option value="comingSoon">Coming Soon</option>
                            </select>
                        </>
                    )}
                    <label htmlFor="roomCode">Room Code:</label>
                    <input type="text" id="roomCode" name="roomName" className="setting-input" value={roomCode} onChange={(e) => setRoomCode(e.target.value)}/>
                    <label htmlFor="lobbyVisibility">Lobby Visibility:</label>
                    <select id="lobbyVisibility" className="setting-dropbox" value={lobbyVisibility} onChange={(e) => setLobbyVisibility(e.target.value)}>
                        <option value="public">Public</option>
                        <option value="private">Private</option>
                    </select>
                    <button className="btn-create" onClick={handleCreate}>Create Game</button>
                </div>
            </div>
        </div>
    );
}

export default GameSetting;