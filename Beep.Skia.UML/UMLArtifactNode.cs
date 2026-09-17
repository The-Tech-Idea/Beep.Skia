using SkiaSharp;
using Beep.Skia;
using Beep.Skia.Model;
using System;
using System.Collections.Generic;

namespace Beep.Skia.UML
{
    /// <summary>
    /// UML Artifact node — a deployable file/artifact with a folded-corner icon and «artifact» stereotype.
    /// </summary>
    public class UMLArtifactNode : UMLControl
    {
        private string _artifactName = "Artifact";
        public string ArtifactName
        {
            get => _artifactName;
            set
            {
                var v = value ?? string.Empty;
                if (_artifactName == v) return;
                _artifactName = v;
                if (NodeProperties.TryGetValue("ArtifactName", out var pi)) pi.ParameterCurrentValue = v;
                InvalidateVisual();
            }
        }

        private string _fileName = string.Empty;
        public string FileName
        {
            get => _fileName;
            set
            {
                var v = value ?? string.Empty;
                if (_fileName == v) return;
                _fileName = v;
                if (NodeProperties.TryGetValue("FileName", out var pi)) pi.ParameterCurrentValue = v;
                InvalidateVisual();
            }
        }

        public UMLArtifactNode()
        {
            Width = 160;
            Height = 84;
            Name = "Artifact";

            NodeProperties["ArtifactName"] = new ParameterInfo { ParameterName = "ArtifactName", ParameterType = typeof(string), DefaultParameterValue = _artifactName, ParameterCurrentValue = _artifactName, Description = "Artifact name" };
            NodeProperties["FileName"] = new ParameterInfo { ParameterName = "FileName", ParameterType = typeof(string), DefaultParameterValue = _fileName, ParameterCurrentValue = _fileName, Description = "File name (e.g., api.dll)" };
        }

        protected override void DrawShape(SKCanvas canvas, DrawingContext context)
        {
            const float fold = 16f;
            var pathBuilder = new SKPathBuilder();
            pathBuilder.MoveTo(X, Y);
            pathBuilder.LineTo(X + Width - fold, Y);
            pathBuilder.LineTo(X + Width, Y + fold);
            pathBuilder.LineTo(X + Width, Y + Height);
            pathBuilder.LineTo(X, Y + Height);
            pathBuilder.Close();

            using var fill = new SKPaint { Color = BackgroundColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var stroke = new SKPaint { Color = BorderColor, Style = SKPaintStyle.Stroke, StrokeWidth = BorderThickness, IsAntialias = true };
            using var path = pathBuilder.Detach();
            canvas.DrawPath(path, fill);
            canvas.DrawPath(path, stroke);

            // Fold triangle
            var foldPathBuilder = new SKPathBuilder();
            foldPathBuilder.MoveTo(X + Width - fold, Y);
            foldPathBuilder.LineTo(X + Width - fold, Y + fold);
            foldPathBuilder.LineTo(X + Width, Y + fold);
            foldPathBuilder.Close();
            using var foldPath = foldPathBuilder.Detach();
            canvas.DrawPath(foldPath, stroke);

            using var stereotypeFont = new SKFont(SKTypeface.Default, 9);
            using var nameFont = new SKFont(SKTypeface.Default, 12) { Embolden = true };
            using var textPaint = new SKPaint { Color = TextColor, IsAntialias = true };

            canvas.DrawText("«artifact»", X + Width / 2f, Y + 22f, SKTextAlign.Center, stereotypeFont, textPaint);
            canvas.DrawText(_artifactName, X + Width / 2f, Y + 44f, SKTextAlign.Center, nameFont, textPaint);
            if (!string.IsNullOrWhiteSpace(_fileName))
                canvas.DrawText(_fileName, X + Width / 2f, Y + 64f, SKTextAlign.Center, stereotypeFont, textPaint);

            DrawConnectionPoints(canvas, context);
        }

        public override Dictionary<string, object> GetProperties(bool includeCommon = true, bool includeNodeProperties = true)
        {
            var props = base.GetProperties(includeCommon, includeNodeProperties);
            props["ArtifactName"] = _artifactName;
            props["FileName"] = _fileName;
            return props;
        }

        public override void SetPropperties(IDictionary<string, object> properties, bool updateNodeProperties = true, bool applyToPublicSetters = true)
        {
            base.SetPropperties(properties, updateNodeProperties, applyToPublicSetters);
            if (properties.TryGetValue("ArtifactName", out var n) && n is string sn) ArtifactName = sn;
            if (properties.TryGetValue("FileName", out var f) && f is string sf) FileName = sf;
        }
    }
}