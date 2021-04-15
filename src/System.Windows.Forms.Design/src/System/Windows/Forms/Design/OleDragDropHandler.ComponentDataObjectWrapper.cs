﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Windows.Forms.Design
{
    internal partial class OleDragDropHandler
    {
        // just so we can recognize the ones we create
        protected class ComponentDataObjectWrapper : DataObject
        {
            public ComponentDataObjectWrapper(ComponentDataObject dataObject) : base(dataObject)
            {
                throw new NotImplementedException(SR.NotImplementedByDesign);
            }

            public ComponentDataObject InnerData => throw new NotImplementedException(SR.NotImplementedByDesign);
        }
    }
}
