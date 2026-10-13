using SkiaSharp;

namespace Beep.Skia;

/// <summary>
/// What a retained component's drawing depends on: its content version (bumped by every change to what it draws) and the
/// host-visible geometry and presentation the renderer applies to it. Two equal keys draw the same pixels.
/// </summary>
public readonly record struct RetainedVisualKey(long Version, float X, float Y, float Width, float Height, SKRect? Clip, bool Visible, float Opacity)
{
    /// <summary>The key of <paramref name="component"/> at content version <paramref name="version"/>.</summary>
    public static RetainedVisualKey Of(SkiaComponent component, long version) => new(version, component.X, component.Y,
        component.Width, component.Height, component.ClipBounds, component.IsVisible, component.Opacity);
}

/// <summary>
/// A component the frame cache repaints by area, as it does sketch shapes: it says when its drawing changed
/// (<see cref="VisualKey"/>) and where it draws (<see cref="TryGetInkBounds"/>). Components without it make every frame
/// a full repaint.
/// </summary>
public interface IRetainedVisual
{
    /// <summary>Changes whenever anything that affects the drawing changes.</summary>
    RetainedVisualKey VisualKey { get; }

    /// <summary>Conservative world-space bounds of everything drawn, as <see cref="Components.SketchShape.TryGetInkBounds"/>
    /// defines them; false when the footprint is unknown, which makes the frame a full repaint.</summary>
    bool TryGetInkBounds(out SKRect bounds);
}
