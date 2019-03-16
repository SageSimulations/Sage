using System.Collections.Generic;

namespace Highpoint.Sage.SystemDynamics
{
    public enum Integrator {  Euler, RK4 }
    public static class Behavior<T> where T : StateBase<T>
    {

        public static IEnumerable<T> Run(T state, Integrator integrator = Integrator.Euler)
        {
            for (double time = state.Start + (state.TimeSliceNdx * state.TimeStep); time < state.Finish; time += state.TimeStep)
            {
                state =
                    (T)
                        ((integrator == Integrator.Euler)
                            ? state.RunOneTimesliceAsEuler(state)
                            : state.RunOneTimeSliceAsRK4(state));
                yield return state;
            }
        }

        public static T RunOneTimeslice(T state, Integrator integrator = Integrator.Euler)
        {
                return (T) ((integrator == Integrator.Euler) ? state.RunOneTimesliceAsEuler(state) : state.RunOneTimeSliceAsRK4(state));
        }
    }
}