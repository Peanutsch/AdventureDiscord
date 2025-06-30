using Adventure.Models.Player;

namespace Adventure.Loaders
{
    public static class PlayerLoader
    {
        public static List<PlayerModel> Load() =>
           JsonDataLoader.LoadListFromJson<PlayerModel>("Data/Player/player.json");
    }
}
