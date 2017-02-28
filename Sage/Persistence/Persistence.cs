/* This source code licensed under the GNU Affero General Public License */

using System;
using _Debug = System.Diagnostics.Debug;
using System.Xml;
using System.Collections;
using System.Reflection;
using System.Xml.Linq;
using System.Collections.Generic;
using Highpoint.Sage.SimCore;
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable UnusedMethodReturnValue.Global

namespace Highpoint.Sage.Persistence
{

    /// <summary>
    /// This interface is implemented by objects that will be serialized and deserialized via LINQ to XML.
    /// </summary>
    public interface IXElementSerializable {

        /// <summary>
        /// Loads and reconstitutes an object's internal state from the element 'self', according to the deserialization context.
        /// </summary>
        /// <param name="self">The self.</param>
        /// <param name="deserializationContext">The deserialization context.</param>
        void LoadFromXElement(XElement self, DeserializationContext deserializationContext);

        /// <summary>
        /// Represents an object's internal state as an XElement with the provided Name..
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>XElement.</returns>
        XElement AsXElement(string name);

    }


    /// <summary>
    /// Class DeserializationContext tracks objects that have been deserialized from an Xml document, and performs
    /// GUID translation so that there are no Guid uniqueness constraints violated. This is useful if objects are 
    /// being deserialized into a model multiple times (such as in a copy/paste operation.)
    /// </summary>
    public class DeserializationContext {

        #region Private Fields

        private readonly Dictionary<Guid, Guid> m_oldGuidToNewGuidMap;
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="DeserializationContext"/> class for managing deserialization from/into the specified model.
        /// </summary>
        /// <param name="model">The model.</param>
        public DeserializationContext(IModel model) {
            Model = model;
            m_oldGuidToNewGuidMap = new Dictionary<Guid, Guid>();
        }

        /// <summary>
        /// Gets the model in which the serialization and deserialization is being done.
        /// </summary>
        /// <value>The model.</value>
        public IModel Model { get; }

        /// <summary>
        /// Sets a new unique identifier to be used for a copy of the object that exists under the old unique identifier.
        /// </summary>
        /// <param name="oldGuid">The old unique identifier.</param>
        /// <param name="newGuid">The new unique identifier.</param>
        public void SetNewGuidForOldGuid(Guid oldGuid, Guid newGuid) {
            m_oldGuidToNewGuidMap.Add(oldGuid, newGuid);
        }

        /// <summary>
        /// Gets the unique identifier to be used for a copy of the object that exists under the old unique identifier.
        /// </summary>
        /// <param name="oldGuid">The old unique identifier.</param>
        /// <returns>Guid.</returns>
        public Guid GetNewGuidForOldGuid(Guid oldGuid) {
            return m_oldGuidToNewGuidMap[oldGuid];
        }

        /// <summary>
        /// Gets the model object that had the old unique identifier.
        /// </summary>
        /// <param name="oldGuid">The old unique identifier.</param>
        /// <returns>IModelObject.</returns>
        public IModelObject GetModelObjectThatHad(Guid oldGuid) {
            return Model.ModelObjects[oldGuid];
        }
    }

    /// <summary>
    /// An object that implements ISerializer knows how to store one or more types of objects
    /// into an archive, and subsequently, to take tham out of the archive. The object that
    /// implements this interface might be thought of as an archive.
    /// </summary>
    public interface ISerializer {

        /// <summary>
        /// Stores the object 'obj' under the key 'key'.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="obj">The object.</param>
        void StoreObject(object key, object obj);

        /// <summary>
        /// Loads the object stored under the key, 'key'.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>System.Object.</returns>
        object LoadObject(object key);

        /// <summary>
        /// Resets this instance.
        /// </summary>
        void Reset();

        /// <summary>
        /// Gets the context entities.
        /// </summary>
        /// <value>The context entities.</value>
        Hashtable ContextEntities { get; }
    }

    /// <summary>
    /// This interface is implemented by any object that can be serialized to a custom XML
    /// stream. (It does not mean that, necessarily, the XmlSerializationContext has been
    /// provisioned with serializers suffient to perform that serialization, but just that
    /// the object implementing it, knows how to break down and stream, and subsequently to
    /// reclaim from the stream, its constituent parts.
    /// </summary>
    public interface IXmlPersistable {

        /// <summary>
        /// Stores this object to the specified XmlSerializationContext.
        /// </summary>
        /// <param name="xmlsc">The specified XmlSerializationContext.</param>
        void SerializeTo(XmlSerializationContext xmlsc);  
           
        /// <summary>
        /// Reconstitutes this object from the specified XmlSerializationContext.
        /// </summary>
        /// <param name="xmlsc">The specified XmlSerializationContext.</param>
        void DeserializeFrom(XmlSerializationContext xmlsc); // After calling default ctor.
    }

    /// <summary>
    /// An ISerializer that knows how to store a wide range of objects into, and retrieve them
    /// from, an XML document.
    /// </summary>
    public class XmlSerializationContext : ISerializer {
        
        #region Private Fields
        private readonly ISerializer m_delegateXmlSerializer;
        private readonly ISerializer m_typeXmlSerializer;
        private readonly Hashtable m_serializers;
        private XmlDocument m_rootDoc;
        private XmlNode m_archive;
        private XmlNode m_typeCatalog;
        private Hashtable m_objectsByKey;
        private Hashtable m_keysByObject;
        private Hashtable m_typesByIndex;
        private Hashtable m_indexesByType;
        private ISerializer m_enumXmlSerializer;
        private Stack m_nodeCursor;
        private int m_typeNum;
        private int m_objectNum;
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlSerializationContext"/> class.
        /// </summary>
        public XmlSerializationContext(){
            Reset();
            m_serializers = new Hashtable
            {
                {typeof (string), new StringXmlSerializer(this)},
                {typeof (double), new DoubleXmlSerializer(this)},
                {typeof (sbyte), new SByteXmlSerializer(this)},
                {typeof (byte), new ByteXmlSerializer(this)},
                {typeof (short), new ShortXmlSerializer(this)},
                {typeof (ushort), new UShortXmlSerializer(this)},
                {typeof (int), new IntXmlSerializer(this)},
                {typeof (uint), new UintXmlSerializer(this)},
                {typeof (long), new LongXmlSerializer(this)},
                {typeof (bool), new BoolXmlSerializer(this)},
                {typeof (DateTime), new DateTimeXmlSerializer(this)},
                {typeof (TimeSpan), new TimeSpanXmlSerializer(this)},
                {typeof (Guid), new GuidXmlSerializer(this)},
                {typeof (Hashtable), new HashtableXmlSerializer(this)},
                {typeof (ArrayList), new ArrayListXmlSerializer(this)},
                {typeof (Type), new TypeXmlSerializer(this)},
                {typeof (DictionaryEntry), new DictionaryEntryXmlSerializer(this)}
            };

            //m_serializers.Add(typeof(Array),new ArrayXmlSerializer(this));

            m_enumXmlSerializer = new EnumXmlSerializer(this);
            m_delegateXmlSerializer = new DelegateXmlSerializer(this);
            m_typeXmlSerializer = new TypeXmlSerializer(this);
        }

        /// <summary>
        /// Determines whether the XmlSerializationContext contains a serializer for the specified target type.
        /// </summary>
        /// <param name="targetType">Type of the target.</param>
        /// <returns><c>true</c> if the XmlSerializationContext contains a serializer for the specified target type; otherwise, <c>false</c>.</returns>
        public bool ContainsSerializer(Type targetType ) {
            return m_serializers.ContainsKey(targetType);
        }

        /// <summary>
        /// Adds a serializer to the XmlSerializationContext for the specified target type.
        /// </summary>
        /// <param name="targetType">Type of the target.</param>
        /// <param name="serializer">The serializer.</param>
        public void AddSerializer( Type targetType, ISerializer serializer ) {
            m_serializers.Add(targetType,serializer);
        }

        /// <summary>
        /// Removes the serializer from the XmlSerializationContext for the specified target type.
        /// </summary>
        /// <param name="targetType">Type of the target.</param>
        public void RemoveSerializer( Type targetType ) {
            m_serializers.Remove(targetType );
        }

        /// <summary>
        /// Pushes the XmlNode into the serializer.
        /// </summary>
        /// <param name="node">The node.</param>
        public void PushNode(XmlNode node){
            m_nodeCursor.Push(node);
        }

        /// <summary>
        /// Pops an XmlNode off the serializer.
        /// </summary>
        /// <returns>XmlNode.</returns>
        public XmlNode PopNode(){
            return (XmlNode)m_nodeCursor.Pop();
        }

        /// <summary>
        /// Gets the current node.
        /// </summary>
        /// <value>The current node.</value>
        public XmlNode CurrentNode => (XmlNode)m_nodeCursor.Peek();

        #region ISerializer Members
        /// <summary>
        /// Persists the object 'obj' to an XmlNode, and appends that node under
        /// the XmlSerializationContext's CurrentNode node.
        /// </summary>
        /// <param name="key">In it's ToString() form, this will be the name of the new node.</param>
        /// <param name="obj">This is the object that will be serialized to the new node.</param>
        /// <returns>The XmlNode that was created.</returns>
        public void StoreObject(object key, object obj) {
            XmlNode currentNode = CurrentNode;
            if ( obj == null ) { // ----------------------- It's null.
                XmlNode node = CreateNullNode(ref m_rootDoc, key.ToString());
                currentNode.AppendChild(node);
            } else if ( m_keysByObject.Contains(obj) ) { // ---- We've seen it before.
                XmlNode node = CreateRefNode(key, m_keysByObject[obj]);
                currentNode.AppendChild(node);
            } else if ( m_serializers.Contains(obj.GetType()) ){
                ((ISerializer)m_serializers[obj.GetType()]).StoreObject(key,obj);
            } else if ( obj is Enum ) {
                m_enumXmlSerializer.StoreObject(key,obj);
            } else if ( obj is Delegate ) {
                m_delegateXmlSerializer.StoreObject(key,obj);
            } else if ( obj is Type ) {
                m_typeXmlSerializer.StoreObject(key,obj);
            } else { // ----------------------------------- It's a new object.
                object oid = GetOidForObject(obj);
                m_keysByObject.Add(obj,oid);
                XmlNode node = CreateEmptyNode(key.ToString(),obj.GetType(),oid);
                currentNode.AppendChild(node);
                PushNode(node);
                try {
                    ((IXmlPersistable)obj).SerializeTo(this);
                } catch ( InvalidCastException ice ) {
                    _Debug.WriteLine(ice.Message);
                    throw new ApplicationException("Attempt to serialize an object of type " + obj.GetType() + " failed. It does not implement IXmlPersistable.");
                }
                PopNode();
                //return node;
            }
        }

        /// <summary>
        /// Loads (reconstitutes) an object from an archive object, and returns
        /// the object. If the object has already been reconstituted (i.e. the
        /// reference being deserialized is the second or later reference to an
        /// object, the original object is located and a reference to it is returned.
        /// </summary>
        /// <param name="key">The key whose node under 'archive' is to be deserialized.</param>
        /// <returns>The deserialized object.</returns>
        public object LoadObject(object key) {
            XmlNode node = CurrentNode.SelectSingleNode(key.ToString());
            object retval;
            
            // If the node contains null, return null.
            if ( NodeIsNull(node) ) return null;
            
            if ( NodeIsRef(node) ) {
                object oid = GetOidFromNode(node);
                if ( m_objectsByKey.Contains(oid) ) {
                    retval = m_objectsByKey[oid];
                } else {
                    throw new ApplicationException("Couldn't find a referenced object");
                }
            } else {
            
                // If the node contains a referenced object, return that object.
                // There is stuff to be deserialized.
                Type type = GetTypeFromNode(node);
                if ( m_serializers.Contains(type) ) {
                    // If it's a type that has a custom deserializer, then we call it.
                    ISerializer serializer = (ISerializer)m_serializers[type];
                    retval = serializer.LoadObject(key);
                } else if ( type.IsEnum ) {
                    retval = m_enumXmlSerializer.LoadObject(key);
                } else if ( typeof(Delegate).IsAssignableFrom(type) ) {
                    retval = m_delegateXmlSerializer.LoadObject(key);
                } else {
                    // We will need to create the object using the default mechanism.
                    retval = CreateEmptyObject(node);
                    m_objectsByKey.Add(GetOidFromNode(node),retval);
                    PushNode(node);
                    ((IXmlPersistable)retval).DeserializeFrom(this);
                    PopNode();
                }
            }
            return retval;
        }

        public Hashtable ContextEntities { get; private set; }

        /// <summary>
        /// Resets this context. Clears the document, object cache, node stack and hashtables.
        /// </summary>
        public void Reset(){
            m_rootDoc = new XmlDocument();
            m_archive         = m_rootDoc.AppendChild(m_rootDoc.CreateElement(s_archive));
            m_typeCatalog     = m_archive.AppendChild(m_rootDoc.CreateElement(s_type_Catalog));
            m_objectsByKey    = new Hashtable();
            m_keysByObject    = new Hashtable();
            m_typesByIndex    = new Hashtable();
            m_indexesByType   = new Hashtable();
            ContextEntities = new Hashtable();
            m_enumXmlSerializer  = new EnumXmlSerializer(this);
            m_nodeCursor      = new Stack();
            m_nodeCursor.Push(m_archive);
            m_typeNum         = 0;
            m_objectNum       = 0;
        }

        #endregion

        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
        /// <summary>
        /// Gets or sets a value indicating whether this context is using a type catalog. Using a type
        /// catalog makes for better compression of an XML file, at the cost of minimally slower performance.
        /// </summary>
        /// <value><c>true</c> if [use catalog]; otherwise, <c>false</c>.</value>
        public bool UseCatalog { get; set; } = true;

        /// <summary>
        /// Populates a new XmlSerializationContext from a specified filename.
        /// </summary>
        /// <param name="filename">The filename.</param>
        public void Load(string filename){
            m_rootDoc.Load(filename);
            m_archive = m_rootDoc.SelectSingleNode(s_archive);
            m_objectsByKey.Clear();
            m_keysByObject.Clear();
            m_nodeCursor.Clear();
            m_nodeCursor.Push(m_archive);
            m_typeNum         = 0;
            m_objectNum       = 0;
            LoadTypeCatalog();
        }

        /// <summary>
        /// Saves the XmlSerializationContext from a specified filename.
        /// </summary>
        /// <param name="filename">The filename.</param>
        public void Save(string filename){
            m_rootDoc.Save(filename);
        }

        private XmlNode CreateEmptyNode(string name, Type type, object oid){
            //_Debug.WriteLine("Creating a node of type " + type.ToString() + " to store " + name );

            XmlNode node;
            try {
                node = m_rootDoc.CreateNode(XmlNodeType.Element, name, null);
            } catch ( XmlException xmlex ) {
                _Debug.WriteLine(xmlex.Message);
                throw;
            }
            _Debug.Assert(node.Attributes != null, "node.Attributes != null");

            XmlAttribute attr;
            if ( oid != null ) { // Primitives won't have an oid.
                attr = m_rootDoc.CreateAttribute(s_ref_Id_Label);
                attr.InnerText = oid.ToString();
                node.Attributes.Append(attr);
            }

            if ( UseCatalog ) {
                if ( !m_indexesByType.Contains(type) ) AddTypeToCatalog(type);
                object typeIndex = m_indexesByType[type];

                attr = m_rootDoc.CreateAttribute(s_typekey_Label);
                attr.InnerText = typeIndex.ToString();
                node.Attributes.Append(attr);
                attr = m_rootDoc.CreateAttribute(s_assy_Label);
                attr.InnerText = type.Assembly.FullName;
                node.Attributes.Append(attr);
            }
            else {
                attr = m_rootDoc.CreateAttribute(s_type_Label);
                attr.InnerText = type.FullName;
                node.Attributes.Append(attr);
                attr = m_rootDoc.CreateAttribute(s_assy_Label);
                attr.InnerText = type.Assembly.FullName;
                node.Attributes.Append(attr);
            }
            return node;
        }

        private static XmlNode CreateNullNode(ref XmlDocument root, string name){
            XmlNode node = root.CreateNode(XmlNodeType.Element, name, null);
            XmlAttribute attr = root.CreateAttribute(s_null_Label);
            attr.InnerText = "true";
            _Debug.Assert(node.Attributes != null, "node.Attributes != null");
            node.Attributes.Append(attr);
            return node;
        }

        private static bool NodeIsNull(XmlNode node){
            _Debug.Assert(node.Attributes != null, "node.Attributes != null");
            XmlAttribute nullness = node.Attributes[s_null_Label];
                return nullness != null && nullness.InnerText.Equals("true");
        }

        private static bool NodeIsRef(XmlNode node){
            _Debug.Assert(node.Attributes != null, "node.Attributes != null");
            XmlAttribute refness = node.Attributes[s_isref_Label];
            return refness != null && refness.InnerText.Equals("true");
        }

        /// <summary>
        /// Retrieves an object's type from that object's node. Goes through the
        /// type catalog if we're using one, or reads the node's type information
        /// directly, if we're not.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private Type GetTypeFromNode(XmlNode node)
        {
            _Debug.Assert(node.Attributes != null, "node.Attributes != null");

            if (UseCatalog)
            {
                string typeId = node.Attributes[s_typekey_Label].InnerText;
                //				if ( !m_typesByIndex.Contains(typeID) ) {
                //					LoadTypeCatalog();
                //				}
                return (Type) m_typesByIndex[typeId];
            }
            else
            {
                Assembly assy = GetAssemblyFromNode(node);
                XmlAttribute typeAttr = node.Attributes[s_type_Label];
                if (typeAttr == null || assy == null) return null;
                return assy.GetType(typeAttr.InnerText);
            }
        }

        private static Assembly GetAssemblyFromNode(XmlNode node){
            _Debug.Assert(node.Attributes != null, "node.Attributes != null");
            XmlAttribute assyAttr = node.Attributes[s_assy_Label];
            if ( assyAttr == null ) return null;
            string assyName = assyAttr.InnerText;
            return Assembly.Load(assyName);
        }

        private void AddTypeToCatalog(Type type){

            object oid = GetOidForType(type);
            m_indexesByType.Add(type,oid);
            m_typesByIndex.Add(oid,type);
            XmlNode typeNode = m_rootDoc.CreateElement(oid.ToString());
            m_typeCatalog.AppendChild(typeNode);
            XmlAttribute attr = m_rootDoc.CreateAttribute(s_type_Label);
            attr.InnerText = type.FullName;
            _Debug.Assert(typeNode.Attributes != null, "typeNode.Attributes != null");
            typeNode.Attributes.Append(attr);
            attr = m_rootDoc.CreateAttribute(s_assy_Label);
            attr.InnerText = type.Assembly.FullName;
            typeNode.Attributes.Append(attr);
        }

        private void LoadTypeCatalog(){
            m_typeCatalog = m_rootDoc.SelectSingleNode(s_archive+"/"+s_type_Catalog);
            m_typesByIndex.Clear();
            m_indexesByType.Clear();
            Type type = null;
            _Debug.Assert(m_typeCatalog?.ChildNodes != null, "m_typeCatalog?.ChildNodes != null");
            foreach ( XmlNode typeNode in m_typeCatalog?.ChildNodes ) {
                string typeKey = typeNode.Name;
                
                Assembly assy = GetAssemblyFromNode(typeNode);
                _Debug.Assert(typeNode.Attributes != null, "typeNode.Attributes != null");
                XmlAttribute typeAttr = typeNode.Attributes[s_type_Label];
                if ( typeAttr != null && assy != null ) type = assy.GetType(typeAttr.InnerText);


                m_typesByIndex.Add(typeKey,type);
                _Debug.Assert(type != null, "type != null");
                m_indexesByType.Add(type,typeKey);
            }
        }
        
        private object CreateEmptyObject(XmlNode node){
            Type type = GetTypeFromNode(node);

            ConstructorInfo ci = type.GetConstructor(new Type[]{});
            if ( ci == null ) {
                throw new ApplicationException("ConstructorInfo was null. (Does type " + type + " have a default constructor?)");
            }
            return ci.Invoke(BindingFlags.CreateInstance,null,null,null);
        }
        
        private XmlNode CreateRefNode(object key, object refid){
            XmlNode node = m_rootDoc.CreateNode(XmlNodeType.Element,key.ToString(),"");
            //node.InnerText = refid.ToString();
            XmlAttribute attr = m_rootDoc.CreateAttribute(s_isref_Label);
            attr.InnerText = "true";
            _Debug.Assert(node.Attributes != null, "node.Attributes != null");
            node.Attributes.Append(attr);
            attr = m_rootDoc.CreateAttribute(s_ref_Id_Label);
            attr.InnerText = refid.ToString();
            node.Attributes.Append(attr);

            return node;
        }
        

        #region >>> Labels for different types and instances of XML Nodes <<<
        private static readonly string s_null_Label="isNull";
        private static readonly string s_isref_Label="isRef";
        private static readonly string s_ref_Id_Label="objKey";
        private static readonly string s_typekey_Label="typeKey";
        private static readonly string s_type_Label="type";
        private static readonly string s_assy_Label="assembly";
        private static readonly string s_type_Catalog="TypeCatalog";
        private static readonly string s_archive="Archive";
        #endregion
        
        // ReSharper disable once UnusedParameter.Local
        private object GetOidForObject(object obj){return "_"+(m_objectNum++);}
        // ReSharper disable once UnusedParameter.Local
        private object GetOidForType(object obj){return "_"+(m_typeNum++);}
        private object GetOidFromNode(XmlNode node){
            _Debug.Assert(node.Attributes != null, "node.Attributes != null");
            XmlAttribute attr = node.Attributes[s_ref_Id_Label];
            if ( attr == null ) return null;
            return node.Attributes[s_ref_Id_Label].InnerText;
        }

        #region >>> Canned Serializers for Basic Types <<< 
        abstract class PrimitiveXmlSerializer : ISerializer {
            private readonly XmlSerializationContext m_xmlsc;
            private readonly Type m_type;
            protected PrimitiveXmlSerializer(XmlSerializationContext xmlsc, Type type){
                m_xmlsc = xmlsc;
                m_type = type;
            }

            #region ISerializer Members
            public void StoreObject(object key, object obj) {
                XmlNode node = m_xmlsc.CreateEmptyNode(key.ToString(),m_type,null);
                node.InnerText = StringFromObject(obj);
                m_xmlsc.CurrentNode.AppendChild(node);
            }

            public object LoadObject(object key) {
                XmlNode node = m_xmlsc.CurrentNode.SelectSingleNode(key.ToString());
                _Debug.Assert(node != null, "node != null");
                return ObjectFromString(node.InnerText);
            }
            public void Reset() {}
            public Hashtable ContextEntities => m_xmlsc.ContextEntities;

            #endregion

            protected abstract object ObjectFromString(string str);
            // ReSharper disable once VirtualMemberNeverOverriden.Global
            protected virtual string StringFromObject(object obj){ return obj.ToString(); }

        }

        private class StringXmlSerializer : PrimitiveXmlSerializer{
            public StringXmlSerializer(XmlSerializationContext xmlsc):base(xmlsc,typeof(string)){}
            protected override object ObjectFromString(string str){ return str; }
        }

        private class DoubleXmlSerializer : PrimitiveXmlSerializer{
            public DoubleXmlSerializer(XmlSerializationContext xmlsc):base(xmlsc,typeof(double)){}
            protected override object ObjectFromString(string str){ return double.Parse(str); }
        }
        private class LongXmlSerializer : PrimitiveXmlSerializer{
            public LongXmlSerializer(XmlSerializationContext xmlsc):base(xmlsc,typeof(long)){}
            protected override object ObjectFromString(string str){ return long.Parse(str); }
        }
        private class ShortXmlSerializer : PrimitiveXmlSerializer{
            public ShortXmlSerializer(XmlSerializationContext xmlsc):base(xmlsc,typeof(short)){}
            protected override object ObjectFromString(string str){ return short.Parse(str); }
        }

        private class UShortXmlSerializer : PrimitiveXmlSerializer{
            public UShortXmlSerializer(XmlSerializationContext xmlsc):base(xmlsc,typeof(ushort)){}
            protected override object ObjectFromString(string str){ return ushort.Parse(str); }
        }

        private class IntXmlSerializer : PrimitiveXmlSerializer{
            public IntXmlSerializer(XmlSerializationContext xmlsc):base(xmlsc,typeof(int)){}
            protected override object ObjectFromString(string str){ return int.Parse(str); }
        }

        private class UintXmlSerializer : PrimitiveXmlSerializer{
            public UintXmlSerializer(XmlSerializationContext xmlsc):base(xmlsc,typeof(uint)){}
            protected override object ObjectFromString(string str){ return uint.Parse(str); }
        }

        private class SByteXmlSerializer : PrimitiveXmlSerializer{
            public SByteXmlSerializer(XmlSerializationContext xmlsc):base(xmlsc,typeof(sbyte)){}
            protected override object ObjectFromString(string str){ return sbyte.Parse(str); }
        }

        private class ByteXmlSerializer : PrimitiveXmlSerializer{
            public ByteXmlSerializer(XmlSerializationContext xmlsc):base(xmlsc,typeof(byte)){}
            protected override object ObjectFromString(string str){ return byte.Parse(str); }
        }

        private class BoolXmlSerializer : PrimitiveXmlSerializer{
            public BoolXmlSerializer(XmlSerializationContext xmlsc):base(xmlsc,typeof(bool)){}
            protected override object ObjectFromString(string str){ return bool.Parse(str); }
        }

        private class GuidXmlSerializer : PrimitiveXmlSerializer{
            public GuidXmlSerializer(XmlSerializationContext xmlsc):base(xmlsc,typeof(Guid)){}
            //protected override string StringFromObject(object obj){ return ((TimeSpan)obj).ToString(; }
            protected override object ObjectFromString(string str){ return new Guid(str); }
        }

        private class TimeSpanXmlSerializer : PrimitiveXmlSerializer{
            public TimeSpanXmlSerializer(XmlSerializationContext xmlsc):base(xmlsc,typeof(TimeSpan)){}
            //protected override string StringFromObject(object obj){ return ((TimeSpan)obj).ToString(; }
            protected override object ObjectFromString(string str){ return TimeSpan.Parse(str); }
        }

        private class DateTimeXmlSerializer : PrimitiveXmlSerializer{
            public DateTimeXmlSerializer(XmlSerializationContext xmlsc):base(xmlsc,typeof(DateTime)){}
            //protected override string StringFromObject(object obj){ return obj.ToString(); }
            protected override object ObjectFromString(string str){ return DateTime.Parse(str); }
        }

        private class TypeXmlSerializer : PrimitiveXmlSerializer{
            public TypeXmlSerializer(XmlSerializationContext xmlsc):base(xmlsc,typeof(Type)){}
            //protected override string StringFromObject(object obj){ return obj.ToString(); }
            protected override object ObjectFromString(string str){ return Type.GetType(str); }
        }
         
        private class HashtableXmlSerializer : ISerializer {
            private readonly XmlSerializationContext m_xmlsc;
            private readonly Type m_type = typeof(Hashtable);
            public HashtableXmlSerializer(XmlSerializationContext xmlsc){
                m_xmlsc = xmlsc;
            }

            #region ISerializer Members

            public void StoreObject(object key, object obj) {
                object oid = m_xmlsc.GetOidForObject(obj);
                XmlNode node = m_xmlsc.CreateEmptyNode(key.ToString(),m_type,oid);
                m_xmlsc.CurrentNode.AppendChild(node);
                m_xmlsc.PushNode(node);
                Hashtable ht = (Hashtable)obj;
                m_xmlsc.StoreObject("NumEntries",ht.Count);
                int i = 0;
                foreach ( DictionaryEntry de in ht ) {
                    m_xmlsc.StoreObject("Key_"+i,de.Key);
                    m_xmlsc.StoreObject("Val_"+i,de.Value);
                    i++;
                }
                m_xmlsc.PopNode();
            }

            public object LoadObject(object key) {
                XmlNode node = m_xmlsc.CurrentNode.SelectSingleNode(key.ToString());
                m_xmlsc.PushNode(node);
                Hashtable ht = new Hashtable();
                object tmpCount = m_xmlsc.LoadObject("NumEntries");
                int count = (int)tmpCount;
                for ( int i = 0 ; i < count ; i++ ) {
                    object dekey = m_xmlsc.LoadObject("Key_"+i);
                    object deval = m_xmlsc.LoadObject("Val_"+i);
                    ht.Add(dekey,deval);
                }
                m_xmlsc.PopNode();
                return ht;
            }

            public void Reset() {
                // TODO:  Add HashtableXmlSerializer.Reset implementation
            }

            public Hashtable ContextEntities => m_xmlsc.ContextEntities;

            #endregion

        }

        private class ArrayListXmlSerializer : ISerializer {
            private readonly XmlSerializationContext m_xmlsc;
            private readonly Type m_type = typeof(ArrayList);
            public ArrayListXmlSerializer(XmlSerializationContext xmlsc){
                m_xmlsc = xmlsc;
            }

            #region ISerializer Members

            public void StoreObject(object key, object obj) {
                object oid = m_xmlsc.GetOidForObject(obj);
                XmlNode node = m_xmlsc.CreateEmptyNode(key.ToString(),m_type,oid);
                m_xmlsc.CurrentNode.AppendChild(node);
                m_xmlsc.PushNode(node);
                ArrayList al = (ArrayList)obj;
                m_xmlsc.StoreObject("NumEntries",al.Count);
                int i = 0;
                foreach ( object entry in al ) {
                    m_xmlsc.StoreObject("Val_"+i,entry);
                    i++;
                }
                m_xmlsc.PopNode();
            }

            public object LoadObject(object key) {
                XmlNode node = m_xmlsc.CurrentNode.SelectSingleNode(key.ToString());
                m_xmlsc.PushNode(node);
                ArrayList al = new ArrayList();
                object tmpCount = m_xmlsc.LoadObject("NumEntries");
                int count = (int)tmpCount;
                for ( int i = 0 ; i < count ; i++ ) {
                    object alval = m_xmlsc.LoadObject("Val_"+i);
                    al.Add(alval);
                }
                m_xmlsc.PopNode();
                return al;
            }

            public void Reset() {
            }

            public Hashtable ContextEntities => m_xmlsc.ContextEntities;

            #endregion

        }

        // Not currently in use - still a problem with an array of primitives being deserialized.
        // It seems that the empty array of primitives is being created, but then the first object,
        // read from the archive is peing pushed into the array - but since it's an object, and
        // the array takes primitives, we throw an exception. Run zTestPersistence's 
        // new DeepPersistenceTester().TestArrayPersistence(); to see the issue in action.
        // ReSharper disable once UnusedMember.Local
        private class ArrayXmlSerializer : ISerializer { 
            private readonly XmlSerializationContext m_xmlsc;
            private readonly Type m_type = typeof(Array);
            public ArrayXmlSerializer(XmlSerializationContext xmlsc){
                m_xmlsc = xmlsc;
            }

            #region ISerializer Members

            public void StoreObject(object key, object obj) { // TODO: Array of primitives - more compact format.
                object oid = m_xmlsc.GetOidForObject(obj);
                XmlNode node = m_xmlsc.CreateEmptyNode(key.ToString(),m_type,oid);
                m_xmlsc.CurrentNode.AppendChild(node);
                m_xmlsc.PushNode(node);

                Array array = (Array)obj;
                int rank = array.Rank;
                int[] lengths = new int[rank];
                for ( int i = 0 ; i < rank ; i++ ) lengths[i] = array.GetLength(i);
                m_xmlsc.StoreObject("ElementType",array.GetType());
                if ( rank > 1 ) {
                    m_xmlsc.StoreObject("Lengths",lengths);
                } else {
                    m_xmlsc.StoreObject("Length",lengths[0]);
                }
                
                int[] indices = new int[rank];
                Array.Clear(indices,0,rank);

                int ndx = 0;
                while ( ndx < rank ) {
                    ndx = 0;
                    string ndxStr = GetNdxString(indices);
                    m_xmlsc.StoreObject(ndxStr,array.GetValue(indices));
                    while ( ( ndx < indices.Length ) && (++indices[ndx]) == lengths[ndx] ) {
                        indices[ndx] = 0;
                        ndx++;
                    }
                }

                m_xmlsc.PopNode();
            }

            private string GetNdxString(int[] indices){
                System.Text.StringBuilder sb = new System.Text.StringBuilder("C_");
                for ( int i = 0 ; i < indices.Length ; i++ ) {
                    sb.Append(indices[i]); if ( i < ( indices.Length-1 ) ) sb.Append("_");
                }
                return sb.ToString();
            }

            
            public object LoadObject(object key) {
                XmlNode node = m_xmlsc.CurrentNode.SelectSingleNode(key.ToString());
                m_xmlsc.PushNode(node);
                Type elementType = (Type)m_xmlsc.LoadObject("ElementType");
                int[] lengths;
                _Debug.Assert(node != null, "node != null");
                if ( node.SelectSingleNode("Lengths") != null ) {
                    lengths = (int[])m_xmlsc.LoadObject("Lengths");
                } else {
                    lengths = new int[1];
                    lengths[0] = (int)m_xmlsc.LoadObject("Length");
                }
                int rank = lengths.Length;

                Array array = Array.CreateInstance(elementType,lengths);

                int[] indices = new int[rank];
                Array.Clear(array,0,rank);

                int ndx = 0;
                while ( ndx < rank ) {
                    ndx = 0;
                    string ndxStr = GetNdxString(indices);
                    // The problem comes in in this next line. LoadObject gives an object, array might take a primitive value type.
                    array.SetValue(m_xmlsc.LoadObject(ndxStr),indices);
                    while ( ( ndx < indices.Length ) && (++indices[ndx]) == lengths[ndx] ) {
                        indices[ndx] = 0;
                        ndx++;
                    }
                }

                m_xmlsc.PopNode();
                return array;
            }

            public void Reset() {
            }

            public Hashtable ContextEntities => m_xmlsc.ContextEntities;

            #endregion

        }

        private class DelegateXmlSerializer : ISerializer {
            private readonly XmlSerializationContext m_xmlsc;
            public DelegateXmlSerializer(XmlSerializationContext xmlsc){
                m_xmlsc = xmlsc;
            }

            #region ISerializer Members

            public void StoreObject(object key, object obj) {
                object oid = m_xmlsc.GetOidForObject(obj);
                XmlNode node = m_xmlsc.CreateEmptyNode(key.ToString(),obj.GetType(),oid);
                m_xmlsc.CurrentNode.AppendChild(node);
                m_xmlsc.PushNode(node);
                Delegate del = (Delegate)obj;
                m_xmlsc.StoreObject("Target",del.Target);
                m_xmlsc.StoreObject("Method",del.Method.Name);
                m_xmlsc.PopNode();
            }

            public object LoadObject(object key) {
                XmlNode node = m_xmlsc.CurrentNode.SelectSingleNode(key.ToString());
                m_xmlsc.PushNode(node);
                object target = m_xmlsc.LoadObject("Target");
                string method = (string)m_xmlsc.LoadObject("Method");
                Delegate del = Delegate.CreateDelegate(m_xmlsc.GetTypeFromNode(node),target,method);
                m_xmlsc.PopNode();
                return del;
            }

            public void Reset() {
            }

            public Hashtable ContextEntities => m_xmlsc.ContextEntities;

            #endregion

        }
        private class DictionaryEntryXmlSerializer : ISerializer {
            private readonly XmlSerializationContext m_xmlsc;
            private readonly Type m_type = typeof(DictionaryEntry);
            public DictionaryEntryXmlSerializer(XmlSerializationContext xmlsc){
                m_xmlsc = xmlsc;
            }

            #region ISerializer Members

            public void StoreObject(object key, object obj) {
                object oid = m_xmlsc.GetOidForObject(obj);
                XmlNode node = m_xmlsc.CreateEmptyNode(key.ToString(),m_type,oid);
                m_xmlsc.CurrentNode.AppendChild(node);
                m_xmlsc.PushNode(node);
                DictionaryEntry de = (DictionaryEntry)obj;
                m_xmlsc.StoreObject("key",de.Key);
                m_xmlsc.StoreObject("Value",de.Value);
                m_xmlsc.PopNode();
            }

            public object LoadObject(object key) {
                XmlNode node = m_xmlsc.CurrentNode.SelectSingleNode(key.ToString());
                m_xmlsc.PushNode(node);
                object entryKey = m_xmlsc.LoadObject("key");
                object entryVal = m_xmlsc.LoadObject("Value");
                m_xmlsc.PopNode();
                return new DictionaryEntry(entryKey,entryVal);
            }

            public void Reset() {}

            public Hashtable ContextEntities => m_xmlsc.ContextEntities;

            #endregion

        }
        private class EnumXmlSerializer : ISerializer {
            private readonly XmlSerializationContext m_xmlsc;
            public EnumXmlSerializer(XmlSerializationContext xmlsc){
                m_xmlsc = xmlsc;
            }

            #region ISerializer Members

            public void StoreObject(object key, object obj) {
                object oid = m_xmlsc.GetOidForObject(obj);
                XmlNode node = m_xmlsc.CreateEmptyNode(key.ToString(),obj.GetType(),oid);
                m_xmlsc.CurrentNode.AppendChild(node);
                m_xmlsc.PushNode(node);
                m_xmlsc.StoreObject(key,obj.ToString());
                m_xmlsc.PopNode();
            }

            public object LoadObject(object key) {
                XmlNode node = m_xmlsc.CurrentNode.SelectSingleNode(key.ToString());
                m_xmlsc.PushNode(node);
                Type type = m_xmlsc.GetTypeFromNode(node);
                string enumText = (string)m_xmlsc.LoadObject(key);
                m_xmlsc.PopNode();
                return Enum.Parse(type,enumText,false);
            }

            public void Reset() {}

            public Hashtable ContextEntities => m_xmlsc.ContextEntities;

            #endregion

        }

        #endregion
    }
}