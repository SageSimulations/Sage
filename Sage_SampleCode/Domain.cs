/* This source code licensed under the GNU Affero General Public License */
using System;
using Highpoint.Sage.SimCore;

namespace Domain {

    namespace Sample1 {

        class Animal {
            private string m_word;
            private string m_name;
            public Animal(string name, string word) { m_name = name; m_word = word; }
            public void Speak(IExecutive exec, object userData) {
                Console.WriteLine("{0} : {1} says {2}!", exec.Now, m_name, m_word);
            }
            public string Name { get { return m_name; } }
        }

        class Dog : Animal { public Dog(string name) : base(name, "Bark") { } }
        class Cat : Animal { public Cat(string name) : base(name, "Meow") { } }

        class Person {
            private string m_name;
            public Person(string name) { m_name = name; }
            public string Name { get { return m_name; } }
        }
    }
}