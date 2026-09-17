using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Components;
using Beep.Skia.Model;

namespace Beep.Skia.StateMachine
{
    /// <summary>
    /// Regular state: rounded rectangle with optional title and entry/exit/do activities,
    /// one input and multiple outputs.
    /// </summary>
    public class StateNode : StateMachineControl
    {
        private string _title = "State";
        public string Title
        {
            get => _title;
            set
            {
                var v = value ?? string.Empty;
                if (_title == v) return;
                _title = v;
                if (NodeProperties.TryGetValue("Title", out var pi)) pi.ParameterCurrentValue = _title;
                InvalidateVisual();
            }
        }

        private string _entryAction = string.Empty;
        /// <summary>
        /// Activity executed when the state is entered (UML entry action).
        /// </summary>
        public string EntryAction
        {
            get => _entryAction;
            set
            {
                var v = value ?? string.Empty;
                if (_entryAction == v) return;
                _entryAction = v;
                if (NodeProperties.TryGetValue("EntryAction", out var pi)) pi.ParameterCurrentValue = _entryAction;
                InvalidateVisual();
            }
        }

        private string _exitAction = string.Empty;
        /// <summary>
        /// Activity executed when the state is exited (UML exit action).
        /// </summary>
        public string ExitAction
        {
            get => _exitAction;
            set
            {
                var v = value ?? string.Empty;
                if (_exitAction == v) return;
                _exitAction = v;
                if (NodeProperties.TryGetValue("ExitAction", out var pi)) pi.ParameterCurrentValue = _exitAction;
                InvalidateVisual();
            }
        }

        private string _doActivity = string.Empty;
        /// <summary>
        /// Activity executed while the state is active (UML do activity).
        /// </summary>
        public string DoActivity
        {
            get => _doActivity;
            set
            {
                var v = value ?? string.Empty;
                if (_doActivity == v) return;
                _doActivity = v;
                if (NodeProperties.TryGetValue("DoActivity", out var pi)) pi.ParameterCurrentValue = _doActivity;
                InvalidateVisual();
            }
        }

        public StateNode()
        {
            Width = 140; Height = 64;
            BackgroundColor = MaterialColors.Surface;
            BorderColor = MaterialColors.Outline;
            TextColor = MaterialColors.OnSurface;
            if (NodeProperties.TryGetValue("TextColor", out var piTxt)) piTxt.ParameterCurrentValue = TextColor;
            EnsurePortCounts(1, 2);

            NodeProperties["Title"] = new ParameterInfo { ParameterName = "Title", ParameterType = typeof(string), DefaultParameterValue = _title, ParameterCurrentValue = _title, Description = "State title" };
            NodeProperties["EntryAction"] = new ParameterInfo { ParameterName = "EntryAction", ParameterType = typeof(string), DefaultParameterValue = _entryAction, ParameterCurrentValue = _entryAction, Description = "Activity executed on entry" };
            NodeProperties["ExitAction"] = new ParameterInfo { ParameterName = "ExitAction", ParameterType = typeof(string), DefaultParameterValue = _exitAction, ParameterCurrentValue = _exitAction, Description = "Activity executed on exit" };
            NodeProperties["DoActivity"] = new ParameterInfo { ParameterName = "DoActivity", ParameterType = typeof(string), DefaultParameterValue = _doActivity, ParameterCurrentValue = _doActivity, Description = "Activity executed while active" };
        }

        protected override void LayoutPorts()
        {
            // Regular state: rounded rectangle with inputs on left, outputs on right
            LayoutPortsRightEdge(6f, 6f);
        }

        protected override void DrawStateMachineContent(SKCanvas canvas, DrawingContext context)
        {
            using var fill = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var stroke = new SKPaint { Color = BorderColor, Style = SKPaintStyle.Stroke, StrokeWidth = BorderThickness, IsAntialias = true };
            var rect = new SKRect(X, Y, X + Width, Y + Height);
            canvas.DrawRoundRect(rect, 10, 10, fill);
            canvas.DrawRoundRect(rect, 10, 10, stroke);

            using var font = new SKFont(SKTypeface.Default, 13) { Embolden = true };
            using var text = new SKPaint { Color = TextColor, IsAntialias = true };
            bool hasActivities = !string.IsNullOrWhiteSpace(_entryAction)
                || !string.IsNullOrWhiteSpace(_exitAction)
                || !string.IsNullOrWhiteSpace(_doActivity);
            float titleY = hasActivities ? Y + Height / 2f - 4f : Y + Height / 2f + 4f;
            canvas.DrawText(Title ?? Name ?? string.Empty, X + Width / 2f, titleY, SKTextAlign.Center, font, text);

            DrawActivities(canvas);

            DrawConnectionPoints(canvas);
        }

        private void DrawActivities(SKCanvas canvas)
        {
            if (string.IsNullOrWhiteSpace(_entryAction) &&
                string.IsNullOrWhiteSpace(_exitAction) &&
                string.IsNullOrWhiteSpace(_doActivity))
            {
                return;
            }

            using var activityFont = new SKFont(SKTypeface.Default, 8);
            using var activityPaint = new SKPaint { Color = TextColor.WithAlpha(180), IsAntialias = true };

            float y = Y + Height - 24f;
            if (!string.IsNullOrWhiteSpace(_entryAction))
            {
                canvas.DrawText($"entry / {_entryAction}", X + 6f, y, SKTextAlign.Left, activityFont, activityPaint);
                y += 10f;
            }
            if (!string.IsNullOrWhiteSpace(_doActivity))
            {
                canvas.DrawText($"do / {_doActivity}", X + 6f, y, SKTextAlign.Left, activityFont, activityPaint);
                y += 10f;
            }
            if (!string.IsNullOrWhiteSpace(_exitAction))
            {
                canvas.DrawText($"exit / {_exitAction}", X + 6f, y, SKTextAlign.Left, activityFont, activityPaint);
            }
        }
    }
}
