using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel;
using LiveChartsCore.Measure;
using SmartX.WPF.ViewModels.History;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SmartX.WPF.Views.Pages.History
{
    /// <summary>
    /// Interaction logic for HistoryPage.xaml
    /// </summary>
    public partial class HistoryPage : Page
    {
        private readonly HistoryViewModel _viewModel;

        private bool _isDraggingChart;

        private Point _chartDragStart;

        private double _chartDragMin;

        private double _chartDragMax;

        public HistoryPage(
            HistoryViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel;

            DataContext = _viewModel;

            Loaded += HistoryPage_Loaded;
        }

        private async void HistoryPage_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            Loaded -= HistoryPage_Loaded;

            await _viewModel.LoadAsync();
        }

        private void ZoomIn_Click(
            object sender,
            RoutedEventArgs e)
        {
            var engine =
                (CartesianChartEngine)
                HistoryChart.CoreChart;

            engine.Zoom(
                ZoomAndPanMode.X,
                new LvcPoint(
                    (float)(HistoryChart.ActualWidth / 2),
                    (float)(HistoryChart.ActualHeight / 2)),
                ZoomDirection.ZoomIn);
        }

        private void ZoomOut_Click(
            object sender,
            RoutedEventArgs e)
        {
            var engine =
                (CartesianChartEngine)
                HistoryChart.CoreChart;

            engine.Zoom(
                ZoomAndPanMode.X,
                new LvcPoint(
                    (float)(HistoryChart.ActualWidth / 2),
                    (float)(HistoryChart.ActualHeight / 2)),
                ZoomDirection.ZoomOut);
        }

        private void ResetZoom_Click(
            object sender,
            RoutedEventArgs e)
        {
            foreach (var axis in HistoryChart.XAxes)
            {
                axis.MinLimit = null;
                axis.MaxLimit = null;
            }
        }

        private void HistoryChart_PreviewMouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            var axis =
                HistoryChart.XAxes?
                    .FirstOrDefault();

            if (axis == null)
                return;

            if (!axis.MinLimit.HasValue ||
                !axis.MaxLimit.HasValue)
            {
                return;
            }

            _chartDragStart =
                e.GetPosition(HistoryChart);

            _chartDragMin =
                axis.MinLimit.Value;

            _chartDragMax =
                axis.MaxLimit.Value;

            _isDraggingChart = true;

            HistoryChart.CaptureMouse();

            e.Handled = true;
        }

        private void HistoryChart_PreviewMouseMove(
            object sender,
            MouseEventArgs e)
        {
            if (!_isDraggingChart)
                return;

            if (e.LeftButton !=
                MouseButtonState.Pressed)
            {
                StopChartDragging();
                return;
            }

            var axis =
                HistoryChart.XAxes?
                    .FirstOrDefault();

            if (axis == null)
                return;

            var current =
                e.GetPosition(HistoryChart);

            var deltaX =
                current.X -
                _chartDragStart.X;

            var width =
                HistoryChart.ActualWidth;

            if (width <= 0)
                return;

            var range =
                _chartDragMax -
                _chartDragMin;

            var valuePerPixel =
                range / width;

            var shift =
                -deltaX *
                valuePerPixel;

            axis.MinLimit =
                _chartDragMin +
                shift;

            axis.MaxLimit =
                _chartDragMax +
                shift;

            e.Handled = true;
        }

        private void HistoryChart_PreviewMouseLeftButtonUp(
            object sender,
            MouseButtonEventArgs e)
        {
            StopChartDragging();

            e.Handled = true;
        }

        private void StopChartDragging()
        {
            if (!_isDraggingChart)
                return;

            _isDraggingChart = false;

            if (HistoryChart.IsMouseCaptured)
            {
                HistoryChart.ReleaseMouseCapture();
            }
        }

        private void HistoryChart_PreviewMouseWheel(
            object sender,
            MouseWheelEventArgs e)
        {
            var axis =
                HistoryChart.XAxes?
                    .FirstOrDefault();

            if (axis == null)
            {
                e.Handled = true;
                return;
            }

            var currentMin =
                axis.MinLimit;

            var currentMax =
                axis.MaxLimit;

            if (!currentMin.HasValue ||
                !currentMax.HasValue)
            {
                var points =
                    _viewModel.History
                        .Where(
                            x => x.Timestamp !=
                                 default)
                        .OrderBy(
                            x => x.Timestamp)
                        .ToArray();

                if (points.Length < 2)
                {
                    e.Handled = true;
                    return;
                }

                currentMin =
                    points.First()
                        .Timestamp
                        .ToLocalTime()
                        .Ticks;

                currentMax =
                    points.Last()
                        .Timestamp
                        .ToLocalTime()
                        .Ticks;

                axis.MinLimit =
                    currentMin;

                axis.MaxLimit =
                    currentMax;
            }

            var range =
                currentMax.Value -
                currentMin.Value;

            if (range <= 0)
            {
                e.Handled = true;
                return;
            }

            var mousePosition =
                e.GetPosition(
                    HistoryChart);

            var width =
                HistoryChart.ActualWidth;

            if (width <= 0)
            {
                e.Handled = true;
                return;
            }

            var mouseRatio =
                Math.Clamp(
                    mousePosition.X / width,
                    0,
                    1);

            var mouseValue =
                currentMin.Value +
                (range * mouseRatio);

            const double zoomFactor = 0.80;

            var newRange =
                e.Delta > 0
                    ? range * zoomFactor
                    : range / zoomFactor;

            var newMin =
                mouseValue -
                (newRange * mouseRatio);

            var newMax =
                mouseValue +
                (newRange *
                 (1 - mouseRatio));

            axis.MinLimit =
                newMin;

            axis.MaxLimit =
                newMax;

            e.Handled = true;
        }
    }
}