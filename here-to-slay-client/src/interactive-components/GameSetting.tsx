import {useState} from "react";
import "../styling/GameSetting.css";


function GameSetting() {

    const [numPlayers, setNumPlayers] = useState(2);
    const [gameType, setGameType] = useState("classic");
    

    return (
        <div className="game-setting-container">
            <h3>Game Settings</h3>
            <div className="setting-box">
                <div className="setting-item">
                    <label htmlFor="numPlayers">Number of Players:</label>
                    <select id="numPlayers" name="numPlayers" className="setting-dropbox">
                        <option value={1}>2</option>
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
                    <input type="text" id="roomCode" name="roomName" className="setting-input" />
                    <label htmlFor="lobbyVisibility">Lobby Visibility:</label>
                    <select id="lobbyVisibility" className="setting-dropbox">
                        <option value="public">Public</option>
                        <option value="private">Private</option>
                    </select>
                    <label htmlFor="invitedPlayers">Invited Players:</label>
                    <input type="text" id="invitedPlayers" name="invitedPlayers" className="setting-input" />
                </div>
            </div>
        </div>
    );
}

export default GameSetting;