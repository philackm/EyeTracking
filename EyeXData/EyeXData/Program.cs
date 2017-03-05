using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EyeXFramework;
using Tobii.EyeX.Framework;


namespace EyeXData
{
    class Program
    {
        static private EyeXDataCollector dataCollector;

        static void Main(string[] args)
        {
            dataCollector = new EyeXDataCollector(DataType.Fixations, 5000, "YOUTUBE");

            while (true)
            {
                String command = Console.ReadLine();

                switch (command)
                {
                    case "start":
                        Console.WriteLine("Starting...");
                        dataCollector.StartCollection();
                        break;
                    case "end":
                        Console.WriteLine("Ended.");
                        dataCollector.EndCollection();
                        break;
                    case "show":
                        Console.WriteLine("Showing...");
                        Console.Write(dataCollector.GenerateCollectedDataCSV());
                        break;
                    default:
                        break;
                }
            }
        }
    }
}

/*
namespace EyeXData
{
    class Program
    {

        static private EyeXHost _eyeXHost;
        static private GazePointDataStream stream;

        static void Main(string[] args)
        {
            // Initialize the EyeX Host 
            _eyeXHost = new EyeXHost();
            _eyeXHost.Start();

            // Create a data stream object and listen to events. 
            stream = _eyeXHost.CreateGazePointDataStream(GazePointDataMode.LightlyFiltered);
            stream.Next += Program.MyEventHandler;

            // Keep the console open.
            Console.ReadLine();
        }

        static void MyEventHandler(object sender, GazePointEventArgs args)
        {
            Console.WriteLine("X: " + args.X.ToString() + " Y: " + args.Y.ToString());
        }

        static void CleanUp()
        {
            // Dispose when done 
            stream.Dispose();
            _eyeXHost.Dispose();
        }
    }
}
*/

/*
namespace MinimalFixationDataStream
{
    using EyeXFramework;
    using System;
    using Tobii.EyeX.Framework;

    public static class Program
    {
        public static void Main(string[] args)
        {
            double lastFixationStartTime = 0;

            using (var eyeXHost = new EyeXHost())
            {
                eyeXHost.Start();

                // Create a data stream: lightly filtered gaze point data.
                // Other choices of data streams include EyePositionDataStream and FixationDataStream.
                using (var fixationGazeDataStream = eyeXHost.CreateFixationDataStream(FixationDataMode.Sensitive))
                {
                    // Write the data to the console.
                    fixationGazeDataStream.Next += (s, e) =>
                    {
                        if (e.EventType == FixationDataEventType.Begin)
                        {
                            Console.WriteLine("Fixation began.");
                            lastFixationStartTime = e.Timestamp;
                        }
                        if(e.EventType == FixationDataEventType.Data)
                        {
                            //Console.WriteLine($"X: {e.X}, Y: {e.Y}");
                        }
                        if (e.EventType == FixationDataEventType.End)
                        {
                            var lastFixationDuration = e.Timestamp - lastFixationStartTime;
                            Console.WriteLine("Last fixation duration: {0:0} milliseconds", lastFixationDuration);
                        }
                    };

                    // Let it run until a key is pressed.
                    Console.WriteLine("Listening for fixation data, press any key to exit...");
                    Console.In.Read();
                }
            }
        }
    }
}
*/