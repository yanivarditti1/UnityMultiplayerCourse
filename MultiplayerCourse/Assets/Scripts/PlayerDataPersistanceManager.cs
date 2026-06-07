using UnityEngine;

public class PlayerDataPersistanceManager
{
    public static PlayerDataPersistanceManager Instance { get; } = new();

    public string Nickname { get; private set; } = "";
    public int MaxHealth { get; private set; } = 100;
    public string PlayerCharacter { get; private set; } = "";
    
    public void SetNickname(string nickname)
    {
        Nickname = nickname;
    }
    public void SetMaxHealth(int maxHealth)
    {
        MaxHealth = maxHealth;
    }
    public void SetPlayerCharacter(string playerCharacter)
    {
        PlayerCharacter = playerCharacter;
    }

    public void Reset()
    {
        Nickname = "";
        MaxHealth = 100;
        PlayerCharacter = "";
    }
}
