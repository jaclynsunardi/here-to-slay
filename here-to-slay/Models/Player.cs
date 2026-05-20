namespace HereToSlay.Models;

public enum PlayerType
{
    Host,
    Guest
}

public class Player
{
    public string Id {get; set;} = string.Empty;
    public string Name {get; set;} = string.Empty;
    public PlayerType Type {get; set;} = PlayerType.Guest;

}
