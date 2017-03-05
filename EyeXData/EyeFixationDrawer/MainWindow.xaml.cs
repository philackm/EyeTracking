using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Serialization;

using EyeXFramework;
using Tobii.EyeX.Framework;
using EyeTracking;

namespace EyeFixationDrawer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private EyeXHost _eyeXHost;
        private GazePointDataStream stream;

        private List<GazePoint> gazePoints = new List<GazePoint>();
        private XmlSerializer serialiser;

        private List<Ellipse> fixationSpheres = new List<Ellipse>();
        private List<Line> saccades = new List<Line>();

        public MainWindow()
        {
            InitializeComponent();
            serialiser = new XmlSerializer(gazePoints.GetType());
            //InitEyeTracker();

            InitSliders();

            button.Click += delegate (object s, RoutedEventArgs e) { InitEyeTracker(); };
        }

        private void InitEyeTracker()
        {
            // Initialize the EyeX Host 
            _eyeXHost = new EyeXHost();
            _eyeXHost.Start();

            // Create a data stream object and listen to events. 
            stream = _eyeXHost.CreateGazePointDataStream(GazePointDataMode.Unfiltered);
            stream.Next += DrawSphereAtGazePoint;
            stream.Next += StoreGazePoint;
        }

        private void StoreGazePoint(object sender, GazePointEventArgs args)
        {
            GazePoint gazePoint = new EyeTracking.GazePoint((float)args.X, (float)args.Y, 0);
            gazePoints.Add(gazePoint);
        }

        private void DrawSphereAtGazePoint(object sender, GazePointEventArgs args)
        {
            DrawSphere(args.X, args.Y, System.Windows.Media.Brushes.Black, 5);
        }

        private void DrawSphere(double screenX, double screenY, SolidColorBrush brush, double size)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                var canvasXY = ScreenToCanvas(new System.Windows.Point(screenX, screenY));

                Ellipse ellipse = new System.Windows.Shapes.Ellipse();
                ellipse.Width = size;
                ellipse.Height = size;
                ellipse.Fill = brush;
                Canvas.SetLeft(ellipse, canvasXY.X);
                Canvas.SetTop(ellipse, canvasXY.Y);

                if(brush == System.Windows.Media.Brushes.Red)
                {
                    fixationSpheres.Add(ellipse);
                }
                canvas.Children.Add(ellipse);
            }));
        }

        private void RemoveFixationSpheres()
        {
            foreach (Ellipse ellipse in fixationSpheres)
            {
                canvas.Children.Remove(ellipse);
            }
        }
        private void DrawSaccades()
        {
            for (int i = 1; i < fixationSpheres.Count; i++)
            {
                Ellipse from = fixationSpheres[i - 1];
                Ellipse to = fixationSpheres[i];

                double startX = Canvas.GetLeft(from);
                double startY = Canvas.GetTop(from);

                double endX = Canvas.GetLeft(to);
                double endY = Canvas.GetTop(to);

                System.Windows.Shapes.Line line = new System.Windows.Shapes.Line();
                line.X1 = startX;
                line.Y1 = startY;

                line.X2 = endX;
                line.Y2 = endY;

                line.StrokeThickness = 2;
                line.Stroke = System.Windows.Media.Brushes.Red;

                saccades.Add(line);
                canvas.Children.Add(line);
            }
        }

        private void RemoveSaccades()
        {
            foreach (Line line in saccades)
            {
                canvas.Children.Remove(line);
            }
        }

        private System.Windows.Point ScreenToCanvas(System.Windows.Point screenPosition)
        {
            double windowX = 0;
            double windowY = 0;

            this.Dispatcher.Invoke((Action)(() =>
            {
                windowX = this.Left;
                windowY = this.Top;
            }));

            double X = screenPosition.X - windowX;
            double Y = screenPosition.Y - windowY;

            return new System.Windows.Point(X, Y);
        }

        // SaveData button
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Saving Data");
            try
            {
                System.IO.FileStream stream = new System.IO.FileStream("C:/Users/philm/Desktop/gazePoints.xml", System.IO.FileMode.OpenOrCreate);
                this.serialiser.Serialize(stream, gazePoints);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }
        }

        // StopCollectingButton
        private void button2_Click(object sender, RoutedEventArgs e)
        {
            stream.Next -= DrawSphereAtGazePoint;
            stream.Next -= StoreGazePoint;
        }

        // Draw fixation button
        private void button3_Click(object sender, RoutedEventArgs e)
        {
            RawToFixationConverter converter = new RawToFixationConverter(gazePoints);
            List<Fixation> fixations = converter.CalculateFixations(currentWindowSize, (float)peakThreshold, (float)radius);

            foreach(Fixation fixation in fixations)
            {
                DrawSphere(fixation.x, fixation.y, System.Windows.Media.Brushes.Red, 20);
            }

            DrawSaccades();
        }

        // Clear fixation button
        private void button4_Click(object sender, RoutedEventArgs e)
        {
            RemoveFixationSpheres();
            RemoveSaccades();
        }

        private int maxWindowSize = 100;
        private double maxPeakThreshold = 1000;
        private double maxRadius = 1000;

        private int currentWindowSize = 25;
        private double peakThreshold = 25;
        private double radius = 25;

        private void InitSliders()
        {
            slider1.Value = (float)currentWindowSize / (float)maxWindowSize;
            slider2.Value = peakThreshold / maxPeakThreshold;
            slider3.Value = radius / maxRadius;

            Console.WriteLine(slider1.Value);
        }

        // Window size changed
        private void slider1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            currentWindowSize = (int)(e.NewValue * maxWindowSize);
            label.Content = "Window size: " + currentWindowSize;
            Console.WriteLine(currentWindowSize);

            UpdateFixationSpheres();
        }

        // Peak threshold changed
        private void slider2_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            peakThreshold = e.NewValue * maxPeakThreshold;
            label1.Content = "Peak Threshold: " + peakThreshold;

            UpdateFixationSpheres();
        }

        // Radius changed
        private void slider3_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            radius = e.NewValue * maxRadius;
            label2.Content = "Radius: " + radius;

            UpdateFixationSpheres();
        }

        private void UpdateFixationSpheres()
        {
            RemoveFixationSpheres();
            RemoveSaccades();
            button3_Click(this, null);
        }


    }
}
