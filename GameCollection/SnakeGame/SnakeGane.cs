using System;
using System.Threading;
using System.Threading.Tasks;

namespace GameCollection.SnakeGame
{
    public static partial class SnakeGame
    {
        const int height = 40;
        const int width = 100;
        
        public static async Task run()
        {
            loading();

            while (true)
            {
                string option = mainMenu();
                switch (option)
                {
                    case "StartGame":
                        runGame();
                        break;
                    case "Exit":
                        return;
                    default:
                        throw new NotSupportedException($"Option {option} not supported");
                }
            }
        }

        static void loading()
        {
            Thread.Sleep(1000);
        }

        static string mainMenu()
        {
            return "StartGame";
        }

        static async Task runGame()
        {
            
            while (true)
            {
                
            }
        }
    }
}