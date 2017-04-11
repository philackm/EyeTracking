using System;
using System.Collections.Generic;

namespace EyeTrackingCore {

    public class RawToFixationConverter {
        
        private List<GazePoint> rawPoints;

        public RawToFixationConverter(List<GazePoint> points) {
            this.rawPoints = points;
        }

        // Converts raw gaze input into fixations.
        public List<Fixation> CalculateFixations(int windowSize, float peakThreshold, float radius) {
            
            Console.WriteLine("Calculating fixations...");
            List<Point> allPoints = ConvertGazePointsToPoints(rawPoints);

            // Create the array to store the differences in the windows for each point.
            float[] differences = new float[rawPoints.Count];
            for(int i = 0; i < rawPoints.Count; i++) {
                differences[i] = 0;
            }

            // 1: find the mean of the sliding window before and after sample i
            for(int i = windowSize; i <= (rawPoints.Count - windowSize) - 1; i++) {

                List<Point> leftWindow = allPoints.GetRange(i - windowSize, windowSize);
                List<Point> rightWindow = allPoints.GetRange(i + 1, windowSize);

                Point meanBefore = GeometricMean(leftWindow);
                Point meanAfter = GeometricMean(rightWindow);

                // 2: create vector "d", with distances between before and after windows
                float axbxSquared = (meanBefore.x - meanAfter.x) * (meanBefore.x - meanAfter.x);
                float aybySquared = (meanBefore.y - meanAfter.y) * (meanBefore.y - meanAfter.y);
                float difference = (float)Math.Sqrt(axbxSquared + aybySquared);

                differences[i] = difference;
            }

            // Testing
            /*
            Console.WriteLine("Differences:");
            int count = 0;
            foreach(float difference in differences) {
                Console.Write(count++ + ":" + difference + ", ");
            }
            Console.Write("\n");
            */
            // End Testing

            // 3: create peak vector and find peaks (that is, find the large(est) differences in means of the sliding windows)
            // Create the array to store the differences in the windows for each point.
            float[] peaks = new float[rawPoints.Count];
            for(int i = 0; i < rawPoints.Count; i++) {
                peaks[i] = 0;
            }

            for(int i = 1; i < rawPoints.Count - 1; i++) {
                // if this is a peak
                if(differences[i] > differences[i-1] && differences[i] > differences[i+1]) {
                    peaks[i] = differences[i];
                }
            }

            // Testing
            /*
            Console.WriteLine("Peaks:");
            count = 0;
            foreach(float peak in peaks) {
                Console.Write(count++ + ":" + peak + ", ");
            }
            Console.Write("\n");
            */
            // End Testing

            // 4: remove peaks that are too close to each other (only want the largest difference per sliding window)
            for(int i = windowSize; i <= (rawPoints.Count - windowSize) - 1; i++) {
                if(peaks[i] != 0) {
                    // check left side
                    for(int j = i - windowSize; j < i; j++) {
                        if(peaks[j] < peaks[i]) {
                            peaks[j] = 0;
                        }
                    }
                    // check right side
                    for(int j = i + 1; j <= i + windowSize; j++) {
                        if(peaks[j] < peaks[i]) {
                            peaks[j] = 0;
                        }
                    }
                }
            }

            // Testing
            /*
            Console.WriteLine("Peaks after removing nearby peaks:");
            count = 0;
            foreach(float peak in peaks) {
                Console.Write(count++ + ":" + peak + ", ");
            }
            Console.Write("\n");
            */
            // End Testing

            // 5: create list with the indices of the peaks in the peak vector
            //      only add the peaks greater than some threshold, this works the same as a naieve algorithm, we only consider it a saccade if the distance is over a vertain threshold.

            List<int> peakIndices = new List<int>();
            for(int i = 0; i < rawPoints.Count; i++) {
                if(peaks[i] > peakThreshold) {
                    peakIndices.Add(i);
                }
            }

            // Testing
            /*
            Console.WriteLine("Peak Indices");
            count = 0;
            foreach(float index in peakIndices) {
                Console.Write(count++ + ":" + index + ", ");
            }
            Console.Write("\n");
            */
            // End Testing

            // 6a: estimate the spacial position of all the fixations between candidate saccades (peaks)                  
            //      use the geometric median of all the raw points 

            // 6b: fixations that are closer together than a specified range are merged together.

            float shortestDistance = 0;
            List<Fixation> fixations = new List<Fixation>();

            while(shortestDistance < radius) {
                
                fixations.Clear();

                // 6a
                for(int i = 1; i <= peakIndices.Count - 1; i++) {

                    int rawFromIndex = peakIndices[i-1];
                    int rawToIndex = peakIndices[i];

                    List<GazePoint> points = new List<GazePoint>();
                    for(int rawIndex = rawFromIndex; rawIndex <= rawToIndex; rawIndex++) {
                        points.Add(rawPoints[rawIndex]);
                    }

                    VSLocation majorityLocation = MajorityVSLocation(points);
                    Point median = GeometricMedian(ConvertGazePointsToPoints(points));
                    int startTime = points[0].timestamp;
                    int endTime = points[points.Count - 1].timestamp;
                    Fixation fixation = new Fixation(median.x, median.y, startTime, endTime, majorityLocation);
                    fixations.Add(fixation);
                }

                // 6b
                shortestDistance = float.MaxValue;

                for(int i = 1; i <= fixations.Count - 1; i++) {

                    int index = 0;
                    Point a = new Point(fixations[i - 1].x, fixations[i - 1].y);
                    Point b = new Point(fixations[i].x, fixations[i].y);

                    float distance = EuclideanDistance(a, b);

                    if(distance < shortestDistance) {
                        shortestDistance = distance;
                        index = i;
                    }

                    if (shortestDistance < radius) {
                        try {
                            peakIndices.RemoveAt(index);
                        }
                        catch(Exception e) {
                            Console.WriteLine(e.Message);
                            Console.WriteLine("index:" + index);
                            Console.WriteLine(peakIndices.Count);
                        }
                    }
                }
            }

            // Testing
            /*
            Console.WriteLine("Fixations:");
            count = 0;
            foreach(Fixation fixation in fixations) {
                Console.Write(count++ + ":" + fixation.x + "," + fixation.y + ", ");
            }
            Console.Write("\n");
            */
            // End Testing

            return fixations;
        }

        public List<Saccade> GenerateSaccades(List<Fixation> fixations)
        {
            List<Saccade> saccades = new List<Saccade>();

            for (int i = 0; i < fixations.Count - 1; i++)
            {
                Fixation from = fixations[i];
                Fixation to = fixations[i + 1];

                Saccade s = new Saccade(new Point(from.x, from.y), new Point(to.x, to.y));
                saccades.Add(s);
            }

            return saccades;
        }

        private List<Point> ConvertGazePointsToPoints(List<GazePoint> gazePoints) {
            List<Point> newPoints = new List<Point>();

            foreach(GazePoint p in gazePoints) {
                newPoints.Add(p.ToPoint());
            }

            return newPoints;
        }

        private float EuclideanDistance(Point a, Point b) {
            return (float)Math.Sqrt((a.x - b.x)*(a.x - b.x) + (a.y - b.y)*(a.y - b.y));
        }

        private VSLocation MajorityVSLocation(GazePoint[] points) {

            Dictionary<VSLocation, int> counts = new Dictionary<VSLocation, int>();

            foreach(GazePoint gazePoint in points) {
                if (counts.ContainsKey(points.location)) {
                    // just increment
                    counts[points.location] = counts[points.location] + 1;
                }
                else {
                    counts[points.location] = 1;
                }
            }

            return MaxInCount(counts);
        }

        private VSLocation MaxInCount(Dictionary<VSLocation, int> counts) {
            VSLocation majority = VSLocation.Nothing;
            int currentMax = 0;

            foreach(VSLocation key in counts.Keys()) {
                if (counts[key] > currentMax) {
                    currentMax = counts[key];
                    majority = key;
                }
            }

            return majority; 
        }

        // Weiszfeld Algorithm
        public Point GeometricMedian(List<Point> points) {

            float epsilon = 0.01f; // The lower this is, the more iterations it will take to approximate the median, but the more accurate it becomes.
            Point previous = GeometricMean(points);;
            Point next = previous;

            int loops = 0;

            do {
                loops++;
                previous = next;
                Point numeratorSum = new Point(0, 0);
                float denominatorSum = 0;

                // BUG: When p is the same as previous (that is, one of the estimates from
                // a previous round happens to be the same as another point in the set of points
                // then the algorithm returns NaN because 'differenceMagnitude' becomes 0, then p/differenceMagnitude
                // = 1 / 0 which is NaN/Inf). To solve this, add a tiny value to the point when it is the same as the previous round.

                foreach(Point p in points) {

                    if(p.Equals(previous)) {
                        p.Add(new Point(epsilon, epsilon));
                    }

                    float differenceMagnitude = p.Minused(previous).Magnitude();
                    Point quotient = p.Divided(differenceMagnitude);
                    numeratorSum.Add(quotient);

                    denominatorSum += 1 / differenceMagnitude;
                }

                next = numeratorSum.Divided(denominatorSum);
            }
            while(previous.Minused(next).Magnitude() > epsilon);
            return next;
        }

        public Point GeometricMean(List<Point> points) {

            Point mean = new Point(0,0);
            float sumX = 0;
            float sumY = 0;
            int count = 0;

            foreach (Point p in points) {
                sumX += p.x;
                sumY += p.y;
                count++;
            }

            float meanX = sumX / count;
            float meanY = sumY / count;

            return new Point(meanX, meanY);
        }
    }
}