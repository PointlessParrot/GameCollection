using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static GameCollection.Extras.UtilFunctions;
using GameCollection.Extras; 
using GameDatabase = GameCollection.Extras.NonSqlGameDatabase;


namespace GameCollection.PongGame
{
    internal static partial class Pong
    {
        static Random rng = new Random();
        
        const double paddleOffset = 0.2;
        const int targetLoopTime = 50;
        const double ratioXY = 2.5;
        const int gameHeight = 40;
        const int gameWidth = (int)(gameHeight * ratioXY);
        
        static double paddleGap => ratioXY - 2 * paddleOffset;
        
        public static async Task run()
        {
            GameScreen.viewSize = (2 * gameWidth + 4, gameHeight + 2);
            GameScreen.cursorVisible = false;
            GameDatabase.trySetupLeaderboard("PongLeaderboard");
            loadingScreen();
            
            ExitInfo exitInfo = ExitInfo.nullExitInfo();
            bool run = true;
            while (run)
            {
                string option = mainMenu(exitInfo);
                switch (option)
                {
                    case "StartGame":
                        exitInfo = await runGame();
                        break;
                        
                    case "Leaderboard":
                        leaderboardMenu();
                        break;
                    
                    case "Exit":
                        run = false;
                        break;
                    
                    default:
                        throw new NotSupportedException($"Unsupported option: {option}");
                }
            }
        }
        static async Task<ExitInfo> runGame()
        {
            GameScreen.clearAll();
            Paddle enemyPaddle = new Paddle(ratioXY - paddleOffset);
            Paddle playerPaddle = new Paddle(paddleOffset);
            Ball ball = new Ball((0.5 * ratioXY, 0.5));
            
            int score = 0;
            bool playerScored = false;
            Stopwatch sw =  new Stopwatch();
            
            drawBorder();
            
            // Main loop
            bool exit = false;
            while (exit == false)
            {
                sw.Restart();
                // Start drawing to screen
                enemyPaddle.draw();
                playerPaddle.draw();
                ball.draw();
                Task writeTask = GameScreen.writeAsync();
                
                // <TEMP>
                // await writeTask;
                // </TEMP>
                
                // Collision handling
                bool collided = true;
                while (collided)
                {
                    collided = false;
                    collided |= enemyPaddle.willCollideWith(ball);
                    collided |= playerPaddle.willCollideWith(ball, ref score);
                    collided |= ball.willCollideWithBoundary();
                }
                
                // Get inputs
                getComputerInputs(ball, enemyPaddle, out bool enemyDown, out bool enemyStop);
                getHumanInputs(out bool playerIgnore, out bool playerDown, out bool playerStop, out exit);
                
                // Move entities
                enemyPaddle.update(false, enemyDown, enemyStop);
                playerPaddle.update(playerIgnore, playerDown, playerStop);
                ball.update();

                // Wait for write to finish
                await writeTask;
                
                // Check win
                if (ball.hasReachedEdge(out playerScored))
                {
                    if (!playerScored)
                        break;

                    ball = new Ball((0.5 * ratioXY, 0.5));
                    score++;
                }
                
                // Get time taken
                int elapsed = (int)sw.ElapsedMilliseconds;
                //lastLoopTime = max(elapsed, targetLoopTime);
                // Wait until minimum time
                if (elapsed < targetLoopTime)
                    await Task.Delay(targetLoopTime - elapsed); 
            }

            return ExitInfo.newExitInfo(!exit, score);
        }
        
        static void getHighscore(out int score, out string name)
        {
            GameDatabase.tryGetHighscores("PongLeaderboard", 1, out var results);
            if (results.Count != 0)
            {
                score = results[0].score;
                name = results[0].name;
            }
            else
            {
                score = -1;
                name = string.Empty;
            }
        } 
        static void addScore(int score)
        {
            Console.OpenStandardInput().Flush();
            GameScreen.clearAll();
            getHighscore(out int maxScore, out string maxName);
            
            GameScreen.newLine(7);
            GameScreen.writeText(maxScore < 0 ? "There is currently no high score" : $"The current highscore is {maxScore} by {maxName}");
            GameScreen.writeText($"Your score was {score}");
            GameScreen.writeText("Would you like to add your score to the leaderboard? (Y/N)");
            
            if (Console.ReadKey(true).Key != ConsoleKey.Y)
                return;
            
            GameScreen.newLine(5);
            GameScreen.writeText(" Select a name: ");
            string name = getName();
            GameDatabase.tryAddScore("PongLeaderboard", score, name);
        }
        static string getName()
        {
            Func<int, char> charFunc = x => (char)(x < 26 ? 'A' + x : '0' + x - 26);
             
            const int len = 3; 
            int[] values = Enumerable.Repeat(0, len).ToArray();
            int index = 0;
            while (true)
            {
                GameScreen.writeText(string.Join(" ", values.Select(charFunc)), true, false);
                
                ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                switch (keyInfo.Key)
                {
                    case ConsoleKey.LeftArrow:
                        index = positiveMod(--index, len);
                        break;
                    case ConsoleKey.RightArrow:
                        index = positiveMod(++index, len);
                        break;
                    case ConsoleKey.UpArrow:
                        values[index] = positiveMod(--values[index], 36);
                        break;
                    case ConsoleKey.DownArrow:
                        values[index] = positiveMod(++values[index], 36);
                        break;
                    case ConsoleKey.Enter:
                    case ConsoleKey.Spacebar:
                        return string.Concat(values.Select(charFunc));
                }
            }

        }
        
        static void leaderboardMenu()
        {
            GameScreen.clearAll();
            GameScreen.newLine(7);
            GameScreen.writeText("LEADERBOARD");
            GameScreen.newLine(4);

            GameDatabase.tryGetHighscores("PongLeaderboard", 25, out var results);
            
            if (results.Count == 0)
                GameScreen.writeText("There are currently no highscores");
            
            for (int i = 0; i < results.Count;)
            {
                var result = results[i];
                GameScreen.writeText($"{++i}) {result.score:D3} by {result.name} {result.date:dd/MM/yy}");
            }
            
            GameScreen.newLine(3);
            GameScreen.writeText("Press any key to continue...");
            Console.ReadKey(true);
        }
        static string mainMenu(ExitInfo exitInfo)
        {
            if (exitInfo.isNull) {}
            
            if (exitInfo.naturalExit)
                addScore(exitInfo.playerScore);
            
            GameScreen.clearAll();
            
            GameScreen.newLine(7);
            GameScreen.writeText("Would you like to view the leaderboard? (Y/N)");
            
            if (Console.ReadKey(true).Key == ConsoleKey.Y)
                leaderboardMenu();
            
            // <TEMP>
            GameScreen.clearAll();
            GameScreen.newLine(7);
            GameScreen.writeText("Press any key to start a new game...");
            Console.ReadKey(true);
            return "StartGame";
            // </TEMP>
        }
        static void loadingScreen()
        {
            Thread.Sleep(1000);
        }

        const double paddleSpeed = 0.0002;
        static void getComputerInputs(Ball ball, Paddle paddle, out bool down, out bool stop)
        {
            Coord positionDiff = ball.currentPosition() - paddle.currentPosition();
            Coord velocityDiff = ball.currentVelocity() - paddle.currentVelocity();

            if (sign(positionDiff.x) == sign(velocityDiff.x))
            {
                positionDiff.x += paddleGap * sign(positionDiff.x);
                velocityDiff.x *= -1;
            }
            
            double t = -positionDiff.x / velocityDiff.x;
            double finalDiff = positionDiff.y + t * velocityDiff.y; 
            stop = abs(finalDiff) < paddleSpeed * t;
            down = finalDiff > 0;
        }
        static void getHumanInputs(out bool ignore, out bool down, out bool stop, out bool exit)
        {
            ignore = !Console.KeyAvailable;
            down = stop = exit = false;

            if (ignore) 
                return;
            
            switch (Console.ReadKey(true).Key)
            {
                case ConsoleKey.W:
                case ConsoleKey.UpArrow:
                    break;
                case ConsoleKey.S:
                case ConsoleKey.DownArrow:
                    down =  true;
                    break;
                case ConsoleKey.A:
                case ConsoleKey.D:
                case ConsoleKey.RightArrow:
                case ConsoleKey.LeftArrow:
                    stop = true;
                    break;
                case ConsoleKey.Delete:
                case ConsoleKey.Backspace:
                    exit = true;
                    break;
                default:
                    ignore = true;
                    break;
            }
        }
        
        static void drawBorder()
        {
            for (int x = -1; x <= gameWidth; x++)
            {
                addPixelAndKeep(x, -1);
                addPixelAndKeep(x, gameHeight);
            }
            for (int y = 0; y < gameHeight; y++)
            {
                addPixelAndKeep(-1, y);
                addPixelAndKeep(gameWidth, y);
            }
        }

        static void addPixelAndKeep(int x, int y)
        {
            GameScreen.queueAndKeep((2 * x + 2, y + 1), '\u2588');
            GameScreen.queueAndKeep((2 * x + 3, y + 1), '\u2588');
        }
        static void addPixel(int x, int y)
        {
            GameScreen.queue((2 * x + 2, y + 1), '\u2588');
            GameScreen.queue((2 * x + 3, y + 1), '\u2588');
        }
        static int snapToGrid(double snapY) => (int)Math.Round(snapY * (gameHeight-1));
    }
}
