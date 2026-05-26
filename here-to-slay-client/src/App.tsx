import { Routes, Route } from "react-router-dom";
import HomeScreen from "./pages/HomeScreen";
import CreateGame from "./pages/CreateGame";
import Lobby from "./pages/Lobby";
import JoinGame from "./pages/JoinGame";
import GameBoard from "./pages/GameBoard";

function App() {
  return (
    <Routes>
      <Route path="/" element={<HomeScreen />} />
      <Route path="/creategame" element={<CreateGame />} />
      <Route path="/lobby/:roomCode" element={<Lobby />} />
      <Route path="/joingame" element={<JoinGame />} />
      <Route path="/game/:roomCode" element={<GameBoard />} />
    </Routes>
  );
}

export default App;
