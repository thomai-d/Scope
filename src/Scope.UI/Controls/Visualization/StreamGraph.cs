using Scope.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        public static readonly DependencyProperty LineConfigurationsProperty = DependencyProperty.Register("LineConfigurations", typeof(IList<ChannelConfiguration>), typeof(StreamGraph), new PropertyMetadata(null, LineConfigurationsChanged));

        public static readonly DependencyProperty GridColorProperty = DependencyProperty.Register("GridColor", typeof(Color), typeof(StreamGraph), new PropertyMetadata(Color.FromRgb(30, 30, 30)));
        public static readonly DependencyPropertyKey AnnotationsPropertyKey;
        public static readonly DependencyProperty AnnotationsProperty;

        // Fields.

        private int verticalCenter;
        private int pos;
        private double min = 0, max = 0;

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

        public ICollection<IBufferedStream<double>> DataStreams
        {
            get { return (ICollection<IBufferedStream<double>>)GetValue(DataStreamsProperty); }
            set { SetValue(DataStreamsProperty, value); }
        }

        public IList<ChannelConfiguration> LineConfigurations
        {
            get { return (IList<ChannelConfiguration>)GetValue(LineConfigurationsProperty); }
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
            if (this.RenderWidth == 0 || this.verticalGridLines == null)
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

                ChannelConfiguration lineConfig;
                if (!this.TryGetLineConfig(streamIndex, out lineConfig)
                    || !lineConfig.IsVisible)
                {
                    // No configuration for this line => ignore.
                    streamIndex++;
                    continue;
                }

                var color = lineConfig.Color;

                // Draw line.
                int p = this.pos;
                var lastRealValue = lineConfig.MinValue + buffer[p] * (lineConfig.MaxValue - lineConfig.MinValue);
                int lastY = this.ValueToY(lastRealValue);
                for (int x = buffer.Length - 1; x >= 0; x--)
                {
                    if (++p == buffer.Length)
                        p = 0;

                    var currentRealValue = lineConfig.MinValue + buffer[p] * (lineConfig.MaxValue - lineConfig.MinValue);
                    int y = this.ValueToY(currentRealValue);
                    this.Bitmap.DrawLine(x + 1, lastY, x, y, color);
                    lastY = y;
                }

                streamIndex++;
            }
        }

        private static void LineConfigurationsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var me = (StreamGraph)d;
            me.RecalculateGrid();
        }

        // Helper functions.

        private int ValueToY(double value)
        {
            var range = this.max - this.min;

            var v = (value - this.min) / range * this.RenderHeight;

            return this.RenderHeight - (int)v;
        }

        public void RecalculateGrid()
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            if (this.LineConfigurations == null
                || !this.LineConfigurations.Any())
            {
                this.min = 0;
                this.max = 0;
                return;
            }

            // Auto range.
            this.min = this.LineConfigurations.Min(c => c.MinValue);
            this.max = this.LineConfigurations.Max(c => c.MaxValue);
            var range = this.max - this.min;
            this.min -= range / 20;
            this.max += range / 20;

            var decimals = Math.Floor(Math.Log10(range * 0.9));
            var step = Math.Pow(10, decimals);

            var annotations = new List<Annotation>();
            var gridLines = new List<Tuple<int, double>>();
            for (double y = Round(this.min, step); y < this.max; y += step)
            {
                var top = this.ValueToY(y);
                gridLines.Add(new Tuple<int, double>(top, y));

                annotations.Add(new Annotation { X = 3, Y = top - 8, Text = $"{y} V" });
            }

            this.verticalGridLines = gridLines.ToArray();
            this.Annotations = annotations;

            this.Render();
        }

        private static double Round(double value, double step)
        {
            return value - (value % step);
        }

        private bool TryGetLineConfig(int streamIndex, out ChannelConfiguration config)
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
    }
} 