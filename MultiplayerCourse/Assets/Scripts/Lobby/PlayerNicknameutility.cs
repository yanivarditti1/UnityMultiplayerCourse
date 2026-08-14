using Fusion;

public static class PlayerNicknameUtility
{
    public static string GetNickname(PlayerRef player)
    {
        if (player == PlayerRef.None)
            return "Unknown Player";

        if (PlayerMatchStats.TryGet(
                player,
                out PlayerMatchStats stats))
        {
            string nickname =
                stats.Nickname.ToString();

            if (!string.IsNullOrWhiteSpace(nickname))
                return nickname;
        }

        if (PlayerManager.Registry.TryGetValue(
                player,
                out PlayerManager playerManager))
        {
            string nickname =
                playerManager.Nickname.ToString();

            if (!string.IsNullOrWhiteSpace(nickname))
                return nickname;
        }

        return $"Player {player.PlayerId}";
    }
}