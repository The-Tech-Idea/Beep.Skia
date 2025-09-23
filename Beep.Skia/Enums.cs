using System;
using Beep.Skia.Model;
namespace Beep.Skia
{
    /// <summary>
    /// Specifies the alignment of text within a control.
    /// </summary>
    public enum TextAlignment
    {
        /// <summary>
        /// Text is aligned to the left.
        /// </summary>
        Left,

        /// <summary>
        /// Text is centered.
        /// </summary>
        Center,

        /// <summary>
        /// Text is aligned to the right.
        /// </summary>
        Right,

        /// <summary>
        /// Text is justified.
        /// </summary>
        Justify
    }

    /// <summary>
    /// Specifies the position of text relative to other elements.
    /// </summary>
    public enum TextPosition
    {
        /// <summary>
        /// Text is positioned to the left.
        /// </summary>
        Left,

        /// <summary>
        /// Text is positioned to the right.
        /// </summary>
        Right,

        /// <summary>
        /// Text is positioned at the above.
        /// </summary>
        Above,

        /// <summary>
        /// Text is positioned at the below.
        /// </summary>
        Below,

        /// <summary>
        /// Text is centered.
        /// </summary>

        Inside
    }

    /// <summary>
    /// Specifies the mouse button that was pressed.
    /// </summary>
    public enum MouseButton
    {
        /// <summary>
        /// The left mouse button.
        /// </summary>
        Left,

        /// <summary>
        /// The right mouse button.
        /// </summary>
        Right,

        /// <summary>
        /// The middle mouse button.
        /// </summary>
        Middle,

        /// <summary>
        /// The first extended mouse button.
        /// </summary>
        XButton1,

        /// <summary>
        /// The second extended mouse button.
        /// </summary>
        XButton2
    }

    /// <summary>
    /// Specifies the state of modifier keys on the keyboard.
    /// </summary>
    [Flags]
    public enum KeyModifiers
    {
        /// <summary>
        /// No modifier keys are pressed.
        /// </summary>
        None = 0,

        /// <summary>
        /// The Control key is pressed.
        /// </summary>
        Control = 1 << 0,

        /// <summary>
        /// The Shift key is pressed.
        /// </summary>
        Shift = 1 << 1,

        /// <summary>
        /// The Alt key is pressed.
        /// </summary>
        Alt = 1 << 2,

        /// <summary>
        /// The Windows key (or Command key on Mac) is pressed.
        /// </summary>
        Meta = 1 << 3
    }

    /// <summary>
    /// Specifies the orientation of a control or layout.
    /// </summary>
    public enum Orientation
    {
        /// <summary>
        /// Horizontal orientation.
        /// </summary>
        Horizontal,

        /// <summary>
        /// Vertical orientation.
        /// </summary>
        Vertical
    }

    /// <summary>
    /// Specifies the border style of a control.
    /// </summary>
    public enum BorderStyle
    {
        /// <summary>
        /// No border.
        /// </summary>
        None,

        /// <summary>
        /// A single line border.
        /// </summary>
        Single,

        /// <summary>
        /// A 3D border.
        /// </summary>
        Fixed3D,

        /// <summary>
        /// A single line border that appears inset.
        /// </summary>
        FixedSingle
    }

    /// <summary>
    /// Specifies the selection mode for list controls.
    /// </summary>
    public enum SelectionMode
    {
        /// <summary>
        /// No items can be selected.
        /// </summary>
        None,

        /// <summary>
        /// Only one item can be selected.
        /// </summary>
        Single,

        /// <summary>
        /// Multiple items can be selected.
        /// </summary>
        MultiSimple,

        /// <summary>
        /// Multiple items can be selected with extended selection.
        /// </summary>
        MultiExtended
    }

    /// <summary>
    /// Specifies the style of a button.
    /// </summary>
    public enum ButtonStyle
    {
        /// <summary>
        /// Standard button style.
        /// </summary>
        Standard,

        /// <summary>
        /// Flat button style.
        /// </summary>
        Flat,

        /// <summary>
        /// Popup button style.
        /// </summary>
        Popup,

        /// <summary>
        /// System button style.
        /// </summary>
        System
    }

    /// <summary>
    /// Specifies the style of a combo box.
    /// </summary>
    public enum ComboBoxStyle
    {
        /// <summary>
        /// Standard combo box style.
        /// </summary>
        Standard,

        /// <summary>
        /// Drop-down combo box style.
        /// </summary>
        DropDown,

        /// <summary>
        /// Drop-down list style.
        /// </summary>
        DropDownList,

        /// <summary>
        /// Simple combo box style.
        /// </summary>
        Simple
    }

    /// <summary>
    /// Specifies the style of a list box.
    /// </summary>
    public enum ListBoxStyle
    {
        /// <summary>
        /// Standard list box style.
        /// </summary>
        Standard,

        /// <summary>
        /// Fixed single border.
        /// </summary>
        FixedSingle,

        /// <summary>
        /// No border.
        /// </summary>
        None
    }

    /// <summary>
    /// Specifies the style of a text box.
    /// </summary>
    public enum TextBoxStyle
    {
        /// <summary>
        /// Standard text box style.
        /// </summary>
        Standard,

        /// <summary>
        /// Multi-line text box style.
        /// </summary>
        MultiLine,

        /// <summary>
        /// Password text box style.
        /// </summary>
        Password
    }

    /// <summary>
    /// Specifies the style of a label.
    /// </summary>
    public enum LabelStyle
    {
        /// <summary>
        /// Standard label style.
        /// </summary>
        Standard,

        /// <summary>
        /// Link label style.
        /// </summary>
        Link
    }

    /// <summary>
    /// Specifies the style of a panel.
    /// </summary>
    public enum PanelStyle
    {
        /// <summary>
        /// Standard panel style.
        /// </summary>
        Standard,

        /// <summary>
        /// Group box style.
        /// </summary>
        GroupBox
    }

    /// <summary>
    /// Specifies the style of a progress bar.
    /// </summary>
    public enum ProgressBarStyle
    {
        /// <summary>
        /// Standard progress bar style.
        /// </summary>
        Standard,

        /// <summary>
        /// Marquee progress bar style.
        /// </summary>
        Marquee,

        /// <summary>
        /// Continuous progress bar style.
        /// </summary>
        Continuous
    }

    /// <summary>
    /// Specifies the style of a radio button.
    /// </summary>
    public enum RadioButtonStyle
    {
        /// <summary>
        /// Standard radio button style.
        /// </summary>
        Standard,

        /// <summary>
        /// Button radio button style.
        /// </summary>
        Button
    }

    /// <summary>
    /// Specifies the size of a radio button.
    /// </summary>
    public enum RadioButtonSize
    {
        /// <summary>
        /// Small radio button size.
        /// </summary>
        Small,

        /// <summary>
        /// Medium radio button size.
        /// </summary>
        Medium,

        /// <summary>
        /// Large radio button size.
        /// </summary>
        Large
    }

    /// <summary>
    /// Specifies the style of a check box.
    /// </summary>
    public enum CheckBoxStyle
    {
        /// <summary>
        /// Standard check box style.
        /// </summary>
        Standard,

        /// <summary>
        /// Button check box style.
        /// </summary>
        Button,

        /// <summary>
        /// Switch check box style.
        /// </summary>
        Switch
    }

    /// <summary>
    /// Specifies the style of a slider.
    /// </summary>
    public enum SliderStyle
    {
        /// <summary>
        /// Standard slider style.
        /// </summary>
        Standard,

        /// <summary>
        /// Range slider style.
        /// </summary>
        Range
    }

    /// <summary>
    /// Specifies the orientation of a slider.
    /// </summary>
    public enum SliderOrientation
    {
        /// <summary>
        /// Horizontal slider orientation.
        /// </summary>
        Horizontal,

        /// <summary>
        /// Vertical slider orientation.
        /// </summary>
        Vertical
    }

    /// <summary>
    /// Specifies the style of a scroll bar.
    /// </summary>
    public enum ScrollBarStyle
    {
        /// <summary>
        /// Standard scroll bar style.
        /// </summary>
        Standard,

        /// <summary>
        /// Flat scroll bar style.
        /// </summary>
        Flat
    }

    /// <summary>
    /// Specifies the orientation of a scroll bar.
    /// </summary>
    public enum ScrollBarOrientation
    {
        /// <summary>
        /// Horizontal scroll bar orientation.
        /// </summary>
        Horizontal,

        /// <summary>
        /// Vertical scroll bar orientation.
        /// </summary>
        Vertical
    }

    /// <summary>
    /// Specifies the style of a spinner.
    /// </summary>
    public enum SpinnerStyle
    {
        /// <summary>
        /// Standard spinner style.
        /// </summary>
        Standard,

        /// <summary>
        /// Ring spinner style.
        /// </summary>
        Ring,

        /// <summary>
        /// Bar spinner style.
        /// </summary>
        Bar
    }

    /// <summary>
    /// Specifies the style of a splitter.
    /// </summary>
    public enum SplitterStyle
    {
        /// <summary>
        /// Standard splitter style.
        /// </summary>
        Standard,

        /// <summary>
        /// Flat splitter style.
        /// </summary>
        Flat
    }

    /// <summary>
    /// Specifies the orientation of a splitter.
    /// </summary>
    public enum SplitterOrientation
    {
        /// <summary>
        /// Horizontal splitter orientation.
        /// </summary>
        Horizontal,

        /// <summary>
        /// Vertical splitter orientation.
        /// </summary>
        Vertical
    }

    /// <summary>
    /// Specifies the style of a status bar.
    /// </summary>
    public enum StatusBarStyle
    {
        /// <summary>
        /// Standard status bar style.
        /// </summary>
        Standard,

        /// <summary>
        /// Flat status bar style.
        /// </summary>
        Flat
    }

    /// <summary>
    /// Specifies the style of a tab control.
    /// </summary>
    public enum TabStyle
    {
        /// <summary>
        /// Standard tab style.
        /// </summary>
        Standard,

        /// <summary>
        /// Flat tab style.
        /// </summary>
        Flat,

        /// <summary>
        /// Buttons tab style.
        /// </summary>
        Buttons
    }

    /// <summary>
    /// Specifies the position of tabs in a tab control.
    /// </summary>
    public enum TabPosition
    {
        /// <summary>
        /// Tabs are positioned at the top.
        /// </summary>
        Top,

        /// <summary>
        /// Tabs are positioned at the bottom.
        /// </summary>
        Bottom,

        /// <summary>
        /// Tabs are positioned on the left.
        /// </summary>
        Left,

        /// <summary>
        /// Tabs are positioned on the right.
        /// </summary>
        Right
    }

    /// <summary>
    /// Specifies the style of a toggle button.
    /// </summary>
    public enum ToggleButtonStyle
    {
        /// <summary>
        /// Standard toggle button style.
        /// </summary>
        Standard,

        /// <summary>
        /// Flat toggle button style.
        /// </summary>
        Flat
    }

    /// <summary>
    /// Specifies the style of a check mark.
    /// </summary>
    public enum CheckMarkStyle
    {
        /// <summary>
        /// Check mark style.
        /// </summary>
        Check,

        /// <summary>
        /// Cross mark style.
        /// </summary>
        Cross,

        /// <summary>
        /// Circle mark style.
        /// </summary>
        Circle
    }

    /// <summary>
    /// Specifies the style of a track bar.
    /// </summary>
    public enum TrackBarStyle
    {
        /// <summary>
        /// Standard track bar style.
        /// </summary>
        Standard,

        /// <summary>
        /// Flat track bar style.
        /// </summary>
        Flat
    }

    /// <summary>
    /// Specifies the orientation of a track bar.
    /// </summary>
    public enum TrackBarOrientation
    {
        /// <summary>
        /// Horizontal track bar orientation.
        /// </summary>
        Horizontal,

        /// <summary>
        /// Vertical track bar orientation.
        /// </summary>
        Vertical
    }

    /// <summary>
    /// Specifies the style of a thumb.
    /// </summary>
    public enum ThumbStyle
    {
        /// <summary>
        /// Circle thumb style.
        /// </summary>
        Circle,

        /// <summary>
        /// Square thumb style.
        /// </summary>
        Square,

        /// <summary>
        /// Triangle thumb style.
        /// </summary>
        Triangle
    }

    /// <summary>
    /// Specifies the style of a tool bar.
    /// </summary>
    public enum ToolBarStyle
    {
        /// <summary>
        /// Standard tool bar style.
        /// </summary>
        Standard,

        /// <summary>
        /// Flat tool bar style.
        /// </summary>
        Flat
    }

    /// <summary>
    /// Specifies the orientation of a tool bar.
    /// </summary>
    public enum ToolBarOrientation
    {
        /// <summary>
        /// Horizontal tool bar orientation.
        /// </summary>
        Horizontal,

        /// <summary>
        /// Vertical tool bar orientation.
        /// </summary>
        Vertical
    }

    /// <summary>
    /// Specifies the style of a tool tip.
    /// </summary>
    public enum ToolTipStyle
    {
        /// <summary>
        /// Standard tool tip style.
        /// </summary>
        Standard,

        /// <summary>
        /// Balloon tool tip style.
        /// </summary>
        Balloon
    }

    /// <summary>
    /// Specifies the style of a tree view.
    /// </summary>
    public enum TreeViewStyle
    {
        /// <summary>
        /// Standard tree view style.
        /// </summary>
        Standard,

        /// <summary>
        /// Flat tree view style.
        /// </summary>
        Flat
    }

    /// <summary>
    /// Specifies the style of a menu.
    /// </summary>
    public enum MenuStyle
    {
        /// <summary>
        /// Standard menu style.
        /// </summary>
        Standard,

        /// <summary>
        /// Flat menu style.
        /// </summary>
        Flat
    }

    /// <summary>
    /// Specifies the orientation of a menu.
    /// </summary>
    public enum MenuOrientation
    {
        /// <summary>
        /// Horizontal menu orientation.
        /// </summary>
        Horizontal,

        /// <summary>
        /// Vertical menu orientation.
        /// </summary>
        Vertical
    }

    /// <summary>
    /// Specifies the style of a context menu.
    /// </summary>
    public enum ContextMenuStyle
    {
        /// <summary>
        /// Standard context menu style.
        /// </summary>
        Standard,

        /// <summary>
        /// Flat context menu style.
        /// </summary>
        Flat
    }

    /// <summary>
    /// Specifies the style of a date picker.
    /// </summary>
    public enum DatePickerStyle
    {
        /// <summary>
        /// Standard date picker style.
        /// </summary>
        Standard,

        /// <summary>
        /// Compact date picker style.
        /// </summary>
        Compact
    }

    /// <summary>
    /// Specifies the style of a time picker.
    /// </summary>
    public enum TimePickerStyle
    {
        /// <summary>
        /// Standard time picker style.
        /// </summary>
        Standard,

        /// <summary>
        /// Spinner time picker style.
        /// </summary>
        Spinner
    }

    /// <summary>
    /// Specifies the style of a color picker.
    /// </summary>
    public enum ColorPickerStyle
    {
        /// <summary>
        /// Standard color picker style.
        /// </summary>
        Standard,

        /// <summary>
        /// Compact color picker style.
        /// </summary>
        Compact
    }

    /// <summary>
    /// Specifies the style of a numeric up-down control.
    /// </summary>
    public enum NumericUpDownStyle
    {
        /// <summary>
        /// Standard numeric up-down style.
        /// </summary>
        Standard,

        /// <summary>
        /// Flat numeric up-down style.
        /// </summary>
        Flat
    }

    /// <summary>
    /// Specifies the style of a group box.
    /// </summary>
    public enum GroupBoxStyle
    {
        /// <summary>
        /// Standard group box style.
        /// </summary>
        Standard,

        /// <summary>
        /// Flat group box style.
        /// </summary>
        Flat
    }

    /// <summary>
    /// Specifies the style of a list view.
    /// </summary>
    public enum ListViewStyle
    {
        /// <summary>
        /// Standard list view style.
        /// </summary>
        Standard,

        /// <summary>
        /// Tile list view style.
        /// </summary>
        Tile,

        /// <summary>
        /// Icon list view style.
        /// </summary>
        Icon,

        /// <summary>
        /// Small icon list view style.
        /// </summary>
        SmallIcon,

        /// <summary>
        /// List list view style.
        /// </summary>
        List,

        /// <summary>
        /// Details list view style.
        /// </summary>
        Details
    }

    /// <summary>
    /// Specifies the style of a navigation bar.
    /// </summary>
    public enum NavigationStyle
    {
        /// <summary>
        /// Standard navigation style.
        /// </summary>
        Standard,

        /// <summary>
        /// Flat navigation style.
        /// </summary>
        Flat
    }

    /// <summary>
    /// Specifies the orientation of a navigation bar.
    /// </summary>
    public enum NavigationOrientation
    {
        /// <summary>
        /// Horizontal navigation orientation.
        /// </summary>
        Horizontal,

        /// <summary>
        /// Vertical navigation orientation.
        /// </summary>
        Vertical
    }

    /// <summary>
    /// Specifies the type of a notification.
    /// </summary>
    public enum NotificationType
    {
        /// <summary>
        /// Information notification.
        /// </summary>
        Information,

        /// <summary>
        /// Warning notification.
        /// </summary>
        Warning,

        /// <summary>
        /// Error notification.
        /// </summary>
        Error,

        /// <summary>
        /// Success notification.
        /// </summary>
        Success
    }

    /// <summary>
    /// Specifies the position of a notification.
    /// </summary>
    public enum NotificationPosition
    {
        /// <summary>
        /// Top left position.
        /// </summary>
        TopLeft,

        /// <summary>
        /// Top right position.
        /// </summary>
        TopRight,

        /// <summary>
        /// Bottom left position.
        /// </summary>
        BottomLeft,

        /// <summary>
        /// Bottom right position.
        /// </summary>
        BottomRight,

        /// <summary>
        /// Center position.
        /// </summary>
        Center
    }

    /// <summary>
    /// Specifies the style of a notification.
    /// </summary>
    public enum NotificationStyle
    {
        /// <summary>
        /// Standard notification style.
        /// </summary>
        Standard,

        /// <summary>
        /// Flat notification style.
        /// </summary>
        Flat
    }

    /// <summary>
    /// Specifies the style of a data grid.
    /// </summary>
    public enum DataGridStyle
    {
        /// <summary>
        /// Standard data grid style.
        /// </summary>
        Standard,

        /// <summary>
        /// Flat data grid style.
        /// </summary>
        Flat
    }

    /// <summary>
    /// Specifies the style of a check box group.
    /// </summary>
    public enum CheckBoxGroupStyle
    {
        /// <summary>
        /// Standard check box group style.
        /// </summary>
        Standard,

        /// <summary>
        /// Flat check box group style.
        /// </summary>
        Flat
    }

    /// <summary>
    /// Specifies the style of a radio group.
    /// </summary>
    public enum RadioGroupStyle
    {
        /// <summary>
        /// Standard radio group style.
        /// </summary>
        Standard,

        /// <summary>
        /// Flat radio group style.
        /// </summary>
        Flat
    }

    /// <summary>
    /// Specifies the style of a text area.
    /// </summary>
    public enum TextAreaStyle
    {
        /// <summary>
        /// Standard text area style.
        /// </summary>
        Standard,

        /// <summary>
        /// Rich text area style.
        /// </summary>
        RichText
    }
}
