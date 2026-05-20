import {useState} from "react";
import '../styling/HomeScreen.css';
import CreateGame from './CreateGame.tsx';

function HomeScreen() {
  
  const [showCreateGame, setShowCreateGame] = useState(false);

  if (showCreateGame) {
    return <CreateGame />;
  }

  return (
    <div className="home-container">
        <div className="game-board">
            <img src="/images/edited/logo.png" className="logo" />
        </div>
        <div className='buttons'>
            <button className="btn btn-primary" onClick={() => setShowCreateGame(true)}>Create Game</button>
            <button className='btn btn-secondary'>Join Game</button>
        </div>
        <div className='side-image-left'>
            <img src="/images/edited/backdrop.png" />
        </div>
        <div className='side-image-right'>
            <img src="/images/edited/backdrop.png" />
        </div>
    </div>
  );
}

export default HomeScreen;
