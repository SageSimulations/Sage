/* This source code licensed under the GNU Affero General Public License */
namespace Highpoint.Sage.SimCore
{

    /// <summary>
    /// Interface IModelService is implemented by anything that can act as a service that has been injected into a model.
    /// </summary>
    public interface IModelService
    {

        /// <summary>
        /// Initializes the service to run in the provided model. This is called by the model immediately after the service is added.
        /// </summary>
        /// <param name="model">The model.</param>
        void InitializeService(IModel model);

        /// <summary>
        /// Gets or sets a value indicating whether this instance has been initialized yet.
        /// </summary>
        /// <value><c>true</c> if this instance is initialized; otherwise, <c>false</c>.</value>
        bool IsInitialized { get; set; }

        /// <summary>
        /// Gets a value indicating whether the service is to be automatically initialized inline when
        /// the service is added to the model, or if the user (i.e. the custom model class) will do so later.
        /// </summary>
        /// <value><c>true</c> if initialization is to occur inline, otherwise, <c>false</c>.</value>
        bool InlineInitialization { get; }
    }
}
