﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Windows.Forms.Design;

namespace System.Windows.Forms.PropertyGridInternal
{
    internal class SingleSelectRootGridEntry : GridEntry, IRootGridEntry
    {
        protected object objValue;
        protected string objValueClassName;
        protected GridEntry propDefault;
        protected IDesignerHost host;
        protected IServiceProvider baseProvider;
        protected PropertyTab tab;
        protected PropertyGridView gridEntryHost;
        protected AttributeCollection browsableAttributes;
        private IComponentChangeService changeService;
        protected bool forceReadOnlyChecked;

        internal SingleSelectRootGridEntry(PropertyGridView gridEntryHost, object value, GridEntry parent, IServiceProvider baseProvider, IDesignerHost host, PropertyTab tab, PropertySort sortType)
        : base(gridEntryHost.OwnerGrid, parent)
        {
            Debug.Assert(value is not null, "Can't browse a null object!");
            this.host = host;
            this.gridEntryHost = gridEntryHost;
            this.baseProvider = baseProvider;
            this.tab = tab;
            objValue = value;
            objValueClassName = TypeDescriptor.GetClassName(objValue);

            IsExpandable = true;
            // default to categories
            PropertySort = sortType;
            InternalExpanded = true;
        }

        internal SingleSelectRootGridEntry(PropertyGridView view, object value, IServiceProvider baseProvider, IDesignerHost host, PropertyTab tab, PropertySort sortType) : this(view, value, null, baseProvider, host, tab, sortType)
        {
        }

        /// <summary>
        ///  The set of attributes that will be used for browse filtering
        /// </summary>
        public override AttributeCollection BrowsableAttributes
        {
            get
            {
                if (browsableAttributes is null)
                {
                    browsableAttributes = new AttributeCollection(new Attribute[] { BrowsableAttribute.Yes });
                }

                return browsableAttributes;
            }
            set
            {
                if (value is null)
                {
                    ResetBrowsableAttributes();
                    return;
                }

                bool same = true;

                if (browsableAttributes is not null && value is not null && browsableAttributes.Count == value.Count)
                {
                    Attribute[] attr1 = new Attribute[browsableAttributes.Count];
                    Attribute[] attr2 = new Attribute[value.Count];

                    browsableAttributes.CopyTo(attr1, 0);
                    value.CopyTo(attr2, 0);

                    Array.Sort(attr1, GridEntry.AttributeTypeSorter);
                    Array.Sort(attr2, GridEntry.AttributeTypeSorter);
                    for (int i = 0; i < attr1.Length; i++)
                    {
                        if (!attr1[i].Equals(attr2[i]))
                        {
                            same = false;
                            break;
                        }
                    }
                }
                else
                {
                    same = false;
                }

                browsableAttributes = value;

                if (!same && Children is not null && Children.Count > 0)
                {
                    DisposeChildren();
                }
            }
        }

        protected override IComponentChangeService ComponentChangeService
        {
            get
            {
                if (changeService is null)
                {
                    changeService = (IComponentChangeService)GetService(typeof(IComponentChangeService));
                }

                return changeService;
            }
        }

        internal override bool AlwaysAllowExpand
        {
            get
            {
                return true;
            }
        }

        public override PropertyTab CurrentTab
        {
            get
            {
                return tab;
            }
            set
            {
                tab = value;
            }
        }

        internal override GridEntry DefaultChild
        {
            get
            {
                return propDefault;
            }
            set
            {
                propDefault = value;
            }
        }

        internal override IDesignerHost DesignerHost
        {
            get
            {
                return host;
            }
            set
            {
                host = value;
            }
        }

        internal override bool ForceReadOnly
        {
            get
            {
                if (!forceReadOnlyChecked)
                {
                    ReadOnlyAttribute readOnlyAttr = (ReadOnlyAttribute)TypeDescriptor.GetAttributes(objValue)[typeof(ReadOnlyAttribute)];
                    if ((readOnlyAttr is not null && !readOnlyAttr.IsDefaultAttribute()) || TypeDescriptor.GetAttributes(objValue).Contains(InheritanceAttribute.InheritedReadOnly))
                    {
                        _flags |= FLAG_FORCE_READONLY;
                    }

                    forceReadOnlyChecked = true;
                }

                return base.ForceReadOnly || (GridEntryHost is not null && !GridEntryHost.Enabled);
            }
        }

        internal override PropertyGridView GridEntryHost
        {
            get
            {
                return gridEntryHost;
            }
            set
            {
                gridEntryHost = value;
            }
        }

        public override GridItemType GridItemType
        {
            get
            {
                return GridItemType.Root;
            }
        }

        /// <summary>
        ///  Retrieves the keyword that the VS help dynamic help window will
        ///  use when this IPE is selected.
        /// </summary>
        public override string HelpKeyword
        {
            get
            {
                HelpKeywordAttribute helpAttribute = (HelpKeywordAttribute)TypeDescriptor.GetAttributes(objValue)[typeof(HelpKeywordAttribute)];

                if (helpAttribute is not null && !helpAttribute.IsDefaultAttribute())
                {
                    return helpAttribute.HelpKeyword;
                }

                return objValueClassName;
            }
        }

        public override string PropertyLabel
        {
            get
            {
                if (objValue is IComponent)
                {
                    ISite site = ((IComponent)objValue).Site;
                    if (site is null)
                    {
                        return objValue.GetType().Name;
                    }

                    return site.Name;
                }

                if (objValue is not null)
                {
                    return objValue.ToString();
                }

                return null;
            }
        }

        /// <summary>
        ///  Gets or sets the value for the property that is represented
        ///  by this GridEntry.
        /// </summary>
        public override object PropertyValue
        {
            get
            {
                return objValue;
            }
            set
            {
                object old = objValue;
                objValue = value;
                objValueClassName = TypeDescriptor.GetClassName(objValue);
                OwnerGrid.ReplaceSelectedObject(old, value);
            }
        }

        protected override bool CreateChildren()
        {
            bool fReturn = base.CreateChildren();
            CategorizePropEntries();
            return fReturn;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                host = null;
                baseProvider = null;
                tab = null;
                gridEntryHost = null;
                changeService = null;
            }

            objValue = null;
            objValueClassName = null;
            propDefault = null;
            base.Dispose(disposing);
        }

        public override object GetService(Type serviceType)
        {
            object service = null;

            if (host is not null)
            {
                service = host.GetService(serviceType);
            }

            if (service is null && baseProvider is not null)
            {
                service = baseProvider.GetService(serviceType);
            }

            return service;
        }

        /// <summary>
        ///  Reset the Browsable attributes to the default (BrowsableAttribute.Yes)
        /// </summary>
        public void ResetBrowsableAttributes()
        {
            browsableAttributes = new AttributeCollection(new Attribute[] { BrowsableAttribute.Yes });
        }

        /// <summary>
        ///  Sets the value of this GridEntry from text
        /// </summary>
        public virtual void ShowCategories(bool fCategories)
        {
            if (((PropertySort &= PropertySort.Categorized) != 0) != fCategories)
            {
                if (fCategories)
                {
                    PropertySort |= PropertySort.Categorized;
                }
                else
                {
                    PropertySort &= ~PropertySort.Categorized;
                }

                // recreate the children
                if (Expandable && ChildCollection is not null)
                {
                    CreateChildren();
                }
            }
        }

        internal void CategorizePropEntries()
        {
            if (Children.Count > 0)
            {
                GridEntry[] childEntries = new GridEntry[Children.Count];
                Children.CopyTo(childEntries, 0);

                if ((PropertySort & PropertySort.Categorized) != 0)
                {
                    // first, walk through all the entires and
                    // group them by their category by adding
                    // them to a hashtable of arraylists.
                    //
                    Hashtable bins = new Hashtable();
                    for (int i = 0; i < childEntries.Length; i++)
                    {
                        GridEntry pe = childEntries[i];
                        Debug.Assert(pe is not null);
                        if (pe is not null)
                        {
                            string category = pe.PropertyCategory;
                            ArrayList bin = (ArrayList)bins[category];
                            if (bin is null)
                            {
                                bin = new ArrayList();
                                bins[category] = bin;
                            }

                            bin.Add(pe);
                        }
                    }

                    // now walk through the hashtable
                    // and create a categorygridentry for each
                    // category that holds all the properties
                    // of that category.
                    //
                    ArrayList propList = new ArrayList();
                    IDictionaryEnumerator enumBins = (IDictionaryEnumerator)bins.GetEnumerator();
                    while (enumBins.MoveNext())
                    {
                        ArrayList bin = (ArrayList)enumBins.Value;
                        if (bin is not null)
                        {
                            string category = (string)enumBins.Key;
                            if (bin.Count > 0)
                            {
                                GridEntry[] rgpes = new GridEntry[bin.Count];
                                bin.CopyTo(rgpes, 0);
                                try
                                {
                                    propList.Add(new CategoryGridEntry(OwnerGrid, this, category, rgpes));
                                }
                                catch
                                {
                                }
                            }
                        }
                    }

                    childEntries = new GridEntry[propList.Count];
                    propList.CopyTo(childEntries, 0);
                    Array.Sort(childEntries, GridEntryComparer.Default);

                    ChildCollection.Clear();
                    ChildCollection.AddRange(childEntries);
                }
            }
        }
    }
}
