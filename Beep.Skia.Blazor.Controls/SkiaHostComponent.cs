using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Runtime.Versioning;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using SkiaSharp;
using SkiaSharp.Views.Blazor;

namespace Beep.Skia.Blazor.Controls;

public sealed class CanvasPointerEvent
{
    public CanvasPointerSample[] Samples { get; set; } = Array.Empty<CanvasPointerSample>();
    public string Action { get; set; } = "";
    public float X { get; set; }
    public float Y { get; set; }
    public int Button { get; set; }
    public bool Control { get; set; }
    public bool Shift { get; set; }
    public bool Alt { get; set; }
    public float Delta { get; set; }
    public float DeltaX { get; set; }
    public int PointerId { get; set; }
    public string PointerType { get; set; } = "mouse";
    public float Pressure { get; set; }
    public float Scale { get; set; } = 1;
    public float Width { get; set; }
    public float Height { get; set; }
    public SKPoint Position => new(X, Y);
}
public sealed class CanvasPointerSample
{
    public float X { get; set; }
    public float Y { get; set; }
    /// <summary>The pointer's pressure at this sample (0 to 1); a pen's own, otherwise what the browser reports.</summary>
    public float Pressure { get; set; }
    public SKPoint Position => new(X, Y);
}

/// <summary>CSS-pixel browser canvas host for the shared scene.</summary>
[SupportedOSPlatform("browser")]
public class SkiaHostComponent : ComponentBase, IDisposable, IAsyncDisposable
{
    protected SKCanvasView _skiaView;
    private ElementReference _host;
    private IJSObjectReference _module;
    private DotNetObjectReference<SkiaHostComponent> _reference;
    private DrawingManager _subscribed;
    private bool _disposed;
    private bool _paintRequested, _painting, _repaintAfterPaint;
    private readonly SceneFrameCache _frameCache = new();
    [Inject] private IJSRuntime JS { get; set; }
    [Parameter] public string Width { get; set; } = "100%";
    [Parameter] public string Height { get; set; } = "600px";
    [Parameter] public string CssClass { get; set; } = "skia-host";
    [Parameter] public string Background { get; set; } = "#ffffff";
    [Parameter] public DrawingManager Manager { get; set; }
    /// <summary>The page shows this canvas with inverted lightness (a dark theme); pictures are drawn to keep their colours.</summary>
    [Parameter] public bool InvertedDisplay { get; set; }
    [Parameter] public bool HandleDefaultInteraction { get; set; } = true;
    [Parameter] public bool EnableViewportGestures { get; set; }
    [Parameter] public string CanvasLabel { get; set; } = "Drawing canvas";
    [Parameter] public string CanvasDescription { get; set; }
    [Parameter] public EventCallback<CanvasPointerEvent> OnPointer { get; set; }
    [Parameter] public Action<SKCanvas> PaintOverlay { get; set; }
    /// <summary>Conservative overlay ink in CSS pixels. Null or absent with an overlay requires full repaint.</summary>
    [Parameter] public Func<SKRect?> OverlayInkBounds { get; set; }
    /// <summary>Optional diagnostic callback: CPU milliseconds for clearing and DrawingManager.Draw,
    /// before the overlay. It does not measure GPU presentation. No clock is read when unset.</summary>
    [Parameter] public Action<double> ScenePaintMeasured { get; set; }
    [Parameter] public Action<SceneRenderProfile> ScenePhasesMeasured { get; set; }
    [Parameter] public bool CaptureRenderCounters { get; set; }
    /// <summary>Host calls into the view's Invalidate; excludes vendor resize/DPI requests.</summary>
    public long InvalidationDispatchCount { get; private set; }
    public long PaintCount { get; private set; }
    public DrawingManager DrawingManager { get; private set; }
    public SkiaComponentDescriptorCollection DesignTimeComponents { get; set; } = new();

    protected override void OnParametersSet()
    {
        DrawingManager = Manager ?? DrawingManager ?? new DrawingManager();
        if (DrawingManager.RenderingHelper.InvertedDisplay != InvertedDisplay) { DrawingManager.RenderingHelper.InvertedDisplay = InvertedDisplay; Invalidate(); }
        if (ReferenceEquals(_subscribed, DrawingManager)) return;
        _frameCache.Reset();
        Unsubscribe();
        _subscribed = DrawingManager;
        _subscribed.DrawSurface += HandleDraw;
        _subscribed.SelectionChanged += HandleDraw;
        _subscribed.HistoryManager.HistoryChanged += HandleDraw;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", CssClass);
        builder.AddAttribute(2, "style", $"width:{Width};height:{Height};position:relative;touch-action:none;outline-offset:-4px;");
        builder.AddAttribute(3, "tabindex", "0");
        builder.AddAttribute(4, "role", "region");
        builder.AddAttribute(5, "aria-label", CanvasLabel);
        builder.AddAttribute(6, "aria-describedby", CanvasDescription);
        builder.AddElementReferenceCapture(7, value => _host = value);
        builder.OpenComponent<SKCanvasView>(8);
        builder.AddAttribute(9, "OnPaintSurface", (Action<SKPaintSurfaceEventArgs>)OnPaintSurface);
        builder.AddAttribute(10, "IgnorePixelScaling", true);
        builder.AddAttribute(11, "style", "width:100%;height:100%;display:block;");
        builder.AddComponentReferenceCapture(12, value => _skiaView = (SKCanvasView)value);
        builder.CloseComponent();
        builder.CloseElement();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        _module = await JS.InvokeAsync<IJSObjectReference>("import", "./_content/Beep.Skia.Blazor.Controls/skiaHost.js");
        _reference = DotNetObjectReference.Create(this);
        await _module.InvokeVoidAsync("attach", _host, _reference, new { viewportGestures = EnableViewportGestures });
        Invalidate();
    }

    protected void OnPaintSurface(SKPaintSurfaceEventArgs args)
    {
        if (_disposed) return;
        _painting = true;
        try
        {
            var renderer = DrawingManager.RenderingHelper;
            var previous = renderer.FrameMeasured;
            try
            {
                renderer.FrameMeasured = ScenePhasesMeasured;
                renderer.DrawFrame(args.Surface, SKColor.Parse(Background), _frameCache, PaintOverlay, OverlayInkBounds, ScenePaintMeasured);
            }
            finally { renderer.FrameMeasured = previous; }
            if (CaptureRenderCounters) PaintCount++;
        }
        finally
        {
            _painting = _paintRequested = false;
            if (_repaintAfterPaint) { _repaintAfterPaint = false; _ = RequestPaintAfterCallback(); }
        }
    }
    private async Task RequestPaintAfterCallback()
    {
        // The vendor clears its own frame request after the synchronous callback
        // returns. Yield before requesting a paint raised by that callback.
        await Task.Yield();
        Invalidate();
    }

    [JSInvokable]
    public async Task HandlePointer(CanvasPointerEvent args)
    {
        if (_disposed || !float.IsFinite(args.X) || !float.IsFinite(args.Y) || !float.IsFinite(args.Delta) || !float.IsFinite(args.DeltaX)
            || !float.IsFinite(args.Scale) || !float.IsFinite(args.Width) || !float.IsFinite(args.Height)
            || !float.IsFinite(args.Pressure) || args.Pressure < 0 || args.Pressure > 1
            || args.Samples == null || args.Samples.Length > 256
            || args.Samples.Any(p => p == null || !float.IsFinite(p.X) || !float.IsFinite(p.Y) || !float.IsFinite(p.Pressure) || p.Pressure < 0 || p.Pressure > 1)) return;
        if (HandleDefaultInteraction)
        {
            var modifiers = SKKeyModifiers.None;
            if (args.Control) modifiers |= SKKeyModifiers.Control;
            if (args.Shift) modifiers |= SKKeyModifiers.Shift;
            if (args.Alt) modifiers |= SKKeyModifiers.Alt;
            switch (args.Action)
            {
                case "down": DrawingManager.HandleMouseDown(args.Position, modifiers, args.Button); break;
                case "move":
                    foreach (var sample in args.Samples) DrawingManager.HandleMouseMove(sample.Position, modifiers);
                    DrawingManager.HandleMouseMove(args.Position, modifiers); break;
                case "up": case "cancel": DrawingManager.HandleMouseUp(args.Position, modifiers, args.Button); break;
                case "wheel": DrawingManager.HandleMouseWheel(args.Position, -args.Delta); break;
            }
        }
        await OnPointer.InvokeAsync(args);
        Invalidate();
    }

    public void Invalidate()
    {
        if (_disposed || _skiaView == null) return;
        if (_painting) { _repaintAfterPaint = true; return; }
        if (_paintRequested) return;
        _paintRequested = true;
        try
        {
            _skiaView.Invalidate();
            if (CaptureRenderCounters) InvalidationDispatchCount++;
        }
        catch { _paintRequested = false; throw; }
    }
    private void HandleDraw(object sender, EventArgs args) => _ = InvokeAsync(Invalidate);

    public void LoadDesignTimeComponents()
    {
        // Only trusted application descriptors; uploaded documents require an allowlist.
        foreach (var desc in DesignTimeComponents)
        {
            var type = Type.GetType(desc.ComponentType, throwOnError: true);
            if (Activator.CreateInstance(type) is not SkiaComponent component)
                throw new InvalidOperationException($"Descriptor {desc.ComponentType} is not a Skia component.");
            component.X = desc.X; component.Y = desc.Y;
            component.Width = desc.Width; component.Height = desc.Height;
            component.Name = desc.Name ?? string.Empty;
            DrawingManager.AddComponent(component);
        }
        Invalidate();
    }

    private void Unsubscribe()
    {
        if (_subscribed == null) return;
        _subscribed.DrawSurface -= HandleDraw;
        _subscribed.SelectionChanged -= HandleDraw;
        _subscribed.HistoryManager.HistoryChanged -= HandleDraw;
        _subscribed = null;
    }
    public void Dispose() { _disposed = true; Unsubscribe(); _frameCache.Dispose(); }
    public async ValueTask DisposeAsync()
    {
        Dispose();
        if (_module != null)
        {
            try { await _module.InvokeVoidAsync("detach", _host); }
            catch (JSDisconnectedException) { }
            await _module.DisposeAsync(); _module = null;
        }
        _reference?.Dispose(); _reference = null;
    }
}
