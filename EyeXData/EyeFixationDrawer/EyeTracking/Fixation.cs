namespace EyeTracking {

    public class Fixation {

        public float x;
        public float y;

        private double length; // length of the fixation in milliseconds

        public Fixation(float x, float y, double length) {
            this.x = x;
            this.y = y;
            this.length = length;
        }
    }
}