/*
 * Copyright 2017 Mikhail Shiryaev
 * All rights reserved
 * 
 * Product  : Rapid SCADA
 * Module   : ScadaData
 * Summary  : Provides data for events caused by an object change
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2014
 * Modified : 2017
 */

using System;

namespace Scada.UI {
    /// <inheritdoc />
    /// <summary>
    /// Provides data for events caused by an object change
    /// <para>Provides data for events triggered by object changes.</para>
    /// </summary>
    public class ObjectChangedEventArgs : EventArgs {
        /// <inheritdoc />
        /// <summary>
        /// Constructor
        /// </summary>
        public ObjectChangedEventArgs(object changedObject)
            : this(changedObject, null) { }

        /// <inheritdoc />
        /// <summary>
        /// Constructor
        /// </summary>
        public ObjectChangedEventArgs(object changedObject, object changeArgument) {
            ChangedObject = changedObject;
            ChangeArgument = changeArgument;
        }


        /// <summary>
        /// Get the changed object
        /// </summary>
        public object ChangedObject { get; protected set; }

        /// <summary>
        /// Get argument describing changes
        /// </summary>
        public object ChangeArgument { get; protected set; }
    }
}