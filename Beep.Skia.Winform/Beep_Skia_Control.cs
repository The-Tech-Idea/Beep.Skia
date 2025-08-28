
using SkiaSharp;

using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Vis;
using SkiaSharp.Views.Desktop;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;

using TheTechIdea.Beep.Vis.Modules;
using TheTechIdea.Beep.Logger;


#nullable enable

namespace Beep.Skia.Winform
{
    [AddinAttribute(Caption = "Skia Editor", Name = "Beep_Skia_Control", misc = "AI", addinType = AddinType.Control)]
    public partial class Beep_Skia_Control : UserControl
    {
        public Beep_Skia_Control()
        {
            InitializeComponent();
            _drawingManager = new DrawingManager();
            SkiaComponent c1 = new Beep.Skia.Components.Button("Circle Component") { X = 10, Y = 10 };
            SkiaComponent c2 = new Beep.Skia.Components.Button("Box Component") { X = 40, Y = 40 };

            // Create an SVG component (you can replace this with your SVG file path or content)
            SkiaComponent svgComponent = new Beep.Skia.Components.SvgImage(@"<svg width='50' height='50' xmlns='http://www.w3.org/2000/svg'><circle cx='25' cy='25' r='20' fill='blue' stroke='black' stroke-width='2'/></svg>")
            {
                X = 100,
                Y = 10,
                Width = 60,
                Height = 60,
                Name = "Sample SVG"
            };

            _drawingManager.AddComponent(c1);
            _drawingManager.AddComponent(c2);
            _drawingManager.AddComponent(svgComponent);
            // Subscribe to the events for handling mouse input
            skControl1.MouseMove += SkControl1_MouseMove;
            skControl1.MouseDown += SkControl1_MouseDown;
            skControl1.MouseUp += SkControl1_MouseUp;
            skControl1.Click += SkControl1_Click;
            skControl1.MouseDoubleClick += SkControl1_MouseDoubleClick;
            skControl1.DragEnter += SkControl1_DragEnter;
            skControl1.DragDrop += SkControl1_DragDrop;

            // Subscribe to the DrawSurface event for drawing additional content
            //_drawingManager.DrawSurface += DrawingManager_DrawSurface;
        }

      

        public string Description { get; set; } = "Skia Editor";
        public string ObjectName { get; set; }
        public string ObjectType { get; set; } = "UserControl";
        public IErrorsInfo ErrorObject { get; set; }
        public IDMLogger Logger { get; set; }
        public IDMEEditor DMEEditor { get; set; }
        public IPassedArgs Passedarg { get; set; }

        //IVisManager visManager;
        //BeepSKiaExtensions beepSKiaExtensions;
        IBranch RootAppBranch;
        IBranch branch;
        private DrawingManager _drawingManager;
        public void Run(IPassedArgs pPassedarg)
        {
            throw new NotImplementedException();
        }

        public void Run(params object[] args)
        {
            // Execute with parameters
        }

        public void SetConfig(IDMEEditor pbl, IDMLogger plogger, IUtil putil, string[] args, IPassedArgs e, IErrorsInfo per)
        {
            Passedarg = e;
            Logger = plogger;
            ErrorObject = per;
            DMEEditor = pbl;

            //visManager = (IVisManager)e.Objects.Where(c => c.Name == "VISUTIL").FirstOrDefault().obj;

            if (e.Objects.Where(c => c.Name == "Branch").Any())
            {
                branch = (IBranch)e.Objects.Where(c => c.Name == "Branch").FirstOrDefault().obj;
            }
            if (e.Objects.Where(c => c.Name == "RootAppBranch").Any())
            {
                RootAppBranch = (IBranch)e.Objects.Where(c => c.Name == "RootAppBranch").FirstOrDefault().obj;
            }
            skControl1.PaintSurface += SkControl1_PaintSurface;
            _drawingManager.DrawSurface += _drawingManager_DrawSurface;
            //beepSKiaExtensions = new BeepSKiaExtensions(DMEEditor, visManager, (ITree)visManager.Tree);

        }

        private void _drawingManager_DrawSurface(object? sender, ConnectionEventArgs e)
        {
           skControl1.Invalidate();
        }

        private void SkControl1_PaintSurface(object? sender, SKPaintSurfaceEventArgs e)
        {
            SKImageInfo info = e.Info;
            SKSurface surface = e.Surface;
            SKCanvas canvas = surface.Canvas;

            // Clear the canvas with a white background color
            canvas.Clear(SKColors.White);

            // Create a paint object for drawing
            using (var paint = new SKPaint())
            {
                _drawingManager.Draw(canvas);
                //// Set paint properties
                //paint.Style = SKPaintStyle.Fill;
                //paint.Color = SKColors.Blue;
                //paint.IsAntialias = true;

                //// Draw a circle on the canvas
                //float x = info.Width / 2;
                //float y = info.Height / 2;
                //float radius = Math.Min(info.Width, info.Height) / 4;
                //canvas.DrawCircle(x, y, radius, paint);
            }
        }
        #region "SKControl Events"
        private void SkControl1_MouseMove(object sender, MouseEventArgs e)
        {
            _drawingManager.HandleMouseMove(e.Location.ToSKPoint());
            skControl1.Invalidate();
        }

        private void SkControl1_MouseDown(object sender, MouseEventArgs e)
        {
            _drawingManager.HandleMouseDown(e.Location.ToSKPoint());
            skControl1.Invalidate();
        }

        private void SkControl1_MouseUp(object sender, MouseEventArgs e)
        {
            _drawingManager.HandleMouseUp(e.Location.ToSKPoint());
            skControl1.Invalidate();
        }

        private void DrawingManager_DrawSurface(object sender, SKSurface surface)
        {
            // Draw additional content on the canvas
        }

        private void skControl1_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            _drawingManager.Draw(e.Surface.Canvas);
        }
        private void SkControl1_DragDrop(object? sender, DragEventArgs e)
        {
           
        }

        private void SkControl1_DragEnter(object? sender, DragEventArgs e)
        {
           
        }

        private void SkControl1_MouseDoubleClick(object? sender, MouseEventArgs e)
        {
          
        }

        private void SkControl1_Click(object? sender, EventArgs e)
        {
          
        }
        #endregion "SKControl Events"
    }
}