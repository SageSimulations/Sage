/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using _Debug = System.Diagnostics.Debug;

namespace Highpoint.Sage.Graphs.PFC.Expressions {

    /// <summary>
    /// Expressions are prepresented as Hostile (with Guids), Friendly (names, including macro names) and Expanded (all names, plus macros evaluated.)
    /// </summary>
    public enum ExpressionType { 
        /// <summary>
        /// The expression has proper names and macros expanded.
        /// </summary>
        Expanded,
        /// <summary>
        /// The expression has proper names and macros referenced by name.
        /// </summary>
        Friendly, 
        /// <summary>
        /// All names and macros are replaced by their guids.
        /// </summary>
        Hostile }

    /// <summary>
    /// An abstract base class for Rote Strings, Dual Mode Strings and Macros.
    /// </summary>
    public abstract class ExpressionElement {

        /// <summary>
        /// Returns the string for this expression element that corresponds to the indicated representation type.
        /// </summary>
        /// <param name="t">The indicated representation type.</param>
        /// <param name="forWhom">The owner of the expression, usually a Transition.</param>
        /// <returns>The string for this expression element.</returns>
        public abstract string ToString(ExpressionType t, object forWhom);

        /// <summary>
        /// Gets or sets the GUID of this expression. Returns Guid.Empty if the expression element will not need to
        /// be correlated to anything (as would, for example, a string and Guid representing a Step Name element.)
        /// </summary>
        /// <value>The GUID.</value>
        public virtual Guid Guid { get { return Guid.Empty; } set { } }

        /// <summary>
        /// Gets or sets the name of this expression. Returns string.Empty if the expression element does
        /// not reasonably have a name. Macros and placeholders for OpSteps, for example, have names, where
        /// rote strings do not.
        /// </summary>
        /// <value>The name.</value>
        public virtual string Name { get { return ""; } set { } }

        /// <summary>
        /// Gets the type of this expression element - used primarily for ascertaining type compatibility between
        /// expressions.
        /// </summary>
        /// <value>The type.</value>
        public object Type { get { return GetType(); } }

        internal bool Marked = false;
    }

    /// <summary>
    /// A class that represents a string in an expression that does not correlate to anything outside the expression,
    /// such as the string &quot;' = TRUE AND '&quot;
    /// </summary>
    public class RoteString : ExpressionElement {
        private string m_theString = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:RoteString"/> class.
        /// </summary>
        /// <param name="str">The rote string that this object will represent.</param>
        public RoteString(string str) {
            m_theString = str;
        }

        /// <summary>
        /// Returns the string for this expression element that corresponds to the indicated representation type.
        /// </summary>
        /// <param name="t">The indicated representation type.</param>
        /// <param name="forWhom">The owner of the expression, usually a Transition.</param>
        /// <returns>The string for this expression element.</returns>
        public override string ToString(ExpressionType t, object forWhom) {
            return m_theString;
        }

        /// <summary>
        /// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
        /// </returns>
        public override string ToString() {
            return m_theString;
        }
    }

    /// <summary>
    /// This class represents a string that correlates to an object. It is given a Guid that
    /// is logged into the participant directory, so that its name or its Guid can be changed
    /// without losing coherency. It is usually an object name.
    /// </summary>
    public class DualModeString : ExpressionElement {

        #region Private Fields

        private Guid m_guid;
        private string m_name;

        #endregion 

        /// <summary>
        /// Initializes a new instance of the <see cref="T:DualModeString"/> class.
        /// </summary>
        /// <param name="guid">The GUID of this object. Typically registered as this object's ModelObject key, and thus, required to be unique in a pan-model context.</param>
        /// <param name="name">The user-friendly name of this object. Typically not required to be unique in a pan-model context.</param>
        public DualModeString(Guid guid, string name) {
            m_guid = guid;
            m_name = name;
        }

        /// <summary>
        /// Returns the string for this DualModeString that corresponds to the indicated representation type.
        /// </summary>
        /// <param name="t">The indicated representation type.</param>
        /// <param name="forWhom">The owner of the expression, usually a Transition.</param>
        /// <returns>The string for this expression element.</returns>
        public override string ToString(ExpressionType t, object forWhom) {
            switch (t) {
                case ExpressionType.Expanded:
                    return m_name;
                case ExpressionType.Friendly:
                    return m_name;
                case ExpressionType.Hostile:
                    return string.Format("{0}", m_guid);
                default:
                    throw new ApplicationException(string.Format("Unknown string format, {0}, was requested.",t));
            }
        }

        /// <summary>
        /// Gets or sets the GUID of this expression. Returns Guid.Empty if the epression element will not need to
        /// be correlated to anything (as would, for example, a string and Guid representing a Step Name element.
        /// </summary>
        /// <value>The GUID.</value>
        public override Guid Guid { get { return m_guid; } set { m_guid = value; } }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public override string Name { get { return m_name; } set { m_name = value; } }

    }

    /// <summary>
    /// Abstract base class for all macros. Derives from ExpressionElement, and from that, obtains
    /// the ability to be expressed as friendly, hostile or expanded format.
    /// </summary>
    public abstract class Macro : ExpressionElement {
        /// <summary>
        /// All Macros start with this string.
        /// </summary>
        public static readonly string MACRO_START = "'µ";
        
        #region Protected Fields
        /// <summary>
        /// The Guid by which this macro will be known.
        /// </summary>
        protected Guid m_guid;
        /// <summary>
        /// The friendly representation of this macro.
        /// </summary>
        protected string m_name;
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Macro"/> class.
        /// </summary>
        public Macro() { }

        /// <summary>
        /// Evaluates the macro using the specified arguments.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns>The evaluated representation of the macro.</returns>
        protected abstract string Evaluate(object[] args);

        /// <summary>
        /// Returns the string for this macro element that corresponds to the indicated representation type.
        /// </summary>
        /// <param name="t">The indicated representation type.</param>
        /// <param name="forWhom">The owner of the expression, usually a Transition.</param>
        /// <returns>The string for this expression element.</returns>
        public override string ToString(ExpressionType t, object forWhom) {
            _Debug.Assert(Name.StartsWith(MACRO_START));

            switch (t) {
                case ExpressionType.Expanded:
                    return Evaluate(new object[] { forWhom });
                case ExpressionType.Friendly:
                    return Name;
                case ExpressionType.Hostile:
                    return Guid.ToString();
                default:
                    throw new ApplicationException(string.Format("Unknown string format, {0}, was requested.", t));
            }
        }

        /// <summary>
        /// Gets macro's name. Overridden in the concrete macro class.
        /// </summary>
        /// <value>The name.</value>
        public override string Name {
            get {
                _Debug.Assert(m_name != String.Empty, "Must provide a value for name in all defined macros.");
                return m_name;
            }
        }
        /// <summary>
        /// Gets or sets the GUID of this expression. Returns Guid.Empty if the epression element will not need to
        /// be correlated to anything (as would, for example, a string and Guid representing a Step Name element.
        /// Overridden in the concrete macro class.
        /// </summary>
        /// <value>The GUID.</value>
        public override Guid Guid { 
            get {
                _Debug.Assert(!m_guid.Equals(Guid.Empty), "Must provide a value for guid in all defined macros.");
                return m_guid; 
            } 
        }
    }

    /// <summary>
    /// A Macro that expands into an expression that evaluates true only if all (explicitly
    /// named) predecessors to the owner transition are true. If there are no predecessors,
    /// then the expression is simply &quot;True&quot;.
    /// </summary>
    public class PredecessorsComplete : Macro {

        /// <summary>
        /// The name by which this macro is known.
        /// </summary>
        public static readonly string NAME = MACRO_START + "PredecessorsComplete'";
        public static readonly string ALTNAME = "'" + Encoding.Unicode.GetString(new byte[] { 188, 3 }) + "PredecessorsComplete'";

        /// <summary>
        /// Initializes a new instance of the <see cref="T:PredecessorsComplete"/> class.
        /// </summary>
        public PredecessorsComplete() {
            m_guid = new Guid("8F9CB586-F5A9-410b-90FA-4B12064218F1");
            m_name = NAME;
        }

        /// <summary>
        /// Evaluates the macro using the specified arguments.
        /// </summary>
        /// <param name="args">The arguments. This macro requires one argument,
        /// the transition that owns it, and it must be of type <see cref="T:IPfcTransitionNode"/></param>
        /// <returns>
        /// The evaluated representation of the macro.
        /// </returns>
        protected override string Evaluate(object[] args) {
            IPfcTransitionNode node = (IPfcTransitionNode)args[0];

            if (node.PredecessorNodes.Count > 0) {
                StringBuilder sb = new StringBuilder();
                sb.Append("( ");
                node.PredecessorNodes.ForEach(delegate(IPfcNode pred) { sb.Append("'" + pred.Name + "/BSTATUS' = '$recipe_state:Complete' AND "); });
                string retval = sb.ToString();
                retval = retval.Substring(0, retval.Length - " AND ".Length);

                retval += " )";
                return retval;
            } else {
                return "TRUE";
            }
        }
    }

    /// <summary>
    /// A participant dictionary is a dictionary of Expression Elements that is mapped by name and by
    /// Guid. Expressions are an array of references to these elements, and formatting of these expressions
    /// is achieved by taking some format of each expression element in sequence. There are three
    /// representations of these expressions - Rote Strings, Dual Mode Strings and Macros.
    /// It is in concatenating the particular (Hostile, Friendly, Expanded) formats of those elements
    /// that an expression expresses itself.<para></para>Note: the reason that guids are needed is to support
    /// serialization through the User-Hostile mapping, and to permit renaming of steps such as when a step
    /// is flattened up into its parent, and its name goes from, e.g. &quot;Prepare-Step&quot; to 
    /// &quot;B : Xfr_Liquid2.Prepare-Step&quot;.
    /// </summary>
    public class ParticipantDirectory : IEnumerable<ExpressionElement> {

        #region Private Fields
        private Dictionary<string, ExpressionElement> m_nameMap = new Dictionary<string, ExpressionElement>();
        private Dictionary<Guid, ExpressionElement> m_guidMap = new Dictionary<Guid, ExpressionElement>();
        private static Dictionary<Type, Macro> _knownMacros = new Dictionary<Type, Macro>();
        private ParticipantDirectory m_parent = null;

        #endregion

        /// <summary>
        /// Registers a macro of the specified type, which must be an extender of the abstract class Macro.
        /// </summary>
        /// <param name="macroType">The type of the macro.</param>
        public void RegisterMacro(Type macroType) {
            if (!typeof(Macro).IsAssignableFrom(macroType)) {
                throw new ApplicationException(macroType.FullName + " is being registered as a macro, but is not one.");
            } else {

                Macro macro = null;
                if (!_knownMacros.TryGetValue(macroType, out macro)) {
                    macro = (Macro)macroType.GetConstructor(new Type[] { }).Invoke(new object[] { });
                    _knownMacros.Add(macroType, macro);
                }

                if (!m_guidMap.ContainsKey(macro.Guid)) {
                    m_nameMap.Add(macro.Name.Trim('\''), macro);
                    m_guidMap.Add(macro.Guid, macro);
                }
            }
        }

        /// <summary>
        /// Registers a mapping of this string as a DualMode string, creating a Guid under which it will be mapped.
        /// This will be a string that represents a step or transition, and which is backed by a Guid so that the
        /// string can be changed (such as when a step is flattened up into its parent, and its name goes from, e.g.
        /// &quot;Prepare-Step&quot; to &quot;B : Xfr_Liquid2.Prepare-Step&quot;.
        /// </summary>
        /// <param name="name">The name that is to become a DualMode string.</param>
        /// <returns>The newly-created (or preexisting, if it was there already) dual mode string element.</returns>
        public ExpressionElement RegisterMapping(string name) {
            string nameKey = name.Trim('\'');
            if (!m_nameMap.ContainsKey(nameKey)) {
                return RegisterMapping(name, Guid.NewGuid());
            } else {
                if (!m_nameMap.ContainsKey(name)) {
                    throw new ApplicationException(Msg_NameMapDoesntContainKey(name));
                }
                return m_nameMap[name];
            }
        }

        /// <summary>
        /// Registers a mapping of this string as a DualMode string, mapped under the provided Guid.
        /// This will be a string that represents a step or transition, and which is backed by a Guid so that the
        /// string can be changed (such as when a step is flattened up into its parent, and its name goes from, e.g.
        /// "Prepare-Step" to "B : Xfr_Liquid2.Prepare-Step".
        /// </summary>
        /// <param name="name">The name that is to become a DualMode string.</param>
        /// <param name="guid">The guid by which this element is to be known.</param>
        /// <returns>
        /// The newly-created (or preexisting, if it was there already) dual mode string element.
        /// </returns>
        public ExpressionElement RegisterMapping(string name, Guid guid) {
            bool guidIsAlreadyKnown = m_guidMap.ContainsKey(guid);
            bool nameIsAlreadyKnown = m_nameMap.ContainsKey(name);
            if (!nameIsAlreadyKnown && !guidIsAlreadyKnown) {
                #region Create a Dual Mode String and add it into the two maps.
                DualModeString dms = new DualModeString(guid, name);
                m_nameMap.Add(name, dms);
                m_guidMap.Add(guid, dms);
                #endregion
            } else if (nameIsAlreadyKnown && !guidIsAlreadyKnown) {
                #region There's already one with this name, but it's got a different guid.
                int i = 1;
                while (m_nameMap.ContainsKey(name + "_" + i)) { i++; }
                name = name + "_" + i;
                DualModeString dms = new DualModeString(guid, name);
                m_nameMap.Add(name, dms);
                m_guidMap.Add(guid, dms);
                #endregion
            } else if (!nameIsAlreadyKnown && guidIsAlreadyKnown) {
                #region There's already one with this Guid, but it has a different name.
                string msg = "Attempting to register a mapping between " + name + " and " + guid + " in a ParticipantDirectory, but the guid is already correlated to a different string, " + m_guidMap[guid].Name + ".";
                throw new ApplicationException(msg);
                #endregion
            } else {
                // Both name and guid are known - object is already added in.
            }

            return m_guidMap[guid];

        }

        /// <summary>
        /// Deletes the name GUID pair.
        /// </summary>
        /// <param name="name">The user-friendly name of this object. Typically not required to be unique in a pan-model context.</param>
        /// <param name="guid">The GUID of this object. Typically registered as this object's ModelObject key, and thus, required to be unique in a pan-model context.</param>
        public void DeleteNameGuidPair(string name, Guid guid) {
            ExpressionElement ee1 = m_nameMap[name];
            ExpressionElement ee2 = m_guidMap[guid];

            if (ee1 != null && ee1.Equals(ee2)) {
                m_nameMap.Remove(name);
                m_guidMap.Remove(guid);
            }
        }

        /// <summary>
        /// Changes the GUID of an expression element from one value to another. This is needed when an Operation 
        /// or OpStep remaps its child steps.
        /// </summary>
        /// <param name="from">The guid of the expression element that the caller wants to remap.</param>
        /// <param name="to">The guid to which the caller wants to remap the expression element.</param>
        public void ChangeGuid(Guid from, Guid to) {
            ExpressionElement expressionElement = m_guidMap[from];
            expressionElement.Guid = to;
            m_guidMap.Remove(from);
            m_guidMap.Add(to, expressionElement);
        }

        /// <summary>
        /// Changes the GUID of an expression element from one value to another. This is needed when an Operation 
        /// or OpStep remaps its child steps.
        /// </summary>
        /// <param name="fromElementsName">The name of the expression element that the caller wants to remap.</param>
        /// <param name="to">The guid to which the caller wants to remap the expression element.</param>
        public void ChangeGuid(string fromElementsName, Guid to) {
            ExpressionElement expressionElement = m_nameMap[fromElementsName];
            m_guidMap.Remove(expressionElement.Guid);
            expressionElement.Guid = to;
            m_guidMap.Add(expressionElement.Guid, expressionElement);
        }

        /// <summary>
        /// Changes the name of an expression element from one value to another.
        /// </summary>
        /// <param name="from">The name of the expression element that the caller wants to remap.</param>
        /// <param name="to">The name to which the caller wants to remap the expression element.</param>
        public void ChangeName(string from, string to) {
            if (!m_nameMap.ContainsKey(from)) {
                throw new ApplicationException(Msg_NameMapDoesntContainKey(from));
            }
            ExpressionElement expressionElement = m_nameMap[from];
            if (expressionElement != null) {
                DualModeString dms = expressionElement as DualModeString;
                if (dms != null) {
                    dms.Name = to;
                    m_nameMap.Remove(from);
                    m_nameMap.Add(to, expressionElement);
                }
            }
        }

        private string Msg_NameMapDoesntContainKey(string from) {
            string msg = "A caller requested an expression element from the ParticipantDirectory associated with the name, \"" + from +
                "\", but it was not there. Some possible alternatives (closely-named entries) are : ";

            List<string> closeMatchers = new List<string>();
            foreach (string potentialName in m_nameMap.Keys) {
                if (from.Contains(potentialName) || potentialName.Contains(from)) {
                    closeMatchers.Add(potentialName);
                }
            }

            msg += Utility.StringOperations.ToCommasAndAndedList(closeMatchers.ToArray());

            closeMatchers = new List<string>(m_nameMap.Keys);

            msg += ". A list of all names in the list is " + Utility.StringOperations.ToCommasAndAndedList(closeMatchers.ToArray());

            return msg;

        }

        /// <summary>
        /// Gets the <see cref="T:ExpressionElement"/> with the specified name.
        /// </summary>
        /// <value></value>
        public ExpressionElement this[string name] {
            get {
                if (m_nameMap.ContainsKey(name)) {
                    return m_nameMap[name];
                } else {
                    if (Parent != null && Parent.Contains(name)) {
                        return Parent[name];
                    } else {
                        return null;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the parent participantDirectory to this one..
        /// </summary>
        /// <value>The parent.</value>
        public ParticipantDirectory Parent {
            get { return m_parent; }
            set { m_parent = value; }
        }

        /// <summary>
        /// Gets the <see cref="T:ExpressionElement"/> with the specified GUID.
        /// </summary>
        /// <value></value>
        public ExpressionElement this[Guid guid] {
            get {
                if (m_guidMap.ContainsKey(guid)) {
                    return m_guidMap[guid];
                } else {
                    if (Parent != null && Parent.Contains(guid)) {
                        return Parent[guid];
                    } else {
                        return null;
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether this ParticipantDirectory contains the specified name.
        /// </summary>
        /// <param name="name">The user-friendly name of this object. Typically not required to be unique in a pan-model context.</param>
        /// <returns>
        /// 	<c>true</c> if this ParticipantDirectory contains the specified name; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(string name) {
            return m_nameMap.ContainsKey(name) || m_nameMap.ContainsKey(name.Trim('\'')) || ( Parent != null && Parent.Contains(name) );
        }

        /// <summary>
        /// Determines whether this ParticipantDirectory contains the specified guid.
        /// </summary>
        /// <param name="guid">The guid of the object-of-interest.</param>
        /// <returns>
        /// 	<c>true</c> if this ParticipantDirectory contains the specified guid; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(Guid guid) {
            return m_guidMap.ContainsKey(guid) || ( Parent != null && Parent.Contains(guid) );
        }

        #region IEnumerable<ExpressionElement> Members

        /// <summary>
        /// Returns an enumerator that iterates through the ExpressionElements in the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public IEnumerator<ExpressionElement> GetEnumerator() {
            return m_nameMap.Values.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// Returns an enumerator that iterates through the ExpressionElements in the collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"></see> object that can be used to iterate through the collection.
        /// </returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return m_nameMap.Values.GetEnumerator();
        }

        #endregion


        /// <summary>
        /// Refreshes the Participant Directory to contain only the ExpressionElements in the specified PFC.
        /// Performed via a Mark-and-Sweep algorithm.
        /// </summary>
        /// <param name="pfc">The PFC.</param>
        internal void Refresh(ProcedureFunctionChart pfc) {

            foreach (ExpressionElement ee in m_nameMap.Values) {
                ee.Marked = false;
                _Debug.Assert(m_guidMap.ContainsValue(ee));
            }

            foreach (ExpressionElement ee in m_guidMap.Values) {
                _Debug.Assert(ee.Marked == false);
                _Debug.Assert(m_nameMap.ContainsValue(ee));
            }

            foreach (IPfcNode node in pfc.Steps) {
                if (m_nameMap.ContainsKey(node.Name)) {
                    m_nameMap[node.Name].Marked = true;
                }
            }

            // PCB20080725: Was this. Changed to the following, to key on name.
            //foreach (IPfcTransitionNode trans in pfc.Transitions) {
            //    foreach (ExpressionElement ee in trans.Expression.Elements) {
            //        ee.Marked = true;
            //    }
            //}

            foreach (IPfcTransitionNode trans in pfc.Transitions) {
                foreach (ExpressionElement ee in trans.Expression.Elements) {
                    if (ee is Macro) {
                        m_nameMap[ee.Name.Trim('\'')].Marked = true;
                    } else if (ee.Name != string.Empty) {
                        m_nameMap[ee.Name].Marked = true;
                    }
                }
            }

            List<ExpressionElement> rejects = new List<ExpressionElement>();
            foreach (ExpressionElement ee in m_nameMap.Values) {
                if (!ee.Marked) {
                    rejects.Add(ee);
                }
                ee.Marked = false;
            }

            foreach (ExpressionElement ee in rejects) {
                m_nameMap.Remove(ee.Name);
                m_guidMap.Remove(ee.Guid);
            }
        }
    }

    /// <summary>
    /// An Expression is a class that contains a list of expression elements, a sequence of text snippets,
    /// references to things that have names (i.e. steps &amp; transitions), and macros. 
    /// </summary>
    public class Expression : ExpressionElement {

        private class UnknownReferenceElement : ExpressionElement {
            private Guid m_guid = Guid.Empty;
            public UnknownReferenceElement(Guid guid) {
                m_guid = guid;
            }
            public override Guid Guid { get { return m_guid; } }

            public override string ToString(ExpressionType t, object forWhom) {
                throw new Exception("Directory was unable to map an expression element to the guid, " + m_guid + ".");
            }
        }

        #region Private Fields
        private bool m_hasUnknowns = false;
        private ParticipantDirectory m_participantDirectory = null;
        private List<ExpressionElement> m_elements;
        private object m_owner = null;
        private static Regex _singQuotes =
            new Regex(@" \'                   " +
                      @"   (?>                " +
                      @"       [^\']+         " +
                      @"     |                " +
                      @"       \' (?<DEPTH>)  " +
                      @"     |                " +
                      @"       \' (?<-DEPTH>) " +
                      @"   )*                 " +
                      @"   (?(DEPTH)(?!))     " +
                      @" \'                   ", RegexOptions.IgnorePatternWhitespace);
        private static Regex _guidFinder =
            new Regex(@"[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}");
        #endregion 

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Expression"/> class.
        /// </summary>
        /// <param name="owner">The owner.</param>
        private Expression(object owner) {
            m_elements = new List<ExpressionElement>();
            m_owner = owner;
        }

        /// <summary>
        /// Creates an Expression from a user-friendly representation of the expression. In that representation, 
        /// a macro is expressed with a leading &quot;'μ&quot;, as in 'μPreviousComplete' ...
        /// </summary>
        /// <param name="uf">Thr user-friendly representation of the expression from which it will be created.</param>
        /// <param name="directory">The directory into which the Expression Elements that are created from this
        /// representation will be stored.</param>
        /// <param name="owner">The owner of the expression - usually, the Transition to which it is attached.</param>
        /// <returns>The newly-created expression.</returns>
        public static Expression FromUf(string uf, ParticipantDirectory directory, object owner) {
            Expression expression = new Expression(owner);
            expression.m_participantDirectory = directory;

            int cursor = 0;
            foreach (Match match in _singQuotes.Matches(uf)) {
                if (match.Value.StartsWith(Macro.MACRO_START)) {
                    Macro macro = (Macro)directory[match.Value.Trim('\'')];
                    expression.m_elements.Add(macro);
                    cursor = match.Index + match.Value.Length;
                } else {
                    expression.m_elements.Add(new RoteString(uf.Substring(cursor, (match.Index - cursor + 1))));
                    string token = match.Value.Substring(1, match.Value.Length - 2);
                    if (token.Contains("/")) {
                        int slashNdx = token.IndexOf('/');
                        expression.m_elements.Add(directory.RegisterMapping(token.Substring(0, slashNdx)));
                        expression.m_elements.Add(new RoteString(token.Substring(slashNdx)));
                        cursor = match.Index + match.Value.Length - 1;
                    } else {
                        expression.m_elements.Add(directory.RegisterMapping(token));
                        expression.m_elements.Add(new RoteString(match.Value.Substring(match.Value.Length - 1, 1)));
                        cursor = match.Index + match.Value.Length;
                    }
                }
            }

            if (cursor != uf.Length) {
                expression.m_elements.Add(new RoteString(uf.Substring(cursor)));
            }

            List<ExpressionElement> tmp = new List<ExpressionElement>(expression.m_elements);
            expression.m_elements.Clear();

            #region Consolidate sequential RoteString elements.

            StringBuilder sb = new StringBuilder();
            foreach (ExpressionElement ee in tmp) {
                if (!ee.GetType().Equals(typeof(RoteString))) {
                    if (sb.Length > 0) {
                        expression.m_elements.Add(new RoteString(sb.ToString()));
                        sb = new StringBuilder();
                    }

                    expression.m_elements.Add(ee);

                } else {
                    sb.Append(ee);
                }
            }

            if (sb.Length > 0) {
                expression.m_elements.Add(new RoteString(sb.ToString()));
            }

            #endregion

            return expression;
        }

        /// <summary>
        /// Creates an Expression from a user-hostile representation of the expression. In that representation,
        /// we have guids and we have rote strings.
        /// </summary>
        /// <param name="uh">The user-hostile representation of the expression.</param>
        /// <param name="directory">The directory from which the Expression Elements that are referenced here by guid, come.</param>
        /// <param name="owner">The owner of the expression - usually, the Transition to which it is attached.</param>
        /// <returns>The newly-created expression.</returns>
        public static Expression FromUh(string uh, ParticipantDirectory directory, object owner) {
            Expression expression = new Expression(owner);
            expression.m_participantDirectory = directory;

            int cursor = 0;
            foreach (Match match in _guidFinder.Matches(uh)) {

                if (match.Index != cursor) {
                    expression.m_elements.Add(new RoteString(uh.Substring(cursor, (match.Index - cursor))));
                    cursor = match.Index;
                }

                Guid guid = new Guid(match.Value);
                cursor += match.Value.Length;

                if (directory.Contains(guid)) {
                    expression.m_elements.Add(directory[guid]);
                } else {
                    expression.m_elements.Add(new UnknownReferenceElement(guid));
                    expression.m_hasUnknowns = true;
                }
            }

            if (cursor != uh.Length) {
                expression.m_elements.Add(new RoteString(uh.Substring(cursor)));
            }

            return expression;
        }

        /// <summary>
        /// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
        /// </returns>
        public override string ToString() {
            return ToString(ExpressionType.Friendly,null);
        }

        /// <summary>
        /// Returns the string for this macro that corresponds to the indicated representation type.
        /// </summary>
        /// <param name="t">The indicated representation type.</param>
        /// <param name="forWhom">The owner of the macro, usually a Transition.</param>
        /// <returns>The string for this macro.</returns>
        public override string ToString(ExpressionType t, object forWhom) {

            if (m_hasUnknowns) {
                ResolveUnknowns();
            }

            StringBuilder sb = new StringBuilder();
            foreach (ExpressionElement ee in m_elements) {
                sb.Append(ee.ToString(t, forWhom));
            }

            return sb.ToString();
        }

        internal void ResolveUnknowns() {
            if (m_hasUnknowns) {
                List<ExpressionElement> temp = m_elements;
                m_elements = new List<ExpressionElement>();

                foreach (ExpressionElement ee in temp) {
                    if (ee is UnknownReferenceElement) {
                        Guid guid = ee.Guid;
                        if (m_participantDirectory.Contains(guid)) {
                            m_elements.Add(m_participantDirectory[guid]);
                        } else {
                            IPfcTransitionNode trans = m_owner as IPfcTransitionNode;
                            string msg;
                            if ( trans != null ) {
                                msg = string.Format("Failed to map Guid {0} into an object on behalf of {1} in Pfc {2}.",guid,trans.Name,trans.Parent.Name);
                            } else {
                                msg = string.Format("Failed to map Guid {0} into an object on behalf of {1}.",guid,m_owner);
                            }
                           throw new ApplicationException(msg);
                        }
                    } else {
                        m_elements.Add(ee);
                    }
                }
                m_hasUnknowns = false;
            }
        }

        internal List<ExpressionElement> Elements {
            get {
                if (m_elements != null) {
                    return m_elements;
                } else {
                    List < ExpressionElement > le = new List<ExpressionElement>();
                    le.Add(this);
                    return le;
                }
            }
        }
    }
}