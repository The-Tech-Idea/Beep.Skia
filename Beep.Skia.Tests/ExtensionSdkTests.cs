using System;
using System.Collections.Generic;
using System.Linq;
using Beep.Skia;
using Beep.Skia.Extensions;
using Beep.Skia.Model;
using SkiaSharp;
using Xunit;

namespace Beep.Skia.Tests
{
    /// <summary>
    /// Tests for the extension SDK: discovery, registration, validation, commands, and error capture.
    /// </summary>
    public class ExtensionSdkTests
    {
        public sealed class TestExtensionNode : SkiaComponent
        {
            public TestExtensionNode() { Width = 80; Height = 40; Name = "TestExtensionNode"; }
            protected override void DrawContent(SKCanvas canvas, DrawingContext context) { }
        }

        public sealed class SecondTestNode : SkiaComponent
        {
            public SecondTestNode() { Width = 80; Height = 40; Name = "SecondTestNode"; }
            protected override void DrawContent(SKCanvas canvas, DrawingContext context) { }
        }

        public sealed class TestExtension : ISkiaExtension
        {
            public string Id => "test.extension";
            public string Name => "Test Extension";
            public string Version => "1.2.3";
            public string Description => "Extension used by unit tests";

            public IEnumerable<Type> GetComponentTypes()
            {
                yield return typeof(TestExtensionNode);
                yield return typeof(string); // invalid — must be rejected
            }

            public void Initialize(ISkiaExtensionContext context)
            {
                context.RegisterComponent(typeof(SecondTestNode), "Testing", "Second Node");
                context.RegisterCommand("test.ping", target => CommandTarget = target);
                context.Log("initialized");
            }

            public static SkiaComponent CommandTarget { get; private set; }
        }

        public sealed class ThrowingExtension : ISkiaExtension
        {
            public string Id => "test.throwing";
            public string Name => "Throwing Extension";
            public string Version => "0.1";
            public string Description => "Throws during initialization";
            public IEnumerable<Type> GetComponentTypes() => Enumerable.Empty<Type>();
            public void Initialize(ISkiaExtensionContext context) => throw new InvalidOperationException("boom");
        }

        [Fact]
        public void LoadFromAssemblies_DiscoversAndRegistersExtension()
        {
            var host = new SkiaExtensionHost();
            var count = host.LoadFromAssemblies(new[] { typeof(TestExtension).Assembly });

            Assert.True(count >= 2);

            var extension = host.Extensions.Single(e => e.Id == "test.extension");
            Assert.True(extension.Success);
            Assert.Equal("Test Extension", extension.Name);
            Assert.Equal("1.2.3", extension.Version);
            Assert.Contains(typeof(TestExtensionNode), extension.ComponentTypes);
        }

        [Fact]
        public void InvalidComponentTypes_AreRejectedWithLog()
        {
            var host = new SkiaExtensionHost();
            host.LoadFromAssemblies(new[] { typeof(TestExtension).Assembly });

            var extension = host.Extensions.Single(e => e.Id == "test.extension");
            Assert.DoesNotContain(typeof(string), extension.ComponentTypes);
            Assert.Contains(host.Log, l => l.Contains("not a concrete SkiaComponent"));
        }

        [Fact]
        public void Initialize_RegistersComponentsWithCustomCategoryAndCommands()
        {
            var host = new SkiaExtensionHost();
            host.LoadFromAssemblies(new[] { typeof(TestExtension).Assembly });

            var second = host.Components.Single(c => c.ComponentType == typeof(SecondTestNode));
            Assert.Equal("Testing", second.Category);
            Assert.Equal("Second Node", second.DisplayName);

            var target = new TestExtensionNode();
            Assert.True(host.InvokeCommand("test.ping", target));
            Assert.Same(target, TestExtension.CommandTarget);
        }

        [Fact]
        public void FailingExtension_IsCapturedWithoutThrowing()
        {
            var host = new SkiaExtensionHost();
            host.LoadFromAssemblies(new[] { typeof(ThrowingExtension).Assembly });

            var failing = host.Extensions.Single(e => e.Id == "test.throwing");
            Assert.False(failing.Success);
            Assert.Contains(failing.Errors, e => e.Contains("boom"));
        }

        [Fact]
        public void DuplicateExtensionId_IsLoadedOnce()
        {
            var host = new SkiaExtensionHost();
            host.LoadFromAssemblies(new[] { typeof(TestExtension).Assembly });
            host.LoadFromAssemblies(new[] { typeof(TestExtension).Assembly });

            Assert.Single(host.Extensions.Where(e => e.Id == "test.extension"));
        }

        [Fact]
        public void CreateComponent_UsesDescriptorTypeName()
        {
            var host = new SkiaExtensionHost();
            host.LoadFromAssemblies(new[] { typeof(TestExtension).Assembly });

            var descriptor = host.Components.Single(c => c.ComponentType == typeof(TestExtensionNode));
            var instance = host.CreateComponent(descriptor.AssemblyQualifiedName);

            Assert.IsType<TestExtensionNode>(instance);
        }

        [Fact]
        public void LoadFromDirectory_MissingDirectory_ReturnsZero()
        {
            var host = new SkiaExtensionHost();
            var count = host.LoadFromDirectory(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "beep_ext_missing_" + Guid.NewGuid().ToString("N")));

            Assert.Equal(0, count);
            Assert.Contains(host.Log, l => l.Contains("Extension directory not found"));
        }

        [Fact]
        public void Clear_ResetsHostState()
        {
            var host = new SkiaExtensionHost();
            host.LoadFromAssemblies(new[] { typeof(TestExtension).Assembly });

            host.Clear();

            Assert.Empty(host.Extensions);
            Assert.Empty(host.Components);
            Assert.Empty(host.Commands);
            Assert.Empty(host.Log);
        }
    }
}
