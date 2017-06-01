using System;
using System.Collections.Generic;
using System.Linq;

namespace EyeTrackingCore
{
    // Long is defined to be a saccade > 1.1 degrees
    public enum SaccadeType
    {
        Short,
        Medium,
        Long
    }

    public enum Sector
    {
        Right,
        Up,
        Left,
        Down
    }

    public enum SectorEight
    {
        Right,
        Up,
        Left,
        Down,
        UpRight,
        UpLeft,
        DownLeft,
        DownRight
    }

    public class Saccade
    {
        private Point from;
        private Point to;

        private Lazy<double> distance;
        private Lazy<double> direction; // in radians
        private Lazy<SaccadeType> type;
        private Lazy<Sector> sector;
        private Lazy<SectorEight> sector8;

        private double distanceFromMonitor = 60; // cm
        private double thresholdAngle = 1.1; // degrees
        private double pixelsPerCm = 96 / 2.54; //average is 96 pixels per inch, and there are 2.54cm per inch

        public static Dictionary<SectorEight, Dictionary<String, SectorEight[]>> sectorRelations = Saccade.SetupRelations();

        public Saccade(Point from, Point to)
        {
            this.from = from;
            this.to = to;

            distance = new Lazy<double>(() => CalculateDistance());
            direction = new Lazy<double>(() => CalculateDirection());
            type = new Lazy<SaccadeType>(() => CalculateSaccadeType(distanceFromMonitor, thresholdAngle, pixelsPerCm));
            sector = new Lazy<Sector>(() => CalculateSectorFromDirection(Direction));
            sector8 = new Lazy<SectorEight>(() => CalculateSector8FromDirection(Direction));
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

        public SectorEight Sector8
        {
            get
            {
                return sector8.Value;
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
            // TODO: Update this to use actual values calculated from the standard deviation.

            /*
            // Using <= 1.1 degrees from distance of 60cm at 96 ppi to classify as short, otherwise long
            // Calculate visual distance of threshold in pixels. (Takes into account pixel density.)
            double visualDistanceThreshold = (Math.Tan(DegreesToRadians(thresholdAngle)) * distanceFromEyesToMonitor) * pixelsPerCM;
            SaccadeType type = this.Distance > visualDistanceThreshold ? SaccadeType.Long : SaccadeType.Short;
            return type;
            */

            // arbitrary values being used at the moment
            if(this.Distance < 200)
            {
                return SaccadeType.Short;
            }
            else if(this.Distance < 600)
            {
                return SaccadeType.Medium;
            }
            else
            {
                return SaccadeType.Long;
            }
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

        // direction is in radians.
        private SectorEight CalculateSector8FromDirection(double direction)
        {
            SectorEight result = SectorEight.Right;


            // get 45 degrees in radians
            double degrees45inRadians = (45 * Math.PI) / 180;
            double half45inRadians = (45 / 2 * Math.PI) / 180;
            double negative360minusHalf45inRadians = ((360 - (45 / 2)) * Math.PI) / 180;


            // Right
            bool in360minus45over2 = direction >= negative360minusHalf45inRadians && direction < 2 * Math.PI;
            bool in0to45over2 = direction >= 0 && direction < half45inRadians;
            if (in360minus45over2 || in0to45over2)
            {
                result = SectorEight.Right;
            }

            // Up Right
            if (direction >= half45inRadians && direction < half45inRadians + (degrees45inRadians * 1))
            {
                result = SectorEight.UpRight;
            }

            // Up
            if (direction >= half45inRadians + (degrees45inRadians * 1) && direction < half45inRadians + (degrees45inRadians * 2))
            {
                result = SectorEight.Up;
            }

            // Up Left
            if (direction >= half45inRadians + (degrees45inRadians * 2) && direction < half45inRadians + (degrees45inRadians * 3))
            {
                result = SectorEight.UpLeft;
            }

            // Left
            if (direction >= half45inRadians + (degrees45inRadians * 3) && direction < half45inRadians + (degrees45inRadians * 4))
            {
                result = SectorEight.Left;
            }

            // Down Left
            if (direction >= half45inRadians + (degrees45inRadians * 4) && direction < half45inRadians + (degrees45inRadians * 5))
            {
                result = SectorEight.DownLeft;
            }

            // Down
            if (direction >= half45inRadians + (degrees45inRadians * 5) && direction < half45inRadians + (degrees45inRadians * 6))
            {
                result = SectorEight.Down;
            }

            // Down Right
            if (direction >= half45inRadians + (degrees45inRadians * 6) && direction < half45inRadians + (degrees45inRadians * 7))
            {
                result = SectorEight.DownRight;
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
            return Saccade.sectorRelations[second.Sector8][relation].ToList().Contains(first.Sector8);
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

        private static Dictionary<SectorEight, Dictionary<String, SectorEight[]>> SetupRelations()
        {
            // up -> down, left, right
            // down -> up, left, right
            // left -> right, up, down
            // right -> left, up, down

            Dictionary<SectorEight, Dictionary<String, SectorEight[]>> relations = new Dictionary<SectorEight, Dictionary<string, SectorEight[]>>();

            var relationsToRight = new Dictionary<String, SectorEight[]>
            {
                { "opposite", new SectorEight[] { SectorEight.Left } },
                { "follow", new SectorEight[] { SectorEight.Right } },
                { "neighbour", new SectorEight[] { SectorEight.UpRight, SectorEight.DownRight } }
            };

            var relationsToUp = new Dictionary<String, SectorEight[]>
            {
                { "opposite", new SectorEight[] { SectorEight.Down } },
                { "follow", new SectorEight[] { SectorEight.Up } },
                { "neighbour", new SectorEight[] { SectorEight.UpLeft, SectorEight.UpRight } }
            };

            var relationsToLeft = new Dictionary<String, SectorEight[]>
            {
                { "opposite", new SectorEight[] { SectorEight.Right } },
                { "follow", new SectorEight[] { SectorEight.Left } },
                { "neighbour", new SectorEight[] { SectorEight.UpLeft, SectorEight.DownLeft } }
            };

            var relationsToDown = new Dictionary<String, SectorEight[]>
            {
                { "opposite", new SectorEight[] { SectorEight.Up } },
                { "follow", new SectorEight[] { SectorEight.Down } },
                { "neighbour", new SectorEight[] { SectorEight.DownLeft, SectorEight.DownRight } }
            };


            var relationsToUpRight = new Dictionary<String, SectorEight[]>
            {
                { "opposite", new SectorEight[] { SectorEight.DownLeft } },
                { "follow", new SectorEight[] { SectorEight.UpRight } },
                { "neighbour", new SectorEight[] { SectorEight.Up, SectorEight.Right } }
            };

            var relationsToUpLeft = new Dictionary<String, SectorEight[]>
            {
                { "opposite", new SectorEight[] { SectorEight.DownRight } },
                { "follow", new SectorEight[] { SectorEight.UpLeft } },
                { "neighbour", new SectorEight[] { SectorEight.Up, SectorEight.Left } }
            };

            var relationsToDownLeft = new Dictionary<String, SectorEight[]>
            {
                { "opposite", new SectorEight[] { SectorEight.UpRight } },
                { "follow", new SectorEight[] { SectorEight.DownLeft } },
                { "neighbour", new SectorEight[] { SectorEight.Left, SectorEight.Down } }
            };

            var relationsToDownRight = new Dictionary<String, SectorEight[]>
            {
                { "opposite", new SectorEight[] { SectorEight.UpLeft } },
                { "follow", new SectorEight[] { SectorEight.DownRight } },
                { "neighbour", new SectorEight[] { SectorEight.Down, SectorEight.Right } }
            };

            relations.Add(SectorEight.Right, relationsToRight);
            relations.Add(SectorEight.Up, relationsToUp);
            relations.Add(SectorEight.Left, relationsToLeft);
            relations.Add(SectorEight.Down, relationsToDown);

            relations.Add(SectorEight.UpRight, relationsToUpRight);
            relations.Add(SectorEight.UpLeft, relationsToUpLeft);
            relations.Add(SectorEight.DownLeft, relationsToDownLeft);
            relations.Add(SectorEight.DownRight, relationsToDownRight);

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