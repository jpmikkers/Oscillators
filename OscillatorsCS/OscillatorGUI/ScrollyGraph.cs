using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.TextFormatting;
using Avalonia.Platform;
using Avalonia.Threading;

namespace OscillatorGUI;


public partial class SpectrogramPlot : Control
{
    private Typeface _typeface = new Typeface("Cascadia Mono", FontStyle.Normal, FontWeight.Normal, FontStretch.Normal);
    //FixedSizeCache<Rune, PrerenderedGlyph> _prerenderedGlyphCache = new(10);

    //private CellInfo _emptyCellTemplate = new CellInfo { Background = Colors.Black, Foreground = Colors.White, Character = new Rune(0) };

    //private RenderTargetBitmap? _glyphRenderTargetBitmap;

    private int _numCols = 20;
    private int _numRows = 20;
    //private int _cellWidth = 10;
    //private int _cellHeight = 10;
    //private double _fontRenderingEmSize = 20;

    private IImage _clearBitmap;
    private IBrush _opacityMaskBrush = new SolidColorBrush(Colors.Black, 0.5);

    int _frameCount = 0;

    private WriteableBitmap? _backgroundBitmap;
    private bool _slowBackground = false;

    private WriteableBitmap? _foregroundBitmap;

    private readonly List<float[]> _dataHistory = [];
    private readonly Queue<float[]> _freedHistory = new();
    private float[] _emptyData = [];
    private static readonly uint[] _gradient = new uint[256];

    public static readonly DirectProperty<SpectrogramPlot, int> NumBandsProperty =
        AvaloniaProperty.RegisterDirect<SpectrogramPlot, int>(
            nameof(NumBands),
            o => o.NumBands,
            (o, v) => o.NumBands = v);

    private int _numBands = 1;

    public int NumBands
    {
        get => _numBands;
        set
        {
            if (SetAndRaise(NumBandsProperty, ref _numBands, value))
            {
                _dataHistory.Clear();
            }
        }
    }

    private float _scaleMinValue = 0.0f;
    private float _scaleMaxValue = 1.0f;

    public static readonly DirectProperty<SpectrogramPlot, float> ScaleMinValueProperty =
    AvaloniaProperty.RegisterDirect<SpectrogramPlot, float>(
        nameof(ScaleMinValue),
        o => o.ScaleMinValue,
        (o, v) => o.ScaleMinValue = v);

    public float ScaleMinValue
    {
        get => _scaleMinValue;
        set => SetAndRaise(ScaleMinValueProperty, ref _scaleMinValue, value);
    }

    public static readonly DirectProperty<SpectrogramPlot, float> ScaleMaxValueProperty =
    AvaloniaProperty.RegisterDirect<SpectrogramPlot, float>(
        nameof(ScaleMaxValue),
        o => o.ScaleMaxValue,
        (o, v) => o.ScaleMaxValue = v);

    public float ScaleMaxValue
    {
        get => _scaleMaxValue;
        set => SetAndRaise(ScaleMaxValueProperty, ref _scaleMaxValue, value);
    }

    /*
        public static readonly StyledProperty<int> NumBandsProperty = AvaloniaProperty.Register<ScrollyGraphControl, int>(nameof(NumBands),10);
        public int NumBands
        {
            get => GetValue(NumBandsProperty);
            set
            {           
                SetValue(NumBandsProperty, value);
            }
        }
    */

    public static readonly StyledProperty<int> NumHistoryProperty = AvaloniaProperty.Register<SpectrogramPlot, int>(nameof(NumHistory),10);

    public int NumHistory
    {
        get => GetValue(NumHistoryProperty);
        set => SetValue(NumHistoryProperty, value);
    }

    static SpectrogramPlot()
    {
        AffectsRender<SpectrogramPlot>(NumBandsProperty, NumHistoryProperty, ScaleMinValueProperty, ScaleMaxValueProperty);

        for (var i = 0; i < 256; i++)
        {
            byte a = 255;     // fully opaque
            byte r = 0;
            byte g = 0;
            byte b = 0;

            if (i < 85)
            {
                // Phase 1: add red (0 → 255)
                r = (byte)(i * 3);
            }
            else if (i < 170)
            {
                // Phase 2: add blue (0 → 255)
                r = 255;
                g = (byte)((i - 85) * 3);
            }
            else
            {
                // Phase 3: add green (0 → 255)
                r = 255;
                g = 255;
                b = (byte)((i - 170) * 3);
            }

            _gradient[i] =
                ((uint)a << 24) |
                ((uint)r << 16) |
                ((uint)g << 8) |
                ((uint)b);
        }
    }

    public SpectrogramPlot()
    {
        _clearBitmap = new WriteableBitmap(new PixelSize(1, 1), new Vector(96, 96));
        var timer = new DispatcherTimer();
        timer.Interval = TimeSpan.FromSeconds(1 / 60.0);
        timer.Tick += (sender, e) => { InvalidateVisual(); };
        timer.Start();
    }



    public void AddData(float[] data)
    {
        if (_freedHistory.TryDequeue(out var freebuf)) 
        {
            if(freebuf.Length != NumBands)
            {
                freebuf = new float[NumBands];
            }
        }
        else
        {
            freebuf = new float[NumBands];
        }

        Array.Copy(data, freebuf, Math.Min(data.Length, NumBands));

        _dataHistory.Add(data);

        while (_dataHistory.Count > NumHistory)
        {
            var removed = _dataHistory[0];
            _dataHistory.RemoveAt(0);
            _freedHistory.Enqueue(removed);
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        //var renderscale = VisualRoot?.RenderScaling ?? 1.0;

        //var shapedBuffer = TextShaper.Current.ShapeText("a", new TextShaperOptions(_typeface.GlyphTypeface, fontRenderingEmSize: _fontRenderingEmSize));
        //var textRun = new ShapedTextRun(
        //    shapedBuffer,
        //    new GenericTextRunProperties(_typeface, _fontRenderingEmSize)
        //);

        //_cellWidth = (int)Math.Ceiling(textRun.Size.Width);
        //_cellHeight = (int)Math.Ceiling(textRun.Size.Height);
    }

    private Color GetRandomColor()
    {
        return Color.FromRgb((byte)Random.Shared.Next(256), (byte)Random.Shared.Next(256), (byte)Random.Shared.Next(256));
    }

    //private void ResizeBuffers()
    //{
    //    var scale = VisualRoot?.RenderScaling ?? 1.0;
    //    var pixelSize = new PixelSize((int)(DesiredSize.Width * scale), (int)(DesiredSize.Height * scale));

    //    int newNumRows = pixelSize.Height > 0 ? (int)(pixelSize.Height / _cellHeight) : 0;
    //    int newNumCols = pixelSize.Width > 0 ? (int)(pixelSize.Width / _cellWidth) : 0;

    //    for (int r = 0; r < _cells.Length; r++)
    //    {
    //        if (_cells[r].Length < newNumCols)
    //        {
    //            var oldNumCols = _cells[r].Length;
    //            Array.Resize(ref _cells[r], newNumCols);

    //            for (int c = oldNumCols; c < newNumCols; c++)
    //            {
    //                _cells[r][c].Cell = _emptyCellTemplate;
    //            }
    //        }
    //    }

    //    if (newNumRows > _cells.Length)
    //    {
    //        var oldNumRows = _cells.Length;
    //        Array.Resize(ref _cells, newNumRows);

    //        for (var i = oldNumRows; i < newNumRows; i++)
    //        {
    //            _cells[i] = new RenderedCellInfo[newNumCols];

    //            for (int c = 0; c < newNumCols; c++)
    //            {
    //                _cells[i][c].Cell = _emptyCellTemplate;
    //            }
    //        }
    //    }

    //    _numCols = newNumCols;
    //    _numRows = newNumRows;
    //}

    private uint ColorFromFloat(float v)
    {
        var t = Math.Clamp(v,ScaleMinValue,ScaleMaxValue);
        t = (t - ScaleMinValue) / (ScaleMaxValue - ScaleMinValue);
        var i = Math.Clamp((int)(t * _gradient.Length),0,_gradient.Length-1);
        return _gradient[i];
    }

    private void RenderBackground(DrawingContext drawingContext)
    {
        if (NumBands > 0 && NumHistory > 0)
        {
            if (_backgroundBitmap == null || _backgroundBitmap.PixelSize.Width != NumBands || _backgroundBitmap.PixelSize.Height != NumHistory)
            {
                _backgroundBitmap = new WriteableBitmap(new PixelSize(NumBands, NumHistory), new Vector(296, 296));
            }

            if(_emptyData.Length < NumBands)
            {
                Array.Resize(ref _emptyData, NumBands);
            }

            using (var lfb = _backgroundBitmap.Lock())
            {
                unsafe
                {
                    uint* ptr = (uint*)lfb.Address;
                    int bytesPerPixel = 4;
                    int pixelsPerRow = lfb.RowBytes / bytesPerPixel;

                    for (int y = 0; y < NumHistory; y++)
                    {
                        uint* row = ptr + y * pixelsPerRow;

                        var historyIndex = NumHistory - 1 - y;                  // 0 is most recent, NumHistory-1 is oldest
                        var dataIndex = _dataHistory.Count - 1 - historyIndex;  // 0 is oldest, Count-1 is most recent

                        var dataRow = (dataIndex>=0 && dataIndex < _dataHistory.Count) ? _dataHistory[dataIndex] : _emptyData;

                        int x = 0;
                        int len = Math.Min(dataRow.Length, NumBands);

                        for (; x < len; x++)
                        {
                            //ref RenderedCellInfo rci = ref logicalRow[x];
                            //rci.RenderedCell.Background = rci.Cell.Background;
                            row[x] = ColorFromFloat(dataRow[x]);
                        }

                        for (; x < NumBands; x++)
                        {
                            row[x] = 0xFF000000; // opaque black
                        }
                    }
                }
            }

            var scale = VisualRoot?.RenderScaling ?? 1.0;
            var pixelSize = new PixelSize((int)(DesiredSize.Width * scale), (int)(DesiredSize.Height * scale));

            using (var bgState = drawingContext.PushRenderOptions(
                new RenderOptions
                {
                    RequiresFullOpacityHandling = false,
                    BitmapBlendingMode = BitmapBlendingMode.Source,
                    BitmapInterpolationMode = BitmapInterpolationMode.None,
                    TextRenderingMode = TextRenderingMode.Alias,
                    EdgeMode = EdgeMode.Aliased,
                }))
            {
                drawingContext.DrawImage(_backgroundBitmap, new Rect(0, 0, _backgroundBitmap.PixelSize.Width, _backgroundBitmap.PixelSize.Height), new Rect(0, 0, pixelSize.Width, pixelSize.Height));
            }
        }
    }

    //private void Scroll(int cols, int rows)
    //{
    //    if (_foregroundBitmap != null)
    //    {
    //        _foregroundBitmap.SafeBlit(
    //            new PixelRect(0, 0, _numCols * _cellWidth, _numRows * _cellHeight),
    //            new PixelPoint(cols * _cellWidth, rows * _cellHeight));
    //    }
    //}

    public override void Render(DrawingContext drawingContext)
    {
        if (Design.IsDesignMode) return;

        Dispatcher.UIThread.VerifyAccess();

        _frameCount++;

        //AddData(Enumerable.Range(0, NumBands).Select(i => (_frameCount % 256) / 255.0f).ToArray());

        AddData(Enumerable.Range(0, NumBands).Select(i => (float)Random.Shared.NextDouble()).ToArray());

        //ResizeBuffers();
        //_prerenderedGlyphCache.SetCapacity(Math.Max(1, 2 * _numCols * _numRows));  // 2 full screens of unique Runes should be an okay limit

        // fill logicalrows with random content
        //for (var row = 0; row < _numRows; row++)
        //{
        //    for (var col = 0; col < _numCols; col++)
        //    {
        //        SetCell(col, row, new CellInfo
        //        {
        //            Background = PlasmaEffect.GetPlasmaColor(_frameCount / 30.0, row, col),
        //            Foreground = GetRandomColor(),
        //            Character = new Rune((char)Random.Shared.Next(32, 255))
        //        });
        //    }
        //}

        var scale = VisualRoot?.RenderScaling ?? 1.0;
        using (var noTrans = drawingContext.PushTransform(Matrix.CreateScale(1.0 / scale, 1.0 / scale)))
        {
            RenderBackground(drawingContext);
            //RenderForeground(drawingContext);
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // Measure children or content here
        return availableSize; // Accept all available space
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        // Arrange children or content here
        return finalSize; // Fill all available space
    }
}
