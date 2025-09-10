using SkiaSharp;
using SkiaSharp.Extended.Svg;
using System;
using System.IO;
using System.Reflection;

namespace Beep.Skia.Components
{
    /// <summary>
    /// A component that displays SVG images with support for scaling and positioning.
    /// </summary>
    public class SvgImage : MaterialControl
    {
        private SkiaSharp.Extended.Svg.SKSvg _svgDocument;
        private string _svgSource;
        private SKPicture _cachedPicture;
        private bool _isSvgLoaded;

        /// <summary>
        /// Gets or sets the SVG source. Can be a file path, embedded resource, or SVG content string.
        /// Supported formats:
        /// - File path: "C:\path\to\file.svg" or "relative\path\file.svg"
        /// - Embedded resource: "AssemblyName.ResourceName" (e.g., "Beep.Skia.svg.door.svg")
        /// - SVG content: Raw SVG XML string
        /// </summary>
        public string SvgSource
        {
            get => _svgSource;
            set
            {
                if (_svgSource != value)
                {
                    _svgSource = value;
                    LoadSvg();
                    // InvalidateVisual(); // Not needed - base class handles this
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the SVG should maintain its aspect ratio when scaled.
        /// </summary>
    public new bool MaintainAspectRatio { get; set; } = true;

        /// <summary>
        /// Gets or sets the scaling mode for the SVG.
        /// </summary>
        public SvgScaleMode ScaleMode { get; set; } = SvgScaleMode.Fit;

        /// <summary>
        /// Gets the original SVG size.
        /// </summary>
        public SKSize OriginalSize => _svgDocument?.Picture?.CullRect.Size ?? SKSize.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="SvgImage"/> class.
        /// </summary>
        public SvgImage()
        {
            Width = 100;
            Height = 100;
            Name = "SvgImage";
        }

        /// <summary>
        /// Loads an embedded SVG resource from the current assembly.
        /// </summary>
        /// <param name="resourceName">The name of the embedded resource.</param>
        public void LoadEmbeddedResource(string resourceName)
        {
            SvgSource = resourceName;
        }

        /// <summary>
        /// Loads an embedded SVG resource from the specified assembly.
        /// </summary>
        /// <param name="assemblyName">The name of the assembly containing the resource.</param>
        /// <param name="resourceName">The name of the embedded resource.</param>
        public void LoadEmbeddedResource(string assemblyName, string resourceName)
        {
            SvgSource = $"{assemblyName}.{resourceName}";
        }

        /// <summary>
        /// Loads an embedded SVG resource from the specified assembly.
        /// </summary>
        /// <param name="assembly">The assembly containing the resource.</param>
        /// <param name="resourceName">The name of the embedded resource.</param>
        public void LoadEmbeddedResource(Assembly assembly, string resourceName)
        {
            SvgSource = $"{assembly.GetName().Name}.{resourceName}";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SvgImage"/> class with an embedded resource from the current assembly.
        /// </summary>
        /// <param name="resourceName">The name of the embedded resource.</param>
        /// <param name="isEmbedded">True if the resource is embedded; false if it's a file path.</param>
        public SvgImage(string resourceName, bool isEmbedded) : this()
        {
            if (isEmbedded)
            {
                LoadEmbeddedResource(resourceName);
            }
            else
            {
                SvgSource = resourceName;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SvgImage"/> class with an embedded resource from the specified assembly.
        /// </summary>
        /// <param name="assemblyName">The name of the assembly containing the resource.</param>
        /// <param name="resourceName">The name of the embedded resource.</param>
        public SvgImage(string assemblyName, string resourceName) : this()
        {
            LoadEmbeddedResource(assemblyName, resourceName);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SvgImage"/> class with SVG content.
        /// </summary>
        /// <param name="svgContent">The SVG content as a string.</param>
        public SvgImage(string svgContent) : this()
        {
            SvgSource = svgContent;
        }

        /// <summary>
        /// Loads the SVG from the specified source.
        /// </summary>
        private void LoadSvg()
        {
            if (string.IsNullOrEmpty(_svgSource))
            {
                _svgDocument = null;
                _cachedPicture = null;
                _isSvgLoaded = false;
                return;
            }

            try
            {
                _svgDocument = new SkiaSharp.Extended.Svg.SKSvg();

                // Check if source is a file path
                if (File.Exists(_svgSource))
                {
                    using (var stream = File.OpenRead(_svgSource))
                    {
                        _svgDocument.Load(stream);
                    }
                }
                // Check if source is an embedded resource
                else if (TryLoadEmbeddedResource(_svgSource, out Stream resourceStream))
                {
                    using (resourceStream)
                    {
                        _svgDocument.Load(resourceStream);
                    }
                }
                else
                {
                    // Assume it's SVG content
                    using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(_svgSource)))
                    {
                        _svgDocument.Load(stream);
                    }
                }

                _cachedPicture = _svgDocument.Picture;
                _isSvgLoaded = true;
            }
            catch (Exception)
            {
                // Handle loading errors gracefully
                _svgDocument = null;
                _cachedPicture = null;
                _isSvgLoaded = false;
                // In a real application, you might want to log this error
            }
        }

        /// <summary>
        /// Draws the SVG content.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            if (!_isSvgLoaded || _cachedPicture == null)
                return;

            // Calculate the destination rectangle based on scale mode
            SKRect destRect = CalculateDestinationRect();

            // Absolute coordinate model: destRect already in canvas coordinates
            var matrix = SKMatrix.CreateTranslation(destRect.Left, destRect.Top);
            canvas.DrawPicture(_cachedPicture, in matrix);
        }

        /// <summary>
        /// Attempts to load an embedded resource from the specified assembly.
        /// </summary>
        /// <param name="resourcePath">The resource path in format "AssemblyName.ResourceName" or "ResourceName".</param>
        /// <param name="stream">The output stream containing the resource data.</param>
        /// <returns>True if the resource was found and loaded; otherwise, false.</returns>
        private bool TryLoadEmbeddedResource(string resourcePath, out Stream stream)
        {
            stream = null;
            Assembly assembly = null;

            try
            {
                string resourceName;

                // Check if the path contains dots (indicating a path-like format)
                if (resourcePath.Contains('.'))
                {
                    // Split the path to extract assembly name and resource name
                    var parts = resourcePath.Split('.');
                    if (parts.Length < 2)
                    {
                        return false;
                    }

                    // Assume the first part is the assembly name and the rest forms the resource name
                    string assemblyName = parts[0];
                    resourceName = string.Join(".", parts.Skip(1));

                    // Try to load the specified assembly
                    try
                    {
                        assembly = Assembly.Load(assemblyName);
                    }
                    catch
                    {
                        // If assembly loading fails, try to find it in the current app domain
                        foreach (var loadedAssembly in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            if (loadedAssembly.GetName().Name == assemblyName)
                            {
                                assembly = loadedAssembly;
                                break;
                            }
                        }

                        if (assembly == null)
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    // Use the current assembly
                    assembly = Assembly.GetExecutingAssembly();
                    resourceName = resourcePath;
                }

                // Get all embedded resources in the assembly
                string[] resourceNames = assembly.GetManifestResourceNames();

                // Look for a resource that ends with the specified name
                string foundResource = null;
                foreach (string name in resourceNames)
                {
                    if (name.EndsWith(resourceName, StringComparison.OrdinalIgnoreCase))
                    {
                        foundResource = name;
                        break;
                    }
                }

                if (foundResource == null)
                {
                    return false;
                }

                // Load the resource stream
                stream = assembly.GetManifestResourceStream(foundResource);
                return stream != null;
            }
            catch
            {
                stream = null;
                return false;
            }
        }
        private SKRect CalculateDestinationRect()
        {
            if (_cachedPicture == null)
                return SKRect.Empty;

            var sourceRect = _cachedPicture.CullRect;
            float destX = X;
            float destY = Y;
            float destWidth = Width;
            float destHeight = Height;

            if (MaintainAspectRatio)
            {
                float scaleX = destWidth / sourceRect.Width;
                float scaleY = destHeight / sourceRect.Height;
                float scale = Math.Min(scaleX, scaleY);

                switch (ScaleMode)
                {
                    case SvgScaleMode.Fit:
                        destWidth = sourceRect.Width * scale;
                        destHeight = sourceRect.Height * scale;
                        // Center the image
                        destX += (Width - destWidth) / 2;
                        destY += (Height - destHeight) / 2;
                        break;

                    case SvgScaleMode.Fill:
                        scale = Math.Max(scaleX, scaleY);
                        destWidth = sourceRect.Width * scale;
                        destHeight = sourceRect.Height * scale;
                        // Center the image
                        destX += (Width - destWidth) / 2;
                        destY += (Height - destHeight) / 2;
                        break;

                    case SvgScaleMode.Stretch:
                        // Use the full destination size
                        break;
                }
            }

            return new SKRect(destX, destY, destX + destWidth, destY + destHeight);
        }

        /// <summary>
        /// Disposes the SVG resources.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cachedPicture?.Dispose();
                // _svgDocument?.Dispose(); // SKSvg doesn't have Dispose method
            }
            base.Dispose(disposing);
        }
    }
}
