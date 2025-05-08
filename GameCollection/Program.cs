using GameCollection.Extras;
using GameCollection.PongGame;
using Database = GameCollection.Extras.SqlGameDatabase;

namespace GameCollection
{
    internal class Program
    {
        static void Main(string[] args)
        {
            GameLog.createLog();
            Database.setupConnection();
            PongGame.PongGame.run().Wait();
        }
    }
}
