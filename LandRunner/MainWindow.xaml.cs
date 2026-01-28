using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using GlassBridge;
using LandRunner.ViewModels;

namespace LandRunner
{
    public partial class MainWindow : Window
    {
        private MainWindowViewModel? _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            
            _viewModel = new MainWindowViewModel();
            DataContext = _viewModel;
        }

        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_viewModel != null)
            {
                await _viewModel.DisconnectAsync();
            }
        }

        // Visualization is drawn based on ViewModel updates
        // This is called from MainWindow.xaml binding to ViewModel properties
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            
            // Draw axis visualization when data is available
            if (_viewModel != null && !string.IsNullOrEmpty(_viewModel.YawText))
            {
                DrawAxisVisualization(_viewModel.GetLastEulerAngles());
            }
        }

        private void DrawAxisVisualization(EulerAngles euler)
        {
            AxisCanvas.Children.Clear();

            double centerX = AxisCanvas.ActualWidth / 2;
            double centerY = AxisCanvas.ActualHeight / 2;
            double axisLength = 60;

            DrawAxis(centerX, centerY, axisLength, 0, 0, Colors.Red, "X");
            DrawAxis(centerX, centerY, axisLength, 90, 0, Colors.LimeGreen, "Y");
            DrawAxis(centerX, centerY, axisLength, 0, 90, Colors.DeepSkyBlue, "Z");

            var yawRad = euler.Yaw * Math.PI / 180.0;
            double rotatedX = axisLength * Math.Cos(yawRad);
            double rotatedY = axisLength * Math.Sin(yawRad);

            var line = new Line
            {
                X1 = centerX,
                Y1 = centerY,
                X2 = centerX + rotatedX,
                Y2 = centerY + rotatedY,
                Stroke = new SolidColorBrush(Colors.Purple),
                StrokeThickness = 3
            };
            AxisCanvas.Children.Add(line);

            var origin = new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = new SolidColorBrush(Colors.Black)
            };
            Canvas.SetLeft(origin, centerX - 4);
            Canvas.SetTop(origin, centerY - 4);
            AxisCanvas.Children.Add(origin);
        }

        private void DrawAxis(double centerX, double centerY, double length, double angleX, double angleY, Color color, string label)
        {
            double x = length * Math.Cos(angleY * Math.PI / 180.0);
            double y = length * Math.Sin(angleX * Math.PI / 180.0);

            var line = new Line
            {
                X1 = centerX,
                Y1 = centerY,
                X2 = centerX + x,
                Y2 = centerY + y,
                Stroke = new SolidColorBrush(color),
                StrokeThickness = 2
            };
            AxisCanvas.Children.Add(line);

            var textBlock = new TextBlock
            {
                Text = label,
                Foreground = new SolidColorBrush(color),
                FontSize = 12,
                FontWeight = System.Windows.FontWeights.Bold
            };
            Canvas.SetLeft(textBlock, centerX + x + 5);
            Canvas.SetTop(textBlock, centerY + y - 8);
            AxisCanvas.Children.Add(textBlock);
        }
    }
}
