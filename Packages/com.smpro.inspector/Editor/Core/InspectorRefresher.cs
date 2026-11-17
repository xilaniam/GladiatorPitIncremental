using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SMPro.Inspector.EditorTools
{
    internal sealed class InspectorRefresher
    {
        public const long PollInterval = 100;

        sealed class Entry
        {
            public InspectorProperty Property;
            public IStateHandler[] States;
        }

        readonly List<Entry> entries = new();
        readonly List<IRefreshable> refreshables = new();
        readonly List<Collapsible> collapsibles = new();
        readonly HashSet<object> failed = new();
        readonly Action refreshAction;
        readonly Undo.UndoRedoCallback undoAction;
        readonly Action<PlayModeStateChange> playModeAction;
        IVisualElementScheduledItem poll;

        InspectorRefresher(IEnumerable<InspectorProperty> properties, IEnumerable<Collapsible> groups)
        {
            refreshAction = Refresh;
            undoAction = Refresh;
            playModeAction = _ => Refresh();
            foreach (InspectorProperty property in properties) Track(property);
            collapsibles.AddRange(groups);
        }

        public bool Polls { get; private set; }

        public static InspectorRefresher Attach(VisualElement root, SerializedObject serializedObject, IEnumerable<InspectorProperty> properties, IEnumerable<Collapsible> groups)
        {
            var refresher = new InspectorRefresher(properties, groups);
            root.TrackSerializedObjectValue(serializedObject, _ => refresher.Refresh());
            root.RegisterCallback<AttachToPanelEvent>(_ => refresher.OnAttach(root));
            root.RegisterCallback<DetachFromPanelEvent>(_ => refresher.OnDetach());
            refresher.Refresh();
            return refresher;
        }

        void Track(InspectorProperty property)
        {
            var states = new List<IStateHandler>();
            foreach (AttributeHandler handler in property.Handlers)
            {
                if (handler is IStateHandler state)
                {
                    states.Add(state);
                    Polls |= state.NeedsPolling;
                }
                if (handler is IRefreshable refreshable)
                {
                    refreshables.Add(refreshable);
                    Polls |= refreshable.NeedsPolling;
                }
            }
            if (states.Count > 0) entries.Add(new Entry { Property = property, States = states.ToArray() });
        }

        void OnAttach(VisualElement root)
        {
            Undo.undoRedoPerformed += undoAction;
            EditorApplication.playModeStateChanged += playModeAction;
            if (!Polls) return;
            if (poll == null) poll = root.schedule.Execute(refreshAction).Every(PollInterval);
            else poll.Resume();
        }

        void OnDetach()
        {
            Undo.undoRedoPerformed -= undoAction;
            EditorApplication.playModeStateChanged -= playModeAction;
            poll?.Pause();
        }

        public void Refresh()
        {
            for (int i = 0; i < entries.Count; i++) RefreshEntry(entries[i]);
            for (int i = 0; i < collapsibles.Count; i++) collapsibles[i].Refresh();
            for (int i = 0; i < refreshables.Count; i++)
            {
                IRefreshable refreshable = refreshables[i];
                if (failed.Contains(refreshable)) continue;
                try
                {
                    refreshable.Refresh();
                }
                catch (Exception exception)
                {
                    Fail(refreshable, exception);
                }
            }
        }

        void RefreshEntry(Entry entry)
        {
            PropertyState state = PropertyState.Default;
            for (int i = 0; i < entry.States.Length; i++)
            {
                IStateHandler handler = entry.States[i];
                if (failed.Contains(handler)) continue;
                try
                {
                    handler.Apply(ref state);
                }
                catch (Exception exception)
                {
                    Fail(handler, exception);
                }
            }

            InspectorProperty property = entry.Property;
            if (state.Visible != property.IsVisible)
            {
                property.IsVisible = state.Visible;
                property.Element.style.display = state.Visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
            if (state.Enabled != property.IsEnabled)
            {
                property.IsEnabled = state.Enabled;
                property.Element.SetEnabled(state.Enabled);
            }
        }

        void Fail(object handler, Exception exception)
        {
            failed.Add(handler);
            Debug.LogException(exception);
        }
    }

    internal sealed class Collapsible
    {
        readonly VisualElement element;
        readonly List<InspectorProperty> properties = new();
        bool visible = true;

        public Collapsible(VisualElement element) => this.element = element;

        public VisualElement Element => element;

        public void Add(InspectorProperty property)
        {
            if (properties.Count == 0 || properties[properties.Count - 1] != property) properties.Add(property);
        }

        public void Refresh()
        {
            bool any = false;
            for (int i = 0; i < properties.Count && !any; i++) any = properties[i].IsVisible;
            if (any == visible) return;
            visible = any;
            element.style.display = any ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
