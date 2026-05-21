import {useState} from "react";
import {useNavigate} from 'react-router-dom';
import '../styling/HomeScreen.css';
import CreateGame from './CreateGame.tsx';

function HomeScreen() {

    const navigate = useNavigate();

  return (
    <div className="home-container">
        <div className="game-board">
            <img src="/images/edited/logo.png" className="logo" />
        </div>
        <div className='buttons'>
            <button className="btn btn-primary" onClick={() => navigate('/creategame')}>Create Game</button>
            <button className='btn btn-secondary' onClick={() => navigate('/joingame')}>Join Game</button>
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
