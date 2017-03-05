using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using EyeXFramework;
using Tobii.EyeX.Framework;

namespace EyeXData
{

    enum DataType {
        Fixations
    }

    class EyeXDataCollector
    {
        private DataType dataType;
        private double instanceLength;
        private String instanceClass;

        private List<Instance> collectedInstances;
        private Thread collectionThread;
        private bool shouldEndCollection = false;

        // Eye tracking data sources
        EyeXHost eyeXHost;
        FixationDataStream fixationDataStream;

        public EyeXDataCollector(DataType dataType, double instanceLength, String instanceClass)
        {
            this.dataType = dataType;
            this.instanceLength = instanceLength;
            this.instanceClass = instanceClass;

            collectedInstances = new List<Instance>();

            eyeXHost = new EyeXHost();
            eyeXHost.Start();
        }

        public void StartCollection()
        {
            collectionThread = new Thread(CollectData);
            collectionThread.Start();
        }

        public void EndCollection()
        {
            shouldEndCollection = true;
        }

        private void CollectData()
        {
            double previousTime = GetUnixTimestampForNow();
            double elapsedTime = 0;

            double startTime = 0;
            double endTime = 0;

            int numFixations = 0;

            double lastFixationStartTime = 0;
            double fixationLengthRunningTotal = 0;

            // Increment the number of fixations each time we receive data from the eye tracker.
            fixationDataStream = eyeXHost.CreateFixationDataStream(FixationDataMode.Sensitive);
            System.EventHandler<FixationEventArgs> inc = delegate (object s, FixationEventArgs e) {
                if (e.EventType == FixationDataEventType.Begin)
                {
                    numFixations++;
                    lastFixationStartTime = e.Timestamp;
                }
                if (e.EventType == FixationDataEventType.End)
                {
                    fixationLengthRunningTotal += e.Timestamp - lastFixationStartTime;
                }
            };

            fixationDataStream.Next += inc;

            // Keep collecting the data until we tell it not to.
            while (!shouldEndCollection)
            {
                if(elapsedTime < instanceLength)
                {
                    elapsedTime += GetUnixTimestampForNow() - previousTime;
                    previousTime = GetUnixTimestampForNow();
                }
                else
                {
                    endTime = GetUnixTimestampForNow();
                    double fixationsPerSecond = numFixations / (instanceLength / 1000);
                    double meanLengthOfFixation = fixationLengthRunningTotal / numFixations;

                    Instance instance = new Instance(startTime, endTime, numFixations, fixationsPerSecond, meanLengthOfFixation, instanceClass);
                    collectedInstances.Add(instance);

                    startTime = GetUnixTimestampForNow();
                    numFixations = 0;
                    fixationLengthRunningTotal = 0;

                    elapsedTime = 0;
                    previousTime = GetUnixTimestampForNow();
                }
            }          
        }

        public string GenerateCollectedDataCSV()
        {
            StringBuilder builder = new StringBuilder();
            foreach (var instance in collectedInstances)
            {
                builder.AppendLine(instance.ToString());
            }

            return builder.ToString();
        }

        private double GetUnixTimestampForNow()
        {
            return (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
        }
    }
    
    class Instance {
        private double startTime = 0;
        private double endTime = 0;

        private int numFixations;

        private double fixationsPerSecond;
        private double meanLengthOfFixation;

        private String instanceClass;

        public Instance(double startTime, double endTime, int numFixations, double fixationsPerSecond, double meanLengthOfFixation, String instanceClass)
        {
            this.startTime = startTime;
            this.endTime = endTime;
            this.numFixations = numFixations;
            this.fixationsPerSecond = fixationsPerSecond;
            this.meanLengthOfFixation = meanLengthOfFixation;
            this.instanceClass = instanceClass;
        }

        public double GetLengthInSeconds()
        {
            return (endTime - startTime) / 1000;
        }

        public override string ToString()
        {
            return $"{startTime}, {endTime}, {fixationsPerSecond}, {meanLengthOfFixation}, {instanceClass}";
        }
    }
}
