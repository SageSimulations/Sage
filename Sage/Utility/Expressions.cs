/* This source code licensed under the GNU Affero General Public License */

#if SUPPORT_LEGACY_SAGE
#endif
using System;
using System.Collections;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Linq;
using System.Reflection;

namespace Highpoint.Sage.SimCore {

    /// <summary>
    /// An exception that encapsulates the errors encountered in trying to compile an expression.
    /// </summary>
    public class CompileFailedException : Exception {

        /// <summary>
        /// A collection of compiler error objects encountered in trying to compile an expression.
        /// </summary>
        private readonly CompilerErrorCollection m_cec;

        /// <summary>
        /// Property that gets the collection of compiler error objects encountered in trying to compile an expression.
        /// </summary>
        public CompilerErrorCollection Errors => m_cec;

        /// <summary>
        /// Constructor for an exception indicating that an attempt to compile an expression, failed.
        /// </summary>
        /// <param name="cec"></param>
        public CompileFailedException(CompilerErrorCollection cec){
            m_cec = cec;
        }

		/// <summary>
		/// Produces a human-readable compilation of the reasons why a compilation attempt failed.
		/// </summary>
		/// <returns></returns>
        public override string ToString(){
            string retval = base.ToString();
            retval += "\r\n\r\n";
            for ( int i = 0 ; i < m_cec.Count ; i++ ) {
                retval += "\t" + i + ") ";
                retval += m_cec[i].ToString();
            }

            return retval;
        }
    }

    /// <summary>
    /// A delegate returned by the evaluator factory, which will evaluate a precompiled expression with the
    /// the supplied argument values. Important to remember that the arguments passed in to an evaluator are
    /// treated as objects, so if it is expecting a double, and you pass in '3', it gets converted to an
    /// instance of System.Int32, and cannot be converted (implicitly) to a double.
    /// <para/>
    /// This means that if it's expecting a double, pass in 3.0, not 3, or you'll get an invalid cast exception. 
    /// </summary>
    public delegate object Evaluator(params object[] args);

    /// <summary>
    /// The Evaluator Factory creates objects that can evaluate expressions provided by the caller.
    /// First an expression is provided and compiled into an object that can evaluate it - that
    /// object is passed back as an Evaluator delegate. Then later the Evaluator delegate is called
    /// with the values appropriate to the variables that are a part of the pre-provided expression.
    /// The result of this evaluation is the (double) value that is computed.
    /// <br></br>
    /// The body of the evaluator can be any compilable statement. If the evaluator is to return a
    /// value, the body must assign that value to the (predefined) variable "Result".
    /// <br></br>
    /// The ArgNames array is an array of strings that contain the names of the arguments to the
    /// evaluation of the expression. The defaults of those argNames is:
    /// <p></p>
    /// {"PA","PB","PC","PD","PE","PF","PG","PH","PJ","PK","RA","RB","RC","RD","RE","RF","RG","RH","RJ","RK"};
    /// <p></p>
    /// An example of using this class might be as follows:
    /// <code>
    /// Evaluator eval = EvaluatorFactory.CreateEvaluator("Result = RA+(RB/RC);",new string[]{"RA","RB","RC"});
    /// </code>
    /// ...and this eval could later be called with:
    /// <code>
    /// _Debug.WriteLine(eval(3.0,4.0,5.0));
    /// </code>
    /// which would result in 3.8 being written to the Trace.
    /// </summary>
    public static class EvaluatorFactory {

        /// <summary>
        /// A counter of Evaluators. Thus, the 4th Evaluator will be called, MyEvaluator4.
        /// </summary>
        private static int _calcCtr;

        /// <summary>
        /// The default argument names placed into the expression call.
        /// </summary>
        private static readonly string[] s_defaultArgNames = new string[]{"PA","PB","PC","PD","PE","PF","PG","PH","PJ","PK",
                                                                  "RA","RB","RC","RD","RE","RF","RG","RH","RJ","RK"};
        /// <summary>
        /// Creates an Evaluator with the specified body and argument names.
        /// <para/>
        /// </summary>
        /// <param name="body">The body of the statement that will be evaluated.</param>
        /// <param name="argNames">The names of the arguments provided to that evaluator.
        /// Note that it must include the names of all non-intrinsic variables in the statement.</param>
        /// <returns>A delegate behind which is the evaluator generated from compiling the provided statement.</returns>
        public static Evaluator CreateEvaluator(string body, string[] argNames){
            body = PrepareBody(body);
            return CreateEvaluator(body,"Result",argNames);
        }
		
        /// <summary>
        /// Creates an Evaluator with the specified body. Argument names default to
        /// <code>new string[]{"PA","PB","PC","PD","PE","PF","PG","PH","PJ","PK",
        /// "RA","RB","RC","RD","RE","RF","RG","RH","RJ","RK"};</code>
        /// </summary>
        /// <param name="body">The body of the statement that will be evaluated.</param>
        /// <returns>A delegate behind which is the evaluator generated from compiling the provided statement.</returns>
        public static Evaluator CreateEvaluator(string body){
            body = PrepareBody(body);
            return CreateEvaluator(body,"Result",s_defaultArgNames);
        }

        /// <summary>
        /// Creates an Evaluator with the specified body, return expression, and argument names.
        /// </summary>
        /// <param name="body">The statement body of the evaluator.</param>
        /// <param name="retExpr">The return expression of the evaluator.</param>
        /// <param name="argNames">The names of the arguments fed into the evaluator.</param>
        /// <returns></returns>
        public static Evaluator CreateEvaluator(string body, string retExpr, string[] argNames){
            string thisClassName = "MyEvaluator"+(_calcCtr++);

            // 1.) Create the evaluation method. [evaluate]
            CodeMemberMethod methEvaluate = new CodeMemberMethod
            {
                Attributes = MemberAttributes.Public,
                ReturnType = new CodeTypeReference(typeof (object)),
                Name = "evaluate"
            };
            foreach (string t in argNames.Where(t => !string.IsNullOrEmpty(t)))
            {
                methEvaluate.Parameters.Add(new CodeParameterDeclarationExpression(typeof(double),t));
            }

            if ( !string.IsNullOrEmpty(body)) {
                methEvaluate.Statements.Add(
                    new CodeSnippetStatement(body));
            }

            if ( !string.IsNullOrEmpty(retExpr)) {
                methEvaluate.Statements.Add(
                    new CodeMethodReturnStatement(
                    new CodeSnippetExpression(retExpr)));
            }

            // 2.) Create the evaluation method caller. [callEvaluate]
            //     This is needed to support varargs into the call, but still have the varargs bind
            //     to argument names.
            string expr = "evaluate(";
            bool argsLaidInline = false;
            for ( int i = 0 ; i < argNames.Length ; i++ ) {
                if ( argNames[i] == null || argNames[i].Equals("")) continue;
                expr += (argsLaidInline?",":"")+(("(double)args["+i+"]"));
                argsLaidInline = true;
            }
            expr += ")";

            CodeMemberMethod methCallEvaluate = new CodeMemberMethod
            {
                Attributes = MemberAttributes.Public,
                ReturnType = new CodeTypeReference(typeof (object)),
                Name = "callEvaluate"
            };
            methCallEvaluate.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object[]),"args"));
            methCallEvaluate.Statements.Add(
                new CodeMethodReturnStatement(
                new CodeSnippetExpression(expr)));
			
            // 3.) Create the object it will live in. [calculator]
            CodeTypeDeclaration calculator = new CodeTypeDeclaration(thisClassName)
            {
                IsClass = true,
                TypeAttributes = TypeAttributes.NotPublic
            };
            calculator.Members.Add(methCallEvaluate);
            calculator.Members.Add(methEvaluate);

            // 4.) Create the compilation unit.
            CodeCompileUnit compileUnit = new CodeCompileUnit();
            CodeNamespace nameSpace = new CodeNamespace("Highpoint.Dynamic.Expressions");
            nameSpace.Imports.Add(new CodeNamespaceImport("System"));
            nameSpace.Types.Add(calculator);
            compileUnit.Namespaces.Add(nameSpace);

            // 5.) Prepare to compile - Create the parameters.
            ArrayList assemblyNames = new ArrayList();
            foreach( Assembly asm in AppDomain.CurrentDomain.GetAssemblies() ) {
                if ( !string.IsNullOrEmpty(asm.Location)) {
                    assemblyNames.Add(new Uri(asm.Location).LocalPath);
                }
            }
            CompilerParameters compilerParameters =
                new CompilerParameters((string[]) assemblyNames.ToArray(typeof (string)))
                {
                    GenerateInMemory = true,
                    TempFiles = {KeepFiles = true}
                };

            // 6.) Create the compiler.
            CodeDomProvider codeDomProvider = CodeDomProvider.CreateProvider("C#");
            if (codeDomProvider == null) {
                throw new ApplicationException("Attempt to obtain a C# compiler failed. The local machine does not appear to have one.");
            }

            // 7.) And compile.
            Evaluator evaluator = null;
            CompilerResults compilerResults = codeDomProvider.CompileAssemblyFromDom(compilerParameters,compileUnit);
            if ( compilerResults == null || compilerResults.Errors.Count > 0 ) {
                throw new CompileFailedException(compilerResults.Errors);
            } else
            {
                object obj = compilerResults.CompiledAssembly.CreateInstance("Highpoint.Dynamic.Expressions."+thisClassName,true);
                if (obj != null)
                    evaluator = (Evaluator)Delegate.CreateDelegate(typeof(Evaluator),obj,"callEvaluate");
            }
            return evaluator;
        }

        /// <summary>
        /// called to decorate the body statement prior to an attempted compilation.
        /// </summary>
        /// <param name="body">The user-supplied statement body.</param>
        /// <returns>The "Ready-to-compile" version of the body.</returns>
        private static string PrepareBody(string body){

            // If the body is, e.g. " = RA/RB;", then we change it to be "Result = RA/RB;"
            if ( body.TrimStart().StartsWith("=") ) body = "Result " + body;

            return "System.Double Result = System.Double.NaN;\r\n\t\t"+body;
        }
    }
}
