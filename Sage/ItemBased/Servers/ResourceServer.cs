/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections;
using Trace = System.Diagnostics.Debug;
using Highpoint.Sage.Resources;
using Highpoint.Sage.SimCore;

namespace Highpoint.Sage.ItemBased.Servers {
	/// <summary>
	/// A resource server is a server that acquires a resource on behalf of an object presented
	/// at its input port, waits a particular duration, releases that resource, and passes the
	/// object to its output port.<p></p>
	/// The ResourceServer is aware of a ResourcePool, and when the ResourceServer is placed in
	/// service, or the ResourcePool fires a Release event, the resource server attampts to pull
	/// and service a new object from its input port.
	/// </summary>
	public class ResourceServer : ServerPlus {

		private IResourceRequest[] m_requestTemplates;
		private Hashtable m_resourcesInUse;
		private bool m_useBlockingCalls = false;

		public ResourceServer(IModel model, string name, Guid guid, IPeriodicity periodicity, IResourceRequest[] requestTemplates)
			:base(model,name,guid,periodicity){

			m_requestTemplates = requestTemplates;
			if ( m_requestTemplates == null ) m_requestTemplates = new IResourceRequest[]{};
			
			m_resourcesInUse = new Hashtable();

			foreach ( IResourceRequest irr in m_requestTemplates ) if ( irr.DefaultResourceManager == null ) m_useBlockingCalls = true;
			if ( !m_useBlockingCalls ) {
				foreach ( IResourceRequest irr in m_requestTemplates ) {
					irr.DefaultResourceManager.ResourceReleased+=new ResourceStatusEvent(DefaultResourceManager_ResourceReleased);
				}
			} else {
				throw new NotSupportedException("Resource Server Templates must specify (and use) default resource manager.");
			}
		}

		protected override bool RequiresAsyncEvents { get { return m_useBlockingCalls; } }

		protected override bool CanWeProcessServiceObjectHandler(IServer server, object obj){
			IResourceRequest[] replicates = MultiRequestProcessor.Replicate(ref m_requestTemplates);
			bool success = MultiRequestProcessor.ReserveAll(ref replicates, m_useBlockingCalls);
			if ( success ) m_resourcesInUse.Add(obj,replicates);
			return success;
		}

		protected override void PreCommencementSetupHandler(IServer server, object obj){
			IResourceRequest[] replicates = (IResourceRequest[])m_resourcesInUse[obj];
			MultiRequestProcessor.AcquireAll(ref replicates,m_useBlockingCalls);
		}

		protected override void PreCompletionTeardownHandler(IServer server, object obj){
			IResourceRequest[] replicates = (IResourceRequest[])m_resourcesInUse[obj];
			MultiRequestProcessor.ReleaseAll(ref replicates);
			m_resourcesInUse.Remove(obj);
		}

		private void DefaultResourceManager_ResourceReleased(IResourceRequest irr, IResource resource) {
			TryToPullServiceObject();
		}
	}
}