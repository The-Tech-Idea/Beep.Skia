using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Beep.Skia.WellLogs
{
    /// <summary>
    /// Parses LAS 2.0 and LAS 3.0 well log files into WellLogDocument.
    /// Supports both wrapped and non-wrapped data sections.
    /// </summary>
    public class LasFileParser
    {
        public WellLogDocument Document { get; private set; }
        public List<string> Warnings { get; } = new List<string>();
        public bool HasErrors { get; private set; }

        private enum Section { Unknown, Version, Well, Parameter, Curve, Other, Data }

        /// <summary>
        /// Parses a LAS file from a file path.
        /// </summary>
        public WellLogDocument ParseFile(string filePath)
        {
            var text = File.ReadAllText(filePath);
            return Parse(text);
        }

        /// <summary>
        /// Parses LAS content from a string.
        /// </summary>
        public WellLogDocument Parse(string lasContent)
        {
            Document = new WellLogDocument { Name = "Imported LAS" };
            Warnings.Clear();
            HasErrors = false;

            if (string.IsNullOrWhiteSpace(lasContent))
            {
                HasErrors = true;
                return Document;
            }

            var lines = NormalizeLines(lasContent);
            var sections = SplitSections(lines);

            // Process sections
            foreach (var section in sections)
            {
                try
                {
                    ProcessSection(section);
                }
                catch (Exception ex)
                {
                    Warnings.Add($"Error processing section {section.Key}: {ex.Message}");
                }
            }

            // After parsing, set depth axis from curve data
            var depthCurve = Document.Tracks.SelectMany(t => t.Curves)
                .FirstOrDefault(c => IsDepthCurve(c.Mnemonic));
            if (depthCurve != null && depthCurve.Samples.Count > 0)
            {
                Document.DepthAxis.MinimumDepth = depthCurve.Samples.Min(s => s.Depth);
                Document.DepthAxis.MaximumDepth = depthCurve.Samples.Max(s => s.Depth);
            }

            return Document;
        }

        private List<string> NormalizeLines(string content)
        {
            // Remove comment lines (lines starting with #)
            var lines = content.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n')
                .Where(l => !l.TrimStart().StartsWith("#"))
                .ToList();

            // Unwrap continued lines (LAS continuation: line ending with blank means next line is a continuation)
            // In practice, data lines are space-delimited and don't need unwrapping for values.
            // Parameter lines with long descriptions may wrap.

            return lines;
        }

        private Dictionary<Section, List<string>> SplitSections(List<string> lines)
        {
            var sections = new Dictionary<Section, List<string>>();
            Section current = Section.Unknown;
            var currentLines = new List<string>();

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("~"))
                {
                    // Flush previous section
                    if (currentLines.Count > 0)
                    {
                        if (!sections.ContainsKey(current)) sections[current] = new List<string>();
                        sections[current].AddRange(currentLines);
                        currentLines.Clear();
                    }
                    current = IdentifySection(trimmed);
                    continue;
                }
                if (trimmed.Length > 0)
                    currentLines.Add(trimmed);
            }

            // Flush last section
            if (currentLines.Count > 0)
            {
                if (!sections.ContainsKey(current)) sections[current] = new List<string>();
                sections[current].AddRange(currentLines);
            }

            return sections;
        }

        private Section IdentifySection(string sectionTag)
        {
            var tag = sectionTag.ToUpperInvariant().TrimStart('~').Trim();
            return tag switch
            {
                "V" or "VERSION" => Section.Version,
                "W" or "WELL" => Section.Well,
                "P" or "PARAMETER" => Section.Parameter,
                "C" or "CURVE" => Section.Curve,
                "A" or "ASCII" or "DATA" => Section.Data,
                _ => sectionTag.StartsWith("~A") || sectionTag.StartsWith("~D") ? Section.Data : Section.Other
            };
        }

        private void ProcessSection(KeyValuePair<Section, List<string>> section)
        {
            switch (section.Key)
            {
                case Section.Version:
                case Section.Well:
                    ParseHeaderSection(section.Value);
                    break;
                case Section.Curve:
                    ParseCurveSection(section.Value);
                    break;
                case Section.Data:
                    ParseDataSection(section.Value);
                    break;
                case Section.Parameter:
                    ParseHeaderSection(section.Value);
                    break;
            }
        }

        private void ParseHeaderSection(List<string> lines)
        {
            var headerProps = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in lines)
            {
                var (mnemonic, unit, value, _) = ParseParameterLine(line);
                if (!string.IsNullOrEmpty(mnemonic))
                {
                    headerProps[mnemonic.ToUpperInvariant()] = value ?? "";
                    // Track units for key parameters
                    if (mnemonic.Equals("STRT", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float strt))
                        Document.DepthAxis.MinimumDepth = strt;
                    if (mnemonic.Equals("STOP", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float stop))
                        Document.DepthAxis.MaximumDepth = stop;
                    if (mnemonic.Equals("STEP", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float step))
                    {
                        Document.DepthAxis.MajorStep = Math.Max(1, (float)Math.Round(step * 2));
                        Document.DepthAxis.MinorStep = step;
                    }
                    if (mnemonic.Equals("NULL", StringComparison.OrdinalIgnoreCase))
                        _nullValue = float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float nv) ? nv : -999.25f;
                }
            }

            if (headerProps.TryGetValue("WELL", out var wellName))
                Document.Name = wellName;
            if (headerProps.TryGetValue("SRVC", out var source))
                Document.SourceReference = source;
        }

        private float _nullValue = -999.25f;

        /// <summary>Curves in file order (used by the data section; tracks reorder them for display).</summary>
        private readonly List<WellLogCurve> _curvesInFileOrder = new List<WellLogCurve>();

        private void ParseCurveSection(List<string> lines)
        {
            _curvesInFileOrder.Clear();
            var curves = new List<WellLogCurve>();
            foreach (var line in lines)
            {
                var (mnemonic, unit, _, description) = ParseParameterLine(line);
                if (string.IsNullOrEmpty(mnemonic)) continue;

                if (curves.Count == 0 && IsDepthCurve(mnemonic))
                {
                    curves.Add(new WellLogCurve
                    {
                        Name = description ?? "Depth",
                        Mnemonic = mnemonic,
                        Unit = unit ?? "M",
                        IsVisible = false
                    });
                }
                else
                {
                    curves.Add(new WellLogCurve
                    {
                        Name = description ?? mnemonic,
                        Mnemonic = mnemonic,
                        Unit = unit ?? "",
                        IsVisible = true
                    });
                }
            }

            if (curves.Count > 0)
            {
                _curvesInFileOrder.AddRange(curves);

                // Auto-assign styles for common curves
                foreach (var c in curves)
                {
                    AutoStyleCurve(c);
                }

                foreach (var track in BuildTracks(curves))
                {
                    Document.Tracks.Add(track);
                }
            }
        }

        /// <summary>
        /// Groups parsed curves into tracks: a depth track plus one track per curve family
        /// (Gamma Ray, Resistivity, Porosity, Sonic, Caliper, Other).
        /// </summary>
        private static IEnumerable<WellLogTrack> BuildTracks(List<WellLogCurve> curves)
        {
            var tracks = new List<WellLogTrack>();

            var depthCurves = curves.Where(c => !c.IsVisible).ToList();
            if (depthCurves.Count > 0)
            {
                var depthTrack = new WellLogTrack
                {
                    Name = "Depth",
                    Role = WellLogTrackRole.Depth,
                    WidthRatio = 0.6f,
                    ShowGrid = false
                };
                depthTrack.Curves.AddRange(depthCurves);
                tracks.Add(depthTrack);
            }

            var groups = curves
                .Where(c => c.IsVisible)
                .GroupBy(c => ClassifyCurve(c.Mnemonic))
                .OrderBy(g => TrackOrder(g.Key))
                .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

            foreach (var group in groups)
            {
                var track = new WellLogTrack
                {
                    Name = group.Key,
                    Role = WellLogTrackRole.Curve,
                    WidthRatio = 1f
                };
                track.Curves.AddRange(group);
                tracks.Add(track);
            }

            return tracks;
        }

        /// <summary>
        /// Classifies a curve mnemonic into a display track family.
        /// </summary>
        private static string ClassifyCurve(string mnemonic)
        {
            var m = (mnemonic ?? string.Empty).ToUpperInvariant();
            if (m.Contains("GR") || m.Contains("GAMMA") || m.Contains("SGR") || m.Contains("CGR") || m == "SP" || m == "NGT") return "Gamma Ray";
            if (m.Contains("RES") || m.Contains("ILD") || m.Contains("LLD") || m.Contains("MSFL") || m.Contains("RXO") || m.StartsWith("RT")) return "Resistivity";
            if (m.Contains("RHOB") || m.Contains("NPHI") || m.Contains("DPHI") || m.Contains("TNPH") || m.Contains("PEF") || m.Contains("PHI")) return "Porosity";
            if (m.Contains("DT") || m.Contains("SONIC") || m == "AC") return "Sonic";
            if (m.Contains("CAL") || m == "BS") return "Caliper";
            return "Other";
        }

        private static int TrackOrder(string category) => category switch
        {
            "Gamma Ray" => 0,
            "Resistivity" => 1,
            "Porosity" => 2,
            "Sonic" => 3,
            "Caliper" => 4,
            _ => 5
        };

        private void AutoStyleCurve(WellLogCurve curve)
        {
            var m = curve.Mnemonic.ToUpperInvariant();
            if (m.Contains("GR") || m.Contains("GAMMA"))
            {
                curve.Style.LineColor = new SkiaSharp.SKColor(0x00, 0x96, 0x88);
                curve.Style.BrushKey = "gamma_ray";
            }
            else if (m.Contains("RES") || m.Contains("RESISTIVITY") || m.Contains("ILD") || m.Contains("LLD"))
            {
                curve.Style.LineColor = new SkiaSharp.SKColor(0xD3, 0x2F, 0x2F);
                curve.Style.StrokeWidth = 1f;
                curve.ScaleType = WellLogScaleType.Log10;
                curve.Style.BrushKey = "resistivity";
            }
            else if (m.Contains("DEN") || m.Contains("RHOB"))
            {
                curve.Style.LineColor = new SkiaSharp.SKColor(0x19, 0x76, 0xD2);
                curve.Style.BrushKey = "density";
            }
            else if (m.Contains("NEU") || m.Contains("NPHI") || m.Contains("NEUTRON"))
            {
                curve.Style.LineColor = new SkiaSharp.SKColor(0xF5, 0x7C, 0x00);
                curve.Style.BrushKey = "porosity";
            }
            else if (m.Contains("SP") || m.Contains("SPONTANEOUS"))
            {
                curve.Style.LineColor = new SkiaSharp.SKColor(0x7B, 0x1F, 0xA2);
                curve.Style.BrushKey = "sp";
            }
            else if (m.Contains("CAL") || m.Contains("CALIPER"))
            {
                curve.Style.LineColor = new SkiaSharp.SKColor(0x45, 0x55, 0xA0);
                curve.Style.StrokeWidth = 0.8f;
                curve.Style.BrushKey = "caliper";
                curve.Style.FillMode = WellLogFillMode.RightToCurve;
            }
            else if (m.Contains("SON") || m.Contains("SONIC") || m.Contains("DT"))
            {
                curve.Style.LineColor = new SkiaSharp.SKColor(0x00, 0x79, 0x6B);
                curve.Style.BrushKey = "sonic";
            }
            else if (m.Contains("PE") || m.Contains("PEF"))
            {
                curve.Style.LineColor = new SkiaSharp.SKColor(0xFF, 0x6D, 0x00);
                curve.Style.BrushKey = "pe";
            }
            else if (m.Contains("DEPT"))
            {
                curve.IsVisible = false;
            }
        }

        private void ParseDataSection(List<string> lines)
        {
            // Curves keep their file order even though they are grouped into display tracks.
            var curves = _curvesInFileOrder;
            if (curves.Count == 0) return;

            int curveCount = curves.Count;
            var depthCurveIdx = curves.TakeWhile(c => !IsDepthCurve(c.Mnemonic)).Count();
            // If no depth curve found, assume first curve is depth
            if (depthCurveIdx >= curveCount) depthCurveIdx = 0;

            foreach (var line in lines)
            {
                // Split by whitespace
                var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) continue;

                // Try to parse enough values
                float depth = 0;
                bool hasDepth = false;
                var values = new List<float>();

                for (int i = 0; i < Math.Min(parts.Length, curveCount); i++)
                {
                    if (float.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out float val))
                    {
                        values.Add(val);
                    }
                    else
                    {
                        values.Add(float.NaN);
                    }
                }

                if (values.Count > depthCurveIdx)
                {
                    depth = values[depthCurveIdx];
                    hasDepth = true;
                }

                if (!hasDepth) continue;

                // Add samples to each curve
                for (int c = 0; c < Math.Min(values.Count, curveCount); c++)
                {
                    float val = values[c];
                    if (Math.Abs(val - _nullValue) < 0.001f) val = float.NaN;

                    curves[c].Samples.Add(new WellLogCurveSample
                    {
                        Depth = depth,
                        Value = val
                    });
                }
            }

            // Calculate curve value ranges
            foreach (var curve in curves.Where(c => c.IsVisible))
            {
                var validSamples = curve.Samples.Where(s => !float.IsNaN(s.Value)).ToList();
                if (validSamples.Count > 0)
                {
                    curve.MinimumValue = validSamples.Min(s => s.Value);
                    curve.MaximumValue = validSamples.Max(s => s.Value);
                }
            }
        }

        private (string mnemonic, string unit, string value, string description) ParseParameterLine(string line)
        {
            // LAS parameter line format: MNEM.UNIT  VALUE : DESCRIPTION
            var match = Regex.Match(line,
                @"^([^.\s]+)\.(\S*)\s+([^:]*?)\s*(?::\s*(.*))?$");
            if (match.Success)
            {
                return (
                    mnemonic: match.Groups[1].Value,
                    unit: match.Groups[2].Value.Trim(),
                    value: match.Groups[3].Value.Trim(),
                    description: match.Groups[4].Success ? match.Groups[4].Value.Trim() : ""
                );
            }
            return (null, null, null, null);
        }

        private bool IsDepthCurve(string mnemonic)
        {
            if (string.IsNullOrEmpty(mnemonic)) return false;
            var m = mnemonic.ToUpperInvariant();
            return m == "DEPT" || m == "DEPTH" || m == "MD" || m == "TVD" || m == "TVDSS";
        }
    }
}
