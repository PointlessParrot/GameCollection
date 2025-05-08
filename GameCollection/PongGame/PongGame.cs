using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static GameCollection.Extras.UtilFunctions;
using GameCollection.Extras; 
using GameDatabase = GameCollection.Extras.SqlGameDatabase;


namespace GameCollection.PongGame
{
    internal static partial class PongGame
    {
        static Random rng = new Random();

        const bool debug = false;
        
        const double paddleOffset = 0.2;
        const int targetLoopTime = 50;
        const double ratioXY = 2.5;
        const int gameHeight = 40;
        const int gameWidth = (int)(gameHeight * ratioXY);
        
        static double paddleGap => ratioXY - 2 * paddleOffset;
        
        public static async Task run()
        {
            GameScreen.changeViewSize((2 * gameWidth + 4, gameHeight + 2));
            GameScreen.cursorVisible = false;
            GameDatabase.trySetupLeaderboard("PongLeaderboard");
            await loadingScreen();
            title();
            
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
                    
                    case "StartTwo":
                        exitInfo = await runTwoPlayerGame();
                        break;
                        
                    case "Leaderboard":
                        leaderboardMenu();
                        exitInfo = ExitInfo.nullExitInfo();
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
            Paddle playerPaddle = new Paddle(paddleOffset, true);
            Paddle enemyPaddle = new Paddle(ratioXY - paddleOffset, false);
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
                playerPaddle.draw();
                enemyPaddle.draw();
                ball.draw();
                Task writeTask = GameScreen.writeAsync();
                
                // Collision handling
                handleCollisions(playerPaddle, enemyPaddle, ball, ref score);
                
                // Get inputsn
                getHumanInputs(out bool playerDown, out bool playerStop, out bool playerIgnore, out exit);
                getComputerInputs(ball, enemyPaddle, out bool enemyDown, out bool enemyStop, out bool enemyIgnore);
                //getComputerInputs(ball, playerPaddle, out bool playerDown, out bool playerStop, out bool playerIgnore);
                
                // Move entities
                playerPaddle.update(playerIgnore, playerDown, playerStop);
                enemyPaddle.update(enemyIgnore, enemyDown, enemyStop);
                ball.update();

                // Wait for write to finish
                await writeTask;
                
                // Check win
                if (ball.hasReachedEdge(out playerScored))
                {
                    if (!playerScored)
                        break;

                    ball = new Ball((0.5 * ratioXY, 0.5));
                    score += 10;
                }
                
                // Get time taken
                int elapsed = (int)sw.ElapsedMilliseconds;
                
                // Wait until minimum time
                if (elapsed < targetLoopTime)
                    await Task.Delay(targetLoopTime - elapsed); 
            }

            return ExitInfo.newExitInfo(!exit, score);
        }
        static async Task<ExitInfo> runTwoPlayerGame()
        {
            GameScreen.clearAll();
            Paddle leftPaddle = new Paddle(paddleOffset, true);
            Paddle rightPaddle = new Paddle(ratioXY - paddleOffset, true);
            Ball ball = new Ball((0.5 * ratioXY, 0.5));
            
            int leftScore = 0;
            int rightScore = 0;
            Stopwatch sw =  new Stopwatch();
            
            drawBorder();
            
            // Main loop
            bool exit = false;
            while (exit == false)
            {
                sw.Restart();
                // Start drawing to screen
                leftPaddle.draw();
                rightPaddle.draw();
                ball.draw();
                Task writeTask = GameScreen.writeAsync();

                // Collision handling
                handleCollisions(leftPaddle, rightPaddle, ball, ref leftScore, ref rightScore);

                // Get inputs
                getHumanPairInputs(out bool leftDown, out bool rightDown, out bool leftStop, out bool rightStop,
                    out bool leftIgnore, out bool rightIgnore, out exit);

                // Move entities
                leftPaddle.update(leftIgnore, leftDown, leftStop);
                rightPaddle.update(rightIgnore, rightDown, rightStop);
                ball.update();

                // Wait for write to finish
                await writeTask;

                GameScreen.setHight(-2);
                GameScreen.writeText($"{leftScore} : {rightScore}");
                
                // Check win
                if (ball.hasReachedEdge(out bool leftWon))
                {
                    ball = new Ball((0.5 * ratioXY, 0.5));
                    if (leftWon)
                        leftScore += 10;
                    else
                        rightScore += 10;
                }

                // Get time taken
                int elapsed = (int)sw.ElapsedMilliseconds;
                
                // Wait until minimum time
                if (elapsed < targetLoopTime)
                    await Task.Delay(targetLoopTime - elapsed); 
            }

            return ExitInfo.nullExitInfo();
        }

        static void handleCollisions(Paddle playerPaddle, Paddle enemyPaddle, Ball ball, ref int score) =>
            handleCollisions(playerPaddle, enemyPaddle, ball, ref score, ref score);
        static void handleCollisions(Paddle leftPaddle, Paddle rightPaddle, Ball ball, ref int leftScore, ref int rightScore)
        {
            bool collided = true;
            while (collided)
            {
                collided = false;
                collided |= leftPaddle.willCollideWith(ball, ref leftScore);
                collided |= rightPaddle.willCollideWith(ball, ref rightScore);
                collided |= ball.willCollideWithBoundary();
            }   
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
            getHighscore(out int maxScore, out string maxName);
            Console.OpenStandardInput().Flush();
            GameScreen.clearAll();
            
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
                GameScreen.writeText($"{++i:D2}) {result.score:D3} by {result.name} {result.date:dd/MM/yy}");
            }
            
            GameScreen.newLine(3);
            GameScreen.writeText("  Press any key to continue... ");
            Console.ReadKey(true);
        }
        static string mainMenu(ExitInfo exitInfo)
        {
            if (exitInfo.isNull) {}
            
            if (exitInfo.naturalExit)  
                addScore(exitInfo.playerScore);
            
            GameScreen.clearAll();
            
            GameScreen.newLine(7);
            GameScreen.writeText("MAIN MENU");
            GameScreen.newLine(4);
            GameScreen.writeText(" Please select an option: ");
            GameScreen.writeText("                          ");
            GameScreen.writeText("  1)  View Leaderboard    ");
            GameScreen.writeText("  2)  Single-Player Game  ");
            GameScreen.writeText("  3)  Multi-Player Game   ");
            GameScreen.writeText("                          ");
            GameScreen.writeText("                          ");
            GameScreen.writeText("                          ");
            GameScreen.writeText("                          "); 
            GameScreen.writeText("                          ");
            GameScreen.writeText("  9)  Exit                ");

            while (true)
            {
                switch (Console.ReadKey(true).Key)
                {
                    case ConsoleKey.D1:
                        return "Leaderboard";
                    case ConsoleKey.D2:
                        return "StartGame";
                    case ConsoleKey.D3:
                        return "StartTwo";
                    case ConsoleKey.D4:
                    case ConsoleKey.D5:
                    case ConsoleKey.D6:
                    case ConsoleKey.D7:
                    case ConsoleKey.D8:
                        break;
                    case ConsoleKey.D9:
                        return "Exit";

                }
            }
        }

        static void title()
        {
            GameScreen.clearAll();
            
            GameScreen.newLine(15);
            GameScreen.writeText("                                                                      ");
            GameScreen.writeText("  ████████      ██████████      ████        ██          ██████████    ");
            GameScreen.writeText("  ██      ██        ██          ████        ██        ██          ██  ");
            GameScreen.writeText("  ██        ██      ██          ██  ██      ██      ██                ");
            GameScreen.writeText("  ██        ██      ██          ██  ██      ██      ██                ");
            GameScreen.writeText("  ██      ██        ██          ██    ██    ██      ██                ");
            GameScreen.writeText("  ████████          ██          ██    ██    ██      ██        ██████  ");
            GameScreen.writeText("  ██                ██          ██      ██  ██      ██            ██  ");
            GameScreen.writeText("  ██                ██          ██      ██  ██      ██            ██  ");
            GameScreen.writeText("  ██                ██          ██        ████        ██          ██  ");
            GameScreen.writeText("  ██            ██████████      ██          ██          ██████████    ");
            GameScreen.writeText("                                                                      ");

            while (Console.KeyAvailable)
                Console.ReadKey(true);
            
            Thread.Sleep(500);
            Console.ReadKey(true);
        }
        static async Task loadingScreen()
        {
            const double div = 500;
            const double cut = 5000 / div;
            const double end = 8250 / div;
            const double rad = 0.2;
            const double sep = 0.75;
            const int wait = 100;
            
            IntegerCoord centre = (gameWidth / 2, gameHeight / 2);
            
            Thread.Sleep(500);
            
            if (debug) return;
            
            double t = 0;
            Stopwatch sw = Stopwatch.StartNew(); 
            while (true)
            {
                for (int i = 0; i < 3; i++)
                    addPixel(circle(centre, rad, bunch(t + sep * i), cut));
                
                
                await GameScreen.writeAsync();
                GameScreen.setHight((int)(gameHeight * 0.2));
                GameScreen.writeText(" Loading ");

                int mil = (int)sw.ElapsedMilliseconds; 
                if (wait - mil > 10)
                    await Task.Delay(wait - (int)sw.ElapsedMilliseconds);
                
                t += sw.ElapsedMilliseconds / div;
                sw.Restart();
                if (t > end)
                    break;
            }

            GameScreen.setHight((int)(gameHeight * 0.2));
            GameScreen.writeText(" Loading ");
            
            Thread.Sleep(500);
        }

        const double stretch = 1.2;
        static double bunch(double t) => t - 0.8 * Math.Cos(t);

        static IntegerCoord circle(IntegerCoord centre, double radius, double t, double cut) =>
            c(centre, radius - clamp(0, radius, Math.Pow((t - cut), 3) / 1000), t, cut);
        static IntegerCoord c(IntegerCoord centre, double radius, double t, double cut) =>
            centre + (snapToGrid(stretch * radius * Math.Cos(t)), snapToGrid(radius * Math.Sin(t)));
        
        const double paddleSpeed = 0.0002;
        static void getComputerInputs(Ball ball, Paddle paddle, out bool down, out bool stop, out bool ignore)
        {
            ignore = false;
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
        static void getHumanInputs(out bool down, out bool stop, out bool ignore, out bool exit)
        {
            ignore = !Console.KeyAvailable;
            down = stop = exit = false;

            if (ignore) 
                return;

            ConsoleKey key = Console.ReadKey(true).Key;
            switch (key)
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
                case ConsoleKey.LeftArrow:
                case ConsoleKey.RightArrow:
                    stop = true;
                    break;
                case ConsoleKey.Tab:
                case ConsoleKey.Backspace:
                    exit = true;
                    break;
                default:
                    ignore = true;
                    break;
            }
            
            GameLog.log(key.ToString());
        }

        static void getHumanPairInputs(out bool leftDown, out bool rightDown, out bool leftStop, out bool rightStop,
            out bool leftIgnore, out bool rightIgnore, out bool exit)
        {
            List<ConsoleKey> keys = new List<ConsoleKey>();
            
            leftDown = rightDown = false;
            leftStop = rightStop = false;
            leftIgnore = rightIgnore = false;
            exit = false;
            
            while (Console.KeyAvailable)
                keys.Add(Console.ReadKey(true).Key);

            if (keys.Contains(ConsoleKey.W))
            {
                
            }
            else if (keys.Contains(ConsoleKey.S))
            {
                leftDown = true;
            }
            else if (keys.Contains(ConsoleKey.A) || keys.Contains(ConsoleKey.D))
            {
                leftStop = true;
            }
            else
            {
                leftIgnore = true;
            }
             
            if (keys.Contains(ConsoleKey.UpArrow))
            {
                
            }
            else if (keys.Contains(ConsoleKey.DownArrow))
            {
                rightDown = true;
            }
            else if (keys.Contains(ConsoleKey.LeftArrow) || keys.Contains(ConsoleKey.RightArrow))
            {
                rightStop = true;
            }
            else
            {
                rightIgnore = true;
            }

            if (keys.Contains(ConsoleKey.Tab) || keys.Contains(ConsoleKey.Backspace))
            {
                exit = true;
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

        static void addPixelAndKeep(IntegerCoord pos) => addPixelAndKeep(pos.x, pos.y);
        static void addPixelAndKeep(int x, int y)
        {
            GameScreen.queueAndKeep((2 * x + 2, y + 1), '\u2588');
            GameScreen.queueAndKeep((2 * x + 3, y + 1), '\u2588');
        }
        static void addPixel(IntegerCoord pos) => addPixel(pos.x, pos.y);
        static void addPixel(int x, int y)
        {
            GameScreen.queue((2 * x + 2, y + 1), '\u2588');
            GameScreen.queue((2 * x + 3, y + 1), '\u2588');
        }
        static int snapToGrid(double snapY) => (int)Math.Round(snapY * (gameHeight-1));
    }
}
