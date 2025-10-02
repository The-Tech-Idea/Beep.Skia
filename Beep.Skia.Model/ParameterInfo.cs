using System;

namespace Beep.Skia.Model
{
    /// <summary>
    /// Describes a configurable parameter for a component/node, including type and values.
    /// </summary>
    public class ParameterInfo
    {
        /// <summary>
        /// Logical parameter name (unique within the owning component's Parameters dictionary key).
        /// </summary>
        public string ParameterName { get; set; } = string.Empty;

        /// <summary>
        /// CLR type of the parameter (e.g., typeof(string), typeof(int), typeof(bool), enum types, etc.).
        /// If null, the editor will infer type based on the current/default value runtime type.
        /// </summary>
        public Type ParameterType { get; set; }

        /// <summary>
        /// Default value used when no current value has been set.
        /// </summary>
        public object DefaultParameterValue { get; set; }

        /// <summary>
        /// The current value (user-edited) for this parameter.
        /// </summary>
        public object ParameterCurrentValue { get; set; }

        /// <summary>
        /// Optional description/help for UI.
        /// </summary>
        public string Description { get; set; } = string.Empty;
        /// <summary>
        /// Optional list of allowed string choices for this parameter. When provided,
        /// the editor will render a dropdown even if ParameterType is string.
        /// </summary>
        public string[] Choices { get; set; }
    }
}
