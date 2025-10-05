using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.PM
{
    /// <summary>
    /// PM Budget node: tracks project budget and costs.
    /// Dollar sign icon with budget vs actual comparison.
    /// </summary>
    public class BudgetNode : PMControl
    {
        private string _budgetName = "Budget";
        public string BudgetName
        {
            get => _budgetName;
            set
            {
                var v = value ?? "";
                if (_budgetName != v)
                {
                    _budgetName = v;
                    if (NodeProperties.TryGetValue("BudgetName", out var p))
                        p.ParameterCurrentValue = _budgetName;
                    InvalidateVisual();
                }
            }
        }

        private decimal _plannedBudget = 0;
        public decimal PlannedBudget
        {
            get => _plannedBudget;
            set
            {
                if (_plannedBudget != value)
                {
                    _plannedBudget = value;
                    if (NodeProperties.TryGetValue("PlannedBudget", out var p))
                        p.ParameterCurrentValue = _plannedBudget;
                    InvalidateVisual();
                }
            }
        }

        private decimal _actualCost = 0;
        public decimal ActualCost
        {
            get => _actualCost;
            set
            {
                if (_actualCost != value)
                {
                    _actualCost = value;
                    if (NodeProperties.TryGetValue("ActualCost", out var p))
                        p.ParameterCurrentValue = _actualCost;
                    InvalidateVisual();
                }
            }
        }

        private string _currency = "USD";
        public string Currency
        {
            get => _currency;
            set
            {
                var v = value ?? "USD";
                if (_currency != v)
                {
                    _currency = v;
                    if (NodeProperties.TryGetValue("Currency", out var p))
                        p.ParameterCurrentValue = _currency;
                    InvalidateVisual();
                }
            }
        }

        public BudgetNode()
        {
            Name = "PM Budget";
            Width = 160;
            Height = 90;
            EnsurePortCounts(1, 1);

            NodeProperties["BudgetName"] = new ParameterInfo
            {
                ParameterName = "BudgetName",
                ParameterType = typeof(string),
                DefaultParameterValue = _budgetName,
                ParameterCurrentValue = _budgetName,
                Description = "Budget category name"
            };
            NodeProperties["PlannedBudget"] = new ParameterInfo
            {
                ParameterName = "PlannedBudget",
                ParameterType = typeof(decimal),
                DefaultParameterValue = _plannedBudget,
                ParameterCurrentValue = _plannedBudget,
                Description = "Planned budget amount"
            };
            NodeProperties["ActualCost"] = new ParameterInfo
            {
                ParameterName = "ActualCost",
                ParameterType = typeof(decimal),
                DefaultParameterValue = _actualCost,
                ParameterCurrentValue = _actualCost,
                Description = "Actual cost incurred"
            };
            NodeProperties["Currency"] = new ParameterInfo
            {
                ParameterName = "Currency",
                ParameterType = typeof(string),
                DefaultParameterValue = _currency,
                ParameterCurrentValue = _currency,
                Description = "Currency code",
                Choices = new[] { "USD", "EUR", "GBP", "JPY", "CNY", "CAD", "AUD" }
            };
        }

        protected override void LayoutPorts()
        {
            LayoutPortsVerticalSegments(topInset: 8f, bottomInset: 8f);
        }

        protected override void DrawPMContent(SKCanvas canvas, DrawingContext context)
        {
            if (!context.Bounds.IntersectsWith(Bounds)) return;

            var r = Bounds;
            bool isOverBudget = _actualCost > _plannedBudget;
            float variance = _plannedBudget > 0 ? (float)((_actualCost - _plannedBudget) / _plannedBudget * 100) : 0;

            SKColor fillColor = isOverBudget 
                ? new SKColor(0xFF, 0xE0, 0xE0)  // Light red
                : new SKColor(0xE8, 0xF5, 0xE9); // Light green

            using var fill = new SKPaint { Color = fillColor, IsAntialias = true };
            using var stroke = new SKPaint { Color = MaterialColors.Outline, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
            using var text = new SKPaint { Color = MaterialColors.OnSurface, IsAntialias = true };

            canvas.DrawRoundRect(r, CornerRadius, CornerRadius, fill);
            canvas.DrawRoundRect(r, CornerRadius, CornerRadius, stroke);

            // Draw dollar sign icon
            float iconX = r.Left + 12;
            float iconY = r.Top + 18;
            using var iconFont = new SKFont(SKTypeface.Default, 20) { Embolden = true };
            using var iconPaint = new SKPaint { Color = isOverBudget ? new SKColor(0xE5, 0x39, 0x35) : new SKColor(0x43, 0xA0, 0x47), IsAntialias = true };
            canvas.DrawText("$", iconX, iconY, SKTextAlign.Left, iconFont, iconPaint);

            // Draw budget name
            using var nameFont = new SKFont(SKTypeface.Default, 12);
            canvas.DrawText(BudgetName, r.Left + 38, r.Top + 18, SKTextAlign.Left, nameFont, text);

            // Draw planned budget
            using var detailFont = new SKFont(SKTypeface.Default, 10);
            using var grayText = new SKPaint { Color = new SKColor(0x60, 0x60, 0x60), IsAntialias = true };
            string plannedStr = $"Planned: {FormatCurrency(_plannedBudget)}";
            canvas.DrawText(plannedStr, r.Left + 12, r.Top + 40, SKTextAlign.Left, detailFont, grayText);

            // Draw actual cost
            using var actualPaint = new SKPaint { Color = isOverBudget ? new SKColor(0xE5, 0x39, 0x35) : new SKColor(0x43, 0xA0, 0x47), IsAntialias = true };
            string actualStr = $"Actual: {FormatCurrency(_actualCost)}";
            canvas.DrawText(actualStr, r.Left + 12, r.Top + 56, SKTextAlign.Left, detailFont, actualPaint);

            // Draw variance bar
            if (_plannedBudget > 0)
            {
                float barWidth = r.Width - 24;
                float usedPercent = System.Math.Min((float)(_actualCost / _plannedBudget), 1.5f);
                float fillWidth = barWidth * usedPercent;

                var barRect = new SKRect(r.Left + 12, r.Bottom - 18, r.Left + 12 + barWidth, r.Bottom - 10);
                using var barBg = new SKPaint { Color = new SKColor(0xE0, 0xE0, 0xE0), IsAntialias = true };
                canvas.DrawRoundRect(barRect, 3f, 3f, barBg);

                var fillRect = new SKRect(r.Left + 12, r.Bottom - 18, r.Left + 12 + fillWidth, r.Bottom - 10);
                SKColor barColor = isOverBudget ? new SKColor(0xE5, 0x39, 0x35) : new SKColor(0x43, 0xA0, 0x47);
                using var barFill = new SKPaint { Color = barColor, IsAntialias = true };
                canvas.DrawRoundRect(fillRect, 3f, 3f, barFill);

                // Draw variance percentage
                using var varianceFont = new SKFont(SKTypeface.Default, 8);
                string varianceStr = variance >= 0 ? $"+{variance:F1}%" : $"{variance:F1}%";
                canvas.DrawText(varianceStr, r.MidX, r.Bottom - 20, SKTextAlign.Center, varianceFont, actualPaint);
            }

            DrawPorts(canvas);
        }

        private string FormatCurrency(decimal amount)
        {
            string symbol = Currency switch
            {
                "EUR" => "€",
                "GBP" => "£",
                "JPY" => "¥",
                "CNY" => "¥",
                _ => "$"
            };

            if (amount >= 1_000_000)
                return $"{symbol}{amount / 1_000_000:F1}M";
            if (amount >= 1_000)
                return $"{symbol}{amount / 1_000:F1}K";
            return $"{symbol}{amount:F0}";
        }
    }
}
