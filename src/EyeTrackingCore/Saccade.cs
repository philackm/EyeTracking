using System;

namespace EyeTrackingCore
{
    public class Saccade
    {
        Point from;
        Point to;

        private Lazy<double> distance;
        private Lazy<double> direction; // in radians

        public Saccade(Point from, Point to)
        {
            this.from = from;
            this.to = to;

            distance = new Lazy<double>(() => CalculateDistance());
            direction = new Lazy<double>(() => CalculateDirection());
        }

        public double Distance
        {
            get
            {
                return distance.Value;
            }
        }

        public double Direction
        {
            get
            {
                return direction.Value;
            }
        }

        private double CalculateDistance()
        {
            double fx = from.x;
            double fy = from.y;

            double tx = to.x;
            double ty = to.y;

            double distance = MathNet.Numerics.Distance.Euclidean(new double[] { fx, fy }, new double[] { tx, ty });
            return distance;
        }

        // returns the direction in radians. 
        // 0 radians is at the positive x axis (right)
        // pi radians is at the negative x axis (left)
        // negative y is up
        // positive y is down
        private double CalculateDirection()
        {
            double opposite = to.y - from.y;
            double adjacent = to.x - from.x;

            return Math.Atan2(-opposite, adjacent);
        }
    }
}