using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using PluginFramework;
using UnityEngine;

namespace DebugHelper
{

    [KSPAddon(KSPAddon.Startup.Flight | KSPAddon.Startup.EditorAny | KSPAddon.Startup.EditorVAB | KSPAddon.Startup.SpaceCentre | KSPAddon.Startup.TrackingStation, false)]
    [WindowInitials(Caption = "Debug helper", ClampToScreen = true, DragEnabled = true, TooltipsEnabled = false, Visible = false)]
    public class DebugHelper : MonoBehaviourWindow
    {
        private string _property = string.Empty;
        private string _method = string.Empty;
        private string _textResult = string.Empty;
        private string _external = string.Empty;
        private Vector2 scrollPosition;
        private object _fixedObject;
        private bool _isFixed;

        private Dictionary<string, PropertyInfo> _externalProperties = new Dictionary<string, PropertyInfo>();
        private Dictionary<string, FieldInfo> _externalFields = new Dictionary<string, FieldInfo>();

        private Rect CenterScreen()
        {
            var height = Screen.height / 2;
            var width = Screen.width / 2;

            var rect_h = 400f;
            var rect_w = 500f;

            return new Rect(width - rect_w / 2, height, rect_w, rect_h);
        }

        public override void Awake()
        {
            WindowRect = CenterScreen();
            WindowStyle = HighLogic.Skin.window;
        }

        public override void DrawWindow(int id)
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            GUILayout.BeginVertical();

            GUILayout.Label("Property name", HighLogic.Skin.label);

            _property = GUILayout.TextField(_property, HighLogic.Skin.textField);

            GUILayout.Label("External object property name", HighLogic.Skin.label);

            _external = GUILayout.TextField(_external, HighLogic.Skin.textField);

            GUILayout.Label("Method invoke", HighLogic.Skin.label);

            _method = GUILayout.TextField(_method, HighLogic.Skin.textField);

            GUILayout.TextArea(_textResult, HighLogic.Skin.textArea, GUILayout.ExpandHeight(true));

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Execute", HighLogic.Skin.button))
            {
                _textResult = string.Empty;
                EvaluateProperty(_property);
                EvaluateExternal(_external);
            }

            if (GUILayout.Button(!_isFixed ? "Fix current" : "Unfix current", HighLogic.Skin.button))
            {
                _isFixed = !_isFixed;
                WindowCaption = (_isFixed ? _fixedObject : "Debug helper").ToString();
            }

            if (GUILayout.Button("Dump", HighLogic.Skin.button))
            {
                ExecuteMethod(_method);
            }

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUILayout.EndScrollView();

        }

        private void EvaluateExternal(string external)
        {
            if (string.IsNullOrEmpty(external)) return;

            var parsedProperty = external.Split('.').ToList();

            if (!parsedProperty.Any()) return;

            if (_isFixed)
            {
                ParseFixed(parsedProperty);
                return;
            }

            switch (parsedProperty.Count)
            {
                case 1:

                    if (GetBasicObjectInfo(parsedProperty))
                    {
                        if (_externalFields.Any())
                        {
                            foreach (var externalField in _externalFields)
                            {
                                _textResult += Environment.NewLine +
                                               string.Format("Field name {0}, Field type {1}, Field value {2}",
                                                   externalField.Key, externalField.Value.FieldType,
                                                   externalField.Value.GetValue(null));
                            }
                        }

                        if (_externalProperties.Any())
                        {
                            foreach (var externalProperty in _externalProperties)
                            {
                                _textResult += Environment.NewLine +
                                               string.Format("Property name {0}, Property type {1}",
                                                   externalProperty.Key, externalProperty.Value.PropertyType);
                            }
                        }
                    }
                    break;
                case 2:
                    if (GetBasicObjectInfo(parsedProperty))
                    {
                        var sourceData = parsedProperty.Skip(1).First();
                        if (_externalFields.ContainsKey(sourceData))
                        {
                            var dataValue = _externalFields[sourceData].GetValue(null);

                            _fixedObject = dataValue;

                            if (dataValue != null)
                            {
                                _textResult += Environment.NewLine + string.Format("Field value {0}", dataValue);
                            }
                        }

                        if (_externalProperties.ContainsKey(sourceData))
                        {
                            var dataValue = _externalProperties[sourceData].GetValue(null, null);

                            _fixedObject = dataValue;

                            if (dataValue != null)
                            {
                                _textResult += Environment.NewLine + string.Format("Property value {0}", dataValue);
                            }
                        }
                    }
                    break;
                default:
                    if (GetBasicObjectInfo(parsedProperty))
                    {
                        var sourceData = parsedProperty.Skip(1).First();

                        if (_externalFields.ContainsKey(sourceData))
                        {
                            var dataValue = _externalFields[sourceData].GetValue(null);
                            if (dataValue != null)
                            {
                                var result = GetValue(parsedProperty.Skip(2).ToList(), dataValue);

                                _fixedObject = result;

                                if (result.GetType().GetInterfaces().Where(x => x != typeof(string)).Contains(typeof(IEnumerable)))
                                {
                                    foreach (var item in (IEnumerable)result)
                                    {
                                        _textResult += Environment.NewLine + item;
                                    }
                                }
                                else
                                {
                                    _textResult += Environment.NewLine + (result != null ? string.Format("Field value {0}", result) : "Not found");
                                }
                            }
                        }

                        if (_externalProperties.ContainsKey(sourceData))
                        {
                            var dataValue = _externalProperties[sourceData].GetValue(null, null);
                            if (dataValue != null)
                            {
                                var result = GetValue(parsedProperty.Skip(2).ToList(), dataValue);

                                _fixedObject = result;

                                if (result.GetType().GetInterfaces().Where(x => x != typeof(string)).Contains(typeof(IEnumerable)))
                                {
                                    foreach (var item in (IEnumerable)result)
                                    {
                                        _textResult += Environment.NewLine + item;
                                    }
                                }
                                else
                                {
                                    _textResult += Environment.NewLine + (result != null ? string.Format("Property value {0}", result) : "Not found");
                                }
                            }
                        }
                    }
                    break;
            }

        }

        private void ParseFixed(IEnumerable<string> parsedProperty)
        {
            var result = GetValue(parsedProperty, _fixedObject);

            _textResult += Environment.NewLine + (result != null ? string.Format("value {0}", result) : "Null");
        }

        private object GetValue(IEnumerable<string> properties, object obj)
        {
            try
            {
                if (!properties.Any()) return null;

                var parsedProperty = properties.First();

                var fieldValue = GetFieldValue(parsedProperty, obj);

                var propValue = GetPropertyValue(parsedProperty, obj);

                if (fieldValue == null && propValue == null) return null;

                if (properties.Count() <= 1) return fieldValue ?? propValue;

                return GetValue(properties.Skip(1).ToList(), fieldValue ?? propValue);
            }
            catch (Exception ex)
            {
                _textResult += Environment.NewLine + ex.Message + Environment.NewLine + ex.StackTrace;
                return null;
            }
        }

        private bool GetBasicObjectInfo(IEnumerable<string> parsedProperty)
        {
            var type = GetPropertyType(parsedProperty);

            if (type == null) return false;

            var properties = type.GetProperties();

            if (properties.Any())
            {
                _externalProperties = properties.ToDictionary(x => x.Name, x => x);
            }

            var fields = type.GetFields();

            if (fields.Any())
            {
                _externalFields = fields.Where(f => f.IsStatic).ToDictionary(x => x.Name, x => x);
            }

            return _externalFields.Any() || _externalProperties.Any();
        }

        private Type GetPropertyType(IEnumerable<string> parsedProperty)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .FirstOrDefault(
                    x => x.Name.Equals(parsedProperty.First(), StringComparison.InvariantCultureIgnoreCase));
            return type;
        }

        private void ExecuteMethod(string method)
        {
        }

        private void EvaluateProperty(string property)
        {
            if (string.IsNullOrEmpty(property)) return;

            var parsed_property = property.Split('.').ToList();

            if (!parsed_property.Any()) return;

            parsed_property =
                parsed_property.SkipWhile(x => x.Equals("this", StringComparison.InvariantCultureIgnoreCase)).ToList();

            switch (parsed_property.Count)
            {
                case 1:
                    var type = this.GetType();

                    var propertyInfo = type.GetProperty(parsed_property.First());

                    if (propertyInfo != null)
                    {
                        var result = propertyInfo.GetValue(this, null);
                        _textResult += Environment.NewLine + result;
                    }
                    else
                    {
                        var field_info = type.GetField(parsed_property.First());

                        if (field_info != null)
                        {
                            _textResult += Environment.NewLine + field_info.GetValue(this);
                        }
                        else
                        {
                            _textResult += Environment.NewLine + "Not found";
                        }
                    }

                    break;
                default:
                    break;
            }


        }


        private object GetFieldValue(string fieldName, object source)
        {
            var type = source.GetType();

            FieldInfo field = null;

            if (fieldName.Contains('['))
            {
                var fieldIndexed = fieldName.Substring(0, fieldName.IndexOf('['));
                var index = fieldName.Substring(fieldName.IndexOf('[') + 1, 1);

                field = type.GetField(fieldIndexed);
                if (field != null)
                {
                    var value = field.IsStatic ? field.GetValue(null) : field.GetValue(source);
                    return value != null
                    ? (value as IEnumerable).Cast<object>().Skip(int.Parse(index) - 1).FirstOrDefault()
                    : null;
                }
            }

            field = type.GetField(fieldName);

            if (field == null) return null;

            return (field.IsStatic ? field.GetValue(null) : field.GetValue(source));
        }

        private object GetPropertyValue(string propertyName, object source)
        {
            try
            {
                var type = source.GetType();

                PropertyInfo prop = null;

                if (propertyName.Contains('['))
                {
                    var prop_indexed = propertyName.Substring(0, propertyName.IndexOf('['));
                    object index = int.Parse(propertyName.Substring(propertyName.IndexOf('[') + 1, 1));

                    prop = type.GetProperty(prop_indexed);
                    return prop != null ? prop.GetValue(source, new[] { index }) : null;
                }

                _textResult += Environment.NewLine + propertyName;

                prop = type.GetProperty(propertyName);

                _textResult += Environment.NewLine + prop;

                return prop != null ? prop.GetValue(source, null) : null;
            }
            catch (Exception ex)
            {
                _textResult += Environment.NewLine + ex.Message + Environment.NewLine + ex.StackTrace;
                return null;
            }
        }

        public override void Update()
        {
            base.Update();

            if (Event.current.alt)
            {
                if (Input.GetKeyDown(KeyCode.F11))
                {
                    Visible = !Visible;
                }
            }
        }
    }
}
