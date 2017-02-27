/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections;

namespace Highpoint.Sage.Resources {
	
	/// <summary>
	/// A delegate with which an event involving a SOMUnit is broadcast. Examples are ResourceManagerAdded and ResourceManagerRemoved. 
	/// </summary>
	public delegate void ResourceManagerChangeListener(object subject, IResourceManager manager);

	public interface IResourceManagerCollection {
		/// <summary>
		/// Adds a resource manager to the model. Fires the ResourceManagerAdded event. A resource manager is any
		/// implementer of IResourceManager, including a self-managing resource.
		/// </summary>
		/// <param name="manager">The manager to be added.</param>
		void Add(IResourceManager manager);

		/// <summary>
		/// Removes a resource manager from the model. Fires the ResourceManagerRemoved event.
		/// </summary>
		/// <param name="manager">The manager to be removed.</param>
		void Remove(IResourceManager manager);

		/// <summary>
		/// Retrieves a resource manager that is known to the SOMModel, by its guid.
		/// </summary>
		/// <param name="guid">The guid for which the resource manager is requested.</param>
		/// <returns>The resource manager for the quid that was requested.</returns>
		IResourceManager GetResourceManager(Guid guid);

		/// <summary>
		/// Returns a collection of all resource managers known to this collection.
		/// </summary>
		/// <returns></returns>
		ICollection GetResourceManagers();

		/// <summary>
		/// Fired when a resource manager is added to the model.
		/// </summary>
		event ResourceManagerChangeListener ResourceManagerAdded;
		
		/// <summary>
		/// Fired when a resource manager is removed from the model.
		/// </summary>
		event ResourceManagerChangeListener ResourceManagerRemoved;
	}

	public class ResourceManagerCollection : IResourceManagerCollection {
	
		private readonly Hashtable m_resourceMgrs;
        private readonly IResourceManagerCollection m_wrappedByWhom;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceManagerCollection"/> class.
        /// </summary>
        public ResourceManagerCollection(){
			m_resourceMgrs = new Hashtable();
			m_wrappedByWhom = this;
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceManagerCollection"/> class.
        /// </summary>
        /// <param name="whoDelegatesToMe">The who delegates to me.</param>
        public ResourceManagerCollection(IResourceManagerCollection whoDelegatesToMe){
			m_resourceMgrs = new Hashtable();
			m_wrappedByWhom = whoDelegatesToMe;
		}

		/// <summary>
		/// Adds a resource manager to the model. Fires the ResourceManagerAdded event. A resource manager is any
		/// implementer of IResourceManager, including a self-managing resource.
		/// </summary>
		/// <param name="manager">The manager to be added.</param>
		public void Add(IResourceManager manager){
			m_resourceMgrs.Add(manager.Guid,manager);
		    ResourceManagerAdded?.Invoke(m_wrappedByWhom,manager);
		}

		/// <summary>
		/// Removes a resource manager from the model. Fires the ResourceManagerRemoved event.
		/// </summary>
		/// <param name="manager">The manager to be removed.</param>
		public void Remove(IResourceManager manager){
			m_resourceMgrs.Remove(manager.Guid);
            ResourceManagerRemoved?.Invoke(m_wrappedByWhom, manager);
        }

		/// <summary>
		/// Retrieves a resource manager that is known to this collection, by its guid.
		/// </summary>
		/// <param name="guid">The guid for which the resource manager is requested.</param>
		/// <returns>The resource manager for the quid that was requested.</returns>
		public IResourceManager GetResourceManager(Guid guid){
			return (IResourceManager)m_resourceMgrs[guid];
		}

		/// <summary>
		/// Returns a collection of all resource managers known to this collection.
		/// </summary>
		/// <returns></returns>
		public ICollection GetResourceManagers(){
			return m_resourceMgrs.Values;
		}

		/// <summary>
		/// Fired when a resource manager is added to the model.
		/// </summary>
		public event ResourceManagerChangeListener ResourceManagerAdded;
		
		/// <summary>
		/// Fired when a resource manager is removed from the model.
		/// </summary>
		public event ResourceManagerChangeListener ResourceManagerRemoved;
	}
}
