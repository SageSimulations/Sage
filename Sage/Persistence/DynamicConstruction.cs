/* This source code licensed under the GNU Affero General Public License */
#if INCLUDE_WIP

using System;
using System.Diagnostics;
using Trace = System.Diagnostics.Debug;
using System.Collections;
using Highpoint.Sage.SimCore;
using Highpoint.Sage.Utility;

namespace Highpoint.Sage.DynamicConstruction {

	 // TODO: Implement dependencies in this!

	public interface IBindsToCreationContext {
		void AddBindableChild(IBindsToCreationContext iucc);
		IList BindableChildren { get; }
		void Bind(CreationContext cc);
		CreationContext CreationContext { get; }
	}

	public interface IHasSubRequirements {
		//void AddSubRequirement(IRequirement iucc);
		ArrayList SubRequirements { get; }
	}

	
	public interface ISettings : IBindsToCreationContext, IHasSubRequirements {
		object PerformSettings(object target);
	}

	public abstract class Settings : ISettings {
#region Fields
		private ArrayList m_bindableChildren = new ArrayList();
		private ArrayList m_subRequirements = new ArrayList();
		protected CreationContext m_creationContext;
#endregion
		
#region IBindsToCreationContext Members
		public void AddBindableChild(IBindsToCreationContext iucc){
			m_bindableChildren.Add(iucc);
			if ( iucc is IRequirement ) m_subRequirements.Add(iucc);
		}
		public virtual void Bind(CreationContext cc){
			//Console.WriteLine("Binding " + this.GetType() + ".");
			if ( cc != m_creationContext ) { 
				m_creationContext = cc;
				foreach ( IBindsToCreationContext iucc in m_bindableChildren ) {
					iucc.Bind(cc);
				}
			}
		}
		public IList BindableChildren { get { return m_bindableChildren; } }
		public CreationContext CreationContext { get { return m_creationContext; } }
#endregion

#region ISettings Members
		public abstract object PerformSettings(object target);
#endregion
 
#region IHasSubRequirements Members	
		//public void AddSubRequirement(IRequirement iucc){ m_subRequirements.Add(iucc); }
		public ArrayList SubRequirements { get { return m_subRequirements; } }
#endregion
	}

	/// <summary>
	/// A Requirement is something that the Recipe needs in a model, but which is not a part of the
	/// recipe. If it is mandatory, it is loaded immediately into the model (if not already present)
	/// as a part of provisioning. If it is not mandatory, then it is loaded only on the first call
	/// to its &quotMeet(...)&quot API. A requirement includes a specification that enables the
	/// requirement to be met when it is needed.
	/// <summary>
	public interface IRequirement : IHasIdentity, IBindsToCreationContext, IHasSubRequirements {
		bool IsMandatory { get; }
		bool IsMet();
		object Meet();
		ISpecification FactorySpec { get; }
	}


	public abstract class Requirement : IRequirement {
#region Fields
		private CreationContext m_creationContext;
		private ArrayList m_bindableChildren = new ArrayList();
		private ArrayList m_subRequirements = new ArrayList();
		protected string m_name;
		protected Guid m_guid;
		private Guid m_factorySpecGuid;
		private OverrideBool m_mandatory = new OverrideBool();
#endregion

#region Constructors
		public Requirement(){}
		public Requirement(bool isMandatory){
			m_mandatory.BoolValue = isMandatory;
		}
#endregion

#region IBindsToCreationContext Members
		public void AddBindableChild(IBindsToCreationContext iucc){
			m_bindableChildren.Add(iucc);
			if ( iucc is IRequirement ) m_subRequirements.Add(iucc);
		}
		public virtual void Bind(CreationContext cc){
			//Console.WriteLine("Binding " + this.Name + ".");
			if ( cc != m_creationContext ) { 
				m_creationContext = cc;
				foreach ( IBindsToCreationContext iucc in m_bindableChildren ) {
					iucc.Bind(cc);
				}
			}
		}
		public IList BindableChildren { get { return m_bindableChildren; } }
		public CreationContext CreationContext { get { return m_creationContext; } }
#endregion
		
#region IRequirement Members
		public virtual bool IsMet() { return (m_creationContext.GetNextGeneration(FactorySpec)>1); }

		public virtual ISpecification FactorySpec{
			get {
				if ( m_factorySpecGuid.Equals(Guid.Empty) ) throw new ApplicationException("m_factorySpecGuid not set in " + this.Name + ".");
				if ( m_creationContext.Specifications.Contains(m_factorySpecGuid) ) {
					return (ISpecification)m_creationContext.Specifications[m_factorySpecGuid];
				} else {
					throw new ApplicationException("Unable to determine factory specification for requirement with Guid = " + m_factorySpecGuid.ToString());
				}
			}
		}

		public Guid FactorySpecGuid { get { return m_factorySpecGuid; } set { m_factorySpecGuid = value; } }
		
		public virtual object Meet() { 
			object obj = m_creationContext.GetInstance(FactorySpecGuid,1);
			if ( obj == null ) obj = FactorySpec.Create(true);
			return obj;
		}

		public virtual bool IsMandatory { 
			get {
				if ( m_mandatory.Override ) {
					return m_mandatory.BoolValue;
				} else {
					throw new ApplicationException("Requirement with unspecified IsMandatory preference does not override IsMandatory. Either specify a preference, or override IsMandatory.");
				}
			}
		}
#endregion

#region IHasIdentity Members
		public string Name { get { return m_name; } }
		private string m_description = null;
		/// <summary>
		/// A description of this Requirement.
		/// </summary>
		public string Description {
			get { return m_description==null?m_name:m_description; }
		}
		public Guid Guid => m_guid;
#endregion

#region IHasSubRequirements Members
		//public void AddSubRequirement(IRequirement iucc){ m_subRequirements.Add(iucc); }
		public ArrayList SubRequirements { get { return m_subRequirements; } }
#endregion

	}

	/// <summary>
	/// An ISpecification describes an object that needs to be a participant in a simulation, and
	/// must first be created. The recipe itself is a specification, containing many sub-specifications such
	/// as units & tasks. Provisioning is called by the parent to a specification.
	/// </summary>
	public interface ISpecification : IHasIdentity, IBindsToCreationContext, IHasSubRequirements {
		object Create(bool deep);
		void Provision(IDictionary graphContext, object target);
		ArrayList GetChildRequirements(bool deep);
		ArrayList GetChildSpecifications(bool deep);
	}

	
	public abstract class Specification : ISpecification {
#region Fields
		protected string m_name;
		protected Guid m_guid;
		protected ArrayList m_myDirectChildSpecifications = new ArrayList();
		protected ArrayList m_myDirectChildRequirements = new ArrayList();
		protected ArrayList m_bindableChildren = new ArrayList();
		protected ArrayList m_subRequirements = new ArrayList();
		protected CreationContext m_creationContext;
#endregion

#region Constructors
		// None, currently
#endregion
		
#region IBindsToCreationContext Members
		public void AddBindableChild(IBindsToCreationContext iucc){
			//Console.Write("Adding bindable child : " + iucc.ToString());
			if ( iucc == null ) {
				//System.Diagnostics.Debugger.Break();
			}
			m_bindableChildren.Add(iucc);
			if ( iucc is IRequirement ) m_subRequirements.Add(iucc);
			//Console.WriteLine((iucc is Requirement)?" - also a requirement":"");
		}
		public virtual void Bind(CreationContext cc){
			//Console.WriteLine("Binding " + this.Name + ".");
			if ( cc != m_creationContext ) { 
				m_creationContext = cc;
				cc.RegisterNewSpecification(this);
				foreach ( IBindsToCreationContext iucc in m_bindableChildren ) {
					iucc.Bind(cc);
				}
			}
		}
		public IList BindableChildren { get { return m_bindableChildren; } }
		public CreationContext CreationContext { get { return m_creationContext; } }
#endregion

#region IHasSubRequirements Members
		//public void AddSubRequirement(IRequirement iucc){ m_subRequirements.Add(iucc); }
		public ArrayList SubRequirements { get { return m_subRequirements; } }
#endregion

#region ISpecification Members

		public abstract object Create(bool deep);

		public virtual void Provision(IDictionary graphContext, object target){}

		public virtual ArrayList GetChildRequirements(bool deep) { 
			if ( m_myDirectChildRequirements == null ) throw new ApplicationException("ChildRequirements not set - null is not permitted.");
			if ( !deep ) return ArrayList.ReadOnly(m_myDirectChildRequirements);
			ArrayList al = new ArrayList(m_myDirectChildRequirements);
			foreach ( IRequirement ireq in m_myDirectChildRequirements ) {
				ISpecification ispec = ireq.FactorySpec;
				if ( ispec == null ) continue;
				al.AddRange(ispec.GetChildRequirements(true));
			}
			return ArrayList.ReadOnly(al);
		}

		public virtual ArrayList GetChildSpecifications(bool deep) { 
			if ( m_myDirectChildSpecifications == null ) throw new ApplicationException("ChildSpecifications not set - null is not permitted.");
			if ( !deep ) return ArrayList.ReadOnly(m_myDirectChildSpecifications);
			ArrayList al = new ArrayList(m_myDirectChildSpecifications);
			foreach ( ISpecification ispec in m_myDirectChildSpecifications ) {
				al.AddRange(ispec.GetChildSpecifications(true));
			}
			return ArrayList.ReadOnly(al);
		}

#endregion

#region IHasIdentity Members
		public string Name { get { return m_name; } }
		private string m_description = null;
		/// <summary>
		/// A description of this Specification.
		/// </summary>
		public string Description {
			get { return m_description==null?m_name:m_description; }
		}

		public Guid Guid => m_guid;
#endregion

		public Guid GenerationizedGuid(int generation){
			return GuidOps.Add(m_guid,generation);
		}

		public override string ToString() {
			return "Specification for " + m_name;
		}

	}

	
	/// <summary>
	/// The CreationContext maintains all information about the surroundings in which a
	/// specification is being met, and provides many convenience methods for exercising
	/// that creation.
	/// </summary>
	public interface ICreationContext {

#region Key Fields
		Model Model { get; } 
		Stack ParentObjectStack { get; }
		Hashtable Whiteboard { get; }
		Hashtable Specifications { get; }
#endregion
		
#region Registration
		void RegisterNewSpecification( ISpecification specification );
		Guid RegisterNewInstance( object instance, Guid guid, ISpecification specification );
#endregion

#region Retrieval
		ISpecification GetSpecification(Guid instanceGuid);
		ISpecification GetSpecification(object instance);
		object GetInstance(Guid instanceGuid);
		object GetInstance(Guid specificationGuid, int generation);
		Guid GetGuid(object instance);
		Guid GetInstanceSpecificGuid(Guid specCentricGuid, object instance);
		int GetNextGeneration(ISpecification iSpec);
#endregion
		
#region Macro Operations
		IModelObject SatisfyRequirement(Guid requirementGuid);
		void EnsureIsMet(IRequirement requirement);
		void ProvisionAll(IDictionary graphContext);
#endregion
	}

	/// <summary>
	/// The CreationContext maintains all information about the surroundings in which a specification is
	/// being met, and provides many convenience methods for implementing that creation.
	/// </summary>
	public class CreationContext : ICreationContext {
#region Private Fields
		private IModel m_model;
		private Guid m_guid;
		private Hashtable m_whiteboard;
		private Stack m_parentStack;
		/*
		private Hashtable m_instanceGuids;
		private Hashtable m_specificationGuids;
		private Hashtable m_instanceToSpecificationMap;
		*/
		private Hashtable m_instanceDataByGuid;
		private Hashtable m_instanceDataByInstance;
		private Hashtable m_specDataByGuid;
		private Hashtable m_specDataBySpecification;
		private Hashtable m_requirements;
		private Hashtable m_specifications;
#endregion

#region Private Support Classes
		public class InstanceData { 
			public InstanceData(object instance, Guid instanceGuid, int generation, ISpecification specification) {
				Instance = instance;
				InstanceGuid = instanceGuid;
				Generation = generation;
				Specification = specification;
			}
			public object Instance;
			public int Generation;
			public ISpecification Specification;
			public Guid InstanceGuid;
		}
		
		public class SpecificationData {
			public SpecificationData(ISpecification specification){
				Specification = specification;
				NextGeneration = 1;
				LastAssignedGuid = specification.Guid;
				SpecificationGuid = specification.Guid;
				//Console.WriteLine("Creating specification data for " + specification.Name + " under guid " + specification.Guid);
			}
			public ISpecification Specification;
			public Guid SpecificationGuid;
			public int NextGeneration;
			public Guid LastAssignedGuid;
			public Guid GetNextGuid(){
				lock(this){
					LastAssignedGuid = GuidOps.Increment(LastAssignedGuid);
					return LastAssignedGuid;
				}
			}
			public int GetGenerationFromGuid(Guid guid){
				for ( int i = 0 ; i < 10 ; i++ ) {
					if ( GuidOps.Add(Specification.Guid,i).Equals(guid)) return i;
				}
				throw new ApplicationException("Generation count exceeded ten - error.");
				// TODO: Need a more robust GUID subtraction routine.
			}
		}

#endregion

#region Constructors
		public CreationContext(ISpecification rootSpec, Model model){
			m_guid = Guid.NewGuid();
			m_whiteboard = new Hashtable();

			m_instanceDataByGuid = new Hashtable();
			m_instanceDataByInstance = new Hashtable();
			m_specDataByGuid = new Hashtable();
			m_specDataBySpecification = new Hashtable();

			m_requirements = new Hashtable();
			m_specifications = new Hashtable();
			m_parentStack = new Stack();
			m_model = model;
			m_model.AddCreationContext(this);
			foreach ( ISpecification spec in rootSpec.GetChildSpecifications(true) ) {
				SpecificationData sd = new SpecificationData(spec);
				m_specDataByGuid.Add(spec.Guid,sd);
				m_specDataBySpecification.Add(spec,sd);
			}
			foreach ( IRequirement ireq in rootSpec.GetChildRequirements(true) ) {
				m_requirements.Add(ireq.Guid,ireq);
			}
		}

#endregion
		
#region Key Fields
		public IModel Model => m_model; 

		public Stack ParentObjectStack { get { return m_parentStack; } }

		public Hashtable Whiteboard { get { return m_whiteboard; } }

		public Hashtable Specifications { get { return m_specifications; } }

		public Hashtable Instances { get { return m_instanceDataByGuid; } }
#endregion
		
#region Registration
		public void RegisterNewSpecification( ISpecification specification ){
			if ( m_specDataByGuid.Contains(specification.Guid) ){
				ISpecification otherSpec = ((SpecificationData)m_specDataByGuid[specification.Guid]).Specification;
//				Console.WriteLine("Trying to add specification named " + specification.Name + " with guid " 
//					+ specification.Guid.ToString() + ", but there's already been a specification registered "
//					+ "with that same guid. It has the name " + otherSpec.Name + ".");
				throw new ApplicationException("Trying to add specification named " + specification.Name + " with guid " 
					+ specification.Guid.ToString() + ", but there's already been a specification registered "
					+ "with that same guid. It has the name " + otherSpec.Name + ".");
			} else {
				SpecificationData sd = new SpecificationData(specification);
				m_specifications.Add(specification.Guid,specification);
				m_specDataByGuid.Add(sd.SpecificationGuid,sd);
				m_specDataBySpecification.Add(sd.Specification,sd);
			}
		}
		public Guid RegisterNewInstance( object instance, Guid guid, ISpecification specification ) {
			if ( m_instanceDataByGuid.Contains(guid) ){
				object otherInstance = m_instanceDataByGuid[guid];
				//				Console.WriteLine("Trying to add specification named " + specification.Name + " with guid " 
				//					+ specification.Guid.ToString() + ", but there's already been a specification registered "
				//					+ "with that same guid. It has the name " + otherSpec.Name + ".");
				throw new ApplicationException("Trying to add an instance from the specification " + specification.Name + " with guid " 
					+ guid.ToString() + ", but there's already been an instance registered "
					+ "with that same guid.");
			} else {
				SpecificationData sd = (SpecificationData)m_specDataBySpecification[specification];
				int generation = sd.GetGenerationFromGuid(guid);
				InstanceData id = new InstanceData(instance,guid,generation,specification);
				m_instanceDataByGuid.Add(id.InstanceGuid,id);
				m_instanceDataByInstance.Add(id.Instance,id);
				return id.InstanceGuid;
			}
		}
		public Guid SeizeNextGuid(ISpecification specification){
			SpecificationData sd = (SpecificationData)m_specDataBySpecification[specification];
			lock(sd){
				Guid g = sd.GetNextGuid();
				sd.NextGeneration++;
				//Console.WriteLine( specification.Guid.ToString() + " is generating new guid " + g.ToString());
				return g;
			}
		}
#endregion

#region Retrieval
		public ISpecification GetSpecification(Guid guid){
			if ( m_specDataByGuid.Contains(guid)){
				return((SpecificationData)m_specDataByGuid[guid]).Specification;
			} else if ( m_instanceDataByGuid.Contains(guid) ){
				return((InstanceData)m_instanceDataByGuid[guid]).Specification;
			} else {
				// System.Diagnostics.Debugger.Break();
				return null;
			}
		}

		public ISpecification GetSpecification(object instance){
			return((InstanceData)m_instanceDataByGuid[instance]).Specification;
		}

		public object GetInstance(Guid instanceGuid){
			return ((InstanceData)m_instanceDataByGuid[instanceGuid]).Instance;
		}
		
		public object GetInstance(Guid specificationGuid, int generation){
			Guid instanceGuid = GuidOps.Add(specificationGuid,generation);
			return ((InstanceData)m_instanceDataByGuid[instanceGuid]).Instance;
		}

		public Guid GetGuid(object instance){
			return ((InstanceData)m_instanceDataByInstance[instance]).InstanceGuid;
		}

		public int GetGeneration(object instance){
			return ((InstanceData)m_instanceDataByInstance[instance]).Generation;
		}

		/// <summary>
		/// Returns the guid for an object created (or to be created) by the specification
		/// with the specCentricGuid that is of the same generation as the instance provided.
		/// This is useful for determining the guid to assign to a port that is owned by a
		/// parent operation.
		/// </summary>
		/// <param name="specCentricGuid">The guid of the specification for which the instance
		/// guid is desired. (In the example above, it would be the port specification.)</param>
		/// <param name="instance">The correlated instance. (In the example above, it would be
		/// the parent task.)</param>
		/// <returns>The correlated guid. (In the example, above, it would be the guid to be
		/// assigned to a new port that is to be added to the given parent task.)</returns>
		public Guid GetInstanceSpecificGuid(Guid specCentricGuid, object instance){
			int instanceGeneration = ((InstanceData)m_instanceDataByInstance[instance]).Generation;
			return GuidOps.Add(specCentricGuid,instanceGeneration);
		}

		public int GetNextGeneration(ISpecification iSpec){
			return ((SpecificationData)m_specDataBySpecification[iSpec]).NextGeneration;
		}


#endregion
		
#region Macro Operations
		public IModelObject SatisfyRequirement(Guid requirementGuid){
			IRequirement tgtRqmt = (IRequirement)m_requirements[requirementGuid];
			if ( !tgtRqmt.IsMet() ) {
				return (IModelObject)tgtRqmt.Meet();
			} else {
				return null;
			}
			
		}

		public void EnsureIsMet(IRequirement requirement){
			if (!requirement.IsMet()) requirement.Meet();
		}
		
		
		public void ProvisionAll(IDictionary graphContext){
				
			foreach ( IRequirement ireq in m_requirements.Values ) EnsureIsMet(ireq);

			foreach ( DictionaryEntry de in m_instanceDataByInstance ){
				object instance = de.Key;
				InstanceData id = (InstanceData)de.Value;
				ISpecification iSpec = id.Specification;
				//Console.WriteLine("Provisioning " + iSpec);
				iSpec.Provision(graphContext,instance);
			}
		}

#endregion

	}
}
#endif

