using System;
using System.Reflection;
using AppExtensionsLoader;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Tests for the BeepDM loader extension: a misconfigured or disposed loader must fail
    /// gracefully rather than throwing from a process-wide assembly-resolve handler.
    /// </summary>
    public class LoaderExtensionTests
    {
        [Fact]
        public void Constructor_WithNullLoader_DoesNotThrow()
        {
            using var extension = new BeepSkiaLoaderExtensions(null);

            Assert.NotNull(extension.CurrentDomain);
        }

        [Fact]
        public void LoadAllAssembly_WithoutLoader_ReportsFailure()
        {
            using var extension = new BeepSkiaLoaderExtensions(null);

            var result = extension.LoadAllAssembly();

            Assert.NotNull(result);
            Assert.Equal("Failed", result.Flag.ToString());
            Assert.False(string.IsNullOrWhiteSpace(result.Message));
        }

        [Fact]
        public void Scan_WithoutLoader_ReportsFailureInsteadOfThrowing()
        {
            using var extension = new BeepSkiaLoaderExtensions(null);

            var result = extension.Scan();

            Assert.NotNull(result);
            Assert.Equal("Failed", result.Flag.ToString());
        }

        [Fact]
        public void ScanAssembly_WithoutLoader_DoesNotThrow()
        {
            using var extension = new BeepSkiaLoaderExtensions(null);

            // The extension assembly contains SkiaComponent-derived types; scanning it without a
            // loader must simply skip registration.
            var result = extension.Scan(typeof(BeepSkiaLoaderExtensions).Assembly);

            Assert.NotNull(result);
        }

        [Fact]
        public void Dispose_IsIdempotent_AndLeavesInstanceUsable()
        {
            var extension = new BeepSkiaLoaderExtensions(null);

            extension.Dispose();
            extension.Dispose();

            var result = extension.LoadAllAssembly();
            Assert.NotNull(result);
        }

        [Fact]
        public void Dispose_UnsubscribesFromAssemblyResolve()
        {
            // Resolving an assembly that does not exist must behave normally (FileNotFoundException)
            // rather than being intercepted by a disposed extension's handler.
            var extension = new BeepSkiaLoaderExtensions(null);
            extension.Dispose();

            Assert.Throws<System.IO.FileNotFoundException>(() =>
                Assembly.Load("Beep.Skia.Does.Not.Exist, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"));
        }
    }
}
