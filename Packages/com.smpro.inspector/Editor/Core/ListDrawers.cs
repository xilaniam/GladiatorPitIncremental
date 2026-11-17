using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace SMPro.Inspector.EditorTools
{
    internal sealed class ArrayEditor
    {
        readonly SerializedProperty array;
        readonly Action rebuild;

        public ArrayEditor(SerializedProperty array, Action rebuild)
        {
            this.array = array;
            this.rebuild = rebuild;
        }

        public int Size => array.arraySize;

        public void Add()
        {
            array.arraySize++;
            Commit();
        }

        public void Remove(int index)
        {
            int size = array.arraySize;
            if (size == 0) return;
            array.DeleteArrayElementAtIndex(index >= 0 && index < size ? index : size - 1);
            Commit();
        }

        public void Move(int from, int to)
        {
            if (from == to) return;
            array.MoveArrayElement(from, to);
            Commit();
        }

        void Commit()
        {
            array.serializedObject.ApplyModifiedProperties();
            rebuild();
        }
    }

    internal sealed class ListDrawer
    {
        readonly List<int> indices = new();

        public ListDrawer(InspectorProperty property, ListSettingsAttribute settings, IReadOnlyList<VisualElement> items, Action rebuild)
        {
            Editor = new ArrayEditor(property.SerializedProperty, rebuild);
            for (int i = 0; i < items.Count; i++) indices.Add(i);
            bool editable = !settings.IsReadOnly;

            View = new ListView
            {
                name = "sm-list:" + property.Path,
                itemsSource = indices,
                reorderable = editable && settings.Draggable,
                reorderMode = ListViewReorderMode.Animated,
                showAddRemoveFooter = editable && settings.ShowAddRemove,
                showBorder = true,
                selectionType = SelectionType.Single,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                makeItem = () => new VisualElement(),
                bindItem = (slot, index) =>
                {
                    slot.Clear();
                    if (index < items.Count) slot.Add(items[index]);
                },
                unbindItem = (slot, _) => slot.Clear(),
                onAdd = _ => Editor.Add(),
                onRemove = view => Editor.Remove(view.selectedIndex)
            };
            View.itemIndexChanged += (from, to) => Editor.Move(from, to);
            View.AddToClassList("sm-list");

            string title = $"{property.Label} ({items.Count})";
            if (settings.ShowFoldout)
            {
                SerializedProperty array = property.SerializedProperty;
                var foldout = new Foldout { text = title, value = array.isExpanded, name = "sm-list-foldout:" + property.Path };
                foldout.RegisterValueChangedCallback(change =>
                {
                    if (change.target == foldout) array.isExpanded = change.newValue;
                });
                foldout.Add(View);
                Element = foldout;
            }
            else
            {
                Element = new VisualElement();
                var header = new Label(title);
                header.AddToClassList("sm-list__header");
                Element.Add(header);
                Element.Add(View);
            }
            Element.AddToClassList("sm-list-container");
            Element.userData = this;
        }

        public VisualElement Element { get; }
        public ListView View { get; }
        public ArrayEditor Editor { get; }
    }

    internal sealed class TableDrawer
    {
        public TableDrawer(InspectorProperty property, TableListAttribute settings, IReadOnlyList<string> columns, IReadOnlyList<IReadOnlyList<VisualElement>> rows, Action rebuild)
        {
            Editor = new ArrayEditor(property.SerializedProperty, rebuild);
            bool editable = !settings.IsReadOnly;
            SerializedProperty array = property.SerializedProperty;

            var foldout = new Foldout { text = $"{property.Label} ({rows.Count})", value = array.isExpanded, name = "sm-table:" + property.Path };
            foldout.RegisterValueChangedCallback(change =>
            {
                if (change.target == foldout) array.isExpanded = change.newValue;
            });
            foldout.AddToClassList("sm-table");
            foldout.userData = this;

            var header = Row("sm-table__header");
            if (settings.ShowIndex) header.Add(IndexCell(string.Empty));
            foreach (string column in columns)
            {
                var title = new Label(column);
                title.AddToClassList("sm-table__cell");
                title.AddToClassList("sm-table__title");
                header.Add(title);
            }
            if (editable) header.Add(new VisualElement { name = "sm-table__spacer" });
            foldout.Add(header);

            for (int i = 0; i < rows.Count; i++)
            {
                var row = Row("sm-table__row");
                row.name = "sm-table-row:" + i;
                if (settings.ShowIndex) row.Add(IndexCell(i.ToString()));
                foreach (VisualElement content in rows[i])
                {
                    var cell = new VisualElement();
                    cell.AddToClassList("sm-table__cell");
                    cell.Add(content);
                    row.Add(cell);
                }
                if (editable)
                {
                    int index = i;
                    var remove = new Button(() => Editor.Remove(index)) { text = "×", name = "sm-table-remove:" + i, tooltip = "Remove" };
                    remove.AddToClassList("sm-table__remove");
                    row.Add(remove);
                }
                foldout.Add(row);
            }

            if (editable)
            {
                var add = new Button(Editor.Add) { text = "+ Add", name = "sm-table-add:" + property.Path };
                add.AddToClassList("sm-table__add");
                foldout.Add(add);
            }
            Element = foldout;
        }

        public VisualElement Element { get; }
        public ArrayEditor Editor { get; }

        static VisualElement Row(string className)
        {
            var row = new VisualElement();
            row.AddToClassList(className);
            return row;
        }

        static Label IndexCell(string text)
        {
            var label = new Label(text);
            label.AddToClassList("sm-table__index");
            return label;
        }
    }
}
