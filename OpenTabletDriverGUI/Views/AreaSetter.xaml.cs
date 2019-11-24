using System;
using Avalonia;
using Avalonia.Input;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

namespace OpenTabletDriverGUI.Views
{
    public class AreaSetter : UserControl
    {
        public AreaSetter()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            var ctrl = this.Find<Control>("AreaGrid");
            ctrl.PointerPressed += AreaPointerPressed;
            ctrl.PointerReleased += AreaPointerReleased;
            ctrl.PointerMoved += AreaPointerMoved;
            ctrl.PointerLeave += AreaLeave;
        }

        private bool IsDragging { set; get; }
        private Nullable<Point> LastPosition { set; get; }

        private void AreaPointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (e.MouseButton == MouseButton.Left)
                IsDragging = true;
        }

        private void AreaPointerReleased(object sender, PointerReleasedEventArgs e)
        {
            if (e.MouseButton == MouseButton.Left)
                IsDragging = false;
        }

        private void AreaLeave(object sender, PointerEventArgs e)
        {
            if (IsDragging)
                IsDragging = false;
        }

        private void AreaPointerMoved(object sender, PointerEventArgs e)
        {
            if (IsDragging)
            {
                var currentPos = e.GetPosition(this);
                if (LastPosition is Point lastPos)
                {
                    var dX = lastPos.X - currentPos.X;
                    var dY = lastPos.Y - currentPos.Y;
                    var scale = GetScale();
                    AreaXOffset -= (float)(dX * scale.Item1);
                    AreaYOffset -= (float)(dY * scale.Item2);
                }
                LastPosition = currentPos;
            }
            else if (LastPosition != null)
                LastPosition = null;
        }

        private (double, double) GetScale()
        {
            var obj = this.Find<Control>("AreaViewbox");
            if (obj.Bounds is Rect bounds)
            {
                var scaleX = BackgroundWidth / bounds.Width;
                var scaleY = BackgroundHeight / bounds.Height;
                return (scaleX, scaleY);
            }
            else
            {
                return (0, 0);
            }
        }

        private void CenterArea()
        {
            AreaXOffset = (BackgroundWidth - AreaWidth) / 2;
            AreaYOffset = (BackgroundHeight - AreaHeight) / 2;
        }

        private void AlignVertical(bool isTop)
        {
            AreaYOffset = isTop ? 0 : BackgroundHeight - AreaHeight;
        }

        private void AlignHorizontal(bool isLeft)
        {
            AreaXOffset = isLeft ? 0 : BackgroundWidth - AreaWidth;
        }

        private void ResetArea()
        {
            AreaWidth = BackgroundWidth;
            AreaHeight = BackgroundHeight;
            AreaXOffset = 0;
            AreaYOffset = 0;
        }

        private void ResizeArea(float percent)
        {
            AreaWidth = BackgroundWidth * percent;
            AreaHeight = BackgroundHeight * percent;
        }

        public static readonly StyledProperty<string> TitleProperty =
            AvaloniaProperty.Register<AreaSetter, string>(nameof(Title), defaultBindingMode: BindingMode.TwoWay);

        public string Title
        {
            set => SetValue(TitleProperty, value);
            get => GetValue(TitleProperty);
        }

        public static readonly StyledProperty<string> UnitProperty = 
            AvaloniaProperty.Register<AreaSetter, string>(nameof(MeasurementUnit), defaultBindingMode: BindingMode.TwoWay);

        public string MeasurementUnit
        {
            set => SetValue(UnitProperty, value);
            get => GetValue(UnitProperty);
        }

        public static readonly StyledProperty<float> AreaWidthProperty =
            AvaloniaProperty.Register<AreaSetter, float>(nameof(AreaWidth), defaultBindingMode: BindingMode.TwoWay);
        
        public float AreaWidth
        {
            set => SetValue(AreaWidthProperty, value);
            get => GetValue(AreaWidthProperty);
        }

        public static readonly StyledProperty<float> AreaHeightProperty =
            AvaloniaProperty.Register<AreaSetter, float>(nameof(AreaHeight), defaultBindingMode: BindingMode.TwoWay);

        public float AreaHeight
        {
            set => SetValue(AreaHeightProperty, value);
            get => GetValue(AreaHeightProperty);
        }

        public static readonly StyledProperty<float> AreaXOffsetProperty = 
            AvaloniaProperty.Register<AreaSetter, float>(nameof(AreaXOffset), defaultBindingMode: BindingMode.TwoWay);

        public float AreaXOffset
        {
            set => SetValue(AreaXOffsetProperty, value);
            get => GetValue(AreaXOffsetProperty);
        }

        public static readonly StyledProperty<float> AreaYOffsetProperty =
            AvaloniaProperty.Register<AreaSetter, float>(nameof(AreaYOffset), defaultBindingMode: BindingMode.TwoWay);

        public float AreaYOffset
        {
            set => SetValue(AreaYOffsetProperty, value);
            get => GetValue(AreaYOffsetProperty);
        }

        public static readonly StyledProperty<float> BackgroundWidthProperty = 
            AvaloniaProperty.Register<AreaSetter, float>(nameof(BackgroundWidth), defaultBindingMode: BindingMode.TwoWay);

        public float BackgroundWidth
        {
            set => SetValue(BackgroundWidthProperty, value);
            get => GetValue(BackgroundWidthProperty);
        }

        public static readonly StyledProperty<float> BackgroundHeightProperty = 
        AvaloniaProperty.Register<AreaSetter, float>(nameof(BackgroundHeight), defaultBindingMode: BindingMode.TwoWay);
        
        public float BackgroundHeight
        {
            set => SetValue(BackgroundHeightProperty, value);
            get => GetValue(BackgroundHeightProperty);
        }
    }
}