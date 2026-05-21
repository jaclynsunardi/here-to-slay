import './App.css'
import {Routes, Route} from 'react-router-dom';
import HomeScreen from './pages/HomeScreen';
import CreateGame from './pages/CreateGame.tsx';
import Lobby from './pages/Lobby.tsx';

function App() {

  return (
    <Routes>
      <Route path="/" element={<HomeScreen />} />
      <Route path="/creategame" element={<CreateGame />} />
      <Route path='/lobby/:roomCode' element={<Lobby />} />
    </Routes>
  );
}

export default App;
