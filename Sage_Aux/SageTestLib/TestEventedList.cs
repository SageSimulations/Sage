/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Highpoint.Sage.Utility;

namespace SageTestLib {
    [TestClass]
    public class EventedListTester {

        public EventedListTester() {}

        EventedList<string> m_uut = null;
        string m_responses = null;
       
        #region Prep Work

        public void Init() {
            m_responses = "";
            m_uut = new EventedList<string>();
            m_uut.AboutToAddItem += m_uut_AboutToAddItem;
            m_uut.AboutToAddItems += m_uut_AboutToAddItems;
            m_uut.AboutToRemoveItem += m_uut_AboutToRemoveItem;
            m_uut.AboutToRemoveItems += m_uut_AboutToRemoveItems;
            m_uut.AboutToReplaceItem += m_uut_AboutToReplaceItem;
            m_uut.AddedItem += m_uut_AddedItem;
            m_uut.AddedItems += m_uut_AddedItems;
            m_uut.RemovedItem += m_uut_RemovedItem;
            m_uut.RemovedItems += m_uut_RemovedItems;
            m_uut.ReplacedItem += m_uut_ReplacedItem;
            m_uut.ContentsChanged += m_uut_ContentsChanged;
        }

        void m_uut_ReplacedItem(EventedList<string> list, string oldItem, string newItem) {
            m_responses += "m_uut_ReplacedItem" + " " + oldItem + " with " + newItem + " | ";
        }

        void m_uut_AboutToReplaceItem(EventedList<string> list, string oldItem, string newItem) {
            m_responses += "m_uut_AboutToReplaceItem" + " " + oldItem + " with " + newItem + " | ";
        }

        void m_uut_ContentsChanged(EventedList<string> list) {
            m_responses += "m_uut_ContentsChanged" + " | ";
        }

        void m_uut_RemovedItems(EventedList<string> list, Predicate<string> match) {
            m_responses += "m_uut_RemovedItems" + " " + match + " | ";
        }

        void m_uut_RemovedItem(EventedList<string> list, string item) {
            m_responses += "m_uut_RemovedItem" + " " + item + " | ";
        }

        void m_uut_AddedItems(EventedList<string> list, System.Collections.Generic.IEnumerable<string> collection) {
            m_responses += "m_uut_AddedItems" + " " + collection + " | ";
        }

        void m_uut_AddedItem(EventedList<string> list, string item) {
            m_responses += "m_uut_AddedItem" + " " + item + " | ";
        }

        void m_uut_AboutToRemoveItems(EventedList<string> list, Predicate<string> match) {
            m_responses += "m_uut_AboutToRemoveItems" + " " + match + " | ";
        }

        void m_uut_AboutToRemoveItem(EventedList<string> list, string item) {
            m_responses += "m_uut_AboutToRemoveItem" + " " + item + " | ";
        }

        void m_uut_AboutToAddItems(EventedList<string> list, System.Collections.Generic.IEnumerable<string> collection) {
            m_responses += "m_uut_AboutToAddItems" + " " + collection + " | ";
        }

        void m_uut_AboutToAddItem(EventedList<string> list, string item) {
            m_responses += "m_uut_AboutToAddItem" + " " + item + " | ";
        }

        [TestCleanup]
        public void destroy() {
            Debug.WriteLine("Done.");
        } 
        #endregion

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Test the Add mechanism.")]
        public void TestAdd() {
            Init();

            string addee = "String 1";
            m_uut.Add(addee);

            Assert.IsTrue(m_responses.Equals("m_uut_AboutToAddItem String 1 | m_uut_AddedItem String 1 | m_uut_ContentsChanged | "));
            Console.WriteLine(m_responses);
        }
        
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Test the AddRange mechanism.")]
        public void TestAddRange() {
            Init();

            string[] addee = new string[] { "String 2", "String 3" };
            m_uut.AddRange(addee);

            Assert.IsTrue(m_responses.Equals("m_uut_AboutToAddItems System.String[] | m_uut_AddedItems System.String[] | m_uut_ContentsChanged | "));
            Assert.IsTrue(m_uut[0].Equals("String 2"));
            Assert.IsTrue(m_uut[1].Equals("String 3"));
            Console.WriteLine(m_responses);
        }
        
        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Test the Remove mechanism.")]
        public void TestRemove() {
            Init();

            m_uut.AddRange(new string[] { "Bob", "Mary", "Sue" });
            m_uut.Remove("Mary");

            Assert.IsTrue(m_uut[0].Equals("Bob"));
            Assert.IsTrue(m_uut[1].Equals("Sue"));
            Assert.IsTrue(m_responses.Equals("m_uut_AboutToAddItems System.String[] | m_uut_AddedItems System.String[] | m_uut_ContentsChanged | m_uut_AboutToRemoveItem Mary | m_uut_RemovedItem Mary | m_uut_ContentsChanged | "));
            Console.WriteLine(m_responses);
        }

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Test the RemoveAll mechanism.")]
        public void TestRemoveAll() {
            Init();

            m_uut.AddRange(new string[] { "Bob", "Mary", "Sue" });
            m_uut.RemoveAll(delegate(string s) { return s.Length.Equals(3); });

            Assert.IsTrue(m_uut[0].Equals("Mary"));
            Assert.IsTrue(m_responses.Equals("m_uut_AboutToAddItems System.String[] | m_uut_AddedItems System.String[] | m_uut_ContentsChanged | m_uut_AboutToRemoveItems System.Predicate`1[System.String] | m_uut_RemovedItems System.Predicate`1[System.String] | m_uut_ContentsChanged | "));
            Console.WriteLine(m_responses);
        }

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Test the RemoveAt mechanism.")]
        public void TestRemoveAt() {
            Init();

            m_uut.AddRange(new string[] { "Bob", "Mary", "Sue" });
            m_uut.RemoveAt(1);

            Assert.IsTrue(m_uut[0].Equals("Bob"));
            Assert.IsTrue(m_uut[1].Equals("Sue"));
            Assert.IsTrue(m_responses.Equals("m_uut_AboutToAddItems System.String[] | m_uut_AddedItems System.String[] | m_uut_ContentsChanged | m_uut_AboutToRemoveItem Mary | m_uut_RemovedItem Mary | m_uut_ContentsChanged | "));
            Console.WriteLine(m_responses);
        }

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Test the Clear mechanism.")]
        public void TestClear() {
            Init();

            m_uut.AddRange(new string[] { "Bob", "Mary", "Sue" });
            m_uut.Clear();

            Assert.IsTrue(m_uut.Count == 0);
            Assert.IsTrue(m_responses.Equals("m_uut_AboutToAddItems System.String[] | m_uut_AddedItems System.String[] | m_uut_ContentsChanged | m_uut_ContentsChanged | "));
            Console.WriteLine(m_responses);
        }

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Test the Indexer mechanism.")]
        public void TestIndexer() {
            Init();

            m_uut.AddRange(new string[] { "Bob", "Mary", "Sue" });
            m_uut[1] = "Steve";

            Assert.AreEqual("Bob",m_uut[0]);
            Assert.AreEqual("Steve",m_uut[1]);
            Assert.AreEqual("Sue",m_uut[2]);
            Assert.AreEqual("m_uut_AboutToAddItems System.String[] | m_uut_AddedItems System.String[] | m_uut_ContentsChanged | m_uut_AboutToReplaceItem Mary with Steve | m_uut_ReplacedItem Mary with Steve | m_uut_ContentsChanged | ", m_responses);
        }

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Test the Insert mechanism.")]
        public void TestInsert()
        {
            Init();

            m_uut.AddRange(new string[] {"Bob", "Mary", "Sue"});

            m_uut.Insert(1, "Paul");

            Assert.AreEqual("Bob", m_uut[0]);
            Assert.AreEqual("Paul", m_uut[1]);
            Assert.AreEqual("Mary", m_uut[2]);
            Assert.AreEqual("Sue", m_uut[3]);
            Assert.AreEqual(
                "m_uut_AboutToAddItems System.String[] | m_uut_AddedItems System.String[] | m_uut_ContentsChanged | m_uut_AboutToAddItem Paul | m_uut_AddedItem Paul | m_uut_ContentsChanged | ",
                m_responses
                );
        }

        [TestMethod]
        [Highpoint.Sage.Utility.FieldDescription("Test the InsertRange mechanism.")]
        public void TestInsertRange() {
            Init();

            m_uut.AddRange(new string[] { "Bob", "Mary", "Tim" });

            m_uut.InsertRange(1, new string[] { "Paul", "Randy", "Sara" });

            Assert.IsTrue(m_uut[0].Equals("Bob"));
            Assert.IsTrue(m_uut[1].Equals("Paul"));
            Assert.IsTrue(m_uut[2].Equals("Randy"));
            Assert.IsTrue(m_uut[3].Equals("Sara"));
            Assert.IsTrue(m_uut[4].Equals("Mary"));
            Assert.IsTrue(m_uut[5].Equals("Tim"));
            Assert.IsTrue(m_responses.Equals("m_uut_AboutToAddItems System.String[] | m_uut_AddedItems System.String[] | m_uut_ContentsChanged | m_uut_AboutToAddItems System.String[] | m_uut_AddedItems System.String[] | m_uut_ContentsChanged | "));
            Console.WriteLine(m_responses);
        }
        

    }
}