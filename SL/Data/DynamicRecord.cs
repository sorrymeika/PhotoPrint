namespace SL.Data
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Dynamic;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    [Serializable]
    public sealed class DynamicRecord : DynamicObject, ICustomTypeDescriptor
    {
        private readonly Dictionary<string, object> fields;

        internal DynamicRecord(Dictionary<string, object> fields)
        {
            this.fields = fields;
            this.Columns = new List<string>(fields.Keys);
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return this.fields.Keys;
        }

        AttributeCollection ICustomTypeDescriptor.GetAttributes()
        {
            return null;
        }

        string ICustomTypeDescriptor.GetClassName()
        {
            return null;
        }

        string ICustomTypeDescriptor.GetComponentName()
        {
            return null;
        }

        TypeConverter ICustomTypeDescriptor.GetConverter()
        {
            return null;
        }

        EventDescriptor ICustomTypeDescriptor.GetDefaultEvent()
        {
            return null;
        }

        PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty()
        {
            return null;
        }

        object ICustomTypeDescriptor.GetEditor(Type editorBaseType)
        {
            return null;
        }

        EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
        {
            return null;
        }

        EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes)
        {
            return null;
        }

        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
        {
            return null;
        }

        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes)
        {
            return null;
        }

        object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd)
        {
            return null;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            this.fields.TryGetValue(binder.Name, out result);
            if (result == DBNull.Value)
            {
                result = null;
            }
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            this.fields[binder.Name] = value;
            return true;
        }

        public IList<string> Columns { get; private set; }

        public bool Remove(string name)
        {
            Columns.Remove(name);
            return this.fields.Remove(name);
        }

        public object this[int index]
        {
            get
            {
                return this[this.Columns[index]];
            }
            set
            {
                this.fields[this.Columns[index]] = value;
            }
        }

        public object this[string name]
        {
            get
            {
                object obj2;
                this.fields.TryGetValue(name, out obj2);
                if (obj2 == DBNull.Value)
                {
                    return null;
                }
                return obj2;
            }
            set
            {
                this.fields[name] = value;
            }
        }
    }
}