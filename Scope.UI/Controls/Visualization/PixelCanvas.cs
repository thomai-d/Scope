using Scope.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace Scope.Controls.Visualization
{
    public class PixelCanvas : Control
    {
        public static readonly DependencyProperty ImageSourceProperty;
        public static readonly DependencyPropertyKey ImageSourcePropertyKey;

        protected WriteableBitmap Bitmap { get; private set; }

        protected int RenderHeight;
        protected int RenderWidth;

        static PixelCanvas()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PixelCanvas), new FrameworkPropertyMetadata(typeof(PixelCanvas)));

            ImageSourcePropertyKey = DependencyProperty.RegisterReadOnly("ImageSource", typeof(BitmapSource), typeof(PixelCanvas), new PropertyMetadata());
            ImageSourceProperty = ImageSourcePropertyKey.DependencyProperty;
        }

        public BitmapSource ImageSource
        {
            get { return (BitmapSource)GetValue(ImageSourceProperty); }
            private set { SetValue(ImageSourcePropertyKey, value); }
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            this.RenderWidth = (int)arrangeBounds.Width;
            this.RenderHeight = (int)arrangeBounds.Height;

            this.Bitmap = BitmapFactory.New(this.RenderWidth, this.RenderHeight);
            this.ImageSource = this.Bitmap;

            this.OnRenderSizeChanged();
            this.Render();

            return base.ArrangeOverride(arrangeBounds);
        }

        public void Render()
        {
            using (var ctx = this.Bitmap.GetBitmapContext(ReadWriteMode.ReadWrite))
            {
                using (Performance.Trace("Rendering"))
                {
                    this.OnRender(ctx);
                }
            }
        }

        protected virtual void OnRender(BitmapContext ctx)
        {
        }

        protected virtual void OnRenderSizeChanged()
        {
        }
    }
}
