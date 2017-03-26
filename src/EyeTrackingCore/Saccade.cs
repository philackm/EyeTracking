using System;

namespace EyeTrackingCore
{

    // Long is defined to be a saccade > 1.1 degrees
    public enum SaccadeType
    {
        Long,
        Short
    }

    public class Saccade
    {
        private Point from;
        private Point to;

        private Lazy<double> distance;
        private Lazy<double> direction; // in radians
        private Lazy<SaccadeType> type;

        private double distanceFromMonitor = 60; // cm
        private double thresholdAngle = 1.1; // degrees
        private double pixelsPerCm = 96 / 2.54; //average is 96 pixels per inch, and there are 2.54cm per inch


        public Saccade(Point from, Point to)
        {
            this.from = from;
            this.to = to;

            distance = new Lazy<double>(() => CalculateDistance());
            direction = new Lazy<double>(() => CalculateDirection());
            type = new Lazy<SaccadeType>(() => CalculateSaccadeType(distanceFromMonitor, thresholdAngle, pixelsPerCm));
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

        public SaccadeType Type
        {
            get
            {
                return type.Value;
            }
        }

        public Point From
        {
            get
            {
                return from;
            }
        }

        public Point To
        {
            get
            {
                return to;
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

            double tan = Math.Atan2(-opposite, adjacent);

            // convert the angle to be between 0 and 2pi

            double angle = 0;

            if (tan < 0)
            {
                angle = Math.PI + (Math.PI + tan);
            }
            else
            {
                angle = tan;
            }

            
            return angle;
        }

        // distanceFromEyesToMonitor is in cm
        // threshold is the number of degrees the eye must move before a short saccade becomes a long saccade
        // pixelspercm is the pixel density of the display
        private SaccadeType CalculateSaccadeType(double distanceFromEyesToMonitor, double thresholdAngle, double pixelsPerCM)
        {
            // Calculate visual distance of threshold in pixels. (Takes into account pixel density.)
            double visualDistanceThreshold = (Math.Tan(DegreesToRadians(thresholdAngle)) * distanceFromEyesToMonitor) * pixelsPerCM;
            SaccadeType type = this.Distance > visualDistanceThreshold ? SaccadeType.Long : SaccadeType.Short;
            return type;
        }

        public static double RadiansToDegrees(double radians)
        {
            return (radians / Math.PI) * 180;
        }

        public static double DegreesToRadians(double degrees)
        {
            return (degrees / 180) * Math.PI;
        }
    }
}