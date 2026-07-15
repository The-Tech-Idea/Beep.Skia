using Beep.Skia.Components;
using SkiaSharp;
using System;

namespace Beep.Skia.WellLogs
{
    public class WellLogCanvas : MaterialControl
    {
        private WellLogDocument _document = WellLogTemplates.CreateTripleComboDemo();
        private float _trackGap = 6f;
        private float _headerHeight = 28f;
        private SKColor _surfaceColor = new SKColor(253, 253, 253);

        public WellLogCanvas()
        {
            Name = "Well Log Canvas";
            Width = 980f;
            Height = 680f;
            TextColor = MaterialColors.OnSurface;
            SeedNodeProperties();
        }

        public WellLogDocument Document
        {
            get => _document;
            set
            {
                _document = value ?? WellLogTemplates.CreateTripleComboDemo();
                UpsertNodeProperty("DocumentName", typeof(string), _document.Name, _document.Name, "Displayed well-log document name");
                UpsertNodeProperty("PrimaryStandard", typeof(string), _document.PrimaryStandard.ToString(), _document.PrimaryStandard.ToString(), "Primary storage standard", Enum.GetNames(typeof(WellLogDataStandard)));
                InvalidateVisual();
            }
        }

        public float TrackGap
        {
            get => _trackGap;
            set
            {
                if (Math.Abs(_trackGap - value) < 0.0001f)
                {
                    return;
                }

                _trackGap = Math.Max(0f, value);
                UpsertNodeProperty("TrackGap", typeof(float), 6f, _trackGap, "Spacing between log tracks");
                InvalidateVisual();
            }
        }

        public float HeaderHeight
        {
            get => _headerHeight;
            set
            {
                if (Math.Abs(_headerHeight - value) < 0.0001f)
                {
                    return;
                }

                _headerHeight = Math.Max(18f, value);
                UpsertNodeProperty("HeaderHeight", typeof(float), 28f, _headerHeight, "Default header height for each track");
                InvalidateVisual();
            }
        }

        public SKColor SurfaceColor
        {
            get => _surfaceColor;
            set
            {
                if (_surfaceColor == value)
                {
                    return;
                }

                _surfaceColor = value;
                UpsertNodeProperty("SurfaceColor", typeof(SKColor), _surfaceColor, _surfaceColor, "Outer surface color");
                InvalidateVisual();
            }
        }

        public string PrimaryStandard
        {
            get => Document.PrimaryStandard.ToString();
            set
            {
                if (!Enum.TryParse<WellLogDataStandard>(value, true, out var parsed) || Document.PrimaryStandard == parsed)
                {
                    return;
                }

                Document.PrimaryStandard = parsed;
                UpsertNodeProperty("PrimaryStandard", typeof(string), parsed.ToString(), parsed.ToString(), "Primary storage standard", Enum.GetNames(typeof(WellLogDataStandard)));
                InvalidateVisual();
            }
        }

        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            var outerBounds = new SKRect(X, Y, X + Width, Y + Height);
            var innerBounds = new SKRect(X + 8f, Y + 8f, X + Width - 8f, Y + Height - 8f);

            using var fillPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = SurfaceColor
            };
            using var borderPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                Color = new SKColor(177, 182, 191),
                StrokeWidth = 1.25f
            };

            canvas.DrawRoundRect(outerBounds, 10f, 10f, fillPaint);
            WellLogRenderer.Draw(canvas, innerBounds, Document, TrackGap, HeaderHeight);
            canvas.DrawRoundRect(outerBounds, 10f, 10f, borderPaint);
        }

        private void SeedNodeProperties()
        {
            UpsertNodeProperty("DocumentName", typeof(string), Document.Name, Document.Name, "Displayed well-log document name");
            UpsertNodeProperty("TrackGap", typeof(float), 6f, _trackGap, "Spacing between log tracks");
            UpsertNodeProperty("HeaderHeight", typeof(float), 28f, _headerHeight, "Default header height for each track");
            UpsertNodeProperty("SurfaceColor", typeof(SKColor), _surfaceColor, _surfaceColor, "Outer surface color");
            UpsertNodeProperty("PrimaryStandard", typeof(string), Document.PrimaryStandard.ToString(), Document.PrimaryStandard.ToString(), "Primary storage standard", Enum.GetNames(typeof(WellLogDataStandard)));
        }

        /// <summary>
        /// Loads a LAS 2.0/3.0 file into this canvas.
        /// </summary>
        public void LoadFromLas(string filePath)
        {
            var parser = new LasFileParser();
            Document = parser.ParseFile(filePath);
            Document.PrimaryStandard = WellLogDataStandard.Las20;
            if (!string.IsNullOrEmpty(parser.Document.SourceReference))
                Document.SourceReference = parser.Document.SourceReference;
            InvalidateVisual();
        }
    }
}