using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameCollection
{
    internal static class Pong
    {
        const double ratioXY = 2;

        static Random rng = new Random();

        static int gameHeight, gameWidth;

        public static void run()
        {
            // :(
            
            throw new NotImplementedException();
        }

        static void addPixel(int x, int y) => GameScreen.queue(x, y, '\u2588');
        static int snapToGrid(double snapY) => (int)Math.Round(snapY * gameHeight);

        struct Coord
        {
            public double x;
            public double y;
            
            public Coord(double xIn, double yIn) => (x, y) = (xIn, yIn);
            
            public static Coord operator +(Coord a, Coord b) => new Coord(a.x + b.x, a.y + b.y);
            public static Coord operator -(Coord a, Coord b) => new Coord(a.x - b.x, a.y - b.y);
            public static Coord operator *(Coord a, double b) => new Coord(a.x * b, a.y * b);
            public static Coord operator *(double a, Coord b) => new Coord(b.x * a, b.y * a);
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

            public Paddle(bool left, double paddleSize = 0.1)
            {
                size = paddleSize; 
                y = 0.5;
                x = ratioXY * (left ? 0.05 : 0.95);
            }
            
            public void draw()
            {
                int h1 = snapToGrid(y - halfSize);
                int h2 = snapToGrid(y + halfSize);
                int w =  snapToGrid(x);
                for (int h = h1; h < h2; h++)
                    addPixel(w, h);
            }
            
            public void update(int acceleration = 0)
            {
                y += yVelocity;
                if (boundaryCheck())
                {
                    yVelocity = 0;
                    y = clamp(halfSize, 1 - halfSize, y);
                }
                
                yVelocity += acceleration;
            }
            
            double clamp(double min, double max,  double value) => Math.Min(Math.Max(value, min), max);
            bool inRange(double min, double max, double value) => value > min && value < max;
            bool inRadius(double mid, double rad, double value) => Math.Abs(mid - value) < rad;
            
            bool boundaryCheck() => Math.Abs(y - 0.5) > halfSize + 0.5;

            public bool checkCollision(Ball ball)
            {
                double xDiff = x - ball.currentPosition().x;
                double t = xDiff / ball.curentVelocity().x;
                
                if(!inRadius(y, halfSize, ball.nextPosition(t).y))
                    return false;
                
                ball.collideVertical(t, yVelocity);
                return true;
            }
        }

        class Ball : IDrawable
        {
            const double bounceSpeedup = 1.1; 
            const double velocityTransfer = 0.25;
            const double startAngleRange = Math.PI / 6;
            const double minStartVelocity = 0.02;
            const double maxStartVelocity = 0.04;

            Coord position, velocity;

            public Ball(Coord? inPosition = null, Coord? inVelocity = null)
            {
                position = inPosition ?? new Coord(0.5, 0.5);

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

            public void update() => position = nextPosition();
            public void update(double proportion) => position = nextPosition(proportion);
            
            public Coord curentVelocity() => velocity;
            public Coord currentPosition() => position; 

            public Coord nextPosition() => position + velocity;
            public Coord nextPosition(double proportion) => position + velocity * proportion;
            
            public void collideVertical(double t, double paddleVelocity)
            {
                update(t);
                velocity.x *= -bounceSpeedup;
                velocity.y += paddleVelocity * velocityTransfer;
                update(1 - t);
            }
        }

    }
}
