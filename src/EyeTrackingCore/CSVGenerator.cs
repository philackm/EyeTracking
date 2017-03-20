using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EyeTrackingCore
{
    public class CSVGenerator
    {
        public static Random random = new Random();
        public static int numSegments = 36;

        // segment 0 = all saccades in 0-9 degrees
        // segment 10 = all saccades in 10 - 19 degrees
        // and so on. 
        public static Dictionary<int, int> CalculateDirectionCounts(Saccade[] saccades)
        {
            Dictionary<int, int> segmentCounts = new Dictionary<int, int>();

            foreach (Saccade saccade in saccades)
            {
                int segment = (int)(RadiansToDegrees(saccade.Direction) / 10);

                if (segmentCounts.ContainsKey(segment))
                {
                    // the segment has already been seen, just increment.
                    int currentCount = segmentCounts[segment];
                    segmentCounts[segment] = ++currentCount;
                }
                else
                {
                    // the segment hasnt been seen, set it to 1.
                    segmentCounts[segment] = 1;
                }
            }

            return segmentCounts;
        }

        public static Saccade[] CreateTestSaccades()
        {
            int min = 20;
            int max = 20;

            Saccade[] saccades = new Saccade[100];
            Saccade start = new Saccade(RandomPoint(min, max), RandomPoint(min, max));
            saccades[0] = start;

            for(int i = 1; i < 100; i++)
            {
                saccades[i] = new Saccade(saccades[i - 1].To, RandomPoint(0, 20));
            }

            return saccades;
        }

        public static Point RandomPoint(int min, int max)
        {
            int x = random.Next(min, max);
            int y = random.Next(min, max);

            return new Point(x, y);
        }

        public static double RadiansToDegrees(double radians)
        {
            return (radians / Math.PI) * 180;
        }

        public static void CreateDirectionCSV(Dictionary<int, int> directionCounts)
        {
            for (int i = 0; i < numSegments; i++)
            {
                int segment = i;
                int value = directionCounts.ContainsKey(i) ? directionCounts[i] : 0;
                Console.WriteLine(segment + "," + value);
            }
        }
    }
}
