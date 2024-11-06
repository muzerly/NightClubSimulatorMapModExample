// UltEvents // https://kybernetik.com.au/ultevents // Copyright 2021-2024 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UltEvents.Editor
{
    /// <summary>[Editor-Only] <see cref="PropertyDrawer"/> for <see cref="UltEventBase"/>.</summary>
    [CustomPropertyDrawer(typeof(UltEventBase), true)]
    public class UltEventDrawer : PropertyDrawer
    {
        /************************************************************************************************************************/

        public const float
            Border = 1,
            Padding = 5,
            IndentSize = 15;

        private static readonly GUIContent
            EventLabel = new(),
            CountLabel = new(),
            PlusLabel = EditorGUIUtility.IconContent("Toolbar Plus", "Add to list");

        private static readonly GUIStyle
            HeaderBackground = new("RL Header"),
            PlusButton = "RL FooterButton";

        private static ReorderableList _CurrentCallList;
        private static int _CurrentCallCount;

        /************************************************************************************************************************/

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return EditorGUIUtility.singleLineHeight + 2;

            if (!DrawerState.Current.TryBeginEvent(property))
                return EditorGUIUtility.singleLineHeight;

            CachePersistentCallList(property);
            DrawerState.Current.EndEvent();

            return _CurrentCallList.GetHeight();
        }

        /************************************************************************************************************************/

        private float CalculateCallHeight(int index)
        {
            if ((uint)index >= (uint)_CurrentCallCount)
                return 0;

            var height = EditorGUI.GetPropertyHeight(_CurrentCallList.serializedProperty.GetArrayElementAtIndex(index));
            height += Border * 2 + Padding;

            if (index == _CurrentCallCount - 1)
                height -= Padding - 1;

            return height;
        }

        /************************************************************************************************************************/

        public override void OnGUI(Rect area, SerializedProperty property, GUIContent label)
        {
            if (!DrawerState.Current.TryBeginEvent(property))
                return;

            EventLabel.text = label.text + DrawerState.Current.Event.ParameterString;
            EventLabel.tooltip = label.tooltip;

            if (BoolPref.UseIndentation)
                area = EditorGUI.IndentedRect(area);

#if UNITY_2022_1_OR_NEWER
            area.width -= 1;
#endif

            var indentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            CachePersistentCallList(property);

            if (property.isExpanded)// Expanded.
            {
                DrawerState.Current.BeginCache();

                _CurrentCallList.DoList(area);

                var calls = DrawerState.Current.Event?.PersistentCallsList;
                if (calls != null && calls.Count > 0)
                    area.height = HeaderBackground.fixedHeight;

                DrawerState.Current.EndCache();
            }
            else// Collapsed.
            {
                if (Event.current.type == EventType.Repaint)
                    HeaderBackground.Draw(area, false, false, false, false);

                DoHeaderGUI(new(area.x + 6, area.y + 1, area.width - 12, area.height - 2));
            }

            CheckDragDrop(area);

            EditorGUI.indentLevel = indentLevel;

            DrawerState.Current.EndEvent();
        }

        /************************************************************************************************************************/

        private readonly Dictionary<string, ReorderableList>
            PropertyPathToList = new();

        private void CachePersistentCallList(SerializedProperty eventProperty)
        {
            var path = eventProperty.propertyPath;
            if (!PropertyPathToList.TryGetValue(path, out _CurrentCallList))
            {
                eventProperty = eventProperty.FindPropertyRelative(Names.UltEvent.PersistentCalls);

                _CurrentCallList = new(eventProperty.serializedObject, eventProperty, true, true, true, true)
                {
                    drawHeaderCallback = DoHeaderGUI,
                    drawElementCallback = DoPersistentCallGUI,
                    drawFooterCallback = DoFooterGUI,
                    onAddCallback = AddNewCall,
                    onReorderCallback = OnReorder,
                    elementHeight = 19,// Used when the list is empty.
                    elementHeightCallback = CalculateCallHeight,
                    drawElementBackgroundCallback = DoElementBackgroundGUI,
                    drawNoneElementCallback = DoNoneElementGUI,
                };

                PropertyPathToList.Add(path, _CurrentCallList);
            }

            _CurrentCallCount = _CurrentCallList.count;
            RecalculateFooter();
        }

        /************************************************************************************************************************/

        private static float _DefaultFooterHeight;

        private void RecalculateFooter()
        {
            if (_DefaultFooterHeight == 0)
                _DefaultFooterHeight = _CurrentCallList.footerHeight;

            if (BoolPref.AutoHideFooter && !DrawerState.Current.Event.HasAnyDynamicCalls())
            {
                _CurrentCallList.footerHeight = 0;
            }
            else
            {
                _CurrentCallList.footerHeight = _DefaultFooterHeight;

                if (DrawerState.Current.EventProperty.isExpanded &&
                    DrawerState.Current.EventProperty.serializedObject.targetObjects.Length == 1)
                {
                    if (DrawerState.Current.Event.HasAnyDynamicCalls())
                        _CurrentCallList.footerHeight +=
                            DrawerState.Current.Event.GetDynamicCallInvocationListCount() * EditorGUIUtility.singleLineHeight + 1;
                }
            }
        }

        /************************************************************************************************************************/

        private void DoHeaderGUI(Rect area)
        {
            var color = GUI.color;
            if (!IsEventValid())
                GUI.color = PersistentCallDrawer.ErrorFieldColor;

            var eventProperty = DrawerState.Current.EventProperty;

            EditorGUI.BeginProperty(area, GUIContent.none, eventProperty);

            const float
                AddButtonWidth = 16,
                AddButtonPadding = 2;

            var labelStyle = eventProperty.prefabOverride
                ? EditorStyles.boldLabel
                : GUI.skin.label;

            CountLabel.text = _CurrentCallCount.ToString();
            var countLabelWidth = labelStyle.CalcSize(CountLabel).x;

            var hierarchyMode = EditorGUIUtility.hierarchyMode;
            EditorGUIUtility.hierarchyMode = false;

            area.width -= AddButtonWidth + AddButtonPadding + countLabelWidth;

            area.xMin -= 5;
            var isExpanded = eventProperty.isExpanded =
                EditorGUI.Foldout(area, eventProperty.isExpanded, EventLabel, true);

            EditorGUIUtility.hierarchyMode = hierarchyMode;

            area.x += area.width;
            area.width = countLabelWidth;

            GUI.Label(area, CountLabel, labelStyle);

            area.x += area.width + AddButtonPadding + 1;
            area.width = AddButtonWidth;

            area.y += 1;

            if (GUI.Button(area, PlusLabel, PlusButton))
            {
                eventProperty.isExpanded = true;
                AddNewCall(_CurrentCallList);
            }

            EditorGUI.EndProperty();

            GUI.color = color;
        }

        /************************************************************************************************************************/

        private static bool IsEventValid()
        {
            var ultEvent = DrawerState.Current.Event;
            if (ultEvent?.PersistentCallsList == null)
                return true;

            for (int i = ultEvent.PersistentCallsList.Count - 1; i >= 0; i--)
            {
                var call = ultEvent.PersistentCallsList[i];
                if (call?.GetMemberSafe() == null)
                    return false;
            }

            return true;
        }

        /************************************************************************************************************************/

        public void DoElementBackgroundGUI(Rect area, int index, bool selected, bool focused)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            area.y -= 4;
            area.height = CalculateCallHeight(index) + 1;

            if (index == _CurrentCallCount - 1)
                area.height += 2;

            ReorderableList.defaultBehaviours.elementBackground.Draw(area, false, selected, selected, focused);

            if (index >= 0 && index < _CurrentCallCount - 1)
            {
                area.xMin += 1;
                area.xMax -= 1;
                area.y += area.height;
                area.height = 1;

                EditorGUI.DrawRect(area, SeparatorLineColor);
            }
        }

        /************************************************************************************************************************/

        private void DoNoneElementGUI(Rect area)
        {
            EditorGUI.BeginProperty(area, GUIContent.none, DrawerState.Current.EventProperty);

            if (GUI.Button(area, "Click to add a listener", GUI.skin.label) &&
                Event.current.button == 0)
            {
                AddNewCall(_CurrentCallList);
            }

            EditorGUI.EndProperty();
        }

        /************************************************************************************************************************/

        private static GUIStyle _SeparatorLineStyle;

        private static Color SeparatorLineColor
            => EditorGUIUtility.isProSkin
            ? new(0.157f, 0.157f, 0.157f)
            : new(0.5f, 0.5f, 0.5f);

        private static void DoSeparatorLineGUI(Rect area)
        {
            if (Event.current.type == EventType.Repaint)
            {
                if (_SeparatorLineStyle == null)
                {
                    _SeparatorLineStyle = new();
                    _SeparatorLineStyle.normal.background = EditorGUIUtility.whiteTexture;
                    _SeparatorLineStyle.stretchWidth = true;
                }

                var oldColor = GUI.color;
                GUI.color = SeparatorLineColor;
                _SeparatorLineStyle.Draw(area, false, false, false, false);
                GUI.color = oldColor;
            }
        }

        /************************************************************************************************************************/

        private void DoPersistentCallGUI(Rect area, int index, bool isActive, bool isFocused)
        {
            DrawerState.Current.callIndex = index;
            var callProperty = _CurrentCallList.serializedProperty.GetArrayElementAtIndex(index);

            area.x += Border;
            area.y += Border;
            area.height -= Border * 2;

            PersistentCallDrawer.includeRemoveButton = true;

            EditorGUI.PropertyField(area, callProperty);

            if (PersistentCallDrawer.DoRemoveButtonGUI(area))
                DelayedRemoveCall(index);

            if (isFocused)
                CheckInput(index);

            DrawerState.Current.callIndex = -1;
        }

        /************************************************************************************************************************/

        private static GUIStyle _FooterBackground;

        public void DoFooterGUI(Rect area)
        {
            if (area.height == 0)
                return;

            const float
                InvokePadding = 2,
                AddRemoveWidth = 16,
                RightSideOffset = 5;

            var width = area.width;
            area.xMin -= 1;

            // Background.
            if (Event.current.type == EventType.Repaint)
            {
                _FooterBackground ??= new(ReorderableList.defaultBehaviours.footerBackground)
                {
                    fixedHeight = 0
                };

                _FooterBackground.Draw(area, false, false, false, false);
            }

            area.y -= 3;
            area.width -= InvokePadding + AddRemoveWidth * 2 + RightSideOffset;
            area.height = EditorGUIUtility.singleLineHeight;

            if (DrawerState.Current.EventProperty.serializedObject.targetObjects.Length > 1)
            {
                // Multiple Objects Selected.
                area.xMin += 2;
                GUI.Label(area, "Can't show Dynamic Listeners for multiple objects");
            }
            else if (DrawerState.Current.Event != null)
            {
                area.xMin += 16;
                var labelWidth = area.width;
                area.xMax = EditorGUIUtility.labelWidth + IndentSize;

                GUI.Label(area, "Dynamic Listeners");

                // Dynamic Listener Foldout.

                var dynamicListenerCount = DrawerState.Current.Event.GetDynamicCallInvocationListCount();
                if (dynamicListenerCount > 0)
                {
                    var isExpanded = EditorGUI.Foldout(area, _CurrentCallList.serializedProperty.isExpanded, GUIContent.none, true);
                    _CurrentCallList.serializedProperty.isExpanded = isExpanded;
                    if (isExpanded && DrawerState.Current.Event.HasAnyDynamicCalls())
                    {
                        DoDynamicListenerGUI(area.x, area.y + EditorGUIUtility.singleLineHeight - 1, width, DrawerState.Current.Event);
                    }
                }

                // Dynamic Listener Count.
                area.x += area.width;
                area.width = labelWidth - area.width;
                GUI.Label(area, dynamicListenerCount.ToString());
            }

            // Add.
            area.x += area.width + InvokePadding;
            area.y -= 1;
            area.width = AddRemoveWidth;
            area.height = _DefaultFooterHeight;
            if (GUI.Button(area, ReorderableList.defaultBehaviours.iconToolbarPlus, ReorderableList.defaultBehaviours.preButton))
            {
                AddNewCall(_CurrentCallList);
            }

            // Remove.
            area.x += area.width;
            using (new EditorGUI.DisabledScope(_CurrentCallList.index < 0 || _CurrentCallList.index >= _CurrentCallCount))
            {
                if (GUI.Button(area, ReorderableList.defaultBehaviours.iconToolbarMinus, ReorderableList.defaultBehaviours.preButton))
                {
                    DelayedRemoveCall(_CurrentCallList.index);
                }
            }
        }

        /************************************************************************************************************************/

        private void DoDynamicListenerGUI(float x, float y, float width, UltEventBase targetEvent)
        {
            x += IndentSize;
            width -= IndentSize * 2;

            var area = new Rect(x, y, width, EditorGUIUtility.singleLineHeight);

            var calls = targetEvent.GetDynamicCallInvocationList();
            for (int i = 0; i < calls.Length; i++)
            {
                var call = calls[i];
                DoDelegateGUI(area, call);
                area.y += area.height;
            }
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Draw the target and name of the specified <see cref="Delegate"/>.
        /// </summary>
        public static void DoDelegateGUI(Rect area, Delegate del)
        {
            var width = area.width;

            area.xMax = EditorGUIUtility.labelWidth + 15;
            var obj = del.Target as Object;
            if (!ReferenceEquals(obj, null))
            {
                // If the target is a Unity Object, draw it in an Object Field so the user can click to ping the object.

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUI.ObjectField(area, obj, typeof(Object), true);
                }
            }
            else if (del.Method.DeclaringType.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), true))
            {
                // Anonymous Methods draw only their method name.

                area.width = width;

                GUI.Label(area, del.Method.GetNameCS());

                return;
            }
            else if (del.Target == null)
            {
                GUI.Label(area, del.Method.DeclaringType.GetNameCS());
            }
            else
            {
                GUI.Label(area, del.Target.ToString());
            }

            area.x += area.width;
            area.width = width - area.width;

            GUI.Label(area, del.Method.GetNameCS(false));
        }

        /************************************************************************************************************************/

        private void AddNewCall(ReorderableList list)
            => AddNewCall(list, list.serializedProperty.serializedObject.targetObject);

        private void AddNewCall(ReorderableList list, Object target)
        {
            var index = list.index;
            if (index >= 0 && index < _CurrentCallCount)
            {
                index++;
                list.index = index;
            }
            else
            {
                index = _CurrentCallCount;
            }

            list.serializedProperty.InsertArrayElementAtIndex(index);

            list.serializedProperty.serializedObject.ApplyModifiedProperties();

            var callProperty = list.serializedProperty.GetArrayElementAtIndex(index);
            DrawerState.Current.BeginCall(callProperty);
            PersistentCallDrawer.SetTarget(target);
            DrawerState.Current.EndCall();
        }

        /************************************************************************************************************************/

        private static void RemoveCall(ReorderableList list, int index)
        {
            var property = list.serializedProperty;
            property.DeleteArrayElementAtIndex(index);

            if (list.index >= property.arraySize - 1)
                list.index = property.arraySize - 1;

            property.serializedObject.ApplyModifiedProperties();
        }

        private void DelayedRemoveCall(int index)
        {
            var list = _CurrentCallList;
            var state = new DrawerState();
            state.CopyFrom(DrawerState.Current);

            EditorApplication.delayCall += () =>
            {
                DrawerState.Current.CopyFrom(state);

                RemoveCall(list, index);

                DrawerState.Current.UpdateLinkedArguments();
                DrawerState.Current.Clear();

                InternalEditorUtility.RepaintAllViews();
            };
        }

        /************************************************************************************************************************/

        private void OnReorder(ReorderableList list)
        {
            list.serializedProperty.serializedObject.ApplyModifiedProperties();
            DrawerState.Current.UpdateLinkedArguments();
        }

        /************************************************************************************************************************/

        private void CheckInput(int index)
        {
            var currentEvent = Event.current;
            if (currentEvent.type == EventType.KeyUp)
            {
                switch (currentEvent.keyCode)
                {
                    case KeyCode.Backspace:
                    case KeyCode.Delete:
                        RemoveCall(_CurrentCallList, index);
                        currentEvent.Use();
                        break;

                    case KeyCode.Plus:
                    case KeyCode.KeypadPlus:
                    case KeyCode.Equals:
                        AddNewCall(_CurrentCallList);
                        currentEvent.Use();
                        break;

                    case KeyCode.C:
                        if (currentEvent.control)
                        {
                            var property = _CurrentCallList.serializedProperty.GetArrayElementAtIndex(index);
                            Clipboard.CopyCall(property);
                            currentEvent.Use();
                        }
                        break;

                    case KeyCode.V:
                        if (currentEvent.control)
                        {
                            var property = _CurrentCallList.serializedProperty;
                            if (currentEvent.shift)
                            {
                                index++;
                                property.InsertArrayElementAtIndex(index);
                                property.serializedObject.ApplyModifiedProperties();
                                property = property.GetArrayElementAtIndex(index);
                                Clipboard.PasteCall(property);
                            }
                            else
                            {
                                property = property.GetArrayElementAtIndex(index);
                                Clipboard.PasteCall(property);
                            }
                            currentEvent.Use();
                        }
                        break;
                }
            }
        }

        /************************************************************************************************************************/

        private void CheckDragDrop(Rect area)
        {
            if (!area.Contains(Event.current.mousePosition) ||
                DragAndDrop.objectReferences.Length == 0)
                return;

            switch (Event.current.type)
            {
                case EventType.Repaint:
                case EventType.DragUpdated:
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    break;

                case EventType.DragPerform:
                    foreach (var drop in DragAndDrop.objectReferences)
                    {
                        AddNewCall(_CurrentCallList, drop);
                    }
                    DrawerState.Current.EventProperty.isExpanded = true;
                    DragAndDrop.AcceptDrag();
                    GUI.changed = true;
                    break;

                default:
                    break;
            }
        }

        /************************************************************************************************************************/
    }
}

#endif