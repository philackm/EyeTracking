using System;

namespace EyeTrackingCore {

    public class Fixation {

        public float x;
        public float y;

        public int startTime;  // The start time of the fixation in milliseconds
        public int endTime;    // The end time of the fixation in milliseconds

        public Fixation(float x, float y, int startTime, int endTime) {
            this.x = x;
            this.y = y;
            this.startTime = startTime;
            this.endTime = endTime;
        }
    }
}