﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml.Serialization;

using EyeXFramework; // Tobii
using Tobii.EyeX.Framework; // Tobii

using EyeTrackingCore; // From EyeTrackingCore project reference

namespace EyeFixationDrawer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Access to the Tobii eyeX
        private EyeXHost _eyeXHost;
        private GazePointDataStream stream;

        // GazePoints, Fixations & Saccades
        private List<GazePoint> gazePoints = new List<GazePoint>();
        // TODO: Fixations
        // TODO: Saccades

        // UI Representations of the fixations and saccades.
        private List<Ellipse> fixationCircles = new List<Ellipse>();
        private List<Line> saccades = new List<Line>();

        // Loading/Saving Data
        private XmlSerializer serialiser;

        // Fixation algorithm arguments
        private int currentWindowSize = 25;
        private double peakThreshold = 25;
        private double radius = 25;

        // Determines the maximum settable by the UI
        private int maxWindowSize = 100;
        private double maxPeakThreshold = 1000;
        private double maxRadius = 1000;

        // Initialisation
        // ##############

        public MainWindow()
        {
            InitializeComponent();
            serialiser = new XmlSerializer(gazePoints.GetType());

            InitSliders();
        }

        private void InitEyeTracker()
        {
            // Initialize the EyeX Host 
            _eyeXHost = new EyeXHost();
            _eyeXHost.Start();

            // Create a data stream object and listen to events. 
            stream = _eyeXHost.CreateGazePointDataStream(GazePointDataMode.Unfiltered);
            stream.Next += DrawCircleAtGazePoint;
            stream.Next += StoreGazePoint;
        }

        private void InitSliders()
        {
            windowSizeSlider.Value = (float)currentWindowSize / (float)maxWindowSize;
            peakThresholdSlider.Value = peakThreshold / maxPeakThreshold;
            radiusSlider.Value = radius / maxRadius;
        }

        private void StoreGazePoint(object sender, GazePointEventArgs args)
        {
            GazePoint gazePoint = new GazePoint((float)args.X, (float)args.Y, 0);
            gazePoints.Add(gazePoint);
        }

        // Drawing / Updating UI
        // #####################

        // Gaze Points
        private void DrawCircleAtGazePoint(object sender, GazePointEventArgs args)
        {
            DrawCircle(args.X, args.Y, System.Windows.Media.Brushes.Black, 5);
        }

        // Fixations
        private void DrawFixations()
        {
            RawToFixationConverter converter = new RawToFixationConverter(gazePoints);
            List<Fixation> fixations = converter.CalculateFixations(currentWindowSize, (float)peakThreshold, (float)radius);

            foreach (Fixation fixation in fixations)
            {
                DrawCircle(fixation.x, fixation.y, System.Windows.Media.Brushes.Red, 20);
            }
        }

        private void DrawCircle(double screenX, double screenY, SolidColorBrush brush, double size)
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

                if (brush == System.Windows.Media.Brushes.Red)
                {
                    fixationCircles.Add(ellipse);
                }
                canvas.Children.Add(ellipse);
            }));
        }

        private void RemoveFixationCircles()
        {
            foreach (Ellipse ellipse in fixationCircles)
            {
                canvas.Children.Remove(ellipse);
            }
        }

        private void UpdateFixationCircles()
        {
            RemoveFixationCircles();
            RemoveSaccades();
            DrawFixation_Click(this, null);
        }

        // Saccades
        private void DrawSaccades()
        {
            for (int i = 1; i < fixationCircles.Count; i++)
            {
                Ellipse from = fixationCircles[i - 1];
                Ellipse to = fixationCircles[i];

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

        // Helper method that converts a point in screen space to canvas space.
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

        // Events
        // ######

        // Start collecting gaze data button
        private void Start_Click(object sender, RoutedEventArgs e)
        {
            InitEyeTracker();
        }

        // Stop collecting gaze data button
        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            stream.Next -= DrawCircleAtGazePoint;
            stream.Next -= StoreGazePoint;
        }

        // Draw fixation button
        private void DrawFixation_Click(object sender, RoutedEventArgs e)
        {
            DrawFixations();
            DrawSaccades();
        }

        // Clear fixation button
        private void ClearFixation_Click(object sender, RoutedEventArgs e)
        {
            RemoveFixationCircles();
            RemoveSaccades();
        }

        // Window size slider changed
        private void WindowSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            currentWindowSize = (int)(e.NewValue * maxWindowSize);
            windowSizeLabel.Content = "Window size: " + currentWindowSize;

            UpdateFixationCircles();
        }

        // Peak threshold slider changed
        private void PeakThreshold_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            peakThreshold = e.NewValue * maxPeakThreshold;
            peakThresholdLabel.Content = "Peak Threshold: " + peakThreshold;

            UpdateFixationCircles();
        }

        // Radius slider changed
        private void Radius_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            radius = e.NewValue * maxRadius;
            radiusLabel.Content = "Radius: " + radius;

            UpdateFixationCircles();
        }

        // Saving / Loading
        // ################

        // TODO: Add loading functionality.

        // SaveData button
        // Currently just saves the raw gaze points out to an xml file in the same directory as the executable.
        private void SaveData_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Saving Data...");
            try
            {
                System.IO.FileStream stream = new System.IO.FileStream("./gazePoints.xml", System.IO.FileMode.OpenOrCreate);
                this.serialiser.Serialize(stream, gazePoints);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }
            Console.WriteLine("Save complete!");
        }
    }
}
