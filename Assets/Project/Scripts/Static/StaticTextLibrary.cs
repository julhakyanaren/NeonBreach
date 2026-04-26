using System.Collections.Generic;
public static class StaticTextLibrary
{
    public static readonly Dictionary<PlayerCommunicationMessageType, string> PlayerCommunicationMessage = 
        new Dictionary<PlayerCommunicationMessageType, string>()
        {
            {PlayerCommunicationMessageType.None, "" },
            {PlayerCommunicationMessageType.Help, "Help me!" },
            {PlayerCommunicationMessageType.FollowMe, "Follow me!" },
            {PlayerCommunicationMessageType.Danger, "Danger!" },
            {PlayerCommunicationMessageType.Thanks, "Thanks!" },
            {PlayerCommunicationMessageType.GoodGame, "Good Game!" },
            {PlayerCommunicationMessageType.GoodLuck, "Good Luck!" },
            {PlayerCommunicationMessageType.Yes, "Yes!" },
            {PlayerCommunicationMessageType.No, "No!" },
            {PlayerCommunicationMessageType.OK, "OK!" },
            {PlayerCommunicationMessageType.SeeEnemy, "See the enemy!" },
            {PlayerCommunicationMessageType.Ready, "Ready !" },
        };
    public static string Get(PlayerCommunicationMessageType type)
    {
        if (PlayerCommunicationMessage.TryGetValue(type, out string text))
        {
            return text;
        }

        return type.ToString();
    }
}
