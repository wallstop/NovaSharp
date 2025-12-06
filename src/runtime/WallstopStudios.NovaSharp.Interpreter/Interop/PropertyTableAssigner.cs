namespace WallstopStudios.NovaSharp.Interpreter.Interop
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Interop.Attributes;
    using WallstopStudios.NovaSharp.Interpreter.Options;

    /// <summary>
    /// Utility class which may be used to set properties on an object of type T, from values contained in a Lua table.
    /// Properties must be decorated with the <see cref="NovaSharpPropertyAttribute"/>.
    /// This is a generic version of <see cref="PropertyTableAssigner"/>.
    /// </summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    public class PropertyTableAssigner<T> : IPropertyTableAssigner
    {
        private readonly PropertyTableAssigner _internalAssigner;

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyTableAssigner{T}"/> class.
        /// </summary>
        /// <param name="expectedMissingProperties">The expected missing properties, that is expected fields in the table with no corresponding property in the object.</param>
        public PropertyTableAssigner(params string[] expectedMissingProperties)
        {
            _internalAssigner = new PropertyTableAssigner(typeof(T), expectedMissingProperties);
        }

        /// <summary>
        /// Adds an expected missing property, that is an expected field in the table with no corresponding property in the object.
        /// </summary>
        /// <param name="name">The name.</param>
        public void AddExpectedMissingProperty(string name)
        {
            _internalAssigner.AddExpectedMissingProperty(name);
        }

        /// <summary>
        /// Assigns properties from tables to an object.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="data">The table.</param>
        /// <exception cref="System.ArgumentNullException">Object is null</exception>
        /// <exception cref="ScriptRuntimeException">A field does not correspond to any property and that property is not one of the expected missing ones.</exception>
        public void AssignObject(T obj, Table data)
        {
            _internalAssigner.AssignObject(obj, data);
        }

        /// <summary>
        /// Gets the type-unsafe assigner corresponding to this object.
        /// </summary>
        public PropertyTableAssigner TypeUnsafeAssigner => _internalAssigner;

        /// <summary>
        /// Sets the subassigner for the given type. Pass null to remove usage of subassigner for the given type.
        /// </summary>
        /// <param name="propertyType">Type of the property for which the subassigner will be used.</param>
        /// <param name="assigner">The property assigner.</param>
        public void SetSubassignerForType(Type propertyType, IPropertyTableAssigner assigner)
        {
            _internalAssigner.SetSubassignerForType(propertyType, assigner);
        }

        /// <summary>
        /// Sets the subassigner for the given type
        /// </summary>
        /// <typeparam name="TSubassignerType">Type of the property for which the subassigner will be used.</typeparam>
        /// <param name="assigner">The property assigner.</param>
        public void SetSubassigner<TSubassignerType>(
            PropertyTableAssigner<TSubassignerType> assigner
        )
        {
            _internalAssigner.SetSubassignerForType(typeof(TSubassignerType), assigner);
        }

        /// <summary>
        /// Assigns the properties of the specified object without checking the type.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <param name="data">The data.</param>
        public virtual void AssignObjectUnchecked(object o, Table data)
        {
            AssignObject((T)o, data);
        }
    }

    /// <summary>
    /// Utility class which may be used to set properties on an object from values contained in a Lua table.
    /// Properties must be decorated with the <see cref="NovaSharpPropertyAttribute"/>.
    /// See <see cref="PropertyTableAssigner{T}"/> for a generic compile time type-safe version.
    /// </summary>
    public class PropertyTableAssigner : IPropertyTableAssigner
    {
        private readonly Type _type;
        private readonly Dictionary<string, PropertyInfo> _propertyMap = new();
        private readonly Dictionary<Type, IPropertyTableAssigner> _subAssigners = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyTableAssigner"/> class.
        /// </summary>
        /// <param name="type">The type of the object.</param>
        /// <param name="expectedMissingProperties">The expected missing properties, that is expected fields in the table with no corresponding property in the object.</param>
        /// <exception cref="System.ArgumentException">
        /// Type cannot be a value type.
        /// </exception>
        public PropertyTableAssigner(Type type, params string[] expectedMissingProperties)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (expectedMissingProperties == null)
            {
                throw new ArgumentNullException(nameof(expectedMissingProperties));
            }

            _type = type;

            if (Framework.Do.IsValueType(_type))
            {
                throw new ArgumentException("Type cannot be a value type.");
            }

            foreach (string property in expectedMissingProperties)
            {
                _propertyMap.Add(property, null);
            }

            foreach (PropertyInfo pi in Framework.Do.GetProperties(_type))
            {
                foreach (
                    NovaSharpPropertyAttribute attr in pi.GetCustomAttributes(true)
                        .OfType<NovaSharpPropertyAttribute>()
                )
                {
                    string name = attr.Name ?? pi.Name;

                    if (!_propertyMap.TryAdd(name, pi))
                    {
                        throw new ArgumentException(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "Type {0} has two definitions for NovaSharp property {1}",
                                _type.FullName,
                                name
                            )
                        );
                    }
                }
            }
        }

        /// <summary>
        /// Adds an expected missing property, that is an expected field in the table with no corresponding property in the object.
        /// </summary>
        /// <param name="name">The name.</param>
        public void AddExpectedMissingProperty(string name)
        {
            _propertyMap.Add(name, null);
        }

        private bool TryAssignProperty(object obj, string name, DynValue value)
        {
            if (_propertyMap.TryGetValue(name, out PropertyInfo pi))
            {
                if (pi != null)
                {
                    object o;

                    if (
                        value.Type == DataType.Table
                        && _subAssigners.TryGetValue(
                            pi.PropertyType,
                            out IPropertyTableAssigner subassigner
                        )
                    )
                    {
                        o = Activator.CreateInstance(pi.PropertyType);
                        subassigner.AssignObjectUnchecked(o, value.Table);
                    }
                    else
                    {
                        o = Converters.ScriptToClrConversions.DynValueToObjectOfType(
                            value,
                            pi.PropertyType,
                            null,
                            false
                        );
                    }

                    Framework.Do.GetSetMethod(pi).Invoke(obj, new object[] { o });
                }

                return true;
            }

            return false;
        }

        private void AssignProperty(object obj, string name, DynValue value)
        {
            if (TryAssignProperty(obj, name, value))
            {
                return;
            }

            if (
                (
                    Script.GlobalOptions.FuzzySymbolMatching
                    & FuzzySymbolMatchingBehavior.UpperFirstLetter
                ) == FuzzySymbolMatchingBehavior.UpperFirstLetter
                && TryAssignProperty(obj, DescriptorHelpers.UpperFirstLetter(name), value)
            )
            {
                return;
            }

            if (
                (Script.GlobalOptions.FuzzySymbolMatching & FuzzySymbolMatchingBehavior.Camelify)
                    == FuzzySymbolMatchingBehavior.Camelify
                && TryAssignProperty(obj, DescriptorHelpers.Camelify(name), value)
            )
            {
                return;
            }

            if (
                (Script.GlobalOptions.FuzzySymbolMatching & FuzzySymbolMatchingBehavior.PascalCase)
                    == FuzzySymbolMatchingBehavior.PascalCase
                && TryAssignProperty(
                    obj,
                    DescriptorHelpers.UpperFirstLetter(DescriptorHelpers.Camelify(name)),
                    value
                )
            )
            {
                return;
            }

            throw new ScriptRuntimeException("Invalid property {0}", name);
        }

        /// <summary>
        /// Assigns properties from tables to an object.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="data">The table.</param>
        /// <exception cref="System.ArgumentNullException">Object is null</exception>
        /// <exception cref="System.ArgumentException">The object is of an incompatible type.</exception>
        /// <exception cref="ScriptRuntimeException">A field does not correspond to any property and that property is not one of the expected missing ones.</exception>
        public void AssignObject(object obj, Table data)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (!Framework.Do.IsInstanceOfType(_type, obj))
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Invalid type of object : got '{0}', expected {1}",
                        obj.GetType().FullName,
                        _type.FullName
                    )
                );
            }

            foreach (TablePair pair in data.Pairs)
            {
                if (pair.Key.Type != DataType.String)
                {
                    throw new ScriptRuntimeException(
                        "Invalid property of type {0}",
                        pair.Key.Type.ToErrorTypeString()
                    );
                }

                AssignProperty(obj, pair.Key.String, pair.Value);
            }
        }

        /// <summary>
        /// Sets the subassigner for the given type. Pass null to remove usage of subassigner for the given type.
        /// </summary>
        /// <param name="propertyType">Type of the property for which the subassigner will be used.</param>
        /// <param name="assigner">The property assigner.</param>
        public void SetSubassignerForType(Type propertyType, IPropertyTableAssigner assigner)
        {
            if (
                Framework.Do.IsAbstract(propertyType)
                || Framework.Do.IsGenericType(propertyType)
                || Framework.Do.IsInterface(propertyType)
                || Framework.Do.IsValueType(propertyType)
            )
            {
                throw new ArgumentException("propertyType must be a concrete, reference type");
            }

            if (assigner == null)
            {
                // Revert to the default CLR conversion when no custom subassigner is configured.
                _subAssigners.Remove(propertyType);
            }
            else
            {
                _subAssigners[propertyType] = assigner;
            }
        }

        /// <summary>
        /// Assigns the properties of the specified object without checking the type.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <param name="data">The data.</param>
        public virtual void AssignObjectUnchecked(object o, Table data)
        {
            AssignObject(o, data);
        }
    }

    /// <summary>
    /// Common interface for property assigners - basically used for sub-assigners
    /// </summary>
    public interface IPropertyTableAssigner
    {
        /// <summary>
        /// Assigns the properties of the specified object without checking the type.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <param name="data">The data.</param>
        public void AssignObjectUnchecked(object o, Table data);
    }
}
