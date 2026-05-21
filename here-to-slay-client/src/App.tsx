import './App.css'
import HomeScreen from './pages/HomeScreen';
import {Routes, Route} from 'react-router-dom';
import CreateGame from './pages/CreateGame.tsx';

function App() {

  return (
    <Routes>
      <Route path="/" element={<HomeScreen />} />
      <Route path="/creategame" element={<CreateGame />} />
    </Routes>
  );
}

export default App;
