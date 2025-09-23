using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Beep.Skia.Model;

namespace Beep.Skia.Components
{
    /// <summary>
    /// Visual property editor for automation nodes, similar to n8n/Make.com node configuration panels.
    /// Provides a comprehensive interface for editing node properties, data input, and configuration.
    /// </summary>
    public class NodePropertyEditor : SkiaComponent
    {
        // Hide from palette/toolbox
        public override bool ShowInPalette { get; set; } = false;
        #region Private Fields
        private AutomationNode _selectedNode;
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
        public event EventHandler<NodePropertiesSavedEventArgs> PropertiesSaved;

        /// <summary>
        /// Event raised when editing is cancelled.
        /// </summary>
        public event EventHandler<NodePropertiesCancelledEventArgs> PropertiesCancelled;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the currently selected node for editing.
        /// </summary>
        public AutomationNode SelectedNode
        {
            get => _selectedNode;
            set
            {
                if (_selectedNode != value)
                {
                    _selectedNode = value;
                    OnSelectedNodeChanged();
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
        /// Initializes a new instance of the NodePropertyEditor class.
        /// </summary>
        public NodePropertyEditor()
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
            // Create title label
            _titleLabel = new Label
            {
                Text = "Node Properties",
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

        #region Drawing
        /// <summary>
        /// Draws the content of the node property editor.
        /// </summary>
        /// <param name="canvas">The Skia canvas to draw on.</param>
        /// <param name="context">The drawing context.</param>
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            // Draw background
            using var backgroundPaint = new SKPaint
            {
                Color = MaterialDesignColors.Surface,
                IsAntialias = true
            };
            var backgroundRect = new SKRect(0, 0, Width, Height);
            canvas.DrawRoundRect(backgroundRect, 8, 8, backgroundPaint);

            // Draw child components
            foreach (var component in _childComponents)
            {
                component.Draw(canvas, context);
            }
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Handles the selected node changed event.
        /// </summary>
        private void OnSelectedNodeChanged()
        {
            if (_selectedNode == null)
            {
                _titleLabel.Text = "Node Properties";
                ClearPropertyControls();
                return;
            }

            _titleLabel.Text = $"{_selectedNode.Name} Properties";
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
        /// Builds the property controls for the selected node.
        /// </summary>
        private void BuildPropertyControls()
        {
            ClearPropertyControls();

            if (_selectedNode == null) return;

            // Add common properties section
            AddSectionHeader("Common Properties");
            AddCommonPropertyControls();

            // Add node-specific properties section
            AddSectionHeader($"{_selectedNode.NodeType} Properties");
            AddNodeSpecificPropertyControls();

            // Add data input section for applicable nodes
            if (RequiresDataInput(_selectedNode))
            {
                AddSectionHeader("Data Input");
                AddDataInputControls();
            }

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
        /// Adds common property controls applicable to all nodes.
        /// </summary>
        private void AddCommonPropertyControls()
        {
            // Node Name
            AddTextProperty("Name", _selectedNode.Name, value =>
            {
                _selectedNode.Name = value;
                _isDirty = true;
            });

            // Node Description (if editable)
            if (_selectedNode is AutomationNode automationNode)
            {
                AddTextProperty("Description", automationNode.Description, value =>
                {
                    // Description is typically read-only, but could be editable for custom nodes
                }, true); // Read-only
            }

            // Node Status (read-only)
            AddTextProperty("Status", _selectedNode.Status.ToString(), null, true);
        }

        /// <summary>
        /// Adds node-specific property controls.
        /// </summary>
        private void AddNodeSpecificPropertyControls()
        {
            // First, add strongly-typed properties for known node types
            switch (_selectedNode)
            {
                case DataInputNode inputNode:
                    AddDataInputNodeProperties(inputNode);
                    break;
                case DataSourceAutomationNode dataSourceNode:
                    AddDataSourceNodeProperties(dataSourceNode);
                    break;
                case DataTransformNode transformNode:
                    AddDataTransformNodeProperties(transformNode);
                    break;
                case ConditionalNode conditionalNode:
                    AddConditionalNodeProperties(conditionalNode);
                    break;
                case ManualTriggerNode triggerNode:
                    AddManualTriggerNodeProperties(triggerNode);
                    break;
                case HttpRequestNode httpNode:
                    AddHttpRequestNodeProperties(httpNode);
                    break;
                default:
                    // For unknown node types, show generic properties
                    AddGenericNodeProperties();
                    break;
            }

            // Then add dynamic configuration properties from the Configuration dictionary
            AddConfigurationProperties();
        }

        /// <summary>
        /// Adds data input controls for nodes that require data input.
        /// </summary>
        private void AddDataInputControls()
        {
            if (_selectedNode is DataInputNode inputNode)
            {
                // Data input controls are handled in AddDataInputNodeProperties
                return;
            }

            // For other nodes, add a simple JSON input for custom data
            AddTextAreaProperty("Custom Data (JSON)", "", value =>
            {
                // Could extend to allow custom data input for any node
                _isDirty = true;
            });
        }


        #region Node-Specific Property Methods

        /// <summary>
        /// Adds a text property control.
        /// </summary>
        private void AddTextProperty(string label, string value, Action<string> onChanged, bool readOnly = false)
        {
            var labelControl = new Label
            {
                Text = $"{label}: {value}",
                X = 10,
                Y = _currentY,
                Width = this.Width - 20,
                Height = 25
            };
            _childComponents.Add(labelControl);
            _propertyControls[label] = labelControl;
            _currentY += 35;
        }

        /// <summary>
        /// Adds a text area property control.
        /// </summary>
        private void AddTextAreaProperty(string label, string value, Action<string> onChanged)
        {
            var labelControl = new Label
            {
                Text = $"{label}: {value}",
                X = 10,
                Y = _currentY,
                Width = this.Width - 20,
                Height = 25
            };
            _childComponents.Add(labelControl);
            _propertyControls[label] = labelControl;
            _currentY += 35;
        }
        #endregion

        #region Node-Specific Property Methods
        /// <summary>
        /// Adds properties specific to DataSourceAutomationNode.
        /// </summary>
        private void AddDataSourceNodeProperties(DataSourceAutomationNode node)
        {
            // Simplified for now - just show basic info
            AddTextProperty("Data Source Type", "Data Source", null, true);
        }

        /// <summary>
        /// Adds properties specific to DataTransformNode.
        /// </summary>
        private void AddDataTransformNodeProperties(DataTransformNode node)
        {
            AddTextProperty("Transform Type", "Data Transform", null, true);
        }

        /// <summary>
        /// Adds properties specific to ConditionalNode.
        /// </summary>
        private void AddConditionalNodeProperties(ConditionalNode node)
        {
            AddTextProperty("Condition Type", "Conditional", null, true);
        }

        /// <summary>
        /// Adds properties specific to DataInputNode.
        /// </summary>
        private void AddDataInputNodeProperties(DataInputNode node)
        {
            // Input Mode selection
            AddTextProperty("Input Mode", node.InputMode, newValue =>
            {
                node.InputMode = newValue;
                _isDirty = true;
                // Rebuild controls to show relevant options for the selected mode
                BuildPropertyControls();
            });

            // Show different controls based on input mode
            switch (node.InputMode?.ToLower())
            {
                case "json":
                    AddTextAreaProperty("JSON Data", node.JsonData, newValue =>
                    {
                        node.JsonData = newValue;
                        _isDirty = true;
                    });
                    break;

                case "form":
                    AddCheckboxProperty("Allow Multiple Entries", node.AllowMultipleEntries, newValue =>
                    {
                        node.AllowMultipleEntries = newValue;
                        _isDirty = true;
                    });

                    if (node.FormFields.Any())
                    {
                        AddTextProperty("Form Fields Count", node.FormFields.Count.ToString(), null, true);
                        // In a full implementation, this would show form field configuration
                    }
                    else
                    {
                        AddTextProperty("Form Fields", "No fields configured", null, true);
                    }
                    break;

                case "code":
                    AddTextAreaProperty("Code Data", node.JsonData, newValue =>
                    {
                        node.JsonData = newValue;
                        _isDirty = true;
                    });
                    break;
            }

            // Common properties
            AddCheckboxProperty("Validate Input", node.ValidateInput, newValue =>
            {
                node.ValidateInput = newValue;
                _isDirty = true;
            });
        }

        /// <summary>
        /// Adds properties specific to ManualTriggerNode.
        /// </summary>
        private void AddManualTriggerNodeProperties(ManualTriggerNode node)
        {
            AddTextProperty("Trigger Type", "Manual", null, true);
        }

        /// <summary>
        /// Adds properties specific to HttpRequestNode.
        /// </summary>
        private void AddHttpRequestNodeProperties(HttpRequestNode node)
        {
            AddTextProperty("Request Type", "HTTP", null, true);
        }

        /// <summary>
        /// Adds generic properties for unknown node types.
        /// </summary>
        private void AddGenericNodeProperties()
        {
            AddTextProperty("Node Type", _selectedNode?.GetType().Name ?? "Unknown", null, true);
        }

        /// <summary>
        /// Adds dynamic configuration properties from the node's Configuration dictionary.
        /// </summary>
        private void AddConfigurationProperties()
        {
            if (_selectedNode?.Configuration == null || !_selectedNode.Configuration.Any())
                return;

            AddSectionHeader("Configuration Parameters");

            foreach (var kvp in _selectedNode.Configuration.OrderBy(k => k.Key))
            {
                string key = kvp.Key;
                object value = kvp.Value;

                switch (value)
                {
                    case string stringValue:
                        AddTextInputProperty(key, stringValue, newValue =>
                        {
                            _selectedNode.Configuration[key] = newValue;
                            _isDirty = true;
                        });
                        break;

                    case bool boolValue:
                        AddCheckboxProperty(key, boolValue, newValue =>
                        {
                            _selectedNode.Configuration[key] = newValue;
                            _isDirty = true;
                        });
                        break;

                    case int intValue:
                        AddNumericProperty(key, intValue, newValue =>
                        {
                            _selectedNode.Configuration[key] = newValue;
                            _isDirty = true;
                        });
                        break;

                    case double doubleValue:
                        AddNumericProperty(key, doubleValue, newValue =>
                        {
                            _selectedNode.Configuration[key] = newValue;
                            _isDirty = true;
                        });
                        break;

                    case Dictionary<string, object> dictValue:
                        AddTextAreaProperty($"{key} (JSON)", JsonSerializer.Serialize(dictValue, new JsonSerializerOptions { WriteIndented = true }), newValue =>
                        {
                            try
                            {
                                var parsed = JsonSerializer.Deserialize<Dictionary<string, object>>(newValue);
                                _selectedNode.Configuration[key] = parsed;
                                _isDirty = true;
                            }
                            catch
                            {
                                // Invalid JSON, don't update
                            }
                        });
                        break;

                    case List<object> listValue:
                        AddTextAreaProperty($"{key} (JSON)", JsonSerializer.Serialize(listValue, new JsonSerializerOptions { WriteIndented = true }), newValue =>
                        {
                            try
                            {
                                var parsed = JsonSerializer.Deserialize<List<object>>(newValue);
                                _selectedNode.Configuration[key] = parsed;
                                _isDirty = true;
                            }
                            catch
                            {
                                // Invalid JSON, don't update
                            }
                        });
                        break;

                    default:
                        // For complex objects, show as JSON
                        string jsonValue = value != null ?
                            JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }) :
                            "{}";
                        AddTextAreaProperty($"{key} (JSON)", jsonValue, newValue =>
                        {
                            _selectedNode.Configuration[key] = newValue; // Store as string for complex objects
                            _isDirty = true;
                        });
                        break;
                }
            }
        }

        #region Helper Methods
        /// <summary>
        /// Adds a text input property control.
        /// </summary>
        private void AddTextInputProperty(string label, string value, Action<string> onChanged)
        {
            // For now, use a simple text display. In a full implementation, this would create an editable textbox
            AddTextProperty(label, value, onChanged);
        }

        /// <summary>
        /// Adds a checkbox property control.
        /// </summary>
        private void AddCheckboxProperty(string label, bool value, Action<bool> onChanged)
        {
            AddTextProperty(label, value ? "True" : "False", newValue =>
            {
                if (bool.TryParse(newValue, out bool boolValue))
                {
                    onChanged(boolValue);
                }
            });
        }

        /// <summary>
        /// Adds a numeric property control.
        /// </summary>
        private void AddNumericProperty(string label, int value, Action<int> onChanged)
        {
            AddTextProperty(label, value.ToString(), newValue =>
            {
                if (int.TryParse(newValue, out int intValue))
                {
                    onChanged(intValue);
                }
            });
        }

        /// <summary>
        /// Adds a numeric property control for doubles.
        /// </summary>
        private void AddNumericProperty(string label, double value, Action<double> onChanged)
        {
            AddTextProperty(label, value.ToString(), newValue =>
            {
                if (double.TryParse(newValue, out double doubleValue))
                {
                    onChanged(doubleValue);
                }
            });
        }

        /// <summary>
        /// Determines if a node requires data input controls.
        /// </summary>
        private bool RequiresDataInput(AutomationNode node)
        {
            return node is DataInputNode;
        }

        /// <summary>
        /// Saves the current property values.
        /// </summary>
        private void SaveProperties()
        {
            if (_selectedNode == null) return;

            // The properties are already updated through the control event handlers
            // Just need to notify that saving occurred
            OnPropertiesSaved(new NodePropertiesSavedEventArgs { Node = _selectedNode });
            _isDirty = false;
        }

        /// <summary>
        /// Cancels the current editing session.
        /// </summary>
        private void CancelEditing()
        {
            // Rebuild controls to reset to original values
            BuildPropertyControls();
            OnPropertiesCancelled(new NodePropertiesCancelledEventArgs { Node = _selectedNode });
            _isDirty = false;
        }

        /// <summary>
        /// Raises the PropertiesSaved event.
        /// </summary>
        protected virtual void OnPropertiesSaved(NodePropertiesSavedEventArgs e)
        {
            PropertiesSaved?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the PropertiesCancelled event.
        /// </summary>
        protected virtual void OnPropertiesCancelled(NodePropertiesCancelledEventArgs e)
        {
            PropertiesCancelled?.Invoke(this, e);
        }
        #endregion
        #endregion
        #endregion
    }

    /// <summary>
    /// Event arguments for node properties saved event.
    /// </summary>
    public class NodePropertiesSavedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the node whose properties were saved.
        /// </summary>
        public AutomationNode Node { get; set; }
    }

    /// <summary>
    /// Event arguments for node properties cancelled event.
    /// </summary>
    public class NodePropertiesCancelledEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the node whose property editing was cancelled.
        /// </summary>
        public AutomationNode Node { get; set; }
    }
}
