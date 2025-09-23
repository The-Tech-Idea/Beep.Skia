using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Beep.Skia.Model;

namespace Beep.Skia.Components
{
    /// <summary>
    /// Generic property editor for any SkiaComponent, providing a comprehensive interface
    /// for editing component properties, configuration, and display settings.
    /// </summary>
    public class ComponentPropertyEditor : SkiaComponent
    {
        // Hide this infrastructure component from the palette/toolbox
        public override bool ShowInPalette { get; set; } = false;
        #region Private Fields
        private SkiaComponent _selectedComponent;
        private Beep.Skia.Model.IConnectionLine _selectedLine;
        private readonly List<SkiaComponent> _childComponents = new List<SkiaComponent>();
        private Label _titleLabel;
        private Dictionary<string, SkiaComponent> _propertyControls = new Dictionary<string, SkiaComponent>();
        private Button _saveButton;
        private Button _cancelButton;
        private bool _isDirty = false;
        private float _currentY = 10; // Current Y position for layout
        #endregion

        #region Events
        /// <summary>
        /// Event raised when properties are saved.
        /// </summary>
        public event EventHandler<ComponentPropertiesSavedEventArgs> PropertiesSaved;

        /// <summary>
        /// Event raised when editing is cancelled.
        /// </summary>
        public event EventHandler<ComponentPropertiesCancelledEventArgs> PropertiesCancelled;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the currently selected component for editing.
        /// </summary>
        public SkiaComponent SelectedComponent
        {
            get => _selectedComponent;
            set
            {
                if (_selectedComponent != value)
                {
                    _selectedComponent = value;
                    // clear line selection when component changes
                    _selectedLine = null;
                    OnSelectedComponentChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the currently selected line for editing.
        /// </summary>
        public Beep.Skia.Model.IConnectionLine SelectedLine
        {
            get => _selectedLine;
            set
            {
                if (_selectedLine != value)
                {
                    _selectedLine = value;
                    // clear component selection when line changes
                    _selectedComponent = null;
                    OnSelectedLineChanged();
                }
            }
        }

        /// <summary>
        /// Gets whether the current properties have unsaved changes.
        /// </summary>
        public bool IsDirty => _isDirty;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the ComponentPropertyEditor class.
        /// </summary>
        public ComponentPropertyEditor()
        {
            InitializeComponents();
        }
        #endregion

        #region Initialization
        /// <summary>
        /// Initializes the component structure.
        /// </summary>
        private void InitializeComponents()
        {
            Width = 400;
            Height = 600;

            // Create title label
            _titleLabel = new Label
            {
                Text = "Component Properties",
                FontSize = 16,
                X = 10,
                Y = 10,
                Width = this.Width - 20,
                Height = 30
            };
            _childComponents.Add(_titleLabel);

            // Create buttons
            _saveButton = new Button
            {
                Text = "Save",
                Width = 80,
                Height = 32
            };
            _saveButton.Clicked += SaveButton_Clicked;

            _cancelButton = new Button
            {
                Text = "Cancel",
                Width = 80,
                Height = 32
            };
            _cancelButton.Clicked += CancelButton_Clicked;

            _childComponents.Add(_saveButton);
            _childComponents.Add(_cancelButton);
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Handles the selected component changed event.
        /// </summary>
        private void OnSelectedComponentChanged()
        {
            if (_selectedComponent == null)
            {
                _titleLabel.Text = "Component Properties";
                ClearPropertyControls();
                return;
            }

            _titleLabel.Text = $"{_selectedComponent.Name ?? _selectedComponent.GetType().Name} Properties";
            BuildPropertyControls();
            _isDirty = false;
            UpdateLayout();
        }

        /// <summary>
        /// Handles the selected line changed event.
        /// </summary>
        private void OnSelectedLineChanged()
        {
            if (_selectedLine == null)
            {
                _titleLabel.Text = "Component Properties";
                ClearPropertyControls();
                return;
            }

            _titleLabel.Text = "Line Properties";
            BuildPropertyControls();
            _isDirty = false;
            UpdateLayout();
        }

        /// <summary>
        /// Handles the save button click event.
        /// </summary>
        private void SaveButton_Clicked(object sender, EventArgs e)
        {
            SaveProperties();
        }

        /// <summary>
        /// Handles the cancel button click event.
        /// </summary>
        private void CancelButton_Clicked(object sender, EventArgs e)
        {
            CancelEditing();
        }
        #endregion

        #region Layout Management
        /// <summary>
        /// Updates the layout of child components.
        /// </summary>
        private void UpdateLayout()
        {
            _currentY = 50; // Start below title

            foreach (var component in _childComponents.Where(c => c != _titleLabel && c != _saveButton && c != _cancelButton))
            {
                component.X = 10;
                component.Y = _currentY;
                component.Width = this.Width - 20;
                _currentY += component.Height + 10;
            }

            // Position buttons at bottom
            _saveButton.X = this.Width - 180;
            _saveButton.Y = this.Height - 50;

            _cancelButton.X = this.Width - 90;
            _cancelButton.Y = this.Height - 50;
        }

        /// <summary>
        /// Clears all property controls.
        /// </summary>
        private void ClearPropertyControls()
        {
            var controlsToRemove = _childComponents.Where(c => c != _titleLabel && c != _saveButton && c != _cancelButton).ToList();
            foreach (var control in controlsToRemove)
            {
                _childComponents.Remove(control);
            }
            _propertyControls.Clear();
            _currentY = 50;
        }
        #endregion

        #region Property Control Building
        /// <summary>
        /// Builds the property controls for the selected component.
        /// </summary>
        private void BuildPropertyControls()
        {
            ClearPropertyControls();

            // If a line is selected, build line property controls
            if (_selectedLine != null)
            {
                AddSectionHeader("Line Properties");
                AddLinePropertyControls();
                UpdateLayout();
                return;
            }

            if (_selectedComponent == null) return;

            // Add common properties section
            AddSectionHeader("Common Properties");
            AddCommonPropertyControls();

            // Add component-specific properties section
            AddSectionHeader("Component Properties");
            AddComponentSpecificPropertyControls();

            UpdateLayout();
        }

        /// <summary>
        /// Adds a section header to the property editor.
        /// </summary>
        /// <param name="title">The section title.</param>
        private void AddSectionHeader(string title)
        {
            var header = new Label
            {
                Text = title,
                FontSize = 14,
                X = 10,
                Y = _currentY,
                Width = this.Width - 20,
                Height = 25
            };
            _childComponents.Add(header);
            _currentY += 35;
        }

        /// <summary>
        /// Adds common property controls applicable to all components.
        /// </summary>
        private void AddCommonPropertyControls()
        {
            // Component Name
            AddTextProperty("Name", _selectedComponent.Name, value =>
            {
                _selectedComponent.Name = value;
                _isDirty = true;
            });

            // Position X
            AddNumericProperty("X", _selectedComponent.X, value =>
            {
                _selectedComponent.X = value;
                _isDirty = true;
            });

            // Position Y
            AddNumericProperty("Y", _selectedComponent.Y, value =>
            {
                _selectedComponent.Y = value;
                _isDirty = true;
            });

            // Width
            AddNumericProperty("Width", _selectedComponent.Width, value =>
            {
                _selectedComponent.Width = value;
                _isDirty = true;
            });

            // Height
            AddNumericProperty("Height", _selectedComponent.Height, value =>
            {
                _selectedComponent.Height = value;
                _isDirty = true;
            });

            // Display Text
            AddTextProperty("Display Text", _selectedComponent.DisplayText, value =>
            {
                _selectedComponent.DisplayText = value;
                _isDirty = true;
            });

            // Show Display Text
            AddBooleanProperty("Show Text", _selectedComponent.ShowDisplayText, value =>
            {
                _selectedComponent.ShowDisplayText = value;
                _isDirty = true;
            });

            // Text Position
            AddEnumProperty("Text Position", _selectedComponent.TextPosition, typeof(TextPosition), value =>
            {
                _selectedComponent.TextPosition = (TextPosition)value;
                _isDirty = true;
            });

            // Text Font Size
            AddNumericProperty("Text Size", _selectedComponent.TextFontSize, value =>
            {
                _selectedComponent.TextFontSize = value;
                _isDirty = true;
            });

            // Is Visible
            AddBooleanProperty("Visible", _selectedComponent.IsVisible, value =>
            {
                _selectedComponent.IsVisible = value;
                _isDirty = true;
            });

            // Is Enabled
            AddBooleanProperty("Enabled", _selectedComponent.IsEnabled, value =>
            {
                _selectedComponent.IsEnabled = value;
                _isDirty = true;
            });
        }

        /// <summary>
        /// Adds component-specific property controls.
        /// </summary>
        private void AddComponentSpecificPropertyControls()
        {
            // Add properties based on component type using reflection
            var componentType = _selectedComponent.GetType();

            // Check for ETL components
            if (componentType.Namespace == "Beep.Skia.ETL")
            {
                AddETLProperties(_selectedComponent);
            }
            // Check for UML components
            else if (componentType.Namespace == "Beep.Skia.UML")
            {
                AddUMLProperties(_selectedComponent);
            }
            // Check for AutomationNode
            else if (_selectedComponent is AutomationNode automationNode)
            {
                AddAutomationNodeProperties(automationNode);
            }
            // Flowchart nodes
            else if (componentType.Namespace == "Beep.Skia.Flowchart")
            {
                AddFlowchartProperties(_selectedComponent);
            }
        }

        /// <summary>
        /// Adds properties specific to AutomationNode.
        /// </summary>
        private void AddAutomationNodeProperties(AutomationNode node)
        {
            AddTextProperty("Description", node.Description, null, true); // Read-only

            AddTextProperty("Status", node.Status.ToString(), null, true);
            AddTextProperty("Node Type", node.NodeType.ToString(), null, true);
        }

        /// <summary>
        /// Adds properties for ETL components using reflection.
        /// </summary>
        private void AddETLProperties(SkiaComponent component)
        {
            // Use reflection to get ETL-specific properties
            var titleProperty = component.GetType().GetProperty("Title");
            if (titleProperty != null)
            {
                var currentValue = titleProperty.GetValue(component) as string ?? "";
                AddTextProperty("Title", currentValue, value => titleProperty.SetValue(component, value));
            }

            var subtitleProperty = component.GetType().GetProperty("Subtitle");
            if (subtitleProperty != null)
            {
                var currentValue = subtitleProperty.GetValue(component) as string ?? "";
                AddTextProperty("Subtitle", currentValue, value => subtitleProperty.SetValue(component, value));
            }
        }

        /// <summary>
        /// Adds properties for UML components using reflection.
        /// </summary>
        private void AddUMLProperties(SkiaComponent component)
        {
            // Use reflection to get UML-specific properties
            var stereotypeProperty = component.GetType().GetProperty("Stereotype");
            if (stereotypeProperty != null)
            {
                var currentValue = stereotypeProperty.GetValue(component) as string ?? "";
                AddTextProperty("Stereotype", currentValue, value => stereotypeProperty.SetValue(component, value));
            }
        }

        /// <summary>
        /// Adds properties for Flowchart components (port counts and placement flags).
        /// </summary>
        private void AddFlowchartProperties(SkiaComponent component)
        {
            var type = component.GetType();

            // Common counts on FlowchartControl: InPortCount / OutPortCount
            var inCountProp = type.GetProperty("InPortCount");
            if (inCountProp != null)
            {
                AddNumericProperty("In Ports", Convert.ToSingle(inCountProp.GetValue(component) ?? 0), v =>
                {
                    inCountProp.SetValue(component, (int)MathF.Round(v));
                    _isDirty = true;
                });
            }

            var outCountProp = type.GetProperty("OutPortCount");
            if (outCountProp != null)
            {
                AddNumericProperty("Out Ports", Convert.ToSingle(outCountProp.GetValue(component) ?? 0), v =>
                {
                    outCountProp.SetValue(component, (int)MathF.Round(v));
                    _isDirty = true;
                });
            }

            // Optional flags per node
            var showTopBottom = type.GetProperty("ShowTopBottomPorts");
            if (showTopBottom != null)
            {
                AddBooleanProperty("Show Top/Bottom Ports", (bool)(showTopBottom.GetValue(component) ?? false), v =>
                {
                    showTopBottom.SetValue(component, v);
                    _isDirty = true;
                });
            }

            var outOnTop = type.GetProperty("OutPortsOnTop");
            if (outOnTop != null)
            {
                AddBooleanProperty("Out Ports On Top", (bool)(outOnTop.GetValue(component) ?? false), v =>
                {
                    outOnTop.SetValue(component, v);
                    _isDirty = true;
                });
            }
        }

        /// <summary>
        /// Adds property controls for the selected connection line.
        /// </summary>
        private void AddLinePropertyControls()
        {
            if (_selectedLine == null) return;

            // Routing & stroke
            AddEnumProperty("Routing Mode", _selectedLine.RoutingMode, typeof(LineRoutingMode), v => { _selectedLine.RoutingMode = (LineRoutingMode)v; _isDirty = true; });
            AddNumericProperty("Stroke Width", _selectedLine.Paint?.StrokeWidth ?? 2f, v => { if (_selectedLine.Paint != null) _selectedLine.Paint.StrokeWidth = v; _isDirty = true; });
            AddColorProperty("Line Color", _selectedLine.LineColor, c => { _selectedLine.LineColor = c; _isDirty = true; });
            AddTextProperty("Dash Pattern", _selectedLine.DashPattern != null ? string.Join(",", _selectedLine.DashPattern) : string.Empty, s => { _selectedLine.DashPattern = ParseDashPattern(s); _isDirty = true; });

            // Arrows
            AddBooleanProperty("Show Start Arrow", _selectedLine.ShowStartArrow, v => { _selectedLine.ShowStartArrow = v; _isDirty = true; });
            AddBooleanProperty("Show End Arrow", _selectedLine.ShowEndArrow, v => { _selectedLine.ShowEndArrow = v; _isDirty = true; });
            AddNumericProperty("Arrow Size", _selectedLine.ArrowSize, v => { _selectedLine.ArrowSize = v; _isDirty = true; });

            // ERD multiplicity markers
            AddEnumProperty("Start Multiplicity", _selectedLine.StartMultiplicity, typeof(ERDMultiplicity), v => { _selectedLine.StartMultiplicity = (ERDMultiplicity)v; _isDirty = true; });
            AddEnumProperty("End Multiplicity", _selectedLine.EndMultiplicity, typeof(ERDMultiplicity), v => { _selectedLine.EndMultiplicity = (ERDMultiplicity)v; _isDirty = true; });

            // Labels
            AddTextProperty("Label 1", _selectedLine.Label1, s => { _selectedLine.Label1 = s; _isDirty = true; });
            AddEnumProperty("Label 1 Placement", _selectedLine.Label1Placement, typeof(LabelPlacement), v => { _selectedLine.Label1Placement = (LabelPlacement)v; _isDirty = true; });
            AddTextProperty("Label 2", _selectedLine.Label2, s => { _selectedLine.Label2 = s; _isDirty = true; });
            AddEnumProperty("Label 2 Placement", _selectedLine.Label2Placement, typeof(LabelPlacement), v => { _selectedLine.Label2Placement = (LabelPlacement)v; _isDirty = true; });
            AddTextProperty("Label 3", _selectedLine.Label3, s => { _selectedLine.Label3 = s; _isDirty = true; });
            AddEnumProperty("Label 3 Placement", _selectedLine.Label3Placement, typeof(LabelPlacement), v => { _selectedLine.Label3Placement = (LabelPlacement)v; _isDirty = true; });
            if (_selectedLine is Beep.Skia.ConnectionLine concrete)
            {
                AddTextProperty("Data Type Label", concrete.DataTypeLabel, s => { concrete.DataTypeLabel = s; _isDirty = true; });
            }
            AddEnumProperty("Data Label Placement", _selectedLine.DataLabelPlacement, typeof(LabelPlacement), v => { _selectedLine.DataLabelPlacement = (LabelPlacement)v; _isDirty = true; });

            // Data flow & animation
            AddEnumProperty("Flow Direction", _selectedLine.FlowDirection, typeof(DataFlowDirection), v => { _selectedLine.FlowDirection = (DataFlowDirection)v; _isDirty = true; });
            AddBooleanProperty("Line Is Animated", _selectedLine.IsAnimated, v => { _selectedLine.IsAnimated = v; if (v) _selectedLine.StartAnimation(); else _selectedLine.StopAnimation(); _isDirty = true; });
            if (_selectedLine is Beep.Skia.ConnectionLine cl)
            {
                AddBooleanProperty("Data Flow Animated", cl.IsDataFlowAnimated, v => { cl.IsDataFlowAnimated = v; _isDirty = true; });
                AddNumericProperty("Data Flow Speed", cl.DataFlowSpeed, v => { cl.DataFlowSpeed = v; _isDirty = true; });
                AddNumericProperty("Particle Size", cl.DataFlowParticleSize, v => { cl.DataFlowParticleSize = v; _isDirty = true; });
                AddColorProperty("Data Flow Color", cl.DataFlowColor, c => { cl.DataFlowColor = c; _isDirty = true; });

                // Optional: ERD crow's-foot styling controls (concrete-only)
                AddNumericProperty("Crow's Foot Spread (deg)", cl.CrowFootSpreadDegrees, v => { cl.CrowFootSpreadDegrees = v; _isDirty = true; });
                AddNumericProperty("Crow's Foot Length", cl.CrowFootLength, v => { cl.CrowFootLength = v; _isDirty = true; });
            }

            // Status indicator
            AddBooleanProperty("Show Status", _selectedLine.ShowStatusIndicator, v => { _selectedLine.ShowStatusIndicator = v; _isDirty = true; });
            AddEnumProperty("Status", _selectedLine.Status, typeof(LineStatus), v => { _selectedLine.Status = (LineStatus)v; _isDirty = true; });
            AddColorProperty("Status Color", _selectedLine.StatusColor, c => { _selectedLine.StatusColor = c; _isDirty = true; });
        }
        #endregion

        #region Property Control Helpers
        /// <summary>
        /// Adds a text property control.
        /// </summary>
        private void AddTextProperty(string label, string value, Action<string> onChanged, bool readOnly = false)
        {
            var textBox = new TextBox
            {
                Text = value ?? "",
                Width = this.Width - 120,
                Height = 25,
                IsReadOnly = readOnly
            };

            if (!readOnly && onChanged != null)
            {
                textBox.TextChanged += (s, e) => onChanged(textBox.Text);
            }

            var labelControl = new Label
            {
                Text = label + ":",
                Width = 100,
                Height = 25,
                X = 10,
                Y = _currentY
            };

            textBox.X = 120;
            textBox.Y = _currentY;

            _childComponents.Add(labelControl);
            _childComponents.Add(textBox);
            _propertyControls[label] = textBox;
            _currentY += 35;
        }

        /// <summary>
        /// Adds a numeric property control.
        /// </summary>
        private void AddNumericProperty(string label, float value, Action<float> onChanged)
        {
            var textBox = new TextBox
            {
                Text = value.ToString("F2"),
                Width = this.Width - 120,
                Height = 25
            };

            textBox.TextChanged += (s, e) =>
            {
                if (float.TryParse(textBox.Text, out float newValue))
                {
                    onChanged(newValue);
                }
            };

            var labelControl = new Label
            {
                Text = label + ":",
                Width = 100,
                Height = 25,
                X = 10,
                Y = _currentY
            };

            textBox.X = 120;
            textBox.Y = _currentY;

            _childComponents.Add(labelControl);
            _childComponents.Add(textBox);
            _propertyControls[label] = textBox;
            _currentY += 35;
        }

        /// <summary>
        /// Adds a boolean property control.
        /// </summary>
        private void AddBooleanProperty(string label, bool value, Action<bool> onChanged)
        {
            var textBox = new TextBox
            {
                Text = value.ToString(),
                Width = this.Width - 120,
                Height = 25
            };

            textBox.TextChanged += (s, e) =>
            {
                if (bool.TryParse(textBox.Text, out bool newValue))
                {
                    onChanged(newValue);
                }
            };

            var labelControl = new Label
            {
                Text = label + ":",
                Width = 100,
                Height = 25,
                X = 10,
                Y = _currentY
            };

            textBox.X = 120;
            textBox.Y = _currentY;

            _childComponents.Add(labelControl);
            _childComponents.Add(textBox);
            _propertyControls[label] = textBox;
            _currentY += 35;
        }

        /// <summary>
        /// Adds an enum property control.
        /// </summary>
        private void AddEnumProperty(string label, object value, Type enumType, Action<object> onChanged)
        {
            var textBox = new TextBox
            {
                Text = value.ToString(),
                Width = this.Width - 120,
                Height = 25
            };

            textBox.TextChanged += (s, e) =>
            {
                if (Enum.TryParse(enumType, textBox.Text, out var newValue))
                {
                    onChanged(newValue);
                }
            };

            var labelControl = new Label
            {
                Text = label + ":",
                Width = 100,
                Height = 25,
                X = 10,
                Y = _currentY
            };

            textBox.X = 120;
            textBox.Y = _currentY;

            _childComponents.Add(labelControl);
            _childComponents.Add(textBox);
            _propertyControls[label] = textBox;
            _currentY += 35;
        }

        /// <summary>
        /// Adds a color property control using hex (#RRGGBB or #AARRGGBB) or R,G,B[,A].
        /// </summary>
        private void AddColorProperty(string label, SKColor value, Action<SKColor> onChanged)
        {
            var textBox = new TextBox
            {
                Text = ColorToString(value),
                Width = this.Width - 120,
                Height = 25
            };

            textBox.TextChanged += (s, e) =>
            {
                if (TryParseColor(textBox.Text, out var c))
                {
                    onChanged(c);
                }
            };

            var labelControl = new Label
            {
                Text = label + ":",
                Width = 100,
                Height = 25,
                X = 10,
                Y = _currentY
            };

            textBox.X = 120;
            textBox.Y = _currentY;

            _childComponents.Add(labelControl);
            _childComponents.Add(textBox);
            _propertyControls[label] = textBox;
            _currentY += 35;
        }

        private static float[] ParseDashPattern(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var parts = text.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var list = new List<float>();
            foreach (var p in parts)
            {
                if (float.TryParse(p.Trim(), out var f) && f >= 0)
                {
                    list.Add(f);
                }
            }
            return list.Count >= 2 ? list.ToArray() : null;
        }

        private static bool TryParseColor(string text, out SKColor color)
        {
            color = SKColors.Black;
            if (string.IsNullOrWhiteSpace(text)) return false;
            text = text.Trim();
            try
            {
                if (text.StartsWith("#"))
                {
                    var hex = text.Substring(1);
                    if (hex.Length == 6)
                    {
                        byte r = Convert.ToByte(hex.Substring(0, 2), 16);
                        byte g = Convert.ToByte(hex.Substring(2, 2), 16);
                        byte b = Convert.ToByte(hex.Substring(4, 2), 16);
                        color = new SKColor(r, g, b);
                        return true;
                    }
                    if (hex.Length == 8)
                    {
                        byte a = Convert.ToByte(hex.Substring(0, 2), 16);
                        byte r = Convert.ToByte(hex.Substring(2, 2), 16);
                        byte g = Convert.ToByte(hex.Substring(4, 2), 16);
                        byte b = Convert.ToByte(hex.Substring(6, 2), 16);
                        color = new SKColor(r, g, b, a);
                        return true;
                    }
                }
                else if (text.Contains(','))
                {
                    var parts = text.Split(',');
                    if (parts.Length >= 3)
                    {
                        byte r = byte.Parse(parts[0].Trim());
                        byte g = byte.Parse(parts[1].Trim());
                        byte b = byte.Parse(parts[2].Trim());
                        byte a = 255;
                        if (parts.Length >= 4) a = byte.Parse(parts[3].Trim());
                        color = new SKColor(r, g, b, a);
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }

        private static string ColorToString(SKColor c)
        {
            return $"#{c.Alpha:X2}{c.Red:X2}{c.Green:X2}{c.Blue:X2}";
        }
        #endregion

        #region Save/Cancel Operations
        /// <summary>
        /// Saves the current property values.
        /// </summary>
        private void SaveProperties()
        {
            PropertiesSaved?.Invoke(this, new ComponentPropertiesSavedEventArgs(_selectedComponent));
            _isDirty = false;
        }

        /// <summary>
        /// Cancels the current editing session.
        /// </summary>
        private void CancelEditing()
        {
            PropertiesCancelled?.Invoke(this, new ComponentPropertiesCancelledEventArgs(_selectedComponent));
            _isDirty = false;
        }
        #endregion

        #region Drawing
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            // Draw background
            using var backgroundPaint = new SKPaint
            {
                Color = SKColors.White,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };
            canvas.DrawRect(new SKRect(X, Y, X + Width, Y + Height), backgroundPaint);

            // Draw border
            using var borderPaint = new SKPaint
            {
                Color = SKColors.LightGray,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1,
                IsAntialias = true
            };
            canvas.DrawRect(new SKRect(X, Y, X + Width, Y + Height), borderPaint);

            // Draw child components
            foreach (var component in _childComponents)
            {
                component.Draw(canvas, context);
            }
        }
        #endregion
    }

    /// <summary>
    /// Event arguments for component properties saved event.
    /// </summary>
    public class ComponentPropertiesSavedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the component whose properties were saved.
        /// </summary>
        public SkiaComponent Component { get; }

        /// <summary>
        /// Initializes a new instance of the ComponentPropertiesSavedEventArgs class.
        /// </summary>
        /// <param name="component">The component whose properties were saved.</param>
        public ComponentPropertiesSavedEventArgs(SkiaComponent component)
        {
            Component = component;
        }
    }

    /// <summary>
    /// Event arguments for component properties cancelled event.
    /// </summary>
    public class ComponentPropertiesCancelledEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the component whose property editing was cancelled.
        /// </summary>
        public SkiaComponent Component { get; }

        /// <summary>
        /// Initializes a new instance of the ComponentPropertiesCancelledEventArgs class.
        /// </summary>
        /// <param name="component">The component whose property editing was cancelled.</param>
        public ComponentPropertiesCancelledEventArgs(SkiaComponent component)
        {
            Component = component;
        }
    }
}