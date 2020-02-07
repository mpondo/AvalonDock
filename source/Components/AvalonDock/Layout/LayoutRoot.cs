﻿/************************************************************************
   AvalonDock

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at https://opensource.org/licenses/MS-PL
 ************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Windows.Markup;
using System.Xml.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Windows.Controls;

namespace AvalonDock.Layout
{
	[ContentProperty(nameof(RootPanel))]
	[Serializable]
	public class LayoutRoot : LayoutElement, ILayoutContainer, ILayoutRoot, IXmlSerializable
	{
		#region Constructors

		public LayoutRoot()
		{
			RightSide = new LayoutAnchorSide();
			LeftSide = new LayoutAnchorSide();
			TopSide = new LayoutAnchorSide();
			BottomSide = new LayoutAnchorSide();
			RootPanel = new LayoutPanel(new LayoutDocumentPane());
		}

		#endregion Constructors

		#region Properties

		#region RootPanel

		private LayoutPanel _rootPanel;
		public LayoutPanel RootPanel
		{
			get => _rootPanel;
			set
			{
				if (_rootPanel == value) return;
				RaisePropertyChanging(nameof(RootPanel));
				if (_rootPanel != null && _rootPanel.Parent == this) _rootPanel.Parent = null;
				_rootPanel = value ?? new LayoutPanel(new LayoutDocumentPane());
				_rootPanel.Parent = this;
				RaisePropertyChanged(nameof(RootPanel));
			}
		}

		#endregion RootPanel

		#region TopSide

		private LayoutAnchorSide _topSide = null;
		public LayoutAnchorSide TopSide
		{
			get => _topSide;
			set
			{
				if (_topSide == value) return;
				RaisePropertyChanging(nameof(TopSide));
				_topSide = value;
				if (_topSide != null) _topSide.Parent = this;
				RaisePropertyChanged(nameof(TopSide));
			}
		}

		#endregion TopSide

		#region RightSide

		private LayoutAnchorSide _rightSide;
		public LayoutAnchorSide RightSide
		{
			get => _rightSide;
			set
			{
				if (_rightSide == value) return;
				RaisePropertyChanging(nameof(RightSide));
				_rightSide = value;
				if (_rightSide != null) _rightSide.Parent = this;
				RaisePropertyChanged(nameof(RightSide));
			}
		}

		#endregion RightSide

		#region LeftSide

		private LayoutAnchorSide _leftSide = null;
		public LayoutAnchorSide LeftSide
		{
			get => _leftSide;
			set
			{
				if (value == _leftSide) return;
				RaisePropertyChanging(nameof(LeftSide));
				_leftSide = value;
				if (_leftSide != null) _leftSide.Parent = this;
				RaisePropertyChanged(nameof(LeftSide));
			}
		}

		#endregion LeftSide

		#region BottomSide

		private LayoutAnchorSide _bottomSide = null;
		public LayoutAnchorSide BottomSide
		{
			get => _bottomSide;
			set
			{
				if (value == _bottomSide) return;
				RaisePropertyChanging(nameof(BottomSide));
				_bottomSide = value;
				if (_bottomSide != null) _bottomSide.Parent = this;
				RaisePropertyChanged(nameof(BottomSide));
			}
		}

		#endregion BottomSide

		#region FloatingWindows

		ObservableCollection<LayoutFloatingWindow> _floatingWindows = null;

		public ObservableCollection<LayoutFloatingWindow> FloatingWindows
		{
			get
			{
				if (_floatingWindows == null)
				{
					_floatingWindows = new ObservableCollection<LayoutFloatingWindow>();
					_floatingWindows.CollectionChanged += _floatingWindows_CollectionChanged;
				}

				return _floatingWindows;
			}
		}

		#endregion FloatingWindows

		#region HiddenAnchorables

		ObservableCollection<LayoutAnchorable> _hiddenAnchorables = null;

		public ObservableCollection<LayoutAnchorable> Hidden
		{
			get
			{
				if (_hiddenAnchorables == null)
				{
					_hiddenAnchorables = new ObservableCollection<LayoutAnchorable>();
					_hiddenAnchorables.CollectionChanged += _hiddenAnchorables_CollectionChanged;
				}

				return _hiddenAnchorables;
			}
		}

		#endregion HiddenAnchorables

		#region Children

		public IEnumerable<ILayoutElement> Children
		{
			get
			{
				if (RootPanel != null)
					yield return RootPanel;
				if (_floatingWindows != null)
				{
					foreach (var floatingWindow in _floatingWindows)
						yield return floatingWindow;
				}
				if (TopSide != null)
					yield return TopSide;
				if (RightSide != null)
					yield return RightSide;
				if (BottomSide != null)
					yield return BottomSide;
				if (LeftSide != null)
					yield return LeftSide;
				if (_hiddenAnchorables != null)
				{
					foreach (var hiddenAnchorable in _hiddenAnchorables)
						yield return hiddenAnchorable;
				}
			}
		}

		public int ChildrenCount => 5 + (_floatingWindows?.Count ?? 0) + (_hiddenAnchorables?.Count ?? 0);

		#endregion Children

		#region ActiveContent

		[field: NonSerialized]
		private WeakReference _activeContent = null;
		private bool _activeContentSet = false;

		[XmlIgnore]
		public LayoutContent ActiveContent
		{
			get
			{
				return _activeContent.GetValueOrDefault<LayoutContent>();
			}
			set
			{
				var currentValue = ActiveContent;
				if (currentValue != value)
				{
					InternalSetActiveContent(currentValue, value);
				}
			}
		}


		#endregion ActiveContent

		#region LastFocusedDocument

		[field: NonSerialized]
		private WeakReference _lastFocusedDocument;

		[XmlIgnore]
		public LayoutContent LastFocusedDocument
		{
			get => _lastFocusedDocument.GetValueOrDefault<LayoutContent>();
			private set
			{
				var currentValue = LastFocusedDocument;
				if (currentValue != value)
				{
					RaisePropertyChanging(nameof(LastFocusedDocument));
					if (currentValue != null) currentValue.IsLastFocusedDocument = false;
					_lastFocusedDocument = new WeakReference(value);
					currentValue = LastFocusedDocument;
					if (currentValue != null) currentValue.IsLastFocusedDocument = true;
					RaisePropertyChanged(nameof(LastFocusedDocument));
				}
			}
		}

		#endregion LastFocusedDocument

		#region Manager

		[NonSerialized]
		private DockingManager _manager = null;

		[XmlIgnore]
		public DockingManager Manager
		{
			get => _manager;
			internal set
			{
				if (value == _manager) return;
				RaisePropertyChanging(nameof(Manager));
				_manager = value;
				RaisePropertyChanged(nameof(Manager));
			}
		}

		#endregion Manager

		#endregion Properties

		#region Overrides

#if TRACE
		public override void ConsoleDump(int tab)
		{
			System.Diagnostics.Trace.Write(new string(' ', tab * 4));
			System.Diagnostics.Trace.WriteLine("RootPanel()");

			RootPanel.ConsoleDump(tab + 1);

			System.Diagnostics.Trace.Write(new string(' ', tab * 4));
			System.Diagnostics.Trace.WriteLine("FloatingWindows()");

			foreach (var fw in FloatingWindows)
				fw.ConsoleDump(tab + 1);

			System.Diagnostics.Trace.Write(new string(' ', tab * 4));
			System.Diagnostics.Trace.WriteLine("Hidden()");

			foreach (var hidden in Hidden)
				hidden.ConsoleDump(tab + 1);
		}
#endif

		#endregion Overrides

		#region Public Methods

		public void RemoveChild(ILayoutElement element)
		{
			if (element == RootPanel)
				RootPanel = null;
			else if (_floatingWindows != null && _floatingWindows.Contains(element))
				_floatingWindows.Remove(element as LayoutFloatingWindow);
			else if (_hiddenAnchorables != null && _hiddenAnchorables.Contains(element))
				_hiddenAnchorables.Remove(element as LayoutAnchorable);
			else if (element == TopSide)
				TopSide = null;
			else if (element == RightSide)
				RightSide = null;
			else if (element == BottomSide)
				BottomSide = null;
			else if (element == LeftSide)
				LeftSide = null;

		}

		public void ReplaceChild(ILayoutElement oldElement, ILayoutElement newElement)
		{
			if (oldElement == RootPanel)
				RootPanel = (LayoutPanel)newElement;
			else if (_floatingWindows != null && _floatingWindows.Contains(oldElement))
			{
				var index = _floatingWindows.IndexOf(oldElement as LayoutFloatingWindow);
				_floatingWindows.Remove(oldElement as LayoutFloatingWindow);
				_floatingWindows.Insert(index, newElement as LayoutFloatingWindow);
			}
			else if (_hiddenAnchorables != null && _hiddenAnchorables.Contains(oldElement))
			{
				var index = _hiddenAnchorables.IndexOf(oldElement as LayoutAnchorable);
				_hiddenAnchorables.Remove(oldElement as LayoutAnchorable);
				_hiddenAnchorables.Insert(index, newElement as LayoutAnchorable);
			}
			else if (oldElement == TopSide)
				TopSide = (LayoutAnchorSide)newElement;
			else if (oldElement == RightSide)
				RightSide = (LayoutAnchorSide)newElement;
			else if (oldElement == BottomSide)
				BottomSide = (LayoutAnchorSide)newElement;
			else if (oldElement == LeftSide)
				LeftSide = (LayoutAnchorSide)newElement;
		}

		/// <summary>
		/// Removes any empty container not directly referenced by other layout items.
		/// </summary>
		public void CollectGarbage()
		{
			var exitFlag = true;

			#region collect empty panes
			do
			{
				exitFlag = true;

				//for each content that references via PreviousContainer a disconnected Pane set the property to null
				foreach (var content in this.Descendents().OfType<ILayoutPreviousContainer>().Where(c => c.PreviousContainer != null &&
					(c.PreviousContainer.Parent == null || c.PreviousContainer.Parent.Root != this)))
				{
					content.PreviousContainer = null;
				}

				//for each pane that is empty
				foreach (var emptyPane in this.Descendents().OfType<ILayoutPane>().Where(p => p.ChildrenCount == 0))
				{
					//...set null any reference coming from contents not yet hosted in a floating window
					foreach (var contentReferencingEmptyPane in this.Descendents().OfType<LayoutContent>()
						.Where(c => ((ILayoutPreviousContainer)c).PreviousContainer == emptyPane && !c.IsFloating))
					{
						if (contentReferencingEmptyPane is LayoutAnchorable &&
							!((LayoutAnchorable)contentReferencingEmptyPane).IsVisible)
							continue;

						((ILayoutPreviousContainer)contentReferencingEmptyPane).PreviousContainer = null;
						contentReferencingEmptyPane.PreviousContainerIndex = -1;
					}

					//...if this pane is the only documentpane present in the layout of the main window (not floating) then skip it
					if (emptyPane is LayoutDocumentPane &&
						 emptyPane.FindParent<LayoutDocumentFloatingWindow>() == null &&
						 this.Descendents().OfType<LayoutDocumentPane>().Count(c => c != emptyPane && c.FindParent<LayoutDocumentFloatingWindow>() == null) == 0)
						continue;

					//...if this empty pane is not referenced by anyone, then remove it from its parent container
					if (!this.Descendents().OfType<ILayoutPreviousContainer>().Any(c => c.PreviousContainer == emptyPane))
					{
						var parentGroup = emptyPane.Parent;
						parentGroup.RemoveChild(emptyPane);
						exitFlag = false;
						break;
					}
				}

				if (!exitFlag)
				{
					//removes any empty anchorable pane group
					foreach (var emptyLayoutAnchorablePaneGroup in this.Descendents().OfType<LayoutAnchorablePaneGroup>().Where(p => p.ChildrenCount == 0))
					{
						var parentGroup = emptyLayoutAnchorablePaneGroup.Parent;
						parentGroup.RemoveChild(emptyLayoutAnchorablePaneGroup);
						exitFlag = false;
						break;
					}
				}

				if (!exitFlag)
				{
					//removes any empty layout panel
					foreach (var emptyLayoutPanel in this.Descendents().OfType<LayoutPanel>().Where(p => p.ChildrenCount == 0))
					{
						var parentGroup = emptyLayoutPanel.Parent;
						parentGroup.RemoveChild(emptyLayoutPanel);
						exitFlag = false;
						break;
					}
					foreach (var emptyLayoutDocumentPane in this.Descendents().OfType<LayoutDocumentPane>().Where(p => p.ChildrenCount == 0))
					{
						var parentGroup = emptyLayoutDocumentPane.Parent;
						if (!(parentGroup.Parent is LayoutDocumentFloatingWindow)) continue;
						var index = RootPanel.IndexOfChild(this.Descendents().OfType<LayoutDocumentPaneGroup>().First());
						parentGroup.RemoveChild(emptyLayoutDocumentPane);
						if (!this.Descendents().OfType<LayoutDocumentPane>().Any())
						{
							// Now the last Pane container is deleted, at least one is required for documents to be added.
							// We did not want to keep an empty window floating, but add a new one to the main window
							RootPanel.Children.Insert(index < 0 ? 0 : index, emptyLayoutDocumentPane);
						}
						exitFlag = false;
						break;
					}
				}

				if (!exitFlag)
				{
					//removes any empty floating window
					foreach (var emptyLayoutFloatingWindow in this.Descendents().OfType<LayoutFloatingWindow>().Where(p => p.ChildrenCount == 0))
					{
						var parentGroup = emptyLayoutFloatingWindow.Parent;
						parentGroup.RemoveChild(emptyLayoutFloatingWindow);
						exitFlag = false;
						break;
					}
				}

				if (!exitFlag)
				{
					//removes any empty anchor group
					foreach (var emptyLayoutAnchorGroup in this.Descendents().OfType<LayoutAnchorGroup>().Where(p => p.ChildrenCount == 0))
					{
						if (!this.Descendents().OfType<ILayoutPreviousContainer>().Any(c => c.PreviousContainer == emptyLayoutAnchorGroup))
						{
							var parentGroup = emptyLayoutAnchorGroup.Parent;
							parentGroup.RemoveChild(emptyLayoutAnchorGroup);
							exitFlag = false;
							break;
						}
					}
				}
			}
			while (!exitFlag);
			#endregion collect empty panes

			#region collapse single child anchorable pane groups
			do
			{
				exitFlag = true;
				//for each pane that is empty
				foreach (var paneGroupToCollapse in this.Descendents().OfType<LayoutAnchorablePaneGroup>().Where(p => p.ChildrenCount == 1 && p.Children[0] is LayoutAnchorablePaneGroup).ToArray())
				{
					var singleChild = paneGroupToCollapse.Children[0] as LayoutAnchorablePaneGroup;
					paneGroupToCollapse.Orientation = singleChild.Orientation;
					while (singleChild.ChildrenCount > 0)
						paneGroupToCollapse.InsertChildAt(paneGroupToCollapse.ChildrenCount, singleChild.Children[0]);
					paneGroupToCollapse.RemoveChild(singleChild);
					exitFlag = false;
					break;
				}
			}
			while (!exitFlag);
			#endregion collapse single child anchorable pane groups

			#region collapse single child document pane groups
			do
			{
				exitFlag = true;
				//for each pane that is empty
				foreach (var paneGroupToCollapse in this.Descendents().OfType<LayoutDocumentPaneGroup>().Where(p => p.ChildrenCount == 1 && p.Children[0] is LayoutDocumentPaneGroup).ToArray())
				{
					var singleChild = paneGroupToCollapse.Children[0] as LayoutDocumentPaneGroup;
					paneGroupToCollapse.Orientation = singleChild.Orientation;
					while (singleChild.ChildrenCount > 0)
						paneGroupToCollapse.InsertChildAt(paneGroupToCollapse.ChildrenCount, singleChild.Children[0]);
					paneGroupToCollapse.RemoveChild(singleChild);
					exitFlag = false;
					break;
				}

			}
			while (!exitFlag);
			#endregion collapse single child document pane groups

			////do
			////{
			////  exitFlag = true;
			////  //for each panel that has only one child
			////  foreach( var panelToCollapse in this.Descendents().OfType<LayoutPanel>().Where( p => p.ChildrenCount == 1 && p.Children[ 0 ] is LayoutPanel ).ToArray() )
			////  {
			////    var singleChild = panelToCollapse.Children[ 0 ] as LayoutPanel;
			////    panelToCollapse.Orientation = singleChild.Orientation;
			////    panelToCollapse.RemoveChild( singleChild );
			////    ILayoutPanelElement[] singleChildChildren = new ILayoutPanelElement[ singleChild.ChildrenCount ];
			////    singleChild.Children.CopyTo( singleChildChildren, 0 );
			////    while( singleChild.ChildrenCount > 0 )
			////    {
			////      panelToCollapse.InsertChildAt(
			////          panelToCollapse.ChildrenCount, singleChildChildren[ panelToCollapse.ChildrenCount ] );
			////    }

			////    exitFlag = false;
			////    break;
			////  }
			////}
			////while( !exitFlag );

			// Update ActiveContent and LastFocusedDocument properties
			UpdateActiveContentProperty();

#if DEBUG
			Debug.Assert(!this.Descendents().OfType<LayoutAnchorablePane>().Any(a => a.ChildrenCount == 0 && a.IsVisible));
			//DumpTree(true);
#if TRACE
            RootPanel.ConsoleDump(4);
#endif
#endif
		}

		/// <inheritdoc />
		public XmlSchema GetSchema() => null;

		/// <inheritdoc />
		public void ReadXml(XmlReader reader)
		{
			reader.MoveToContent();
			if (reader.IsEmptyElement)
			{
				reader.Read();
				return;
			}

			var layoutPanelElements = ReadRootPanel(reader, out var orientation);
			if (layoutPanelElements != null)
			{
				RootPanel = new LayoutPanel { Orientation = orientation };
				//Add all children to RootPanel
				foreach (var panel in layoutPanelElements) RootPanel.Children.Add(panel);
			}

			TopSide = new LayoutAnchorSide();
			if (ReadElement(reader) != null) FillLayoutAnchorSide(reader, TopSide);
			RightSide = new LayoutAnchorSide();
			if (ReadElement(reader) != null) FillLayoutAnchorSide(reader, RightSide);
			LeftSide = new LayoutAnchorSide();
			if (ReadElement(reader) != null) FillLayoutAnchorSide(reader, LeftSide);
			BottomSide = new LayoutAnchorSide();
			if (ReadElement(reader) != null) FillLayoutAnchorSide(reader, BottomSide);

			FloatingWindows.Clear();
			var floatingWindows = ReadElementList(reader, true);
			foreach (var floatingWindow in floatingWindows) FloatingWindows.Add((LayoutFloatingWindow)floatingWindow);

			Hidden.Clear();
			var hidden = ReadElementList(reader, false);
			foreach (var hiddenObject in hidden) Hidden.Add((LayoutAnchorable)hiddenObject);

			//Read the closing end element of LayoutRoot 
			reader.ReadEndElement();
		}

		/// <inheritdoc />
		public void WriteXml(XmlWriter writer)
		{
			writer.WriteStartElement(nameof(RootPanel));
			RootPanel?.WriteXml(writer);
			writer.WriteEndElement();

			writer.WriteStartElement(nameof(TopSide));
			TopSide?.WriteXml(writer);
			writer.WriteEndElement();

			writer.WriteStartElement(nameof(RightSide));
			RightSide?.WriteXml(writer);
			writer.WriteEndElement();

			writer.WriteStartElement(nameof(LeftSide));
			LeftSide?.WriteXml(writer);
			writer.WriteEndElement();

			writer.WriteStartElement(nameof(BottomSide));
			BottomSide?.WriteXml(writer);
			writer.WriteEndElement();

			// Write all floating windows (can be LayoutDocumentFloatingWindow or LayoutAnchorableFloatingWindow).
			// To prevent "can not create instance of abstract type", the type is retrieved with GetType().Name
			writer.WriteStartElement(nameof(FloatingWindows));
			foreach (var layoutFloatingWindow in FloatingWindows)
			{
				writer.WriteStartElement(layoutFloatingWindow.GetType().Name);
				layoutFloatingWindow.WriteXml(writer);
				writer.WriteEndElement();
			}
			writer.WriteEndElement();

			writer.WriteStartElement(nameof(Hidden));
			foreach (var layoutAnchorable in Hidden)
			{
				writer.WriteStartElement(layoutAnchorable.GetType().Name);
				layoutAnchorable.WriteXml(writer);
				writer.WriteEndElement();
			}
			writer.WriteEndElement();
		}

		#endregion Public Methods

		#region Internal Methods

		internal static Type FindType(string name)
		{
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
				foreach (var type in assembly.GetTypes())
					if (type.Name.Equals(name)) return type;
			return null;
		}

		internal void FireLayoutUpdated() => Updated?.Invoke(this, EventArgs.Empty);

		internal void OnLayoutElementAdded(LayoutElement element) => ElementAdded?.Invoke(this, new LayoutElementEventArgs(element));

		internal void OnLayoutElementRemoved(LayoutElement element)
		{
			if (element.Descendents().OfType<LayoutContent>().Any(c => c == LastFocusedDocument))
				LastFocusedDocument = null;
			if (element.Descendents().OfType<LayoutContent>().Any(c => c == ActiveContent))
				ActiveContent = null;
			ElementRemoved?.Invoke(this, new LayoutElementEventArgs(element));
		}

		#endregion Internal Methods

		#region Private Methods

		private void _floatingWindows_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			var bNotifyChildren = false;

			if (e.OldItems != null && (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove || e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Replace))
			{
				foreach (LayoutFloatingWindow element in e.OldItems)
				{
					if (element.Parent != this) continue;
					element.Parent = null;
					bNotifyChildren = true;
				}
			}

			if (e.NewItems != null && (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add || e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Replace))
			{
				foreach (LayoutFloatingWindow element in e.NewItems)
				{
					element.Parent = this;
					bNotifyChildren = true;
				}
			}

			// descendants of LayoutElement notify when their Children and ChildrenCount properties change
			// https://github.com/xceedsoftware/wpftoolkit/issues/1313
			//
			if (!bNotifyChildren) return;
			switch (e.Action)
			{
				case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
				case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
					RaisePropertyChanged(nameof(Children));
					RaisePropertyChanged(nameof(ChildrenCount));
					break;
				case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
					RaisePropertyChanged(nameof(Children));
					break;
			}
		}

		private void _hiddenAnchorables_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			var bNotifyChildren = false;

			if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove || e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Replace)
			{
				if (e.OldItems != null)
				{
					foreach (LayoutAnchorable element in e.OldItems)
					{
						if (element.Parent != this) continue;
						element.Parent = null;
						bNotifyChildren = true;
					}
				}
			}

			if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add || e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Replace)
			{
				if (e.NewItems != null)
				{
					foreach (LayoutAnchorable element in e.NewItems)
					{
						if (element.Parent == this) continue;
						element.Parent?.RemoveChild(element);
						element.Parent = this;
						bNotifyChildren = true;

					}
				}
			}

			// descendants of LayoutElement notify when their Children and ChildrenCount properties change
			// https://github.com/xceedsoftware/wpftoolkit/issues/1313
			//
			if (!bNotifyChildren) return;
			switch (e.Action)
			{
				case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
				case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
					RaisePropertyChanged(nameof(Children));
					RaisePropertyChanged(nameof(ChildrenCount));
					break;
				case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
					RaisePropertyChanged(nameof(Children));
					break;
			}
		}

		private void InternalSetActiveContent(LayoutContent currentValue, LayoutContent newActiveContent)
		{
			RaisePropertyChanging(nameof(ActiveContent));
			if (currentValue != null) currentValue.IsActive = false;
			_activeContent = new WeakReference(newActiveContent);
			currentValue = ActiveContent;
			if (currentValue != null) currentValue.IsActive = true;
			RaisePropertyChanged(nameof(ActiveContent));
			_activeContentSet = currentValue != null;
			if (currentValue != null)
			{
				if (currentValue.Parent is LayoutDocumentPane || currentValue is LayoutDocument)
					LastFocusedDocument = currentValue;
			}
			else
				LastFocusedDocument = null;
		}

		private void UpdateActiveContentProperty()
		{
			var activeContent = ActiveContent;
			if (_activeContentSet && (activeContent == null || activeContent.Root != this))
			{
				_activeContentSet = false;
				InternalSetActiveContent(activeContent, null);
			}
		}

		private void FillLayoutAnchorSide(XmlReader reader, LayoutAnchorSide layoutAnchorSide)
		{
			var result = new List<LayoutAnchorGroup>();

			while (true)
			{
				//Read all layoutAnchorSide children
				if (ReadElement(reader) is LayoutAnchorGroup element) result.Add(element);
				else if (reader.NodeType == XmlNodeType.EndElement) break;
			}

			reader.ReadEndElement();
			foreach (var las in result)
			{
				layoutAnchorSide.Children.Add(las);
			}
		}

		private List<ILayoutPanelElement> ReadRootPanel(XmlReader reader, out Orientation orientation)
		{
			orientation = Orientation.Horizontal;
			var result = new List<ILayoutPanelElement>();
			var startElementName = reader.LocalName;
			reader.Read();
			if (reader.LocalName.Equals(startElementName) && reader.NodeType == XmlNodeType.EndElement) return null;

			while (reader.NodeType == XmlNodeType.Whitespace) reader.Read();

			if (reader.LocalName.Equals(nameof(RootPanel)))
			{
				orientation = (Orientation)Enum.Parse(typeof(Orientation),reader.GetAttribute(nameof(Orientation)), true);
				reader.Read();
				while (true)
				{
					//Read all RootPanel children
					if (ReadElement(reader) is ILayoutPanelElement element) result.Add(element);
					else if (reader.NodeType == XmlNodeType.EndElement) break;
				}
			}

			reader.ReadEndElement();
			return result;
		}

		private List<object> ReadElementList(XmlReader reader, bool isFloatingWindow)
		{
			var resultList = new List<object>();
			while (reader.NodeType == XmlNodeType.Whitespace) reader.Read();

			if (reader.IsEmptyElement)
			{
				reader.Read();
				return resultList;
			}

			var startElementName = reader.LocalName;
			reader.Read();
			if (reader.LocalName.Equals(startElementName) && reader.NodeType == XmlNodeType.EndElement) return null;

			while (reader.NodeType == XmlNodeType.Whitespace) reader.Read();

			while (true)
			{
				if (isFloatingWindow)
				{
					var result = ReadElement(reader) as LayoutFloatingWindow;
					if (result == null) break;
					resultList.Add(result);
				}
				else
				{
					var result = ReadElement(reader) as LayoutAnchorable;
					if (result == null) break;
					resultList.Add(result);
				}
			}

			reader.ReadEndElement();
			return resultList;
		}

		private object ReadElement(XmlReader reader)
		{
			while (reader.NodeType == XmlNodeType.Whitespace) reader.Read();
			if (reader.NodeType == XmlNodeType.EndElement) return null;

			XmlSerializer serializer;
			switch (reader.LocalName)
			{
				case nameof(LayoutAnchorablePaneGroup):
					serializer = new XmlSerializer(typeof(LayoutAnchorablePaneGroup));
					break;
				case nameof(LayoutAnchorablePane):
					serializer = new XmlSerializer(typeof(LayoutAnchorablePane));
					break;
				case nameof(LayoutAnchorable):
					serializer = new XmlSerializer(typeof(LayoutAnchorable));
					break;
				case nameof(LayoutDocumentPaneGroup):
					serializer = new XmlSerializer(typeof(LayoutDocumentPaneGroup));
					break;
				case nameof(LayoutDocumentPane):
					serializer = new XmlSerializer(typeof(LayoutDocumentPane));
					break;
				case nameof(LayoutDocument):
					serializer = new XmlSerializer(typeof(LayoutDocument));
					break;
				case nameof(LayoutAnchorGroup):
					serializer = new XmlSerializer(typeof(LayoutAnchorGroup));
					break;
				case nameof(LayoutPanel):
					serializer = new XmlSerializer(typeof(LayoutPanel));
					break;
				case nameof(LayoutDocumentFloatingWindow):
					serializer = new XmlSerializer(typeof(LayoutDocumentFloatingWindow));
					break;
				case nameof(LayoutAnchorableFloatingWindow):
					serializer = new XmlSerializer(typeof(LayoutAnchorableFloatingWindow));
					break;
				case nameof(LeftSide):
				case nameof(RightSide):
				case nameof(TopSide):
				case nameof(BottomSide):
					if (reader.IsEmptyElement)
					{
						reader.Read();
						return null;
					}
					return reader.Read();
				default:
					var type = FindType(reader.LocalName);
					if (type == null)
						throw new ArgumentException("AvalonDock.LayoutRoot doesn't know how to deserialize " + reader.LocalName);
					serializer = new XmlSerializer(type);
					break;
			}

			return serializer.Deserialize(reader);
		}

		#endregion Private Methods

		#region Events

		public event EventHandler Updated;
		public event EventHandler<LayoutElementEventArgs> ElementAdded;
		public event EventHandler<LayoutElementEventArgs> ElementRemoved;

		#endregion Events

		#region Diagnostic tools

#if DEBUG
		public void DumpTree(bool shortPropertyNames = false)
		{
			void DumpElement(ILayoutElement element, StringBuilder indent, int childID, bool isLastChild)
			{
				Debug.Write($"{indent}{(indent.Length > 0 ? isLastChild ? " └─ " : " ├─ " : "")}{childID:D2} 0x{element.GetHashCode():X8} " +
								$"{element.GetType().Name} {(shortPropertyNames ? "P" : "Parent")}:0x{element.Parent?.GetHashCode() ?? 0:X8} " +
								$"{(shortPropertyNames ? "R" : "Root")}:0x{element.Root?.GetHashCode() ?? 0:X8}");
				var containerElement = element as ILayoutContainer;
				if (containerElement == null)
				{
					Debug.WriteLine("");
					return;
				}
				Debug.WriteLine($" {(shortPropertyNames ? "C" : "Children")}:{containerElement.ChildrenCount}");
				var nrChild = 0;
				indent.Append(isLastChild ? "   " : " │ ");
				foreach (var child in containerElement.Children)
				{
					var lastChild = nrChild == containerElement.ChildrenCount - 1;
					DumpElement(child, indent, nrChild++, lastChild);
				}
				indent.Remove(indent.Length - 3, 3);
			}

			DumpElement(this, new StringBuilder(), 0, true);
		}
#endif

		#endregion Diagnostic tools
	}
}
