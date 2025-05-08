using System.Collections.Generic;
using System.Linq;
using GameCollection.Extras;

namespace GameCollection.SnakeGame
{
    public static partial class SnakeGame
    {
        class Snake
        {
            List<IntegerCoord> body;
            IntegerCoord direction;
            bool dead = false;

            IntegerCoord head => body[0];
            
            public Snake(IntegerCoord pos, IntegerCoord direction, int length = 1)
            {
                body = Enumerable.Repeat(pos, length).ToList();
                this.direction = direction;
            }

            public void update(IntegerCoord? newDirection)
            {
                if (newDirection.HasValue)
                    direction = newDirection.Value;
                
                body.Add(head + direction);
            }

            public void checkCollision(ICollideable collideable)
            {
                if (collideable.currentPosition().Equals(head))
                    return;
                
                collideable.collideWith(this);
            }

            public bool hasDied() => !dead;
            public void kill() => dead = true;
        }
        
        
        interface IDrawable
        {
            void draw();
        }

        interface ICollideable
        {
            IntegerCoord currentPosition();
            void collideWith(object sender);
        }

    }
}