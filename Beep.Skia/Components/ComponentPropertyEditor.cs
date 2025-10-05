using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Beep.Skia.Model;
using System.Windows.Forms;

namespace Beep.Skia.Components
{
    // Minimal key enumeration for property editor keyboard navigation/editing
    public enum PropertyEditorKey
    {
        Left,
        Right,
        Back,
        Delete,
        Home,
        End
    }

    /// <summary>
    /// Generic property editor for any SkiaComponent, providing a comprehensive interface
    /// for editing component properties, configuration, and display settings.
    /// </summary>
    public class ComponentPropertyEditor : SkiaComponent
    {
        // Owning DrawingManager (set by host) to enable actions like schema inference
        public DrawingManager Manager { get; set; }
        // Hide this infrastructure component from the palette/toolbox
        public override bool ShowInPalette { get; set; } = false;
        // Note: Render as an overlay; do not move/scale with canvas pan/zoom
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
    // Focused input tracking
    private TextBox _focusedTextBox;
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

        /// <summary>
        /// Event raised whenever a property value changes via the editor controls.
        /// </summary>
        public event EventHandler PropertyValueChanged;
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
            this.IsStatic = true;
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
            try { _titleLabel.IsStatic = true; } catch { }
            _titleLabel.Tag = "section";
            _childComponents.Add(_titleLabel);

            // Create buttons
            _saveButton = new Button
            {
                Text = "Save",
                Width = 80,
                Height = 32
            };
            _saveButton.Clicked += SaveButton_Clicked;
            try { _saveButton.IsStatic = true; } catch { }

            _cancelButton = new Button
            {
                Text = "Cancel",
                Width = 80,
                Height = 32
            };
            _cancelButton.Clicked += CancelButton_Clicked;
            try { _cancelButton.IsStatic = true; } catch { }

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

            // Place children inside the editor panel with absolute coordinates (screen space)
            float absLeft = this.X;
            float absTop = this.Y;
            float contentWidth = Math.Max(0, this.Width - 20);

            // Ensure title header is positioned at top inside panel
            if (_titleLabel != null)
            {
                _titleLabel.X = absLeft + 10;
                _titleLabel.Y = absTop + 10;
                _titleLabel.Width = contentWidth;
            }

            // Layout sequence: headers (Tag=="section") and property rows as label/value pairs
            // Build a working list excluding title and buttons
            var body = _childComponents.Where(c => c != _titleLabel && c != _saveButton && c != _cancelButton).ToList();
            int i = 0;
            while (i < body.Count)
            {
                var c = body[i];
                if (Equals(c?.Tag, "section"))
                {
                    // Section header spans full width
                    c.X = absLeft + 10;
                    c.Y = absTop + _currentY;
                    c.Width = contentWidth;
                    c.Height = 25;
                    _currentY += 35;
                    i++;
                    continue;
                }

                // Property row expects label then input
                var label = c as Label;
                SkiaComponent valueCtrl = null;
                if (i + 1 < body.Count)
                {
                    valueCtrl = body[i + 1];
                }

                // Label
                if (label != null)
                {
                    label.X = absLeft + 10;
                    label.Y = absTop + _currentY;
                    label.Width = 100;
                    label.Height = 25;
                }
                else
                {
                    // Non-label first item: treat as full-width control
                    c.X = absLeft + 10;
                    c.Y = absTop + _currentY;
                    c.Width = contentWidth;
                    _currentY += c.Height + 10;
                    i++;
                    continue;
                }

                // Value control
                if (valueCtrl != null)
                {
                    valueCtrl.X = absLeft + 120;
                    valueCtrl.Y = absTop + _currentY;
                    valueCtrl.Width = Math.Max(80, this.Width - 130);
                    valueCtrl.Height = Math.Max(25, valueCtrl.Height);
                }

                _currentY += ((valueCtrl?.Height) ?? 25) + 10;
                i += 2; // advance past the pair
            }

            // Position buttons at bottom, inside panel
            if (_saveButton != null)
            {
                _saveButton.X = absLeft + Math.Max(10, this.Width - 180);
                _saveButton.Y = absTop + Math.Max(10, this.Height - 50);
            }
            if (_cancelButton != null)
            {
                _cancelButton.X = absLeft + Math.Max(10, this.Width - 90);
                _cancelButton.Y = absTop + Math.Max(10, this.Height - 50);
            }
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
            // Clear focused input if it was removed
            _focusedTextBox = null;
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

            // Add NodeProperties if present on the selected component
            if (_selectedComponent.NodeProperties != null && _selectedComponent.NodeProperties.Count > 0)
            {
                AddSectionHeader("Node Properties");
                AddNodeProperties(_selectedComponent);
            }

            UpdateLayout();
        }

        /// <summary>
        /// Adds editors for NodeProperties on the selected component.
        /// </summary>
        private void AddNodeProperties(SkiaComponent component)
        {
            foreach (var kv in component.NodeProperties.OrderBy(k => k.Key))
            {
                var key = kv.Key;
                var p = kv.Value;
                // Provide a structured editor for common schema keys
                if (TryAddSchemaGridEditor(component, key, p))
                {
                    continue;
                }
                if (p == null)
                {
                    AddTextProperty(key, string.Empty, v =>
                    {
                        component.NodeProperties[key] = new Beep.Skia.Model.ParameterInfo { ParameterName = key, ParameterCurrentValue = v, DefaultParameterValue = v, ParameterType = typeof(string) };
                        _isDirty = true;
                        OnPropertyValueChanged();
                        MaybeTriggerInference(component, key);
                    });
                    continue;
                }

                var type = p.ParameterType ?? p.ParameterCurrentValue?.GetType() ?? p.DefaultParameterValue?.GetType() ?? typeof(string);
                var value = p.ParameterCurrentValue ?? p.DefaultParameterValue;

                if (type == typeof(string))
                {
                    // Prefer multiline editor for schema/JSON and row lists
                    var keyLower = key?.ToLowerInvariant() ?? string.Empty;
                    bool preferMultiline = keyLower.Contains("json") || keyLower.Contains("schema") || keyLower == "rows" || keyLower == "rowstext" || keyLower == "columns" || keyLower == "outputschema";
                    var choices = p.Choices;
                    if (choices != null && choices.Length > 0)
                    {
                        // Render a dropdown with provided choices
                        var dd = new Dropdown { Width = this.Width - 120, Height = 28 };
                        try { dd.IsStatic = true; } catch { }
                        foreach (var ch in choices)
                            dd.Items.Add(new Dropdown.DropdownItem(ch, ch));
                        dd.SelectedValue = Convert.ToString(value) ?? string.Empty;
                        // label
                        var labelCtrl = new Label { Text = key + ":", Width = 100, Height = 25, X = 10, Y = _currentY };
                        try { labelCtrl.IsStatic = true; } catch { }
                        labelCtrl.Tag = "prop-label";
                        dd.Tag = "prop-value";
                        dd.SelectedItemChanged += (s, item) => { p.ParameterCurrentValue = item?.Value; _isDirty = true; OnPropertyValueChanged(); MaybeTriggerInference(component, key); };
                        _childComponents.Add(labelCtrl);
                        _childComponents.Add(dd);
                        _propertyControls[key] = dd;
                        _currentY += 35;
                    }
                    else
                    {
                        var textVal = Convert.ToString(value) ?? string.Empty;
                        if (preferMultiline)
                            AddMultilineTextProperty(key, textVal, v => { p.ParameterCurrentValue = v; _isDirty = true; OnPropertyValueChanged(); }, lines: 6);
                        else
                            AddTextProperty(key, textVal, v => { p.ParameterCurrentValue = v; _isDirty = true; OnPropertyValueChanged(); MaybeTriggerInference(component, key); });
                    }
                }
                else if (type == typeof(bool))
                {
                    AddBooleanProperty(key, Convert.ToBoolean(value ?? false), v => { p.ParameterCurrentValue = v; _isDirty = true; OnPropertyValueChanged(); });
                }
                else if (type == typeof(int))
                {
                    AddNumericProperty(key, value != null ? Convert.ToSingle(value) : 0f, v => { p.ParameterCurrentValue = (int)MathF.Round(v); _isDirty = true; OnPropertyValueChanged(); });
                }
                else if (type == typeof(float))
                {
                    AddNumericProperty(key, value != null ? Convert.ToSingle(value) : 0f, v => { p.ParameterCurrentValue = v; _isDirty = true; OnPropertyValueChanged(); });
                }
                else if (type == typeof(double))
                {
                    AddNumericProperty(key, value != null ? Convert.ToSingle(value) : 0f, v => { p.ParameterCurrentValue = (double)v; _isDirty = true; OnPropertyValueChanged(); });
                }
                else if (type.IsEnum)
                {
                    // If Choices not provided, populate from enum names for dropdowns elsewhere
                    try
                    {
                        if ((p.Choices == null || p.Choices.Length == 0) && type != null)
                        {
                            var names = System.Enum.GetNames(type);
                            if (names != null && names.Length > 0) p.Choices = names;
                        }
                    }
                    catch { }
                    var current = value ?? Activator.CreateInstance(type);
                    AddEnumProperty(key, current, type, nv => { p.ParameterCurrentValue = nv; _isDirty = true; OnPropertyValueChanged(); MaybeTriggerInference(component, key); });
                }
                else if (type == typeof(System.TimeSpan))
                {
                    var ts = value is System.TimeSpan tsv ? tsv : System.TimeSpan.Zero;
                    AddTextProperty(key, ts.ToString(), s =>
                    {
                        if (System.TimeSpan.TryParse(s, out var parsed))
                        {
                            p.ParameterCurrentValue = parsed;
                            _isDirty = true;
                            OnPropertyValueChanged();
                            MaybeTriggerInference(component, key);
                        }
                    });
                }
                else if (type == typeof(SkiaSharp.SKColor))
                {
                    var c = value is SkiaSharp.SKColor sc ? sc : SkiaSharp.SKColors.Black;
                    AddColorProperty(key, c, col => { p.ParameterCurrentValue = col; _isDirty = true; OnPropertyValueChanged(); MaybeTriggerInference(component, key); });
                }
                else
                {
                    // Use JSON as a fallback for complex types
                    try
                    {
                        var json = System.Text.Json.JsonSerializer.Serialize(value);
                        AddTextProperty(key + " (JSON)", json, s =>
                        {
                            try
                            {
                                var obj = System.Text.Json.JsonSerializer.Deserialize(s, type);
                                p.ParameterCurrentValue = obj;
                                _isDirty = true;
                                OnPropertyValueChanged();
                                MaybeTriggerInference(component, key);
                            }
                            catch { }
                        });
                    }
                    catch
                    {
                        AddTextProperty(key, Convert.ToString(value) ?? string.Empty, v => { p.ParameterCurrentValue = v; _isDirty = true; OnPropertyValueChanged(); MaybeTriggerInference(component, key); });
                    }
                }
            }
        }

        private void MaybeTriggerInference(SkiaComponent component, string key)
        {
            try
            {
                if (component == null || Manager == null) return;
                var ns = component.GetType().Namespace ?? string.Empty;
                if (!string.Equals(ns, "Beep.Skia.ETL", StringComparison.Ordinal)) return;
                if (string.IsNullOrWhiteSpace(key)) return;
                var k = key.Trim();
                // Only trigger on relevant keys
                if (k.Equals("GroupBy", StringComparison.OrdinalIgnoreCase) ||
                    k.Equals("JoinKeyLeft", StringComparison.OrdinalIgnoreCase) ||
                    k.Equals("JoinKeyRight", StringComparison.OrdinalIgnoreCase) ||
                    k.Equals("Kind", StringComparison.OrdinalIgnoreCase))
                {
                    Manager.InferOutputSchemaForComponent(component);
                }
            }
            catch { }
        }

        /// <summary>
        /// Attempts to render a simple grid-like editor for schema JSON NodeProperties such as Columns/OutputSchema.
        /// Returns true if handled.
        /// </summary>
        private bool TryAddSchemaGridEditor(SkiaComponent component, string key, ParameterInfo p)
        {
            if (p == null) return false;
            var k = key?.ToLowerInvariant() ?? string.Empty;

            // 1) Column-based schemas (Columns/OutputSchema/ExpectedSchema)
            if (k == "columns" || k == "outputschema" || k == "expectedschema")
            {
                var cols = new List<Beep.Skia.Model.ColumnDefinition>();
                try
                {
                    var raw = p.ParameterCurrentValue as string ?? p.DefaultParameterValue as string;
                    if (!string.IsNullOrWhiteSpace(raw))
                    {
                        cols = System.Text.Json.JsonSerializer.Deserialize<List<Beep.Skia.Model.ColumnDefinition>>(raw) ?? new();
                    }
                }
                catch { }

                var labelCtrl = new Label { Text = key + ":", Width = 100, Height = 25, X = 10, Y = _currentY };
                try { labelCtrl.IsStatic = true; } catch { }
                labelCtrl.Tag = "prop-label";
                _childComponents.Add(labelCtrl);

                var extraChoices = new List<string>();
                try
                {
                    if (p.Choices is IEnumerable<string> chs)
                    {
                        foreach (var ch in chs)
                            if (!string.IsNullOrWhiteSpace(ch)) extraChoices.Add(ch.Trim());
                    }
                }
                catch { }

                var gridContainer = new SkiaComponentGrid(cols, updatedList =>
                {
                    try
                    {
                        p.ParameterCurrentValue = System.Text.Json.JsonSerializer.Serialize(updatedList);
                        _isDirty = true;
                        OnPropertyValueChanged();
                    }
                    catch { }
                }, extraChoices)
                { Width = this.Width - 120, Height = 160 };
                try { gridContainer.IsStatic = true; } catch { }
                gridContainer.Tag = "prop-value";
                _childComponents.Add(gridContainer);
                _propertyControls[key] = gridContainer;
                _currentY += gridContainer.Height + 10;
                return true;
            }

            // 2) Indexes (array of IndexDefinition)
            if (k == "indexes")
            {
                var header = new Label { Text = "Indexes:", Width = 100, Height = 25, X = 10, Y = _currentY };
                try { header.IsStatic = true; } catch { }
                header.Tag = "section";
                _childComponents.Add(header);

                var items = new List<Beep.Skia.Model.IndexDefinition>();
                try
                {
                    var raw = p.ParameterCurrentValue as string ?? p.DefaultParameterValue as string;
                    if (!string.IsNullOrWhiteSpace(raw))
                        items = System.Text.Json.JsonSerializer.Deserialize<List<Beep.Skia.Model.IndexDefinition>>(raw) ?? new();
                }
                catch { }

                var panel = new SimpleListEditor<Beep.Skia.Model.IndexDefinition>(items,
                    drawHeader: "Name | Unique | Columns (comma) | Where",
                    onRenderRow: (row, y, setDirty) =>
                    {
                        float x = 0;
                        var tbName = new TextBox { Text = row.Name, Width = 140, Height = 24, X = x, Y = y }; x += 150;
                        tbName.TextChanged += (s, e) => { row.Name = tbName.Text; setDirty(); };
                        var cbUnique = new Checkbox { IsChecked = row.IsUnique, Width = 24, Height = 24, X = x, Y = y }; x += 40;
                        cbUnique.CheckStateChanged += (s, e) => { row.IsUnique = cbUnique.IsChecked; setDirty(); };
                        var tbCols = new TextBox { Text = string.Join(",", row.Columns ?? new List<string>()), Width = 180, Height = 24, X = x, Y = y }; x += 190;
                        tbCols.TextChanged += (s, e) => { row.Columns = (tbCols.Text ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s1 => s1.Trim()).ToList(); setDirty(); };
                        var tbWhere = new TextBox { Text = row.Where, Width = 120, Height = 24, X = x, Y = y };
                        tbWhere.TextChanged += (s, e) => { row.Where = tbWhere.Text; setDirty(); };
                        return new SkiaComponent[] { tbName, cbUnique, tbCols, tbWhere };
                    },
                    onCommit: list =>
                    {
                        try { p.ParameterCurrentValue = System.Text.Json.JsonSerializer.Serialize(list); _isDirty = true; OnPropertyValueChanged(); } catch { }
                    })
                { Width = this.Width - 120, Height = 150 };
                try { panel.IsStatic = true; } catch { }
                _childComponents.Add(panel);
                _propertyControls[key] = panel;
                _currentY += panel.Height + 10;
                return true;
            }

            // 3) Foreign keys (array of ForeignKeyDefinition)
            if (k == "foreignkeys")
            {
                var header = new Label { Text = "Foreign Keys:", Width = 100, Height = 25, X = 10, Y = _currentY };
                try { header.IsStatic = true; } catch { }
                header.Tag = "section";
                _childComponents.Add(header);

                var items = new List<Beep.Skia.Model.ForeignKeyDefinition>();
                try
                {
                    var raw = p.ParameterCurrentValue as string ?? p.DefaultParameterValue as string;
                    if (!string.IsNullOrWhiteSpace(raw))
                        items = System.Text.Json.JsonSerializer.Deserialize<List<Beep.Skia.Model.ForeignKeyDefinition>>(raw) ?? new();
                }
                catch { }

                var panel = new SimpleListEditor<Beep.Skia.Model.ForeignKeyDefinition>(items,
                    drawHeader: "Name | Columns (comma) | Ref Entity | Ref Columns | OnDelete | OnUpdate",
                    onRenderRow: (row, y, setDirty) =>
                    {
                        float x = 0;
                        var tbName = new TextBox { Text = row.Name, Width = 120, Height = 24, X = x, Y = y }; x += 130;
                        tbName.TextChanged += (s, e) => { row.Name = tbName.Text; setDirty(); };
                        var tbCols = new TextBox { Text = string.Join(",", row.Columns ?? new List<string>()), Width = 160, Height = 24, X = x, Y = y }; x += 170;
                        tbCols.TextChanged += (s, e) => { row.Columns = (tbCols.Text ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s1 => s1.Trim()).ToList(); setDirty(); };
                        var tbRefEntity = new TextBox { Text = row.ReferencedEntity, Width = 120, Height = 24, X = x, Y = y }; x += 130;
                        tbRefEntity.TextChanged += (s, e) => { row.ReferencedEntity = tbRefEntity.Text; setDirty(); };
                        var tbRefCols = new TextBox { Text = string.Join(",", row.ReferencedColumns ?? new List<string>()), Width = 160, Height = 24, X = x, Y = y }; x += 170;
                        tbRefCols.TextChanged += (s, e) => { row.ReferencedColumns = (tbRefCols.Text ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s1 => s1.Trim()).ToList(); setDirty(); };
                        var tbOnDelete = new TextBox { Text = row.OnDelete, Width = 90, Height = 24, X = x, Y = y }; x += 100;
                        tbOnDelete.TextChanged += (s, e) => { row.OnDelete = tbOnDelete.Text; setDirty(); };
                        var tbOnUpdate = new TextBox { Text = row.OnUpdate, Width = 90, Height = 24, X = x, Y = y };
                        tbOnUpdate.TextChanged += (s, e) => { row.OnUpdate = tbOnUpdate.Text; setDirty(); };
                        return new SkiaComponent[] { tbName, tbCols, tbRefEntity, tbRefCols, tbOnDelete, tbOnUpdate };
                    },
                    onCommit: list =>
                    {
                        try { p.ParameterCurrentValue = System.Text.Json.JsonSerializer.Serialize(list); _isDirty = true; OnPropertyValueChanged(); } catch { }
                    })
                { Width = this.Width - 120, Height = 180 };
                try { panel.IsStatic = true; } catch { }
                _childComponents.Add(panel);
                _propertyControls[key] = panel;
                _currentY += panel.Height + 10;
                return true;
            }

            // 4) Derived Columns (array of DerivedColumnDefinition)
            if (k == "derivedcolumns")
            {
                var header = new Label { Text = "Derived Columns:", Width = 100, Height = 25, X = 10, Y = _currentY };
                try { header.IsStatic = true; } catch { }
                header.Tag = "section";
                _childComponents.Add(header);

                var items = new List<Beep.Skia.Model.DerivedColumnDefinition>();
                try
                {
                    var raw = p.ParameterCurrentValue as string ?? p.DefaultParameterValue as string;
                    if (!string.IsNullOrWhiteSpace(raw))
                        items = System.Text.Json.JsonSerializer.Deserialize<List<Beep.Skia.Model.DerivedColumnDefinition>>(raw) ?? new();
                }
                catch { }

                var panel = new SimpleListEditor<Beep.Skia.Model.DerivedColumnDefinition>(items,
                    drawHeader: "Name | Expression | DataType | Description",
                    onRenderRow: (row, y, setDirty) =>
                    {
                        float x = 0;
                        var tbName = new TextBox { Text = row.Name, Width = 120, Height = 24, X = x, Y = y }; x += 130;
                        tbName.TextChanged += (s, e) => { row.Name = tbName.Text; setDirty(); };
                        var tbExpr = new TextBox { Text = row.Expression, Width = 200, Height = 24, X = x, Y = y }; x += 210;
                        tbExpr.TextChanged += (s, e) => { row.Expression = tbExpr.Text; setDirty(); };
                        var tbType = new TextBox { Text = row.DataType, Width = 80, Height = 24, X = x, Y = y }; x += 90;
                        tbType.TextChanged += (s, e) => { row.DataType = tbType.Text; setDirty(); };
                        var tbDesc = new TextBox { Text = row.Description, Width = 160, Height = 24, X = x, Y = y };
                        tbDesc.TextChanged += (s, e) => { row.Description = tbDesc.Text; setDirty(); };
                        return new SkiaComponent[] { tbName, tbExpr, tbType, tbDesc };
                    },
                    onCommit: list =>
                    {
                        try { p.ParameterCurrentValue = System.Text.Json.JsonSerializer.Serialize(list); _isDirty = true; OnPropertyValueChanged(); } catch { }
                    })
                { Width = this.Width - 120, Height = 150 };
                try { panel.IsStatic = true; } catch { }
                _childComponents.Add(panel);
                _propertyControls[key] = panel;
                _currentY += panel.Height + 10;
                return true;
            }

            // 5) Split Conditions (array of SplitCondition)
            if (k == "conditions")
            {
                var header = new Label { Text = "Split Conditions:", Width = 100, Height = 25, X = 10, Y = _currentY };
                try { header.IsStatic = true; } catch { }
                header.Tag = "section";
                _childComponents.Add(header);

                var items = new List<Beep.Skia.Model.SplitCondition>();
                try
                {
                    var raw = p.ParameterCurrentValue as string ?? p.DefaultParameterValue as string;
                    if (!string.IsNullOrWhiteSpace(raw))
                        items = System.Text.Json.JsonSerializer.Deserialize<List<Beep.Skia.Model.SplitCondition>>(raw) ?? new();
                }
                catch { }

                var panel = new SimpleListEditor<Beep.Skia.Model.SplitCondition>(items,
                    drawHeader: "Name | Expression | Order | Description",
                    onRenderRow: (row, y, setDirty) =>
                    {
                        float x = 0;
                        var tbName = new TextBox { Text = row.Name, Width = 120, Height = 24, X = x, Y = y }; x += 130;
                        tbName.TextChanged += (s, e) => { row.Name = tbName.Text; setDirty(); };
                        var tbExpr = new TextBox { Text = row.Expression, Width = 200, Height = 24, X = x, Y = y }; x += 210;
                        tbExpr.TextChanged += (s, e) => { row.Expression = tbExpr.Text; setDirty(); };
                        var tbOrder = new TextBox { Text = row.Order.ToString(), Width = 50, Height = 24, X = x, Y = y }; x += 60;
                        tbOrder.TextChanged += (s, e) => { if (int.TryParse(tbOrder.Text, out var ord)) { row.Order = ord; setDirty(); } };
                        var tbDesc = new TextBox { Text = row.Description, Width = 180, Height = 24, X = x, Y = y };
                        tbDesc.TextChanged += (s, e) => { row.Description = tbDesc.Text; setDirty(); };
                        return new SkiaComponent[] { tbName, tbExpr, tbOrder, tbDesc };
                    },
                    onCommit: list =>
                    {
                        try { p.ParameterCurrentValue = System.Text.Json.JsonSerializer.Serialize(list); _isDirty = true; OnPropertyValueChanged(); } catch { }
                    })
                { Width = this.Width - 120, Height = 150 };
                try { panel.IsStatic = true; } catch { }
                _childComponents.Add(panel);
                _propertyControls[key] = panel;
                _currentY += panel.Height + 10;
                return true;
            }

            // 6) Data Quality Rules (array of DataQualityRule)
            if (k == "dataqualityrules")
            {
                var header = new Label { Text = "Data Quality Rules:", Width = 100, Height = 25, X = 10, Y = _currentY };
                try { header.IsStatic = true; } catch { }
                header.Tag = "section";
                _childComponents.Add(header);

                var items = new List<Beep.Skia.Model.DataQualityRule>();
                try
                {
                    var raw = p.ParameterCurrentValue as string ?? p.DefaultParameterValue as string;
                    if (!string.IsNullOrWhiteSpace(raw))
                        items = System.Text.Json.JsonSerializer.Deserialize<List<Beep.Skia.Model.DataQualityRule>>(raw) ?? new();
                }
                catch { }

                var panel = new SimpleListEditor<Beep.Skia.Model.DataQualityRule>(items,
                    drawHeader: "Name | Column | Type | Expression | Error Message",
                    onRenderRow: (row, y, setDirty) =>
                    {
                        float x = 0;
                        var tbName = new TextBox { Text = row.Name, Width = 100, Height = 24, X = x, Y = y }; x += 110;
                        tbName.TextChanged += (s, e) => { row.Name = tbName.Text; setDirty(); };
                        var tbColumn = new TextBox { Text = row.ColumnName, Width = 100, Height = 24, X = x, Y = y }; x += 110;
                        tbColumn.TextChanged += (s, e) => { row.ColumnName = tbColumn.Text; setDirty(); };
                        var tbType = new TextBox { Text = row.Type.ToString(), Width = 80, Height = 24, X = x, Y = y }; x += 90;
                        tbType.TextChanged += (s, e) => { if (Enum.TryParse<Beep.Skia.Model.RuleType>(tbType.Text, true, out var rt)) { row.Type = rt; setDirty(); } };
                        var tbExpr = new TextBox { Text = row.Expression, Width = 140, Height = 24, X = x, Y = y }; x += 150;
                        tbExpr.TextChanged += (s, e) => { row.Expression = tbExpr.Text; setDirty(); };
                        var tbErr = new TextBox { Text = row.ErrorMessage, Width = 140, Height = 24, X = x, Y = y };
                        tbErr.TextChanged += (s, e) => { row.ErrorMessage = tbErr.Text; setDirty(); };
                        return new SkiaComponent[] { tbName, tbColumn, tbType, tbExpr, tbErr };
                    },
                    onCommit: list =>
                    {
                        try { p.ParameterCurrentValue = System.Text.Json.JsonSerializer.Serialize(list); _isDirty = true; OnPropertyValueChanged(); } catch { }
                    })
                { Width = this.Width - 120, Height = 180 };
                try { panel.IsStatic = true; } catch { }
                _childComponents.Add(panel);
                _propertyControls[key] = panel;
                _currentY += panel.Height + 10;
                return true;
            }

            return false;
        }

        /// <summary>
        /// A very lightweight grid-like component rendering rows of ColumnDefinition with inline editing using TextBoxes and Dropdowns.
        /// </summary>
    private class SkiaComponentGrid : SkiaComponent
        {
            private readonly List<Beep.Skia.Model.ColumnDefinition> _columns;
            private readonly Action<List<Beep.Skia.Model.ColumnDefinition>> _onChanged;
            private readonly List<string> _typeChoices;

            public SkiaComponentGrid(List<Beep.Skia.Model.ColumnDefinition> columns, Action<List<Beep.Skia.Model.ColumnDefinition>> onChanged, IEnumerable<string> extraTypeChoices = null)
            {
                ShowInPalette = false;
                _columns = columns ?? new List<Beep.Skia.Model.ColumnDefinition>();
                _onChanged = onChanged;
                var baseTypes = new[] { "string", "int", "long", "float", "double", "decimal", "bool", "datetime", "date", "time", "guid", "binary", "json" };
                _typeChoices = new List<string>(baseTypes);
                if (extraTypeChoices != null)
                {
                    foreach (var t in extraTypeChoices)
                    {
                        if (!string.IsNullOrWhiteSpace(t) && !_typeChoices.Contains(t, StringComparer.OrdinalIgnoreCase))
                            _typeChoices.Add(t);
                    }
                }
                BuildChildren();
            }

            private void BuildChildren()
            {
                Children.Clear();
                ChildNodes.Clear();
                float rowY = Y;
                float rowH = 28;

                // Header row
                var hdr = new Label { Text = "Name | Type | PK | FK | Null", Height = rowH, Width = Width };
                try { hdr.IsStatic = true; } catch { }
                Children.Add(hdr);
                rowY += rowH + 4;

                // Rows
                for (int i = 0; i < _columns.Count; i++)
                {
                    var col = _columns[i];
                    float x = 0;

                    var nameTb = new TextBox { Text = col.Name, Width = 140, Height = 24, X = x, Y = rowY };
                    try { nameTb.IsStatic = true; } catch { }
                    nameTb.TextChanged += (s, e) => { col.Name = nameTb.Text; _onChanged?.Invoke(_columns); };
                    Children.Add(nameTb); x += 150;

                    // DataType dropdown with common choices + merged extras and "+ Custom…"
                    var typeDd = new Dropdown { Width = 110, Height = 24, X = x, Y = rowY };
                    try { typeDd.IsStatic = true; } catch { }
                    foreach (var t in _typeChoices)
                        typeDd.Items.Add(new Dropdown.DropdownItem(t, t));
                    // Allow existing custom type to be preserved/selectable
                    if (!string.IsNullOrWhiteSpace(col.DataType) && !_typeChoices.Contains(col.DataType, StringComparer.OrdinalIgnoreCase))
                        typeDd.Items.Add(new Dropdown.DropdownItem(col.DataType, col.DataType));
                    // Append + Custom… sentinel
                    typeDd.Items.Add(new Dropdown.DropdownItem("+ Custom…", "__custom__"));
                    typeDd.SelectedValue = col.DataType;
                    typeDd.SelectedItemChanged += (s, item) =>
                    {
                        if (item?.Value == "__custom__")
                        {
                            // Inline textbox to enter custom type
                            var customTb = new TextBox { Text = string.IsNullOrWhiteSpace(col.DataType) ? string.Empty : col.DataType, Width = 110, Height = 24, X = x, Y = rowY };
                            try { customTb.IsStatic = true; } catch { }
                            customTb.LostFocus += (s2, e2) => { col.DataType = customTb.Text?.Trim() ?? string.Empty; BuildChildren(); _onChanged?.Invoke(_columns); };
                            Children.Add(customTb);
                        }
                        else
                        {
                            col.DataType = item?.Value ?? string.Empty; _onChanged?.Invoke(_columns);
                        }
                    };
                    Children.Add(typeDd); x += 120;

                    var pkCb = new Checkbox { IsChecked = col.IsPrimaryKey, Width = 24, Height = 24, X = x, Y = rowY };
                    try { pkCb.IsStatic = true; } catch { }
                    pkCb.CheckStateChanged += (s, e) => { col.IsPrimaryKey = pkCb.IsChecked; _onChanged?.Invoke(_columns); };
                    Children.Add(pkCb); x += 40;

                    var fkCb = new Checkbox { IsChecked = col.IsForeignKey, Width = 24, Height = 24, X = x, Y = rowY };
                    try { fkCb.IsStatic = true; } catch { }
                    fkCb.CheckStateChanged += (s, e) => { col.IsForeignKey = fkCb.IsChecked; _onChanged?.Invoke(_columns); };
                    Children.Add(fkCb); x += 40;

                    var nullCb = new Checkbox { IsChecked = col.IsNullable, Width = 24, Height = 24, X = x, Y = rowY };
                    try { nullCb.IsStatic = true; } catch { }
                    nullCb.CheckStateChanged += (s, e) => { col.IsNullable = nullCb.IsChecked; _onChanged?.Invoke(_columns); };
                    Children.Add(nullCb); x += 40;

                    // Remove button
                    var removeBtn = new Button { Text = "Delete", Width = 70, Height = 24, X = x, Y = rowY };
                    try { removeBtn.IsStatic = true; } catch { }
                    int idx = i;
                    removeBtn.Clicked += (s, e) => { if (idx >= 0 && idx < _columns.Count) { _columns.RemoveAt(idx); BuildChildren(); _onChanged?.Invoke(_columns); } };
                    Children.Add(removeBtn);

                    rowY += rowH + 4;
                }

                // Add button
                var addBtn = new Button { Text = "+ Add Column", Width = 120, Height = 26, X = 0, Y = rowY };
                try { addBtn.IsStatic = true; } catch { }
                addBtn.Clicked += (s, e) =>
                {
                    _columns.Add(new Beep.Skia.Model.ColumnDefinition { Name = "NewColumn", DataType = "string" });
                    BuildChildren();
                    _onChanged?.Invoke(_columns);
                };
                Children.Add(addBtn);
            }

            protected override void DrawContent(SKCanvas canvas, DrawingContext context)
            {
                // Background box
                using var bg = new SKPaint { Color = new SKColor(245, 245, 245), Style = SKPaintStyle.Fill, IsAntialias = true };
                canvas.DrawRect(new SKRect(X, Y, X + Width, Y + Height), bg);

                // Let children render; Update positions relative to this container
                foreach (var child in Children)
                {
                    // Ensure child is placed relative to panel origin
                    child.X = this.X + child.X; // absolute placement retained by consumers
                    child.Y = this.Y + child.Y;
                    child.Draw(canvas, context);
                    // Reset to logical positions to avoid drift on next layout
                    child.X -= this.X;
                    child.Y -= this.Y;
                }
            }
        }

        /// <summary>
        /// Lightweight list editor panel for simple arrays of objects. Renders rows using callbacks.
        /// </summary>
        private class SimpleListEditor<T> : SkiaComponent
        {
            private readonly List<T> _items;
            private readonly string _header;
            private readonly Func<T, float, Action, IEnumerable<SkiaComponent>> _renderRow;
            private readonly Action<List<T>> _commit;

            public SimpleListEditor(List<T> items, string drawHeader, Func<T, float, Action, IEnumerable<SkiaComponent>> onRenderRow, Action<List<T>> onCommit)
            {
                ShowInPalette = false;
                _items = items ?? new List<T>();
                _header = drawHeader ?? string.Empty;
                _renderRow = onRenderRow;
                _commit = onCommit;
                Build();
            }

            private void Build()
            {
                Children.Clear();
                float y = 0f;
                var hdr = new Label { Text = _header, Height = 24, Width = this.Width };
                try { hdr.IsStatic = true; } catch { }
                Children.Add(hdr);
                y += 28f;

                for (int i = 0; i < _items.Count; i++)
                {
                    var item = _items[i];
                    void SetDirty()
                    {
                        try { _commit?.Invoke(_items); } catch { }
                    }
                    var rowControls = _renderRow(item, y, SetDirty) ?? Array.Empty<SkiaComponent>();
                    foreach (var c in rowControls) { try { c.IsStatic = true; } catch { } Children.Add(c); }
                    // Delete button
                    var del = new Button { Text = "Delete", Width = 70, Height = 24, X = this.Width - 80, Y = y };
                    try { del.IsStatic = true; } catch { }
                    int idx = i;
                    del.Clicked += (s, e) => { if (idx >= 0 && idx < _items.Count) { _items.RemoveAt(idx); Build(); try { _commit?.Invoke(_items); } catch { } } };
                    Children.Add(del);
                    y += 28f;
                }

                var add = new Button { Text = "+ Add", Width = 80, Height = 26, X = 0, Y = y };
                try { add.IsStatic = true; } catch { }
                add.Clicked += (s, e) => { try { _items.Add(Activator.CreateInstance<T>()); Build(); _commit?.Invoke(_items); } catch { } };
                Children.Add(add);
            }

            protected override void DrawContent(SKCanvas canvas, DrawingContext context)
            {
                using var bg = new SKPaint { Color = new SKColor(245, 245, 245), Style = SKPaintStyle.Fill, IsAntialias = true };
                canvas.DrawRect(new SKRect(X, Y, X + Width, Y + Height), bg);
                foreach (var child in Children)
                {
                    child.X = this.X + child.X;
                    child.Y = this.Y + child.Y;
                    child.Draw(canvas, context);
                    child.X -= this.X; child.Y -= this.Y;
                }
            }
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
            try { header.IsStatic = true; } catch { }
            header.Tag = "section";
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
                OnPropertyValueChanged();
            });

            // Position X
            AddNumericProperty("X", _selectedComponent.X, value =>
            {
                _selectedComponent.X = value;
                _isDirty = true;
                OnPropertyValueChanged();
            });

            // Position Y
            AddNumericProperty("Y", _selectedComponent.Y, value =>
            {
                _selectedComponent.Y = value;
                _isDirty = true;
                OnPropertyValueChanged();
            });

            // Width
            AddNumericProperty("Width", _selectedComponent.Width, value =>
            {
                _selectedComponent.Width = value;
                _isDirty = true;
                OnPropertyValueChanged();
            });

            // Height
            AddNumericProperty("Height", _selectedComponent.Height, value =>
            {
                _selectedComponent.Height = value;
                _isDirty = true;
                OnPropertyValueChanged();
            });

            // Display Text
            AddTextProperty("Display Text", _selectedComponent.DisplayText, value =>
            {
                _selectedComponent.DisplayText = value;
                _isDirty = true;
                OnPropertyValueChanged();
            });

            // Show Display Text
            AddBooleanProperty("Show Text", _selectedComponent.ShowDisplayText, value =>
            {
                _selectedComponent.ShowDisplayText = value;
                _isDirty = true;
                OnPropertyValueChanged();
            });

            // Text Position
            AddEnumProperty("Text Position", _selectedComponent.TextPosition, typeof(TextPosition), value =>
            {
                _selectedComponent.TextPosition = (TextPosition)value;
                _isDirty = true;
                OnPropertyValueChanged();
            });

            // Text Font Size
            AddNumericProperty("Text Size", _selectedComponent.TextFontSize, value =>
            {
                _selectedComponent.TextFontSize = value;
                _isDirty = true;
                OnPropertyValueChanged();
            });

            // Text Color (hex or R,G,B[,A])
            AddColorProperty("Text Color", _selectedComponent.TextColor, value =>
            {
                _selectedComponent.TextColor = value;
                _isDirty = true;
                OnPropertyValueChanged();
            });

            // Is Visible
            AddBooleanProperty("Visible", _selectedComponent.IsVisible, value =>
            {
                _selectedComponent.IsVisible = value;
                _isDirty = true;
                OnPropertyValueChanged();
            });

            // Is Enabled
            AddBooleanProperty("Enabled", _selectedComponent.IsEnabled, value =>
            {
                _selectedComponent.IsEnabled = value;
                _isDirty = true;
                OnPropertyValueChanged();
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
                // Add special property grids for specific ETL components
                AddETLSpecialProperties(_selectedComponent);
            }
            // Check for ERD components
            else if (componentType.Namespace == "Beep.Skia.ERD")
            {
                AddERDProperties(_selectedComponent);
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
            // PM nodes
            else if (componentType.Namespace == "Beep.Skia.PM")
            {
                AddPMProperties(_selectedComponent);
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

            // Add an on-demand inference button when the component supports inference
            try
            {
                var hasInference = component.GetType().GetMethod("InferOutputSchemaFromUpstreams", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance, binder: null, types: new[] { typeof(Func<int, string>) }, modifiers: null) != null;
                if (hasInference)
                {
                    var inferBtnLabel = new Label { Text = "", Width = 100, Height = 25, X = 10, Y = _currentY };
                    try { inferBtnLabel.IsStatic = true; } catch { }
                    inferBtnLabel.Tag = "prop-label";

                    var inferBtn = new Button { Text = "Infer Output Schema", Width = Math.Max(140, this.Width - 120), Height = 28 };
                    try { inferBtn.IsStatic = true; } catch { }
                    inferBtn.Tag = "prop-value";
                    inferBtn.Clicked += (s, e) => { try { this.Manager?.InferOutputSchemaForComponent(component); } catch { } };

                    _childComponents.Add(inferBtnLabel);
                    _childComponents.Add(inferBtn);
                    _currentY += 35;
                }
            }
            catch { }
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
        /// Adds special property editors for ETL components (DerivedColumns, Conditions, DataQualityRules).
        /// </summary>
        private void AddETLSpecialProperties(SkiaComponent component)
        {
            var type = component.GetType();

            // ETLDerivedColumn: DerivedColumns grid
            if (type.Name == "ETLDerivedColumn")
            {
                if (component.NodeProperties.TryGetValue("DerivedColumns", out var dcProp))
                {
                    TryAddSchemaGridEditor(component, "derivedcolumns", dcProp);
                }
            }

            // ETLConditionalSplit: Conditions grid
            if (type.Name == "ETLConditionalSplit")
            {
                if (component.NodeProperties.TryGetValue("Conditions", out var condProp))
                {
                    TryAddSchemaGridEditor(component, "conditions", condProp);
                }
            }

            // ETLTarget: DataQualityRules grid
            if (type.Name == "ETLTarget")
            {
                if (component.NodeProperties.TryGetValue("DataQualityRules", out var dqProp))
                {
                    TryAddSchemaGridEditor(component, "dataqualityrules", dqProp);
                }
            }
        }

        /// <summary>
        /// Adds properties for ERD components.
        /// </summary>
        private void AddERDProperties(SkiaComponent component)
        {
            var type = component.GetType();
            
            // ERDEntity: DDL Export button (stores DDL in LastGeneratedDDL property)
            if (type.Name == "ERDEntity")
            {
                var exportLabel = new Label { Text = "", Width = 100, Height = 25, X = 10, Y = _currentY };
                try { exportLabel.IsStatic = true; } catch { }
                exportLabel.Tag = "prop-label";

                var exportBtn = new Button { Text = "Export DDL", Width = Math.Max(140, this.Width - 120), Height = 28 };
                try { exportBtn.IsStatic = true; } catch { }
                exportBtn.Tag = "prop-value";
                exportBtn.Clicked += (s, e) =>
                {
                    try
                    {
                        // Use reflection to avoid direct type dependency
                        var exporterType = Type.GetType("Beep.Skia.ERD.DDLExporter, Beep.Skia.ERD");
                        if (exporterType != null)
                        {
                            var dialectType = exporterType.GetNestedType("SQLDialect");
                            var ansiValue = dialectType?.GetField("ANSI")?.GetValue(null);
                            var exporter = Activator.CreateInstance(exporterType, ansiValue);
                            var exportMethod = exporterType.GetMethod("ExportEntity");
                            var ddl = exportMethod?.Invoke(exporter, new[] { component }) as string;
                            
                            // Store DDL in component's Tag for retrieval by host
                            if (!string.IsNullOrWhiteSpace(ddl))
                            {
                                component.Tag = new { DDL = ddl, GeneratedAt = DateTime.UtcNow };
                                // Host can listen to component.PropertyChanged or check Tag after button click
                                // For now, just mark as dirty to trigger save
                                _isDirty = true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Store error in Tag for host to handle
                        component.Tag = new { Error = ex.Message };
                    }
                };

                _childComponents.Add(exportLabel);
                _childComponents.Add(exportBtn);
                _currentY += 35;
            }
            
            // ERDRelationship: Identifying checkbox
            if (type.Name == "ERDRelationship")
            {
                var identProp = type.GetProperty("Identifying");
                if (identProp != null)
                {
                    AddBooleanProperty("Identifying", (bool)(identProp.GetValue(component) ?? false), value =>
                    {
                        identProp.SetValue(component, value);
                        _isDirty = true;
                    });
                }
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
        /// Adds properties for Project Management (PM) components using reflection.
        /// Supports common port counts and typical PM fields like Title/Label/PercentComplete.
        /// </summary>
        private void AddPMProperties(SkiaComponent component)
        {
            var type = component.GetType();

            // Common counts on PMControl: InPortCount / OutPortCount
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

            // Typical PM properties by convention
            var titleProp = type.GetProperty("Title");
            if (titleProp != null)
            {
                AddTextProperty("Title", (string)(titleProp.GetValue(component) ?? string.Empty), s =>
                {
                    titleProp.SetValue(component, s);
                    _isDirty = true;
                });
            }

            var labelProp = type.GetProperty("Label");
            if (labelProp != null)
            {
                AddTextProperty("Label", (string)(labelProp.GetValue(component) ?? string.Empty), s =>
                {
                    labelProp.SetValue(component, s);
                    _isDirty = true;
                });
            }

            var percentProp = type.GetProperty("PercentComplete");
            if (percentProp != null)
            {
                var val = percentProp.GetValue(component);
                float f = 0f;
                try { f = Convert.ToSingle(val ?? 0); } catch { }
                AddNumericProperty("% Complete", f, v =>
                {
                    // clamp 0..100
                    var iv = (int)MathF.Round(Math.Clamp(v, 0, 100));
                    percentProp.SetValue(component, iv);
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
            try { textBox.IsStatic = true; } catch { }

            // When any textbox gains focus, make it the active input for keyboard routing
            textBox.GotFocus += (s, e) => { _focusedTextBox = (TextBox)s; };
            textBox.LostFocus += (s, e) => { if (ReferenceEquals(_focusedTextBox, s)) _focusedTextBox = null; };

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
            try { labelControl.IsStatic = true; } catch { }
            labelControl.Tag = "prop-label";

            textBox.Tag = "prop-value";

            // X/Y will be positioned in UpdateLayout relative to the editor panel

            _childComponents.Add(labelControl);
            _childComponents.Add(textBox);
            _propertyControls[label] = textBox;
            _currentY += 35;
        }

        /// <summary>
        /// Adds a multiline text property control (useful for JSON or row lists).
        /// </summary>
        private void AddMultilineTextProperty(string label, string value, Action<string> onChanged, int lines = 5)
        {
            var textBox = new TextBox
            {
                Text = value ?? "",
                Width = this.Width - 120,
                Height = Math.Max(25 * lines, 60),
                IsMultiline = true
            };
            try { textBox.IsStatic = true; } catch { }

            textBox.GotFocus += (s, e) => { _focusedTextBox = (TextBox)s; };
            textBox.LostFocus += (s, e) => { if (ReferenceEquals(_focusedTextBox, s)) _focusedTextBox = null; };
            textBox.TextChanged += (s, e) => onChanged?.Invoke(textBox.Text);

            var labelControl = new Label
            {
                Text = label + ":",
                Width = 100,
                Height = 25,
                X = 10,
                Y = _currentY
            };
            try { labelControl.IsStatic = true; } catch { }
            labelControl.Tag = "prop-label";

            textBox.Tag = "prop-value";

            _childComponents.Add(labelControl);
            _childComponents.Add(textBox);
            _propertyControls[label] = textBox;
            _currentY += textBox.Height + 10;
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
            try { textBox.IsStatic = true; } catch { }

            textBox.GotFocus += (s, e) => { _focusedTextBox = (TextBox)s; };
            textBox.LostFocus += (s, e) => { if (ReferenceEquals(_focusedTextBox, s)) _focusedTextBox = null; };

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
            try { labelControl.IsStatic = true; } catch { }
            labelControl.Tag = "prop-label";
            textBox.Tag = "prop-value";

            // Positioned in UpdateLayout

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
            var checkbox = new Checkbox
            {
                IsChecked = value,
                Width = 24,
                Height = 24
            };
            try { checkbox.IsStatic = true; } catch { }

            checkbox.CheckStateChanged += (s, e) =>
            {
                onChanged?.Invoke(checkbox.IsChecked);
            };

            var labelControl = new Label
            {
                Text = label + ":",
                Width = 100,
                Height = 25,
                X = 10,
                Y = _currentY
            };
            try { labelControl.IsStatic = true; } catch { }
            labelControl.Tag = "prop-label";
            checkbox.Tag = "prop-value";

            _childComponents.Add(labelControl);
            _childComponents.Add(checkbox);
            _propertyControls[label] = checkbox;
            _currentY += 35;
        }

        /// <summary>
        /// Adds an enum property control.
        /// </summary>
        private void AddEnumProperty(string label, object value, Type enumType, Action<object> onChanged)
        {
            var dropdown = new Dropdown
            {
                Width = this.Width - 120,
                Height = 28
            };
            try { dropdown.IsStatic = true; } catch { }

            // Populate items from enum names
            foreach (var name in Enum.GetNames(enumType))
            {
                dropdown.Items.Add(new Dropdown.DropdownItem(name, name));
            }

            dropdown.SelectedValue = value?.ToString();
            dropdown.Tag = "prop-value";
            dropdown.SelectedItemChanged += (s, item) =>
            {
                if (item?.Value == null) return;
                if (Enum.TryParse(enumType, item.Value, true, out var parsed))
                {
                    onChanged?.Invoke(parsed);
                    OnPropertyValueChanged();
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
            try { labelControl.IsStatic = true; } catch { }
            labelControl.Tag = "prop-label";

            _childComponents.Add(labelControl);
            _childComponents.Add(dropdown);
            _propertyControls[label] = dropdown;
            _currentY += 35;
        }
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
            try { textBox.IsStatic = true; } catch { }

            textBox.GotFocus += (s, e) => { _focusedTextBox = (TextBox)s; };
            textBox.LostFocus += (s, e) => { if (ReferenceEquals(_focusedTextBox, s)) _focusedTextBox = null; };

            textBox.TextChanged += (s, e) =>
            {
                if (TryParseColor(textBox.Text, out var c))
                {
                    onChanged(c);
                    OnPropertyValueChanged();
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
            try { labelControl.IsStatic = true; } catch { }
            labelControl.Tag = "prop-label";
            textBox.Tag = "prop-value";

            // Positioned in UpdateLayout

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
            try
            {
                // Apply NodeProperties back to the selected component's public setters (generic flow)
                if (_selectedComponent != null && _selectedComponent.NodeProperties != null && _selectedComponent.NodeProperties.Count > 0)
                {
                    var dict = new System.Collections.Generic.Dictionary<string, object>(System.StringComparer.OrdinalIgnoreCase);
                    foreach (var kv in _selectedComponent.NodeProperties)
                    {
                        var p = kv.Value;
                        var val = p?.ParameterCurrentValue ?? p?.DefaultParameterValue;
                        dict[kv.Key] = val;
                    }
                    // applyToPublicSetters: true, updateNodeProperties: false (we're the source of truth here)
                    _selectedComponent.SetProperties(dict, updateNodeProperties: false, applyToPublicSetters: true);
                }
            }
            catch { }

            PropertiesSaved?.Invoke(this, new ComponentPropertiesSavedEventArgs(_selectedComponent));
            _isDirty = false;
        }

        private void OnPropertyValueChanged()
        {
            try { PropertyValueChanged?.Invoke(this, EventArgs.Empty); } catch { }
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

            // Draw child components; render expanded dropdowns last so their popup list stays on top
            foreach (var component in _childComponents)
            {
                if (component is Dropdown dd && dd.IsExpanded)
                {
                    continue; // defer
                }
                component.Draw(canvas, context);
            }
            // Now draw any expanded dropdowns on top
            foreach (var dd in _childComponents.OfType<Dropdown>())
            {
                if (dd.IsExpanded)
                {
                    dd.Draw(canvas, context);
                }
            }
        }

        // --- Input routing for embedded controls ---
        public override bool HandleMouseDown(SKPoint point, InteractionContext context)
        {
            // Forward to children in reverse order (topmost first)
            for (int i = _childComponents.Count - 1; i >= 0; i--)
            {
                var child = _childComponents[i];
                if (!child.IsVisible || !child.IsEnabled) continue;
                // Use the child's own hit test to support controls with dynamic bounds (e.g., expanded dropdown)
                if (child.ContainsPoint(point))
                {
                    if (child.HandleMouseDown(point, context)) return true;
                }
            }
            // If the click is inside the editor panel itself, consume it to prevent canvas selection/drag.
            if (new SKRect(X, Y, X + Width, Y + Height).Contains(point))
            {
                // If click is inside the editor but no child handled it,
                // close any expanded dropdowns to commit/cancel their state.
                foreach (var dd in _childComponents.OfType<Dropdown>())
                {
                    if (dd.IsExpanded)
                    {
                        dd.CloseDropdown();
                    }
                }
                return true;
            }
            return base.HandleMouseDown(point, context);
        }

        public override bool HandleMouseMove(SKPoint point, InteractionContext context)
        {
            for (int i = _childComponents.Count - 1; i >= 0; i--)
            {
                var child = _childComponents[i];
                if (!child.IsVisible || !child.IsEnabled) continue;
                if (child.ContainsPoint(point))
                {
                    if (child.HandleMouseMove(point, context)) return true;
                }
            }
            // Consume hover within editor to avoid unintended canvas hover effects
            if (new SKRect(X, Y, X + Width, Y + Height).Contains(point))
            {
                return true;
            }
            return base.HandleMouseMove(point, context);
        }

        public override bool HandleMouseUp(SKPoint point, InteractionContext context)
        {
            for (int i = _childComponents.Count - 1; i >= 0; i--)
            {
                var child = _childComponents[i];
                if (!child.IsVisible || !child.IsEnabled) continue;
                if (child.ContainsPoint(point))
                {
                    if (child.HandleMouseUp(point, context)) return true;
                }
            }
            if (new SKRect(X, Y, X + Width, Y + Height).Contains(point))
            {
                return true;
            }
            return base.HandleMouseUp(point, context);
        }

        // Receive keyboard input from host and route to focused textbox
        public void HandleKeyChar(char ch)
        {
            if (_focusedTextBox == null) return;
            if (ch == '\r' || ch == '\n') return; // ignore enter in single-line
            if (ch == '\b')
            {
                _focusedTextBox.DeleteText(forward: false);
            }
            else if (!char.IsControl(ch))
            {
                _focusedTextBox.InsertText(ch.ToString());
            }
        }

        public void HandleKeyDown(PropertyEditorKey key)
        {
            if (_focusedTextBox == null) return;
            switch (key)
            {
                case PropertyEditorKey.Left:
                    _focusedTextBox.MoveCursor(-1); break;
                case PropertyEditorKey.Right:
                    _focusedTextBox.MoveCursor(1); break;
                case PropertyEditorKey.Back:
                    _focusedTextBox.DeleteText(false); break;
                case PropertyEditorKey.Delete:
                    _focusedTextBox.DeleteText(true); break;
                case PropertyEditorKey.Home:
                    // move to start
                    while (_focusedTextBox.HasFocus) { _focusedTextBox.MoveCursor(-10000); break; }
                    break;
                case PropertyEditorKey.End:
                    while (_focusedTextBox.HasFocus) { _focusedTextBox.MoveCursor(10000); break; }
                    break;
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