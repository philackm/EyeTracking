using System;
using System.Collections.Generic;
using System.Xml.Serialization;

using EyeTrackingCore;

namespace ConsoleApplication
{
    public class Program
    {
        static private List<GazePoint> points = new List<GazePoint>();
        static private XmlSerializer serialiser = new XmlSerializer(points.GetType());

        public static void Main(string[] args)
        {
            Console.WriteLine(points.ToString());
            Console.WriteLine("Starting array serialisation to file...");

            points.Add(new GazePoint(1, 1, 0, VSLocation.Nothing));
            points.Add(new GazePoint(2, 2, 1, VSLocation.Nothing));

            try
            {
                System.IO.FileStream stream = new System.IO.FileStream("output.txt", System.IO.FileMode.CreateNew);
                serialiser.Serialize(stream, points);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            Console.WriteLine("Starting read byte by byte from text file...");
            try
            {
                System.IO.FileStream stream = new System.IO.FileStream("hello.txt", System.IO.FileMode.Open);

                int currentByte = stream.ReadByte();
                while (currentByte != -1)
                {
                    Console.WriteLine(Convert.ToChar(currentByte));
                    currentByte = stream.ReadByte();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            //TestGeometricMedian();

            TestConverter();

            Console.Read();
        }

        private static void TestConverter()
        {


            /*
            // line with centre at 3, 3
            List<GazePoint> gazePoints = new List<GazePoint>();
            gazePoints.Add(new GazePoint(1, 1, 1));
            gazePoints.Add(new GazePoint(0.8f, 0.8f, 2));
            gazePoints.Add(new GazePoint(0.9f, 0.9f, 3));
            gazePoints.Add(new GazePoint(1.1f, 1.1f, 4));
            gazePoints.Add(new GazePoint(10, 10, 5));
            gazePoints.Add(new GazePoint(9.8f, 9.8f, 6));
            gazePoints.Add(new GazePoint(9.9f, 9.9f, 7));
            gazePoints.Add(new GazePoint(11, 11, 8));
            gazePoints.Add(new GazePoint(30, 30, 9));
            gazePoints.Add(new GazePoint(31f, 31f, 10));
            gazePoints.Add(new GazePoint(29f, 29f, 11));
            gazePoints.Add(new GazePoint(25f, 25f, 12));
            gazePoints.Add(new GazePoint(50, 50, 13));
            gazePoints.Add(new GazePoint(51f, 51f, 14));
            gazePoints.Add(new GazePoint(49f, 49f, 15));
            gazePoints.Add(new GazePoint(50.5f, 50.5f, 16));
             */

            System.IO.FileStream stream = new System.IO.FileStream("gazePoints.xml", System.IO.FileMode.Open);
            List<GazePoint> gazePoints = serialiser.Deserialize(stream) as List<GazePoint>;

            RawToFixationConverter converter = new RawToFixationConverter(gazePoints);
            List<Fixation> fixations = converter.CalculateFixations(4, 5, 25, 0);
        }

        private static void TestGeometricMedian()
        {
            // square with centre at (1, 1)
            // List<Point> points = new List<Point>();
            //points.Add(new Point(0, 0));
            //points.Add(new Point(0, 2));
            //points.Add(new Point(2, 2));
            //points.Add(new Point(2, 0));

            // line with centre at 3, 3
            List<Point> points = new List<Point>();
            points.Add(new Point(1, 1));
            points.Add(new Point(2, 2));
            points.Add(new Point(3, 3));
            points.Add(new Point(4, 4));
            points.Add(new Point(5, 5));

            // Bunch of points.
            //List<Point> points = new Point[1000];
            //for(int i = 0; i < 1000; i++){
            //    points.Add(new Point(i*2, i*3));
            //}

            RawToFixationConverter converter = new RawToFixationConverter(null);
            Point median = converter.GeometricMedian(points);

            Console.WriteLine("Median was: X:" + median.x + " Y:" + median.y);
        }
    }
}