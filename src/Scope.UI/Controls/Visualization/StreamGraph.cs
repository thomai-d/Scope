using Scope.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Threading;

namespace Scope.UI.Controls.Visualization
{
    /// <summary>
    /// Visualizes a stream of data.
    /// </summary>
    public class StreamGraph : PixelCanvas
    {
        public static readonly DependencyProperty DataStreamsProperty = DependencyProperty.Register("DataStreams", typeof(ICollection<IBufferedStream<double>>), typeof(StreamGraph), new PropertyMetadata(null));
        public static readonly DependencyProperty LineConfigurationsProperty = DependencyProperty.Register("LineConfigurations", typeof(IList<LineConfiguration>), typeof(StreamGraph), new PropertyMetadata(null));
        public static readonly DependencyProperty GridColorProperty = DependencyProperty.Register("GridColor", typeof(Color), typeof(StreamGraph), new PropertyMetadata(Color.FromRgb(30, 30, 30)));
        public static readonly DependencyProperty MaxProperty = DependencyProperty.Register("Max", typeof(double), typeof(StreamGraph), new PropertyMetadata(5.1));
        public static readonly DependencyProperty MinProperty = DependencyProperty.Register("Min", typeof(double), typeof(StreamGraph), new PropertyMetadata(-0.1));
        public static readonly DependencyPropertyKey AnnotationsPropertyKey;
        public static readonly DependencyProperty AnnotationsProperty;

        // Fields.

        private int verticalCenter;
        private int pos;

        private Tuple<int, double>[] verticalGridLines;

        // Construction.

        static StreamGraph()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(StreamGraph), new FrameworkPropertyMetadata(typeof(StreamGraph)));

            AnnotationsPropertyKey = DependencyProperty.RegisterReadOnly("Annotations", typeof(List<Annotation>), typeof(StreamGraph), new PropertyMetadata(null));
            AnnotationsProperty = AnnotationsPropertyKey.DependencyProperty;
        }

        // Properties.

        public Color GridColor
        {
            get { return (Color)GetValue(GridColorProperty); }
            set { SetValue(GridColorProperty, value); }
        }

        public double Max
        {
            get { return (double)GetValue(MaxProperty); }
            set { SetValue(MaxProperty, value); }
        }

        public double Min
        {
            get { return (double)GetValue(MinProperty); }
            set { SetValue(MinProperty, value); }
        }

        public ICollection<IBufferedStream<double>> DataStreams
        {
            get { return (ICollection<IBufferedStream<double>>)GetValue(DataStreamsProperty); }
            set { SetValue(DataStreamsProperty, value); }
        }

        public IList<LineConfiguration> LineConfigurations
        {
            get { return (IList<LineConfiguration>)GetValue(LineConfigurationsProperty); }
            set { SetValue(LineConfigurationsProperty, value); }
        }

        public List<Annotation> Annotations
        {
            get { return (List<Annotation>)GetValue(AnnotationsProperty); }
            private set { SetValue(AnnotationsPropertyKey, value); }
        }

        // Callbacks.

        protected override void OnRenderSizeChanged()
        {
            base.OnRenderSizeChanged();

            this.pos = 0;
            this.verticalCenter = this.RenderHeight / 2;
            this.RecalculateGrid();
        }

        protected override void OnRender(BitmapContext ctx)
        {
            if (this.RenderWidth == 0)
                return;

            this.Bitmap.Clear(Colors.Black);

            // Draw V-grid
            foreach (var line in this.verticalGridLines)
            {
                this.Bitmap.DrawLine(0, line.Item1, this.RenderWidth, line.Item1, this.GridColor);
            }

            if (this.DataStreams == null 
                || this.DataStreams.Count == 0)
                return;

            int streamIndex = 0;
            foreach (var stream in this.DataStreams)
            {
                var buffer = stream.Last(this.RenderWidth);
                LineConfiguration lineConfig;
                if (!this.TryGetLineConfig(streamIndex, out lineConfig)
                    || !lineConfig.IsVisible)
                {
                    streamIndex++;
                    continue;
                }

                var color = lineConfig.Color;

                // Draw line.
                int p = this.pos;
                int lastY = this.ValueToY(buffer[p]);
                for (int x = buffer.Length - 1; x >= 0; x--)
                {
                    if (++p == buffer.Length)
                        p = 0;

                    int y = this.ValueToY(buffer[p]);
                    this.Bitmap.DrawLine(x + 1, lastY, x, y, color);
                    lastY = y;
                }

                streamIndex++;
            }
        }

        private bool TryGetLineConfig(int streamIndex, out LineConfiguration config)
        {
            if (this.LineConfigurations == null
                || this.LineConfigurations.Count <= streamIndex)
            {
                config = null;
                return false;
            }

            config = this.LineConfigurations[streamIndex];
            return true;
        }

        // Helper functions.

        private int ValueToY(double value)
        {
            var range = this.Max - this.Min;

            var v = (value - this.Min) / range * this.RenderHeight;

            return this.RenderHeight - (int)v;
        }

        private void RecalculateGrid()
        {
            var range = this.Max - this.Min;
            var decimals = Math.Floor(Math.Log10(range));
            var step = Math.Pow(10, decimals);

            var annotations = new List<Annotation>();
            var gridLines = new List<Tuple<int, double>>();
            for (double y = Round(this.Min, step); y < this.Max; y += step)
            {
                var top = this.ValueToY(y);
                gridLines.Add(new Tuple<int, double>(top, y));

                if (y != 0)
                {
                    annotations.Add(new Annotation { X = 3, Y = top, Text = $"{y} V" });
                }
            }

            this.verticalGridLines = gridLines.ToArray();
            this.Annotations = annotations;
        }

        private static double Round(double value, double step)
        {
            return value - (value % step);
        }
    }
} 