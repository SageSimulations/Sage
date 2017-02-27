/* This source code licensed under the GNU Affero General Public License */
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Demo.RandomServer {
    using System;
    using Highpoint.Sage.SimCore;
    using Highpoint.Sage.Randoms;

    class SimpleDefaultServer {

        [Description(@"This demonstration shows the default use of a RandomServer. It obtains, 
for each of five test case seed-and-buffer-size combinations, an instance
of IRandomChannel from the GlobalRandomServer, and outputs the first ten
values from that random channel. The demonstration calls ""NextDouble()"",
but there are a number of other familiar methods for getting randoms as
well.")]
        public static void Run() {

            ulong[][] testValues = new ulong[][] { 
                new ulong[] { 12345, 100 }, 
                new ulong[] { 54321, 100 }, 
                new ulong[] { 12345, 200 }, 
                new ulong[] { 51903, 200 } };

            foreach (ulong[] ula in testValues) {
                ulong seed = ula[0];
                int bufferSize = (int)ula[1];
                IRandomChannel irc = GlobalRandomServer.Instance.GetRandomChannel(seed, bufferSize);

                for (int i = 0; i < 10; i++)
                {
                    Console.WriteLine("RandomChannel[seed={0}][{1}] = {2}", seed, i, irc.NextDouble());
                }

            }
        }
    }

    class DecorrellatedActivities {

        [Description(@"This demonstration shows the random server's ability to maintain two
separate streams of random numbers that do not affect each other. The
demo uses two instances of class ""Activity"" as agents in the simulation.
The demo occurs in three parts. 

In the first part, we create a random server with one hyperseed, hs1, and 
then create two activities with different seeds, s1 and s2. The activities 
generate different outputs from each other. 

In the second part, we create a random server with a second hyperseed, hs2,
and then create two activities with the same seeds, s1 and s2, as before.
The activities generate different outputs from each other, and from their
outputs in the first part, demonstrating that a model can have fixed seeds
for elements within it, and achieve variability by altering the hyperseed only.

In the third part, we essentially replicate the second part, but this time,
we impose a change of behavior on the second instance of ""Activity."" The
behavior of the first instance of ""Activity"" remains identical to its
behavior in the second part of the demo, illustrating the decorrellated nature
of random channels. They can be used to hold one part of a simulation constant
and allow another to change."
)]
        public static void Run() {
            RandomServer rs = null;
            IExecutive exec = null;
            Activity fooActivity = null;
            Activity barActivity = null;


            int defaultBufferSize = 100;
            foreach (ulong hyperSeed in new ulong[] { 87654, 23456 }) {
                Console.WriteLine("Test with hyperSeed of {0}.", hyperSeed);
                rs = new RandomServer(hyperSeed, defaultBufferSize);
                exec = ExecFactory.Instance.CreateExecutive();

                fooActivity = new Activity("fooActivity", 5, exec, rs, 12345);
                barActivity = new Activity("barActivity", 6, exec, rs, 10558);

                exec.Start();

            }

            ulong _hyperSeed = 24680;
            Console.WriteLine("Test with hyperSeed of {0}. First, with no interference:", _hyperSeed);
            rs = new RandomServer(_hyperSeed, defaultBufferSize);
            exec = ExecFactory.Instance.CreateExecutive();

            fooActivity = new Activity("fooActivity", 5, exec, rs, 12345);
            barActivity = new Activity("barActivity", 6, exec, rs, 10558);

            exec.Start();


            exec.Reset();
            Console.WriteLine("Test with hyperSeed of {0}. This time, we interfere with Bobby:", _hyperSeed);
            rs = new RandomServer(_hyperSeed, defaultBufferSize);
            exec = ExecFactory.Instance.CreateExecutive();

            fooActivity = new Activity("fooActivity", 5, exec, rs, 12345);
            barActivity = new Activity("barActivity", 6, exec, rs, 10558);

            DateTime when = DateTime.Parse("1/1/0001 7:13:29 AM");
            exec.RequestEvent(
                delegate(IExecutive _exec, object userData) {
                    Console.WriteLine("{0} : Doubling barActivity's multiplier.", _exec.Now); barActivity.Multiplier = 2.0;
                },
                when);

            exec.Start();

        }
        class Activity {

            private static readonly int m_defaultBufferSize = 100;
            private readonly IRandomChannel m_rc;
            private readonly int m_numIterations;
            private readonly string m_name;
            private double m_multiplier = 1.0;

            public Activity(string name, int numIterations, IExecutive executive, RandomServer rs, ulong seed) {
                m_name = name;
                m_numIterations = numIterations;
                m_rc = rs.GetRandomChannel(seed, m_defaultBufferSize);
                executive.ExecutiveStarted += delegate(IExecutive exec) {
                    exec.RequestEvent(Run, exec.Now, 0.0, null, ExecEventType.Detachable);
                };
            }

            private void Run(IExecutive exec, object userData) {
                for (int iterationNum = 1; iterationNum <= m_numIterations; iterationNum++) {
                    Console.WriteLine("{0} : {1} beginning iteration {2}.", exec.Now, m_name, iterationNum);
                    TimeSpan iterationDuration = TimeSpan.FromHours(m_multiplier * m_rc.NextDouble(2.0, 4.0));
                    exec.CurrentEventController.SuspendFor(iterationDuration);
                    Console.WriteLine("{0} : {1} ending iteration {2}.", exec.Now, m_name, iterationNum);
                }
            }

            public double Multiplier { set { m_multiplier = value; } }
        }
    }
}