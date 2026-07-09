/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Highpoint.Sage.Transport {

    /// <summary>
    /// What a traffic signal shows a given approach at a given instant.
    /// </summary>
    public enum SignalIndication {
        Red,
        Yellow,
        Green
    }

    /// <summary>
    /// A fixed-time traffic signal, expressed as a pure timetable: a repeating cycle of phases,
    /// anchored at a cycle epoch, each phase granting green (and optionally yellow) to sets of
    /// approach indices for its duration. What an approach sees at any instant is computed
    /// arithmetically, so an idle signal schedules no events and never keeps the executive
    /// alive; blocks that wait on the signal (e.g. SignalizedApproach) schedule their own
    /// wake-ups at the next permissive indication.
    /// </summary>
    public class TrafficSignal {

        /// <summary>
        /// One phase of a signal cycle: a name, a duration, the approaches that are green for
        /// that duration, and the approaches that are yellow. Approaches in neither set see red.
        /// </summary>
        public class Phase {
            /// <summary>
            /// Creates a new instance of the <see cref="T:Phase"/> class with green approaches only.
            /// </summary>
            /// <param name="name">A user-friendly name for the phase (e.g. "Main green").</param>
            /// <param name="duration">The phase duration. Must be positive.</param>
            /// <param name="greenApproaches">The approach indices that are green during this phase.</param>
            public Phase(string name, TimeSpan duration, params int[] greenApproaches)
                : this(name, duration, greenApproaches, null) { }

            /// <summary>
            /// Creates a new instance of the <see cref="T:Phase"/> class with green and yellow approaches.
            /// </summary>
            /// <param name="name">A user-friendly name for the phase (e.g. "Main yellow").</param>
            /// <param name="duration">The phase duration. Must be positive.</param>
            /// <param name="greenApproaches">The approach indices that are green during this phase.</param>
            /// <param name="yellowApproaches">The approach indices that are yellow during this phase.</param>
            public Phase(string name, TimeSpan duration, int[] greenApproaches, int[] yellowApproaches) {
                if (duration <= TimeSpan.Zero) throw new ArgumentException("A signal phase must have a positive duration.", "duration");
                Name = name;
                Duration = duration;
                GreenApproaches = greenApproaches ?? new int[0];
                YellowApproaches = yellowApproaches ?? new int[0];
                foreach (int g in GreenApproaches) {
                    foreach (int y in YellowApproaches) {
                        if (g == y) throw new ArgumentException(string.Format("Phase {0} shows approach {1} both green and yellow.", name, g));
                    }
                }
            }
            public string Name { get; private set; }
            public TimeSpan Duration { get; private set; }
            public int[] GreenApproaches { get; private set; }
            public int[] YellowApproaches { get; private set; }

            internal bool IsGreenFor(int approach) {
                foreach (int a in GreenApproaches) if (a == approach) return true;
                return false;
            }

            internal bool IsYellowFor(int approach) {
                foreach (int a in YellowApproaches) if (a == approach) return true;
                return false;
            }

            internal SignalIndication IndicationFor(int approach) {
                if (IsGreenFor(approach)) return SignalIndication.Green;
                if (IsYellowFor(approach)) return SignalIndication.Yellow;
                return SignalIndication.Red;
            }
        }

        private readonly string m_name;
        private readonly DateTime m_cycleEpoch;
        private readonly ReadOnlyCollection<Phase> m_phases;
        private readonly TimeSpan m_cycle;

        /// <summary>
        /// Creates a new instance of the <see cref="T:TrafficSignal"/> class.
        /// </summary>
        /// <param name="name">A user-friendly name for the signal.</param>
        /// <param name="cycleEpoch">The instant at which (the first phase of) a cycle begins.
        /// The timetable extends indefinitely in both directions from this anchor.</param>
        /// <param name="phases">The cycle's phases, in order. Must be non-empty.</param>
        public TrafficSignal(string name, DateTime cycleEpoch, IList<Phase> phases) {
            if (phases == null || phases.Count == 0) throw new ArgumentException("A TrafficSignal requires at least one phase.", "phases");
            m_name = name;
            m_cycleEpoch = cycleEpoch;
            m_phases = new ReadOnlyCollection<Phase>(new List<Phase>(phases));
            long cycleTicks = 0;
            foreach (Phase p in m_phases) cycleTicks += p.Duration.Ticks;
            m_cycle = TimeSpan.FromTicks(cycleTicks);
        }

        /// <summary>
        /// The signal's user-friendly name.
        /// </summary>
        public string Name { get { return m_name; } }

        /// <summary>
        /// The duration of one full cycle (the sum of the phase durations).
        /// </summary>
        public TimeSpan CycleTime { get { return m_cycle; } }

        /// <summary>
        /// The signal's phases, in cycle order.
        /// </summary>
        public IList<Phase> Phases { get { return m_phases; } }

        /// <summary>
        /// Whether any phase of the cycle grants green to the specified approach.
        /// </summary>
        /// <param name="approach">The approach index.</param>
        public bool EverGreen(int approach) {
            foreach (Phase p in m_phases) if (p.IsGreenFor(approach)) return true;
            return false;
        }

        /// <summary>
        /// The phase in effect at the specified instant. Each phase's interval is closed at its
        /// start and open at its end: at an exact phase boundary, the incoming phase governs.
        /// </summary>
        /// <param name="at">The instant of interest.</param>
        public Phase PhaseAt(DateTime at) {
            long offset = OffsetTicksIntoCycle(at);
            foreach (Phase p in m_phases) {
                if (offset < p.Duration.Ticks) return p;
                offset -= p.Duration.Ticks;
            }
            return m_phases[m_phases.Count - 1]; // Unreachable; offset < cycle by construction.
        }

        /// <summary>
        /// Whether the specified approach is green at the specified instant.
        /// </summary>
        /// <param name="approach">The approach index.</param>
        /// <param name="at">The instant of interest.</param>
        public bool IsGreen(int approach, DateTime at) {
            return PhaseAt(at).IsGreenFor(approach);
        }

        /// <summary>
        /// What the signal shows the specified approach at the specified instant.
        /// </summary>
        /// <param name="approach">The approach index.</param>
        /// <param name="at">The instant of interest.</param>
        public SignalIndication Indication(int approach, DateTime at) {
            return PhaseAt(at).IndicationFor(approach);
        }

        /// <summary>
        /// The earliest instant, at or after the specified time, at which the specified approach
        /// is green. Returns the given time itself if the approach is green then, and
        /// DateTime.MaxValue if no phase ever grants this approach green.
        /// </summary>
        /// <param name="approach">The approach index.</param>
        /// <param name="notBefore">The earliest acceptable instant.</param>
        public DateTime NextGreen(int approach, DateTime notBefore) {
            return NextSatisfying(delegate(Phase p) { return p.IsGreenFor(approach); }, notBefore);
        }

        /// <summary>
        /// The earliest instant, at or after the specified time, at which the specified approach
        /// is green or yellow (the crossable indications for a driver willing to run a yellow).
        /// Returns the given time itself if the approach is green or yellow then, and
        /// DateTime.MaxValue if no phase ever grants this approach either.
        /// </summary>
        /// <param name="approach">The approach index.</param>
        /// <param name="notBefore">The earliest acceptable instant.</param>
        public DateTime NextGreenOrYellow(int approach, DateTime notBefore) {
            return NextSatisfying(delegate(Phase p) { return p.IsGreenFor(approach) || p.IsYellowFor(approach); }, notBefore);
        }

        private DateTime NextSatisfying(Predicate<Phase> test, DateTime notBefore) {
            if (test(PhaseAt(notBefore))) return notBefore;

            bool everSatisfied = false;
            foreach (Phase p in m_phases) if (test(p)) { everSatisfied = true; break; }
            if (!everSatisfied) return DateTime.MaxValue;

            long offset = OffsetTicksIntoCycle(notBefore);
            // Locate the current phase, then walk forward (at most one full cycle) to the next
            // phase boundary at which the predicate becomes true.
            long boundary = 0;
            int currentPhase = 0;
            for (int i = 0; i < m_phases.Count; i++) {
                if (offset < boundary + m_phases[i].Duration.Ticks) { currentPhase = i; break; }
                boundary += m_phases[i].Duration.Ticks;
            }
            long ticksUntil = (boundary + m_phases[currentPhase].Duration.Ticks) - offset;
            for (int step = 1; step <= m_phases.Count; step++) {
                Phase candidate = m_phases[(currentPhase + step) % m_phases.Count];
                if (test(candidate)) return notBefore + TimeSpan.FromTicks(ticksUntil);
                ticksUntil += candidate.Duration.Ticks;
            }
            return DateTime.MaxValue; // Unreachable; everSatisfied was checked above.
        }

        private long OffsetTicksIntoCycle(DateTime at) {
            long offset = (at - m_cycleEpoch).Ticks % m_cycle.Ticks;
            if (offset < 0) offset += m_cycle.Ticks;
            return offset;
        }
    }
}
