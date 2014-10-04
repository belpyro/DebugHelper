using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DebugHelper.Reflection
{
    class ReflectInfo
    {
        private Dictionary<string, FieldInfo> _fields = new Dictionary<string, FieldInfo>();
        private Dictionary<string, PropertyInfo> _properties = new Dictionary<string, PropertyInfo>();
        private Dictionary<string, MethodInfo> _methods = new Dictionary<string, MethodInfo>();

        private readonly object _target;

        internal ReflectInfo(object source)
        {
            _target = source;
            Initialize();
        }

        internal ReflectInfo(Type type)
        {
            Initialize(type);
        }

        private void Initialize(Type t = null)
        {
            var type = t ?? _target.GetType();

            _fields = type.GetFields().ToDictionary(x => x.Name, x => x);
            _properties = type.GetProperties().ToDictionary(x => x.Name, x => x);
            _methods = type.GetMethods().ToDictionary(x => x.Name, x => x);
        }

        public object Get(string name)
        {
            if (_fields.ContainsKey(name))
            {
                return GetFieldValue(name);
            }

            return _properties.ContainsKey(name) ? GetPropertyValue(name) : null;
        }

        private object GetPropertyValue(string name)
        {
            var info = _properties[name];
            return info.GetValue(_target, null);
        }

        private object GetFieldValue(string name)
        {
            var info = _fields[name];
            return info.GetValue(_target);
        }
    }
}
