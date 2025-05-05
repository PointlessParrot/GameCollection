using GameCollection.Extras;
using GameCollection.PongGame;
using Database = GameCollection.Extras.NonSqlGameDatabase;

namespace GameCollection
{
    internal class Program
    {
        static void Main(string[] args)
        {
            GameLog.createLog();
            Database.setupConnection();
            Pong.run().Wait();
        }
    }
}
