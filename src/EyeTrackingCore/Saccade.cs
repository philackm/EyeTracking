using System;
using System.Collections.Generic;
using System.Linq;

namespace EyeTrackingCore
{
    // Long is defined to be a saccade > 1.1 degrees
    public enum SaccadeType
    {
        Long,
        Short
    }

    public enum Sector
    {
        Right,
        Up,
        Left,
        Down
    }

    public class Saccade
    {
        private Point from;
        private Point to;

        private Lazy<double> distance;
        private Lazy<double> direction; // in radians
        private Lazy<SaccadeType> type;
        private Lazy<Sector> sector;

        private double distanceFromMonitor = 60; // cm
        private double thresholdAngle = 1.1; // degrees
        private double pixelsPerCm = 96 / 2.54; //average is 96 pixels per inch, and there are 2.54cm per inch

        public static Dictionary<Sector, Dictionary<String, Sector[]>> sectorRelations = Saccade.SetupRelations();

        public Saccade(Point from, Point to)
        {
            this.from = from;
            this.to = to;

            distance = new Lazy<double>(() => CalculateDistance());
            direction = new Lazy<double>(() => CalculateDirection());
            type = new Lazy<SaccadeType>(() => CalculateSaccadeType(distanceFromMonitor, thresholdAngle, pixelsPerCm));
            sector = new Lazy<Sector>(() => CalculateSectorFromDirection(Direction));
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

        public Sector Sector4
        {
            get
            {
                return sector.Value;
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

        // direction is in radians.
        private Sector CalculateSectorFromDirection(double direction)
        {
            Sector result = Sector.Right;

            // Right
            bool in315to360 = direction >= (7 * Math.PI) / 4 && direction < 2 * Math.PI;
            bool in0to45 = direction >= 0 && direction < Math.PI / 4;
            if (in315to360 || in0to45)
            {
                result = Sector.Right;
            }

            // Up
            bool in45to135 = direction >= Math.PI / 4 && direction < (3 * Math.PI) / 4;
            if(in45to135)
            {
                result = Sector.Up;
            }

            // Left
            bool in135to225 = direction >= (3 * Math.PI) / 4 && direction < (5 * Math.PI) / 4;
            if(in135to225)
            {
                result = Sector.Left;
            }

            // Down
            bool in225to315 = direction >= (5 * Math.PI) / 4 && direction < (7 * Math.PI) / 4;
            if(in225to315)
            {
                result = Sector.Down;
            }

            return result;
        }

        public static double RadiansToDegrees(double radians)
        {
            return (radians / Math.PI) * 180;
        }

        public static double DegreesToRadians(double degrees)
        {
            return (degrees / 180) * Math.PI;
        }

        public static Relation Compare(Saccade second, Saccade first)
        {
            Relation result = Relation.Follow;

            if(Saccade.IsFollow(second, first))
            {
                result = Relation.Follow;
            }
            else if(Saccade.IsNeighbour(second, first))
            {
                result = Relation.Neighbour;
            }
            else if(Saccade.IsOpposite(second, first))
            {
                result = Relation.Opposite;
            }

            return result;
        }

        private static bool IsRelatedBy(String relation, Saccade second, Saccade first)
        {
            return Saccade.sectorRelations[second.Sector4][relation].ToList().Contains(first.Sector4);
        }

        // Ask: Is the following saccade opposite to the one preceding it, following it (in the same sector), or is it a neighbour (in a neighbouring sector)
        private static bool IsOpposite(Saccade second, Saccade first)
        {
            return IsRelatedBy("opposite", second, first);
        }

        private static bool IsFollow(Saccade second, Saccade first)
        {
            return IsRelatedBy("follow", second, first);
        }

        private static bool IsNeighbour(Saccade second, Saccade first)
        {
            return IsRelatedBy("neighbour", second, first);
        }

        private static Dictionary<Sector, Dictionary<String, Sector[]>> SetupRelations()
        {
            // up -> down, left, right
            // down -> up, left, right
            // left -> right, up, down
            // right -> left, up, down

            Dictionary<Sector, Dictionary<String, Sector[]>> relations = new Dictionary<Sector, Dictionary<string, Sector[]>>();

            var relationsToRight = new Dictionary<String, Sector[]>
            {
                { "opposite", new Sector[] { Sector.Left } },
                { "follow", new Sector[] { Sector.Right } },
                { "neighbour", new Sector[] { Sector.Up, Sector.Down } }
            };

            var relationsToUp = new Dictionary<String, Sector[]>
            {
                { "opposite", new Sector[] { Sector.Down } },
                { "follow", new Sector[] { Sector.Up } },
                { "neighbour", new Sector[] { Sector.Left, Sector.Right } }
            };

            var relationsToLeft = new Dictionary<String, Sector[]>
            {
                { "opposite", new Sector[] { Sector.Right } },
                { "follow", new Sector[] { Sector.Left } },
                { "neighbour", new Sector[] { Sector.Up, Sector.Down } }
            };

            var relationsToDown = new Dictionary<String, Sector[]>
            {
                { "opposite", new Sector[] { Sector.Up } },
                { "follow", new Sector[] { Sector.Down } },
                { "neighbour", new Sector[] { Sector.Left, Sector.Right } }
            };

            relations.Add(Sector.Right, relationsToRight);
            relations.Add(Sector.Up, relationsToUp);
            relations.Add(Sector.Left, relationsToLeft);
            relations.Add(Sector.Down, relationsToDown);

            return relations;
        }

        public enum Relation
        {
            Follow,
            Neighbour,
            Opposite
        }
    }
}