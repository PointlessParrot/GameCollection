using System;
using GameCollection.Extras;
using static GameCollection.Extras.UtilFunctions;

namespace GameCollection.PongGame
{
    internal static partial class Pong
    {
        struct ExitInfo
        {
            public bool isNull;
            public bool naturalExit;
            public int playerScore;

            public static ExitInfo nullExitInfo() => new ExitInfo
                { isNull = true };
            public static ExitInfo newManualExit() => new ExitInfo();
            public static ExitInfo newNaturalExit(int playerScoreIn) => new ExitInfo
                { naturalExit = true, playerScore = playerScoreIn };
            public static ExitInfo newExitInfo(bool naturalExitIn, int playerScoreIn) => new ExitInfo
                { naturalExit = naturalExitIn, playerScore = playerScoreIn };
        }

        interface IDrawable
        {
            void draw();
        }
        
        class Paddle :  IDrawable
        {
            double size;
            double halfSize => size / 2;
            double x, y, yVelocity;

            public Paddle(double xPosition, double paddleSize = 0.25)
            {
                size = paddleSize; 
                x = xPosition;
                y = 0.5;
            }
            
            public void draw()
            {
                int h1 = snapToGrid(y - halfSize);
                int h2 = snapToGrid(y + halfSize);
                int w =  snapToGrid(x);
                for (int h = h1; h < h2; h++)
                    addPixel(w, h);
            }
            
            public void update(bool ignore, bool down, bool stop)
            {
                y += yVelocity * targetLoopTime;
                if (boundaryCheck())
                {
                    yVelocity = 0;
                    y = clamp(halfSize, 1 - halfSize, y);
                }

                if (ignore) return;
                
                yVelocity = stop ? 0 : down ? paddleSpeed : -paddleSpeed;
            }
            
            
            bool boundaryCheck() => Math.Abs(y - 0.5) + halfSize > 0.5;

            public Coord currentVelocity() => (0, yVelocity);
            public Coord currentPosition() => (x, y);
            public Coord nextPosition() => currentPosition() + currentVelocity();
            public Coord nextPosition(double proportion) => currentPosition() + currentVelocity() * proportion;
            
            public bool willCollideWith(Ball ball, ref int score)
            {
                if (willCollideWith(ball) == false)
                    return false;

                score++;
                return true;
            }
            public bool willCollideWith(Ball ball)
            {
                double xDiff = x - ball.currentPosition().x;
                double t = xDiff / ball.currentVelocity().x;
                
                if(!ball.hasEnoughMovement(t))
                    return false;

                double contact = ball.nextPosition(t).y;
                double relative = (contact - y) / halfSize;
                if (!inRadius(0, 1, relative))
                    return false;
                
                ball.collideVertical(t, f(relative));
                return true;
            }
            static double f(double value) => value * value * value;
        }

        class Ball : IDrawable
        {
            const double bounceSpeedup = 1.1; 
            const double startAngleRange = Math.PI / 6;
            const double maxStartVelocity = 0.0005 * targetLoopTime;
            const double minStartVelocity = 0.5 * maxStartVelocity;

            double remainingMovement;
            Coord position, velocity;

            public Ball(Coord inPosition, Coord? inVelocity = null)
            {
                remainingMovement = 1;
                position = inPosition;

                if (inVelocity != null)
                {
                    velocity = inVelocity.Value;
                    return;
                }

                double angle = startAngleRange * rng.NextDouble();
                double speed = maxStartVelocity + rng.NextDouble() * (maxStartVelocity - minStartVelocity) * (2 * rng.Next(2) - 1);

                velocity = new Coord(Math.Cos(angle) * speed, Math.Sin(angle) * speed);
            }

            public void draw()
            {
                int w = snapToGrid(position.x);
                int h = snapToGrid(position.y);
                addPixel(w, h);
            }

            void update(double proportion) => position = nextPosition(proportion);
            public void update()
            {
                position = nextPosition();
                remainingMovement = 1;
            }
            
            public Coord currentVelocity() => velocity;
            public Coord currentPosition() => position; 

            public Coord nextPosition() => position + velocity * remainingMovement;
            public Coord nextPosition(double proportion) => position + velocity * proportion;
            
            public bool hasEnoughMovement(double t) => inRange(0, remainingMovement, t); 
            
            public void collideVertical(double t, double velocityFactor, bool doSpeedup = true)
            {
                update(t);
                remainingMovement -= t;
                velocity.x *= doSpeedup ? -bounceSpeedup : -1;
                velocity.y += velocityFactor * Coord.abs(velocity);
            }
            public void collideHorizontal(double t, double velocityFactor, bool doSpeedup = true)
            {
                update(t);
                remainingMovement -= t;
                velocity.y *= doSpeedup ? -bounceSpeedup : -1;
                velocity.x += velocityFactor * Coord.abs(velocity);
            }
            
            public bool willCollideWithBoundary()
            {
                double height = velocity.y > 0 ? 1 : 0;
                double yDiff = height - position.y;
                double t = yDiff / velocity.y;
                
                if (!hasEnoughMovement(t))
                    return false;
                
                collideHorizontal(t, 0, false);
                return true;
            }

            public bool hasReachedEdge(out bool playerWon)
            {
                playerWon = position.x > ratioXY / 2;
                return !inRange(0, ratioXY, position.x);
            }
        }

    }
}