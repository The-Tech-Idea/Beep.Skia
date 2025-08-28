using SkiaSharp;
using System;
using System.Collections.Generic;

namespace Beep.Skia.Components
{
    /// <summary>
    /// A Material Design 3.0 Context Menu component that appears on right-click.
    /// </summary>
    public class ContextMenu : Menu
    {
        private SKPoint _triggerPoint;
        private object _contextObject;

        /// <summary>
        /// Gets or sets the point where the context menu was triggered.
        /// </summary>
        public SKPoint TriggerPoint
        {
            get => _triggerPoint;
            private set => _triggerPoint = value;
        }

        /// <summary>
        /// Gets or sets the context object associated with this menu.
        /// </summary>
        public object ContextObject
        {
            get => _contextObject;
            set => _contextObject = value;
        }

        /// <summary>
        /// Occurs when the context menu is about to be shown.
        /// </summary>
        public event EventHandler<ContextMenuEventArgs> Showing;

        /// <summary>
        /// Initializes a new instance of the ContextMenu class.
        /// </summary>
        public ContextMenu()
        {
            Position = MenuPosition.BottomRight;
        }

        /// <summary>
        /// Shows the context menu at the specified position with the given context object.
        /// </summary>
        /// <param name="triggerPoint">The point where the context menu was triggered.</param>
        /// <param name="contextObject">The context object associated with the menu.</param>
        public void Show(SKPoint triggerPoint, object contextObject = null)
        {
            TriggerPoint = triggerPoint;
            ContextObject = contextObject;

            // Raise the Showing event to allow customization
            var args = new ContextMenuEventArgs(triggerPoint, contextObject);
            Showing?.Invoke(this, args);

            // Show the menu at the trigger point
            base.Show(triggerPoint);
        }

        /// <summary>
        /// Adds a context menu item with automatic icon assignment based on common actions.
        /// </summary>
        /// <param name="text">The menu item text.</param>
        /// <param name="action">The action to perform.</param>
        /// <param name="autoIcon">Whether to automatically assign an icon based on the text.</param>
        public void AddContextItem(string text, EventHandler action, bool autoIcon = true)
        {
            string icon = "";
            if (autoIcon)
            {
                icon = GetAutoIcon(text);
            }

            var item = new MenuItem(text, icon);
            item.Clicked += action;
            AddItem(item);
        }

        /// <summary>
        /// Adds standard context menu items for common operations.
        /// </summary>
        /// <param name="includeCopy">Whether to include copy operation.</param>
        /// <param name="includeCut">Whether to include cut operation.</param>
        /// <param name="includePaste">Whether to include paste operation.</param>
        /// <param name="includeDelete">Whether to include delete operation.</param>
        /// <param name="includeSelectAll">Whether to include select all operation.</param>
        public void AddStandardItems(
            bool includeCopy = true,
            bool includeCut = true,
            bool includePaste = true,
            bool includeDelete = true,
            bool includeSelectAll = false)
        {
            var items = new List<MenuItem>();

            if (includeCut)
                items.Add(new MenuItem("Cut", "✂", "Ctrl+X"));
            if (includeCopy)
                items.Add(new MenuItem("Copy", "📋", "Ctrl+C"));
            if (includePaste)
                items.Add(new MenuItem("Paste", "📄", "Ctrl+V"));

            if (includeCut || includeCopy || includePaste)
                items.Add(MenuItem.Separator());

            if (includeDelete)
                items.Add(new MenuItem("Delete", "🗑", "Del"));
            if (includeSelectAll)
                items.Add(new MenuItem("Select All", "☑", "Ctrl+A"));

            foreach (var item in items)
            {
                AddItem(item);
            }
        }

        private string GetAutoIcon(string text)
        {
            string lowerText = text.ToLower();

            if (lowerText.Contains("copy")) return "📋";
            if (lowerText.Contains("cut")) return "✂";
            if (lowerText.Contains("paste")) return "📄";
            if (lowerText.Contains("delete") || lowerText.Contains("remove")) return "🗑";
            if (lowerText.Contains("edit")) return "✏";
            if (lowerText.Contains("save")) return "💾";
            if (lowerText.Contains("open")) return "📂";
            if (lowerText.Contains("new")) return "➕";
            if (lowerText.Contains("close")) return "✖";
            if (lowerText.Contains("settings")) return "⚙";
            if (lowerText.Contains("help")) return "❓";
            if (lowerText.Contains("info")) return "ℹ";
            if (lowerText.Contains("refresh")) return "🔄";
            if (lowerText.Contains("search")) return "🔍";
            if (lowerText.Contains("zoom")) return "🔍";
            if (lowerText.Contains("print")) return "🖨";

            return ""; // No auto icon
        }

        /// <summary>
        /// Context menu event arguments.
        /// </summary>
        public class ContextMenuEventArgs : EventArgs
        {
            /// <summary>
            /// Gets the trigger point.
            /// </summary>
            public SKPoint TriggerPoint { get; }

            /// <summary>
            /// Gets the context object.
            /// </summary>
            public object ContextObject { get; }

            /// <summary>
            /// Initializes a new instance of the ContextMenuEventArgs class.
            /// </summary>
            /// <param name="triggerPoint">The trigger point.</param>
            /// <param name="contextObject">The context object.</param>
            public ContextMenuEventArgs(SKPoint triggerPoint, object contextObject)
            {
                TriggerPoint = triggerPoint;
                ContextObject = contextObject;
            }
        }
    }

    /// <summary>
    /// Extension methods for context menu functionality.
    /// </summary>
    public static class ContextMenuExtensions
    {
        /// <summary>
        /// Shows a context menu for the specified control at the given position.
        /// </summary>
        /// <param name="control">The control to show the context menu for.</param>
        /// <param name="menu">The context menu to show.</param>
        /// <param name="position">The position to show the menu at.</param>
        /// <param name="contextObject">The context object.</param>
        public static void ShowContextMenu(this MaterialControl control, ContextMenu menu, SKPoint position, object contextObject = null)
        {
            if (menu != null)
            {
                menu.Show(position, contextObject);
            }
        }

        /// <summary>
        /// Attaches a context menu to a control with automatic right-click handling.
        /// </summary>
        /// <param name="control">The control to attach the context menu to.</param>
        /// <param name="menu">The context menu to attach.</param>
        public static void AttachContextMenu(this MaterialControl control, ContextMenu menu)
        {
            if (control == null || menu == null) return;

            // Store the menu reference
            control.Tag = menu;

            // Override mouse down to handle right-click
            var originalMouseDown = control.GetType().GetMethod("OnMouseDown",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Since we can't easily override methods, we'll handle this through events
            // This is a simplified approach - in a real implementation, you'd want to
            // properly integrate with the control's mouse handling
        }
    }
}
