/* This source code licensed under the GNU Affero General Public License */

namespace Highpoint.Sage.SimCore {

    /// <summary>
    /// Implemented by an object that can visit.
    /// <p></p>
    /// The purpose of the Visitor Pattern is to encapsulate
    /// an operation that you want to perform on the elements
    /// of a data structure. In this way, you can change the
    /// operation being performed on a structure without the
    /// need of changing the classes of the elements that you
    /// are operating on. Using a Visitor pattern allows you to
    /// decouple the classes for the data structure and the
    /// algorithms used upon them.
    /// <p></p>
    /// Each node in the data structure "accepts" a Visitor, which
    /// sends a message to the Visitor which includes the node's
    /// class. The visitor will then execute its algorithm for that
    /// element. This process is known as "Double Dispatching." The
    /// node makes a call to the Visitor, passing itself in, and the
    /// Visitor executes its algorithm on the node. In Double Dispatching,
    /// the call made depends upon the type of the Visitor and of the Host
    /// (data structure node), not just of one component.
    /// </summary>
    public interface IVisitor
    {
        /// <summary>
        /// Called by the object being visited, asking the visitor to do its
        /// thing. In derived classes, new methods that provide more specific
        /// classes of visitee result in the visitee calling implementation-
        /// specific methods in the visitor.
        /// </summary>
        /// <param name="visitee">The object being visited.</param>
        void Visit(object visitee);
    }

    /// <summary>
    /// An interface that is implemented by any object that can be visited. It
    /// typically does very little more than call iv.Visit(this);
    /// </summary>
    public interface IVisitable
    {
        /// <summary>
        /// Requests that the IVisitable allow the visitor to visit.
        /// </summary>
        /// <param name="iv">The visitor.</param>
        void Accept(IVisitor iv);
    }

    /// <summary>
    /// Implemented by an object that can visit.
    /// <p></p>
    /// The purpose of the Visitor Pattern is to encapsulate
    /// an operation that you want to perform on the elements
    /// of a data structure. In this way, you can change the
    /// operation being performed on a structure without the
    /// need of changing the classes of the elements that you
    /// are operating on. Using a Visitor pattern allows you to
    /// decouple the classes for the data structure and the
    /// algorithms used upon them.
    /// <p></p>
    /// Each node in the data structure "accepts" a Visitor, which
    /// sends a message to the Visitor which includes the node's
    /// class. The visitor will then execute its algorithm for that
    /// element. This process is known as "Double Dispatching." The
    /// node makes a call to the Visitor, passing itself in, and the
    /// Visitor executes its algorithm on the node. In Double Dispatching,
    /// the call made depends upon the type of the Visitor and of the Host
    /// (data structure node), not just of one component.
    /// </summary>
    public interface IVisitor<in T>
    {
        /// <summary>
        /// Called by the object being visited, asking the visitor to do its
        /// thing. In derived classes, new methods that provide more specific
        /// classes of visitee result in the visitee calling implementation-
        /// specific methods in the visitor.
        /// </summary>
        /// <param name="visitee">The object being visited.</param>
        void Visit(T visitee);
    }

    /// <summary>
    /// An interface that is implemented by any object that can be visited. It
    /// typically does very little more than call iv.Visit(this);
    /// </summary>
    public interface IVisitable<out T>
    {
        /// <summary>
        /// Requests that the IVisitable allow the visitor to visit.
        /// </summary>
        /// <param name="iv">The visitor.</param>
        void Accept(IVisitor<T> iv);
    }

}