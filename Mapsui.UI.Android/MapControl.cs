using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Util;
using Android.Views;
using Java.Lang;
using Mapsui.Fetcher;
using Mapsui.Layers;
using Mapsui.Logging;
using Mapsui.Widgets;
using SkiaSharp;
using SkiaSharp.Views.Android;
using Math = System.Math;

namespace Mapsui.UI.Android
{
    public partial class MapControl : ViewGroup, IMapControl
    {
        private SKGLSurfaceView _canvas;
        public Map _map;
        private double _innerRotation;
        private GestureDetector _gestureDetector;
        private Handler _mainLooperHandler;

        public event EventHandler ViewportInitialized;

        public MapControl(Context context, IAttributeSet attrs) :
            base(context, attrs)
        {
            Initialize();
        }

        public MapControl(Context context, IAttributeSet attrs, int defStyle) :
            base(context, attrs, defStyle)
        {
            Initialize();
        }

        public void Initialize()
        {
            SetBackgroundColor(Color.Transparent);
            _scale = DetermineSkiaScale();
            _canvas = new SKGLSurfaceView(Context); //new SKCanvasView(Context);
            //_canvas.PaintSurface += CanvasOnPaintSurface;
            Map = new Map();
            _canvas.SetRenderer(new R(this, _scale));
            AddView(_canvas);

            _mainLooperHandler = new Handler(Looper.MainLooper);

            //Map = new Map();
            TryInitializeViewport();
            Touch += MapView_Touch;

            _gestureDetector = new GestureDetector(Context, new GestureDetector.SimpleOnGestureListener());
            _gestureDetector.SingleTapConfirmed += OnSingleTapped;
            _gestureDetector.DoubleTap += OnDoubleTapped;
        }

        private float DetermineSkiaScale()
        {
            return Resources.DisplayMetrics.Density;
        }

        private void OnDoubleTapped(object sender, GestureDetector.DoubleTapEventArgs e)
        {
            var position = GetScreenPosition(e.Event, this);
            Map.InvokeInfo(position, position, _scale, Renderer.SymbolCache, WidgetTouched, 2);
        }

        private void OnSingleTapped(object sender, GestureDetector.SingleTapConfirmedEventArgs e)
        {
            var position = GetScreenPosition(e.Event, this);
            Map.InvokeInfo(position, position, _scale, Renderer.SymbolCache, WidgetTouched, 1);
        }

        protected override void OnSizeChanged(int width, int height, int oldWidth, int oldHeight)
        {
            base.OnSizeChanged(width, height, oldWidth, oldHeight);
            PushSizeOntoViewport(width, height);
        }

        private void CanvasOnPaintSurface(object sender, SKPaintSurfaceEventArgs args)
        {
            TryInitializeViewport();
            if (!_map.Viewport.Initialized) return;

            args.Surface.Canvas.Scale(_scale, _scale); // we can only set the scale in the render loop

            Renderer.Render(args.Surface.Canvas, _map.Viewport, _map.Layers, _map.Widgets, _map.BackColor);
        }

        public void TryInitializeViewport()
        {
            if (_map.Viewport.Initialized) return;

            if (_map.Viewport.TryInitializeViewport(_map.Envelope, GetCanvasWidth(Width), GetCanvasHeight(Height)))
            {
                Map.ViewChanged(true);
                OnViewportInitialized();
            }
        }

        private void OnViewportInitialized()
        {
            ViewportInitialized?.Invoke(this, EventArgs.Empty);
        }

        public void MapView_Touch(object sender, TouchEventArgs args)
        {
            if (_gestureDetector.OnTouchEvent(args.Event))
                return;

            var touchPoints = GetMapPositions(args.Event, this);

            switch (args.Event.Action)
            {
                case MotionEventActions.Up:
                    RefreshGraphics();
                    _mode = TouchMode.None;
                    _map.ViewChanged(true);
                    break;
                case MotionEventActions.Down:
                case MotionEventActions.Pointer1Down:
                case MotionEventActions.Pointer2Down:
                case MotionEventActions.Pointer3Down:
                    if (touchPoints.Count >= 2)
                    {
                        (_previousCenter, _previousRadius, _previousAngle) = GetPinchValues(touchPoints);
                        _mode = TouchMode.Zooming;
                        _innerRotation = _map.Viewport.Rotation;
                    }
                    else
                    {
                        _mode = TouchMode.Dragging;
                        _previousCenter = touchPoints.First();
                    }
                    break;
                case MotionEventActions.Pointer1Up:
                case MotionEventActions.Pointer2Up:
                case MotionEventActions.Pointer3Up:
                    // Remove the touchPoint that was released from the locations to reset the
                    // starting points of the move and rotation
                    touchPoints.RemoveAt(args.Event.ActionIndex);

                    if (touchPoints.Count >= 2)
                    {
                        (_previousCenter, _previousRadius, _previousAngle) = GetPinchValues(touchPoints);
                        _mode = TouchMode.Zooming;
                        _innerRotation = _map.Viewport.Rotation;
                    }
                    else
                    {
                        _mode = TouchMode.Dragging;
                        _previousCenter = touchPoints.First();
                    }
                    break;
                case MotionEventActions.Move:
                    switch (_mode)
                    {
                        case TouchMode.Dragging:
                            {
                                if (touchPoints.Count != 1)
                                    return;

                                var touchPosition = touchPoints.First();
                                if (_previousCenter != null && !_previousCenter.IsEmpty())
                                {
                                    _map.Viewport.Transform(touchPosition.X, touchPosition.Y, _previousCenter.X, _previousCenter.Y);

                                    ViewportLimiter.LimitExtent(_map.Viewport, _map.PanMode, _map.PanLimits, _map.Envelope);

                                    RefreshGraphics();
                                }
                                _previousCenter = touchPosition;
                            }
                            break;
                        case TouchMode.Zooming:
                            {
                                if (touchPoints.Count < 2)
                                    return;

                                var (prevCenter, prevRadius, prevAngle) = (_previousCenter, _previousRadius, _previousAngle);
                                var (center, radius, angle) = GetPinchValues(touchPoints);

                                double rotationDelta = 0;

                                if (!RotationLock)
                                {
                                    _innerRotation += angle - prevAngle;
                                    _innerRotation %= 360;

                                    if (_innerRotation > 180)
                                        _innerRotation -= 360;
                                    else if (_innerRotation < -180)
                                        _innerRotation += 360;

                                    if (_map.Viewport.Rotation == 0 && Math.Abs(_innerRotation) >= Math.Abs(UnSnapRotationDegrees))
                                        rotationDelta = _innerRotation;
                                    else if (_map.Viewport.Rotation != 0)
                                    {
                                        if (Math.Abs(_innerRotation) <= Math.Abs(ReSnapRotationDegrees))
                                            rotationDelta = -_map.Viewport.Rotation;
                                        else
                                            rotationDelta = _innerRotation - _map.Viewport.Rotation;
                                    }
                                }

                                _map.Viewport.Transform(center.X, center.Y, prevCenter.X, prevCenter.Y, radius / prevRadius, rotationDelta);

                                (_previousCenter, _previousRadius, _previousAngle) = (center, radius, angle);

                                ViewportLimiter.Limit(_map.Viewport,
                                    _map.ZoomMode, _map.ZoomLimits, _map.Resolutions,
                                    _map.PanMode, _map.PanLimits, _map.Envelope);

                                RefreshGraphics();
                            }
                            break;
                    }
                    break;
            }
        }

        private List<Geometries.Point> GetMapPositions(MotionEvent me, View view)
        {
            var result = new List<Geometries.Point>();
            for (var i = 0; i < me.PointerCount; i++)
            {
                result.Add(new Geometries.Point((me.GetX(i) - view.Left) / _scale, (me.GetY(i) - view.Top) / _scale));
            }
            return result;
        }

        private static Geometries.Point GetScreenPosition(MotionEvent motionEvent, View view)
        {
            return new PointF(
                motionEvent.GetX(0) - view.Left,
                motionEvent.GetY(0) - view.Top).ToMapsui();
        }

        public Map Map
        {
            get => _map;
            set
            {
                if (_map != null)
                {
                    UnsubscribeFromMapEvents(_map);
                    _map = null;
                }

                _map = value;

                if (_map != null)
                {
                    SubscribeToMapEvents(_map);
                    _map.ViewChanged(true);
                    PushSizeOntoViewport(Width, Height);
                }

                RefreshGraphics();
            }
        }

        private void MapRefreshGraphics(object sender, EventArgs eventArgs)
        {
            RefreshGraphics();
        }

        private void MapPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Layer.Enabled))
            {
                RefreshGraphics();
            }
            else if (e.PropertyName == nameof(Layer.Opacity))
            {
                RefreshGraphics();
            }
        }

        private void MapDataChanged(object sender, DataChangedEventArgs e)
        {
            if (e.Cancelled || e.Error != null)
            {
                Logger.Log(LogLevel.Warning, "An error occurred while fetching data", e.Error);
            }
            else if (e.Cancelled)
            {
                Logger.Log(LogLevel.Warning, "Fetching data was cancelled", e.Error);
            }
            else // no problems
            {
                RefreshGraphics();
            }
        }

        protected override void OnDraw(Canvas canvas)
        {
            Invalidate();
            base.OnDraw(canvas);
        }

        public void RefreshGraphics()
        {
            RunOnUIThread(RefreshGraphicsWithTryCatch);
        }

        private void RefreshGraphicsWithTryCatch()
        {
            try
            {
                // Calling Invalidate on the MapControl itself is not enough in some case (observed in XF).
                Invalidate();
                _canvas?.Invalidate();
            }
            catch (ObjectDisposedException e)
            {
                // See issue: https://github.com/Mapsui/Mapsui/issues/433
                // What seems to be happening. The Activity is Disposed. Appently it's children get Disposed
                // explicitly by some in Xamarin. During this Dispose the MessageCenter, which is itself not
                // disposed get another notification to call RefreshGraphics.
                Logger.Log(LogLevel.Warning, "This can happen when the parent Activity is disposing.", e);
            }
        }

        private void RunOnUIThread(Action action)
        {
            if (SynchronizationContext.Current == null)
                _mainLooperHandler.Post(action);
            else
                action();
        }

        public void RefreshData()
        {
            _map.ViewChanged(true);
        }

        public void Refresh()
        {
            RefreshData();
            RefreshGraphics();
        }

        protected override void OnLayout(bool changed, int l, int t, int r, int b)
        {
            Position(_canvas, l, t, r, b);
        }

        private void Position(View view, int l, int t, int r, int b)
        {
            view.Top = t;
            view.Bottom = b;
            view.Left = l;
            view.Right = r;
        }

        private void WidgetTouched(IWidget widget, Geometries.Point screenPosition)
        {
            if (widget is Hyperlink hyperlink)
            {
                global::Android.Net.Uri uri = global::Android.Net.Uri.Parse(hyperlink.Url);
                Intent intent = new Intent(Intent.ActionView);
                intent.SetData(uri);

                Intent chooser = Intent.CreateChooser(intent, "Open with");

                Context.StartActivity(chooser);
            }

            widget.HandleWidgetTouched(screenPosition);
        }

        public new void Dispose()
        {
            Unsubscribe();
            base.Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            Unsubscribe();
            base.Dispose(disposing);
        }

    }

    public class R : SKGLSurfaceView.ISKRenderer
    {
        private float _scale;

        public R(MapControl m, float _scale)
        {
            this.M = m;
            this._scale = _scale;
        }

        public MapControl M { get; }

        [Obsolete]
        public void OnDrawFrame(SKSurface surface, GRBackendRenderTargetDesc renderTarget)
        {
            this.M.TryInitializeViewport();
            if (!M._map.Viewport.Initialized) return;

            surface.Canvas.Scale(_scale, _scale); // we can only set the scale in the render loop

            this.M.Renderer.Render(surface.Canvas, M._map.Viewport, M._map.Layers, M._map.Widgets, M._map.BackColor);
        }
    }
}