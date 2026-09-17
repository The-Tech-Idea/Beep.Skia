using System;
using System.Collections.Generic;
using System.Linq;
using Beep.Skia.Model;

namespace Beep.Skia.StateMachine
{
    /// <summary>
    /// Severity of a state-machine validation issue.
    /// </summary>
    public enum StateMachineIssueSeverity { Info, Warning, Error }

    /// <summary>
    /// A single state-machine validation issue.
    /// </summary>
    public class StateMachineIssue
    {
        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        public string Message { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the severity.
        /// </summary>
        public StateMachineIssueSeverity Severity { get; set; } = StateMachineIssueSeverity.Warning;
        /// <summary>
        /// Gets or sets the component.
        /// </summary>
        public SkiaComponent? Component { get; set; }
        /// <summary>
        /// Gets or sets the line.
        /// </summary>
        public IConnectionLine? Line { get; set; }
        /// <summary>
        /// Gets or sets the fix suggestion.
        /// </summary>
        public string? FixSuggestion { get; set; }

        /// <summary>
        /// Gets or sets the to string.
        /// </summary>
        public override string ToString() => $"[{Severity}] {Message}";
    }

    /// <summary>
    /// Validates state-machine diagrams: initial/final states, reachability,
    /// and guards/triggers on transitions (read from ConnectionLine.GuardCondition/TriggerEvent,
    /// falling back to Label1/Label2 for older diagrams).
    /// </summary>
    public class StateMachineValidator
    {
        /// <summary>
        /// Gets or sets the issues.
        /// </summary>
        public List<StateMachineIssue> Issues { get; } = new List<StateMachineIssue>();

        /// <summary>
        /// Runs all checks. Returns true when no errors were found (warnings are allowed).
        /// </summary>
        public bool Validate(IReadOnlyList<SkiaComponent> components, IReadOnlyList<IConnectionLine> lines)
        {
            Issues.Clear();
            if (components == null) return true;

            var states = components
                .Where(c => c is StateMachineControl && !c.IsStatic)
                .Cast<StateMachineControl>()
                .ToList();
            if (states.Count == 0) return true;

            var transitions = (lines ?? Array.Empty<IConnectionLine>())
                .Where(l => l?.Start?.Component is StateMachineControl || l?.End?.Component is StateMachineControl)
                .ToList();

            CheckInitialState(states);
            CheckFinalState(states);
            CheckUnreachableStates(states, transitions);
            CheckUnguardedChoiceTransitions(states, transitions);
            CheckUnlabeledTransitions(transitions);

            return Issues.All(i => i.Severity != StateMachineIssueSeverity.Error);
        }

        private void CheckInitialState(List<StateMachineControl> states)
        {
            var initials = states.OfType<InitialStateNode>().ToList();
            if (initials.Count == 0)
            {
                Issues.Add(new StateMachineIssue
                {
                    Message = "State machine has no initial state",
                    Severity = StateMachineIssueSeverity.Warning,
                    FixSuggestion = "Add an InitialStateNode and connect it to the first state"
                });
            }
            else if (initials.Count > 1)
            {
                Issues.Add(new StateMachineIssue
                {
                    Message = $"State machine has {initials.Count} initial states; exactly one is expected",
                    Severity = StateMachineIssueSeverity.Warning,
                    Component = initials[1],
                    FixSuggestion = "Remove extra initial states"
                });
            }
        }

        private void CheckFinalState(List<StateMachineControl> states)
        {
            if (!states.OfType<FinalStateNode>().Any())
            {
                Issues.Add(new StateMachineIssue
                {
                    Message = "State machine has no final state",
                    Severity = StateMachineIssueSeverity.Info,
                    FixSuggestion = "Add a FinalStateNode to mark a terminal state"
                });
            }
        }

        private void CheckUnreachableStates(List<StateMachineControl> states, List<IConnectionLine> transitions)
        {
            foreach (var state in states)
            {
                if (state is InitialStateNode) continue;

                bool hasIncoming = transitions.Any(l => l?.End?.Component == state);
                if (!hasIncoming)
                {
                    var name = GetStateName(state);
                    Issues.Add(new StateMachineIssue
                    {
                        Message = $"State '{name}' is unreachable (no incoming transitions)",
                        Severity = StateMachineIssueSeverity.Warning,
                        Component = state,
                        FixSuggestion = "Connect a transition into this state or remove it"
                    });
                }
            }
        }

        private void CheckUnguardedChoiceTransitions(List<StateMachineControl> states, List<IConnectionLine> transitions)
        {
            foreach (var choice in states.OfType<ChoiceNode>())
            {
                var outgoing = transitions.Where(l => l?.Start?.Component == choice).ToList();
                foreach (var line in outgoing)
                {
                    if (string.IsNullOrWhiteSpace(GetGuard(line)))
                    {
                        Issues.Add(new StateMachineIssue
                        {
                            Message = $"Choice '{choice.Name ?? "Choice"}' has an outgoing transition without a guard condition",
                            Severity = StateMachineIssueSeverity.Warning,
                            Component = choice,
                            Line = line,
                            FixSuggestion = "Set GuardCondition on the transition (e.g., 'amount > 100')"
                        });
                    }
                }
            }
        }

        private void CheckUnlabeledTransitions(List<IConnectionLine> transitions)
        {
            foreach (var line in transitions)
            {
                if (string.IsNullOrWhiteSpace(GetGuard(line))
                    && string.IsNullOrWhiteSpace(GetTrigger(line))
                    && string.IsNullOrWhiteSpace(line.Label1))
                {
                    Issues.Add(new StateMachineIssue
                    {
                        Message = "Transition has no trigger, guard, or label",
                        Severity = StateMachineIssueSeverity.Info,
                        Line = line,
                        FixSuggestion = "Set TriggerEvent or GuardCondition to document the transition"
                    });
                }
            }
        }

        private static string GetStateName(StateMachineControl state)
        {
            if (state == null) return "(unknown)";
            if (!string.IsNullOrWhiteSpace(state.Name)) return state.Name;

            var titleProp = state.GetType().GetProperty("Title");
            var title = titleProp?.GetValue(state) as string;
            return string.IsNullOrWhiteSpace(title) ? state.GetType().Name : title;
        }

        /// <summary>
        /// Gets the guard condition for a transition, falling back to Label1 for legacy diagrams.
        /// </summary>
        public static string? GetGuard(IConnectionLine line)
            => (line as ConnectionLine)?.GuardCondition ?? line?.Label1;

        /// <summary>
        /// Gets the trigger event for a transition, falling back to Label2 for legacy diagrams.
        /// </summary>
        public static string? GetTrigger(IConnectionLine line)
            => (line as ConnectionLine)?.TriggerEvent ?? line?.Label2;

        /// <summary>
        /// Sets guard/action/trigger fields on a concrete connection line.
        /// </summary>
        public static bool SetTransition(IConnectionLine line, string trigger, string guard, string action)
        {
            if (!(line is ConnectionLine concrete)) return false;
            concrete.TriggerEvent = trigger;
            concrete.GuardCondition = guard;
            concrete.TransitionAction = action;
            return true;
        }
    }
}
