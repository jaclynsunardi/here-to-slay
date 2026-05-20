using System.ComponentModel.DataAnnotations;

namespace HereToSlay.Models;


public enum GameState
{
    Waiting,
    InProgress,
    Finished
}
public class Game
{
    public string RoomCode {get; set;} = string.Empty;
    public List<Player> Players {get; set;} = new();
    public List<Card> Deck {get; set;} = new();
    public int CurrentPlayerIndex {get; set;} = 0;
    public GameState State {get; set;} = GameState.Waiting;

}