using System;

namespace EyeTracking
{

    public struct Point
    {
        public float x;
        public float y;

        public Point(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            Point pointObj = (Point)obj;
            return this.x == pointObj.x && this.y == pointObj.y;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public void Add(Point p)
        {
            this.x += p.x;
            this.y += p.y;
        }

        public Point Divided(float divisor)
        {
            return new Point(this.x / divisor, this.y / divisor);
        }

        public Point Minused(Point rhs)
        {
            return new Point(this.x - rhs.x, this.y - rhs.y);
        }

        public float Magnitude()
        {
            //Console.WriteLine("=========");
            //Console.WriteLine("x:" + this.x);
            //Console.WriteLine("y:" + this.y);
            float squared = (this.x * this.x) + (this.y * this.y);
            //Console.WriteLine("squared: " + squared);
            float sqrt = (float)Math.Sqrt(squared);
            //Console.WriteLine("sqrt:" + sqrt);
            return sqrt;
        }
    }

    public class GazePoint
    {

        public float x;
        public float y;

        public double timestamp; // milliseconds since start of tracking
        public bool exists; // don't think this is actually needed with tobii eyex

        public GazePoint()
        {
            this.x = 0;
            this.y = 0;
            this.timestamp = 0;
            this.exists = false;
        }

        public GazePoint(float x, float y, double timestamp)
        {
            this.x = x;
            this.y = y;

            this.timestamp = timestamp;
            this.exists = true;
        }

        public Point ToPoint()
        {
            return new Point(this.x, this.y);
        }
    }
}