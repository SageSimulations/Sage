/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections;
using Highpoint.Sage.SimCore;

namespace Highpoint.Sage.Materials
{

	/// <summary>
	/// A class that holds the details of a material charge, transfer or discharge.
	/// </summary>
	public class MaterialTransferRecord : Utility.IHasSerialNumber {
		/// <summary>
		/// A MaterialTransferRecord is a class that holds data representing a transfer of material
		/// from one place to another in the SOM.
		/// </summary>
		public MaterialTransferRecord(){
			MaterialTypeGuid = Guid.Empty;
			MaterialMass = Double.NaN;
			SourceGuid = Guid.Empty;
			DestinationGuid = Guid.Empty;
			SourcePortGuid = Guid.Empty;
			DestinationPortGuid = Guid.Empty;
			BatchGuid = Guid.Empty;
			StartTime = Double.NaN;
			Duration = Double.NaN;
			MaterialTransferType = TransferType.Uninitialized;
			ConcurrencyGuid = Guid.Empty;
			ParentMaterialTypeGuid = Guid.Empty;
            ParentMaterialTypeSpecGuid = Guid.Empty;
			MaterialTemperature = double.NaN;
			MaterialSpecificationGuid = Guid.Empty;
			m_serialNumber = Utility.SerialNumberService.GetNext();
		}

		/// <summary>
		/// The Guid associated with the material that was transferred.
		/// </summary>
		public Guid BatchGuid;

		/// <summary>
		/// The Guid associated with the material that was transferred.
		/// </summary>
		public Guid MaterialTypeGuid;

		/// <summary>
		/// The mass of the material that was transferred.
		/// </summary>
		public double MaterialMass;
		
		/// <summary>
		/// The temperature of the material that was transferred.
		/// </summary>
		public double MaterialTemperature;
		
		/// <summary>
		/// The GUID of the source (supplying) entity. This will be a SOMOperation if an internal transfer 
		/// or a discharge, and a SOMService if a charge.
		/// </summary>
		public Guid SourceGuid;
		
		/// <summary>
		/// The GUID of the destination (receiving) entity. This will be a SOMOperation if an internal transfer
		///  or a charge, and a SOMService if a discharge.
		/// </summary>
		public Guid DestinationGuid;
		
		/// <summary>
		/// The key of the Source's Port through which the material is being charged or discharged.
		/// </summary>
		public Guid SourcePortGuid;

		/// <summary>
		/// The key of the Destination's Port through which the material is being charged or discharged.
		/// </summary>
		public Guid DestinationPortGuid;
		
		/// <summary>
		/// The start time of the transfer, measured in minutes after the commencement of execution of this batch.
		/// </summary>
		public double StartTime; // Minutes from batch start.
		
		/// <summary>
		/// The duration, in minutes, of the transfer.
		/// </summary>
		public double Duration; // Minutes of transfer time.
		
		/// <summary>
		/// Enumeration to declare whether this transferRecord refers to a charge, a discharge, 
		/// or an internal (i.e. between units) transfer.
		/// </summary>
		public TransferType MaterialTransferType;

		/// <summary>
		/// Identifies substance transfers that occurred at the same time through the same connection.
		/// </summary>
		public Guid ConcurrencyGuid;

		/// <summary>
		/// The guid of a materialType that is to be considered the parent type of this transfer. Applies only to charges.
		/// For example, if this transfer has a mixture with 100 kg of water, and 4 kg of salt, and the parent material
		/// type guid refers to the substance 'Saline', then the actual charge came from the 'saline' inventory, not the
		/// 'water' and 'salt' inventories.
		/// </summary>
		public Guid ParentMaterialTypeGuid;

        /// <summary>
        /// Refer to the commentary for 'ParentMaterialTypeGuid'. In that case, the ParentMaterialTypeSpecGuid might be
        /// used to distinguish between saline that was purchased from Vendor A, and saline that was purchased from Vendor B.
        /// </summary>
        public Guid ParentMaterialTypeSpecGuid;

		/// <summary>
		/// The MaterialSpecificationGuid of the substance in this transfer. If Guid.Empty, the material specification is &lt;nothing&gt;.
		/// </summary>
		public Guid MaterialSpecificationGuid;

		/// <summary>
		/// True if the material in this transfer is a part of the product stream
		/// </summary>
		public bool IsProduct;

		private long m_serialNumber = 0;
		/// <summary>
		/// The serial number of this MTR.
		/// </summary>
		public long SerialNumber { get { return m_serialNumber; } set { m_serialNumber = value; } }
		
		private string m_note="";
		public string Note { get { return m_note; } set { m_note = value; } }
		/// <summary>
		/// Represents this transfer as a string, with additional details provided by the model.
		/// </summary>
		/// 
		/// <returns>A string that describes this transfer.</returns>
		public string Detail(Model model){
			string type;
			switch ( MaterialTransferType ) {
				case TransferType.Charge: type = "charged"; break;
				case TransferType.Discharge: type = "discharged"; break;
				case TransferType.Internal: type = "transferred"; break;
				case TransferType.Emission: type = "emitted"; break;
				default: throw new ApplicationException("Unknown TransferType encountered.");
			}

			string parent = GetModelObjectName(model,ParentMaterialTypeGuid,"no parent material.");

			string material = GetModelObjectName(model,MaterialTypeGuid,"material [{0}]");

			string source = GetModelObjectName(model,SourceGuid,"an unknown source [{0}]");

			string destination = GetModelObjectName(model,DestinationGuid,"an unknown destination [{0}]");

			string note = (m_note.Length == 0?".":". Note : " + m_note);

			return string.Format(s_mtr_Report_String,StartTime,MaterialMass,MaterialTypeGuid,MaterialTemperature,type,SourceGuid,DestinationGuid,Duration,parent,note);
		}

		private static readonly string s_mtr_Report_String = "At {0:F2} minutes after the start of the batch, {1:F4} kg of Material[{2}], at temperature {3:F2} degrees C, was {4} from source[{5}] to destination [{6}]. The transfer took {7:F2} minutes, and the material had {8}{9}";

		/// <summary>
		/// Represents this transfer as a string.
		/// </summary>
		/// <returns>A string that describes this transfer.</returns>
		public string Detail(){
			string type;
			switch ( MaterialTransferType ) {
				case TransferType.Charge: type = "charged"; break;
				case TransferType.Discharge: type = "discharged"; break;
				case TransferType.Internal: type = "transferred"; break;
				case TransferType.Emission: type = "emitted"; break;
				default: throw new ApplicationException("Unknown TransferType encountered.");
			}

			string parent = ParentMaterialTypeGuid.Equals(Guid.Empty)?"no parent material.":"a parent material [" + ParentMaterialTypeGuid + "].";
			string note = (m_note.Length == 0?".":". Note : " + m_note);

			return string.Format(s_mtr_Report_String,StartTime,MaterialMass,MaterialTypeGuid,MaterialTemperature,type,SourceGuid,DestinationGuid,Duration,parent,note);
		}

		private string GetModelObjectName(Model model, Guid tgtGuid, string fmtString){
			if ( tgtGuid!=Guid.Empty ) {
				IModelObject mo = ((IModelObject)model.ModelObjects[tgtGuid]);
				if ( mo != null ) {
					return ((IModelObject)model.ModelObjects[tgtGuid]).Name;
				}
			}
			return string.Format(fmtString,tgtGuid);
		}

		/// <summary>
		/// An enumeration that describes whether this transfer is entering, leaving, or within the model.
		/// </summary>
		public enum TransferType {
			/// <summary>
			/// The transfer record is not initialized.
			/// </summary>
			Uninitialized,
			/// <summary>
			/// The transfer is a charge into the model.
			/// </summary>
			Charge,
			/// <summary>
			/// The transfer is a discharge from the model.
			/// </summary>
			Discharge,
			/// <summary>
			/// The transfer is between operations or operation steps, but within the model.
			/// </summary>
			Internal,
			/// <summary>
			/// The transfer represents an emission.
			/// </summary>
			Emission
		} // If you add one, update both APIs called Detail(...) in this class! Note that you may have to update Catalyst enums as well.

		public class Sorter : IComparer {
			#region IComparer Members

			public int Compare(object x, object y) {
				MaterialTransferRecord mtrx = (MaterialTransferRecord)x;
				MaterialTransferRecord mtry = (MaterialTransferRecord)y;

				int retval = Comparer.Default.Compare(mtrx.ConcurrencyGuid,mtry.ConcurrencyGuid);
				if ( retval == 0 ) retval = Comparer.Default.Compare(mtrx.SerialNumber,mtry.SerialNumber);

				return retval;
			}

			#endregion
		}
	}
}
