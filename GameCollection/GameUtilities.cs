using System;
using System.Collections.Generic;

namespace GameCollection
{
    static class GameScreen
    {
        struct GameScreenChar
        {
            public char c;
            public int y;
            public int x;
            
            public GameScreenChar(char cIn, int yIn, int xIn) => (c, y, x) = (cIn, yIn, xIn);
        }
        
        static Queue<GameScreenChar> writeQueue = new Queue<GameScreenChar>();
        
        public static void write()
        {
            while (writeQueue.Count > 0)
            {
                GameScreenChar gameScreenChar = writeQueue.Dequeue();
                Console.SetCursorPosition(gameScreenChar.x, gameScreenChar.y);
                Console.Write(gameScreenChar.c);   
            }
        }

        public static void queue(int x, int y, char c) => writeQueue.Enqueue(new GameScreenChar(c, x, y));
        
    }
}