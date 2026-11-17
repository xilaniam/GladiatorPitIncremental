using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SMPro.Inspector.EditorTools
{
    internal sealed class InspectorRefresher
    {
        sealed class Entry
        {
            public InspectorProperty Property;
            public IStateHandler[] States;
        }

        readonly SerializedObject serializedObject;
        readonly List<InspectorProperty> properties = new();
        readonly List<Entry> entries = new();
        readonly List<IRefreshable> refreshables = new();
        readonly List<Collapsible> collapsibles = new();
        readonly HashSet<object> failed = new();
        readonly Action pollAction;
        IVisualElementScheduledItem poll;

        public InspectorRefresher(SerializedObject serializedObject, IEnumerable<InspectorProperty> built, IEnumerable<Collapsible> groups,
            IEnumerable<IRefreshable> extras, InspectorValidation validation)
        {
            this.serializedObject = serializedObject;
            pollAction = RefreshStates;
            Validation = validation;
            properties.AddRange(built);
            foreach (InspectorProperty property in properties) Track(property);
            foreach (IRefreshable extra in extras)
            {
                refreshables.Add(extra);
                Polls |= extra.NeedsPolling;
            }
            collapsibles.AddRange(groups);
        }

        public bool Polls { get; private set; }
        public IReadOnlyList<InspectorProperty> Properties => properties;
        public InspectorValidation Validation { get; }

        public InspectorProperty Find(string path)
        {
            foreach (InspectorProperty property in properties)
                if (property.Path == path) return property;
            return null;
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

        public void Start(VisualElement root)
        {
            if (!Polls) return;
            long interval = SMInspectorSettings.instance.PollInterval;
            if (poll == null) poll = root.schedule.Execute(pollAction).Every(interval);
            else poll.Every(interval).Resume();
        }

        public void Stop() => poll?.Pause();

        public void Refresh()
        {
            if (serializedObject.targetObject == null) return;
            serializedObject.UpdateIfRequiredOrScript();
            Validation?.Refresh();
            RefreshStates();
        }

        public void RefreshStates()
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
