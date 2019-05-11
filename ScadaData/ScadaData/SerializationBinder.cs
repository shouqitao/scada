/*
 * Copyright 2017 Mikhail Shiryaev
 * All rights reserved
 * 
 * Product  : Rapid SCADA
 * Module   : ScadaData
 * Summary  : Control class loading during serialization
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2014
 * Modified : 2017
 */

using System;
using System.Reflection;

namespace Scada {
    /// <inheritdoc />
    /// <summary>
    /// Control class loading during serialization
    /// <para>Control of loadable classes during serialization</para>
    /// </summary>
    /// <remarks>The class is necessary because of the peculiarities of .NET,
    /// the object must be created in the assembly in which it is used.</remarks>
    public class SerializationBinder : System.Runtime.Serialization.SerializationBinder {
        /// <summary>
        /// Assembly where types are searched
        /// </summary>
        protected Assembly assembly;

        /// <summary>
        /// Assembly extraction function by name
        /// </summary>
        protected Func<AssemblyName, Assembly> assemblyResolver;

        /// <summary>
        /// Type Extraction Function
        /// </summary>
        protected Func<Assembly, string, bool, Type> typeResolver;


        /// <inheritdoc />
        /// <summary>
        /// Constructor
        /// </summary>
        public SerializationBinder()
            : base() {
            assembly = Assembly.GetExecutingAssembly();
            InitResolvers();
        }

        /// <inheritdoc />
        /// <summary>
        /// Constructor
        /// </summary>
        public SerializationBinder(Assembly assembly)
            : base() {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            this.assembly = assembly;
            InitResolvers();
        }

        /// <summary>
        /// Initialize assembly and type extraction functions.
        /// </summary>
        protected void InitResolvers() {
            assemblyResolver = asmName =>
                string.Equals(asmName.FullName, assembly.FullName, StringComparison.Ordinal)
                    ? assembly
                    : Assembly.Load(asmName);

            typeResolver = (asm, typeName, ignoreCase) => asm.GetType(typeName, false, ignoreCase);
        }


        /// <inheritdoc />
        /// <summary>
        /// Controls the binding of a serialized object to a type.
        /// </summary>
        public override Type BindToType(string assemblyName, string typeName) {
            return string.Equals(assemblyName, assembly.FullName, StringComparison.Ordinal)
                ? assembly.GetType(typeName, true, false)
                : Type.GetType($"{typeName}, {assemblyName}",
                    assemblyResolver, typeResolver, true, false);
        }
    }
}