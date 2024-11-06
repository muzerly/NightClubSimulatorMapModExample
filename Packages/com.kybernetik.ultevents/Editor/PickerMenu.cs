// UltEvents // https://kybernetik.com.au/ultevents // Copyright 2021-2024 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UltEvents.Editor
{
    /// <summary>[Editor-Only] An <see cref="AdvancedDropdown"/> with generic callback delegates.</summary>
    public class PickerMenu : AdvancedDropdown
    {
        /************************************************************************************************************************/

        /// <summary>
        /// An <see cref="AdvancedDropdownItem"/> with a delegate to call when it's selected.
        /// </summary>
        private class Item : AdvancedDropdownItem
        {
            public GenericMenu.MenuFunction Function { get; set; }

            public Item(string name)
                : base(name)
            { }
        }

        /************************************************************************************************************************/

        private static readonly Texture2D
            SelectedIcon = EditorGUIUtility.Load("d_Toggle Icon") as Texture2D;

        /// <summary>
        /// If true, a <see cref="UnityEditor.GenericMenu"/> will be used
        /// instead of the <see cref="AdvancedDropdown"/>.
        /// </summary>
        public readonly BoolPref ContextMenuStyle;

        /// <summary>
        /// The first item in the <see cref="AdvancedDropdown"/>
        /// (null if <see cref="ContextMenuStyle"/> is true).
        /// </summary>
        private readonly Item RootItem;

        /// <summary>
        /// The <see cref="UnityEditor.GenericMenu"/> being constructed
        /// (null if <see cref="ContextMenuStyle"/> is null or false).
        /// </summary>
        private readonly GenericMenu GenericMenu;

        /************************************************************************************************************************/

        /// <summary>Creates a new <see cref="PickerMenu"/>.</summary>
        public PickerMenu(ref AdvancedDropdownState state, string title, BoolPref contextMenuStyle = null)
            : base(contextMenuStyle != null && contextMenuStyle.Value
                  ? null
                  : state ??= new())
        {
            if (contextMenuStyle != null && contextMenuStyle.Value)
                GenericMenu = new();
            else
                RootItem = new(title);
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override AdvancedDropdownItem BuildRoot()
            => RootItem;

        /// <inheritdoc/>
        protected override void ItemSelected(AdvancedDropdownItem item)
            => (item as Item)?.Function?.Invoke();

        /************************************************************************************************************************/

        /// <summary>Adds an item which will run the `function` when selected.</summary>
        public void AddItem(string path, bool on, GenericMenu.MenuFunction function)
        {
            if (GenericMenu != null)
            {
                GenericMenu.AddItem(new(path), on, function);
                return;
            }

            var item = GetOrCreateItem(path);
            item.enabled = true;
            item.Function = function;
            item.icon = on ? SelectedIcon : null;
        }

        /************************************************************************************************************************/

        /// <summary>Adds an item which will run the `function` for each target of the `property`.</summary>
        public void AddItem(
            SerializedProperty property,
            string path,
            MenuFunctionState state,
            Action<SerializedProperty> function)
        {
            if (GenericMenu != null)
            {
                GenericMenu.AddPropertyModifierFunction(property, path, state, function);
                return;
            }

            var item = GetOrCreateItem(path);
            item.enabled = state != MenuFunctionState.Disabled;
            item.Function = () =>
            {
                Serialization.ForEachTarget(property, function);
                GUIUtility.keyboardControl = 0;
                GUIUtility.hotControl = 0;
                EditorGUIUtility.editingTextField = false;
            };
            item.icon = state == MenuFunctionState.Selected ? SelectedIcon : null;
        }

        /************************************************************************************************************************/

        /// <summary>Adds an item which can't be selected.</summary>
        public void AddDisabledItem(string path)
        {
            if (GenericMenu != null)
            {
                GenericMenu.AddDisabledItem(new(path));
                return;
            }

            var item = GetOrCreateItem(path);
            item.enabled = false;
        }

        /************************************************************************************************************************/

        /// <summary>Adds a separator line.</summary>
        public void AddSeparator(string path)
        {
            if (GenericMenu != null)
            {
                GenericMenu.AddSeparator(new(path));
                return;
            }

            var parent = GetOrCreateItem(path);
            parent.AddSeparator();
        }

        /************************************************************************************************************************/

        /// <summary>Shows this menu relative to the `area`.</summary>
        public void ShowContext(Rect area)
        {
            if (GenericMenu != null)
                GenericMenu.DropDown(area);
            else
                Show(area);
        }

        /************************************************************************************************************************/

        /// <summary>Gets the <see cref="Item"/> with the specified `path` or creates one if it didn't exist.</summary>
        private Item GetOrCreateItem(string path)
        {
            var item = RootItem;

            var start = 0;
            var end = 0;
            while (end < path.Length)
            {
                end = path.IndexOf('/', start);
                if (end < 0)
                    end = path.Length;

                if (end <= start)
                    break;

                var childPath = path[..end];
                var name = path[start..end];

                item = GetOrCreateChild(item, childPath, name);

                start = end + 1;
            }

            return item;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Gets the <see cref="Item"/> with the specified `name` as a child of the `parent`
        /// or creates one if it didn't exist.
        /// </summary>
        /// <remarks>
        /// If the `parent` was a leaf, it's given a new child as a copy of itself which steals its function.
        /// For example, if "Fruits/Apple" exists and you add "Fruits/Apple/Green Apple" then it will add
        /// "Fruits/Apple/Apple" as well so that "Apple" remains selectable.
        /// </remarks>
        private Item GetOrCreateChild(AdvancedDropdownItem parent, string path, string name)
        {
            if (parent is Item parentItem && parentItem.Function != null)
            {
                var reparentName = $"{path[..(path.Length - name.Length)]}{parentItem.name}";
                var reparentItem = new Item(parentItem.name)
                {
                    id = reparentName.GetHashCode(),
                    enabled = parentItem.enabled,
                    icon = parentItem.icon,
                    Function = parentItem.Function
                };
                parent.AddChild(reparentItem);

                parentItem.enabled = true;
                parentItem.icon = null;
                parentItem.Function = null;
            }

            foreach (var child in parent.children)
                if (child.name == name)
                    return (Item)child;

            var item = new Item(name)
            {
                id = path.GetHashCode(),
            };
            parent.AddChild(item);
            return item;
        }

        /************************************************************************************************************************/

        private static readonly GUIContent
            TypeFieldContent = new();

        /// <summary>Draws a field which lets you pick a <see cref="Type"/> from a list.</summary>
        public static void DrawTypeField(
            Rect area,
            SerializedProperty property,
            string selectedTypeName,
            bool showFullTypeNames,
            GUIStyle style,
            ref AdvancedDropdownState state,
            Func<List<Type>> getTypes,
            Action<SerializedProperty, Type> setValue,
            Action onOpenMenu = null)
        {
            var selectedType = Type.GetType(selectedTypeName);

            if (selectedType != null)
            {
                TypeFieldContent.text = selectedType.GetNameCS(showFullTypeNames);
                TypeFieldContent.tooltip = selectedType.GetNameCS(true);
            }
            else
            {
                TypeFieldContent.text = "No Type Selected";
                TypeFieldContent.tooltip = "";
            }

            if (GUI.Button(area, TypeFieldContent, style))
            {
                onOpenMenu?.Invoke();

                property = property.Copy();

                var menu = new PickerMenu(
                    ref state,
                    selectedType != null ? TypeFieldContent.tooltip : "Pick a Type"
#if UNITY_2023_1_OR_NEWER// Unity 2023 has searchable context menus so we can allow them here.
                    , BoolPref.ContextMenuStyle
#endif
                    );

                foreach (var type in getTypes())
                {
                    string path, typeName;
                    if (type == null)
                    {
                        path = typeName = "Null";
                    }
                    else
                    {
                        path = type.GetNameCS(true);
                        path = path.Replace('.', '/');

                        typeName = type.AssemblyQualifiedName;
                    }

                    var itemState = typeName == selectedTypeName
                        ? MenuFunctionState.Selected
                        : MenuFunctionState.Normal;

                    menu.AddItem(property, path, itemState, targetProperty =>
                    {
                        setValue(targetProperty, type);
                    });
                }

                menu.ShowContext(area);
            }

            HandleDragAndDrop(area, property, getTypes, setValue);
        }

        /************************************************************************************************************************/

        /// <summary>Applies <see cref="DragAndDrop"/> operations on the `area`.</summary>
        private static void HandleDragAndDrop(
            Rect area,
            SerializedProperty property,
            Func<List<Type>> getTypes,
            Action<SerializedProperty, Type> setValue)
        {
            var dragging = DragAndDrop.objectReferences;
            if (dragging.Length != 1)
                return;

            var currentEvent = Event.current;
            if (!area.Contains(currentEvent.mousePosition))
                return;

            var drop = dragging[0].GetType();

            // If the dragged object is a valid type, continue.
            if (!getTypes().Contains(drop))
                return;

            if (currentEvent.type == EventType.DragUpdated || currentEvent.type == EventType.MouseDrag)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
            }
            else if (currentEvent.type == EventType.DragPerform)
            {
                setValue(property, drop);
                DragAndDrop.AcceptDrag();
                GUI.changed = true;
            }
        }

        /************************************************************************************************************************/
    }
}

#endif
