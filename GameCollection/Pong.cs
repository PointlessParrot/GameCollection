using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameCollection
{
    internal static class Pong
    {
        const double xSize = 2;
        const double ySize = 1;

        static Random rng = new Random();

        static int height, width;

        public static void run()
        {
            throw new NotImplementedException();
        }

        class Paddle
        {
            public double x { get; private set; }
            public double y { get; private set; }
            public double yVelocity { get; private set; }

            public Paddle(bool left)
            {
                y = ySize * 0.5;
                x = xSize * (left ? 0.1 : 0.9);
            }
        }

        class Ball
        {
            const double startAngleRange = Math.PI / 6;
            const double minStartVelocity = 0.1;
            const double maxStartVelocity = 0.8;

            public double x { get; private set; }
            public double y { get; private set; }
            public double xVelocity { get; private set; }
            public double yVelocity { get; private set; }

            public Ball((double x, double y)? velocity = null)
            {
                if (velocity == null)
                {
                    double angle = rng.NextDouble() * startAngleRange;
                    double speed = rng.NextDouble() * (maxStartVelocity - minStartVelocity) + maxStartVelocity;

                    xVelocity = Math.Cos(angle) * speed * ((rng.Next(2) + 1) / 2);
                    yVelocity = Math.Sin(angle) * speed;
                }
                else
                {
                    xVelocity = velocity.Value.x;
                    yVelocity = velocity.Value.y;
                }
            }

        }

    }
}
