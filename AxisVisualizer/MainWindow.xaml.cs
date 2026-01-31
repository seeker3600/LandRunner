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
            _viewModel.EulerAnglesUpdated += OnEulerAnglesUpdated;
            DataContext = _viewModel;
        }

        private void OnEulerAnglesUpdated(EulerAngles euler)
        {
            // Ensure we're on the UI thread for drawing
            Dispatcher.Invoke(() => DrawAxisVisualization(euler));
        }

        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.EulerAnglesUpdated -= OnEulerAnglesUpdated;
                await _viewModel.DisconnectAsync();
            }
        }

        private void DrawAxisVisualization(EulerAngles euler)
        {
            AxisCanvas.Children.Clear();

            double centerX = AxisCanvas.ActualWidth / 2;
            double centerY = AxisCanvas.ActualHeight / 2;
            double axisLength = 80;

            // Convert euler angles to radians
            double rollRad = euler.Roll * Math.PI / 180.0;
            double pitchRad = euler.Pitch * Math.PI / 180.0;
            double yawRad = euler.Yaw * Math.PI / 180.0;

            // Draw reference axes (fixed, semi-transparent)
            DrawReferenceAxis(centerX, centerY, axisLength, 0, Colors.Red, "X?");
            DrawReferenceAxis(centerX, centerY, axisLength, 90, Colors.LimeGreen, "Y?");

            // Apply 3D rotation using rotation matrix (Yaw -> Pitch -> Roll order)
            // X axis after rotation
            var (xEndX, xEndY) = RotatePoint3DTo2D(axisLength, 0, 0, rollRad, pitchRad, yawRad);
            DrawRotatedAxis(centerX, centerY, xEndX, xEndY, Colors.Red, "X");

            // Y axis after rotation
            var (yEndX, yEndY) = RotatePoint3DTo2D(0, axisLength, 0, rollRad, pitchRad, yawRad);
            DrawRotatedAxis(centerX, centerY, yEndX, yEndY, Colors.LimeGreen, "Y");

            // Z axis after rotation (pointing out of screen initially)
            var (zEndX, zEndY) = RotatePoint3DTo2D(0, 0, axisLength, rollRad, pitchRad, yawRad);
            DrawRotatedAxis(centerX, centerY, zEndX, zEndY, Colors.DeepSkyBlue, "Z");

            // Draw origin point
            var origin = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = new SolidColorBrush(Colors.Black)
            };
            Canvas.SetLeft(origin, centerX - 5);
            Canvas.SetTop(origin, centerY - 5);
            AxisCanvas.Children.Add(origin);
        }

        /// <summary>
        /// Rotate a 3D point and project to 2D canvas
        /// </summary>
        private (double x, double y) RotatePoint3DTo2D(double x, double y, double z, double roll, double pitch, double yaw)
        {
            // Rotation around Z axis (Yaw)
            double x1 = x * Math.Cos(yaw) - y * Math.Sin(yaw);
            double y1 = x * Math.Sin(yaw) + y * Math.Cos(yaw);
            double z1 = z;

            // Rotation around Y axis (Pitch)
            double x2 = x1 * Math.Cos(pitch) + z1 * Math.Sin(pitch);
            double y2 = y1;
            double z2 = -x1 * Math.Sin(pitch) + z1 * Math.Cos(pitch);

            // Rotation around X axis (Roll)
            double x3 = x2;
            double y3 = y2 * Math.Cos(roll) - z2 * Math.Sin(roll);
            // double z3 = y2 * Math.Sin(roll) + z2 * Math.Cos(roll); // Not needed for 2D projection

            // Project to 2D (orthographic projection: X -> right, Y -> down)
            return (x3, -y3);  // Flip Y: WPF Y-axis is down, math Y-axis is up
        }

        private void DrawReferenceAxis(double centerX, double centerY, double length, double angleDeg, Color color, string label)
        {
            double angleRad = angleDeg * Math.PI / 180.0;
            double x = length * Math.Cos(angleRad);
            double y = length * Math.Sin(angleRad);

            var line = new Line
            {
                X1 = centerX,
                Y1 = centerY,
                X2 = centerX + x,
                Y2 = centerY + y,
                Stroke = new SolidColorBrush(color) { Opacity = 0.2 },
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 4, 2 }
            };
            AxisCanvas.Children.Add(line);

            var textBlock = new TextBlock
            {
                Text = label,
                Foreground = new SolidColorBrush(color) { Opacity = 0.4 },
                FontSize = 10
            };
            Canvas.SetLeft(textBlock, centerX + x + 5);
            Canvas.SetTop(textBlock, centerY + y - 6);
            AxisCanvas.Children.Add(textBlock);
        }

        private void DrawRotatedAxis(double centerX, double centerY, double endX, double endY, Color color, string label)
        {
            var line = new Line
            {
                X1 = centerX,
                Y1 = centerY,
                X2 = centerX + endX,
                Y2 = centerY + endY,
                Stroke = new SolidColorBrush(color),
                StrokeThickness = 3
            };
            AxisCanvas.Children.Add(line);

            // Draw arrowhead
            double angle = Math.Atan2(endY, endX);
            double arrowSize = 10;
            var arrow1 = new Line
            {
                X1 = centerX + endX,
                Y1 = centerY + endY,
                X2 = centerX + endX - arrowSize * Math.Cos(angle - 0.4),
                Y2 = centerY + endY - arrowSize * Math.Sin(angle - 0.4),
                Stroke = new SolidColorBrush(color),
                StrokeThickness = 2
            };
            var arrow2 = new Line
            {
                X1 = centerX + endX,
                Y1 = centerY + endY,
                X2 = centerX + endX - arrowSize * Math.Cos(angle + 0.4),
                Y2 = centerY + endY - arrowSize * Math.Sin(angle + 0.4),
                Stroke = new SolidColorBrush(color),
                StrokeThickness = 2
            };
            AxisCanvas.Children.Add(arrow1);
            AxisCanvas.Children.Add(arrow2);

            var textBlock = new TextBlock
            {
                Text = label,
                Foreground = new SolidColorBrush(color),
                FontSize = 14,
                FontWeight = System.Windows.FontWeights.Bold
            };
            Canvas.SetLeft(textBlock, centerX + endX + 8);
            Canvas.SetTop(textBlock, centerY + endY - 10);
            AxisCanvas.Children.Add(textBlock);
        }
    }
}
