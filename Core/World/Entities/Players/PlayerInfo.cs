namespace Helion.World.Entities.Players;

public class PlayerInfo
{
    /// <summary>
    /// The name of the player.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    public PlayerGender Gender { get; set; } = PlayerGender.Other;

    /// <summary>
    /// The gender string of the player e.g. 'male' or 'female'.
    /// </summary>
    public string GetGender()
    {
        return Gender switch
        {
            PlayerGender.Male => "male",
            PlayerGender.Female => "female",
            _ => "other",
        };
    }
    /// <summary>
    /// The gender subject of the player e.g. 'he' or 'she'.
    /// </summary>
    public string GetGenderSubject()
    {
        return Gender switch
        {
            PlayerGender.Male => "he",
            PlayerGender.Female => "she",
            _ => "they",
        };
    }
    /// <summary>
    /// The gender object of the player e.g. 'him' or 'her'.
    /// </summary>
    public string GetGenderObject()
    {
        return Gender switch
        {
            PlayerGender.Male => "him",
            PlayerGender.Female => "her",
            _ => "them",
        };
    }
}
