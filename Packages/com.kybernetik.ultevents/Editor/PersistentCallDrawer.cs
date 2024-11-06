// UltEvents // https://kybernetik.com.au/ultevents // Copyright 2021-2024 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UltEvents.Editor
{
    [CustomPropertyDrawer(typeof(PersistentCall), true)]
    internal sealed class PersistentCallDrawer : PropertyDrawer
    {
        /************************************************************************************************************************/

        public static float LineHeight
            => EditorGUIUtility.singleLineHeight;

        public static float Padding
            => EditorGUIUtility.standardVerticalSpacing;

        public static readonly GUIStyle
            PopupButtonStyle = EditorStyles.popup,
            PopupLabelStyle,
            TypePickerButtonStyle;

        private static readonly GUIContent
            ArgumentLabel = new(),
            MethodNameSuggestionLabel = new("?", "Suggest a method name");

        public static readonly Color
            ErrorFieldColor = new(1, 0.65f, 0.65f);

        /************************************************************************************************************************/

        static PersistentCallDrawer()
        {
            PopupLabelStyle = new(GUI.skin.label)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleLeft,
                padding = new(4, 14, 0, 0),
            };

            TypePickerButtonStyle = new(EditorStyles.miniButton)
            {
                alignment = TextAnchor.MiddleLeft,
            };
        }

        /************************************************************************************************************************/

        public override float GetPropertyHeight(SerializedProperty callProperty, GUIContent label)
        {
            if (callProperty.hasMultipleDifferentValues)
            {
                if (DrawerState.GetPersistentArgumentsProperty(callProperty).hasMultipleDifferentValues)
                    return LineHeight;

                if (DrawerState.GetMemberNameProperty(callProperty).hasMultipleDifferentValues)
                    return LineHeight;
            }

            if (DrawerState.GetCall(callProperty).GetMemberSafe() == null)
                return LineHeight;

            callProperty = DrawerState.GetPersistentArgumentsProperty(callProperty);

            var height = LineHeight;
            var arraySize = callProperty.arraySize;
            for (int i = 0; i < arraySize; i++)
            {
                var argumentProperty = callProperty.GetArrayElementAtIndex(i);
                height += EditorGUI.GetPropertyHeight(argumentProperty, label);
            }
            height += (arraySize - 1) * Padding;

            return height;
        }

        /************************************************************************************************************************/

        public override void OnGUI(Rect area, SerializedProperty callProperty, GUIContent label)
        {
            DrawerState.Current.BeginCall(callProperty);

            var propertyArea = area;

            // If we're in the reorderable list of an event, adjust the property area to cover the list bounds.
            if (DrawerState.Current.CachePreviousCalls)
            {
                area.y -= 2;

                propertyArea.xMin -= 20;
                propertyArea.yMin -= 4;
                propertyArea.width += 4;
            }

            label = EditorGUI.BeginProperty(propertyArea, label, callProperty);
            {
                // Target Field.

                var x = area.x;
                var xMax = area.xMax;

                area.height = LineHeight;
                area.width *= 0.35f;

                DoTargetFieldGUI(
                    area,
                    DrawerState.Current.TargetProperty,
                    DrawerState.Current.MemberNameProperty,
                    out var autoOpenMemberMenu);

                // Member Name Dropdown.

                EditorGUI.showMixedValue =
                    DrawerState.Current.PersistentArgumentsProperty.hasMultipleDifferentValues ||
                    DrawerState.Current.MemberNameProperty.hasMultipleDifferentValues;

                var member = EditorGUI.showMixedValue
                    ? null
                    : DrawerState.Current.call.GetMemberSafe();

                area.x += area.width + EditorGUIUtility.standardVerticalSpacing;
                area.xMax = xMax;

                DoMemberFieldGUI(area, member, autoOpenMemberMenu);

                // Persistent Arguments.
                if (member != null)
                {
                    area.x = x;
                    area.xMax = xMax;

                    var offset =
#if UNITY_2022_1_OR_NEWER
                        1;
#else
                        18;
#endif

                    var labelWidth = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth -= area.x - offset;

                    if (member is MethodBase method)
                        DrawMethodArguments(area, method);
                    else if (member is FieldInfo field)
                        DrawFieldArgument(area, field, DrawerState.Current.call.IsGetter);

                    EditorGUIUtility.labelWidth = labelWidth;
                }

                EditorGUI.showMixedValue = false;
            }
            EditorGUI.EndProperty();

            DrawerState.Current.EndCall();
        }

        /************************************************************************************************************************/

        private void DrawMethodArguments(Rect area, MethodBase method)
        {
            var parameters = DrawerState.Current.callParameters = method.GetParameters();
            var arguments = DrawerState.Current.PersistentArgumentsProperty;

            if (parameters.Length ==
                arguments.arraySize)
            {
                for (int i = 0; i < parameters.Length; i++)
                    if (!DrawArgument(ref area, i))
                        break;
            }
            else
            {
                Debug.LogError(
                    $"Method parameter count doesn't match serialized argument count " +
                    $"{parameters.Length} : " +
                    $"{arguments.arraySize}");
            }

            DrawerState.Current.callParameters = null;
        }

        /************************************************************************************************************************/

        private void DrawFieldArgument(Rect area, FieldInfo field, bool isGetter)
        {
            if (isGetter)
                return;

            DrawerState.Current.currentField = field;

            DrawArgument(ref area, 0);

            DrawerState.Current.currentField = null;
        }

        /************************************************************************************************************************/

        private bool DrawArgument(ref Rect area, int index)
        {
            var parameters = DrawerState.Current.callParameters;
            var arguments = DrawerState.Current.PersistentArgumentsProperty;

            DrawerState.Current.parameterIndex = index;
            area.y += area.height + Padding;

            ArgumentLabel.text = parameters != null
                ? parameters[index].Name
                : "value";

            var argumentProperty = arguments.GetArrayElementAtIndex(index);

            area.height = EditorGUI.GetPropertyHeight(argumentProperty, ArgumentLabel);

            if (argumentProperty.propertyPath != "")
            {
                EditorGUI.PropertyField(area, argumentProperty, ArgumentLabel);

                return true;
            }
            else
            {
                if (GUI.Button(area, new GUIContent(
                    "Reselect these objects to show arguments",
                    "This is the result of a bug in the way Unity updates the SerializedProperty" +
                    " for an array after it is resized while multiple objects are selected")))
                {
                    var selection = Selection.objects;
                    Selection.objects = new Object[0];
                    EditorApplication.delayCall += () => Selection.objects = selection;
                }

                return false;
            }
        }

        /************************************************************************************************************************/
        #region Target Field
        /************************************************************************************************************************/

        private static AdvancedDropdownState _TypeFieldState;

        private static void DoTargetFieldGUI(
            Rect area,
            SerializedProperty targetProperty,
            SerializedProperty memberNameProperty,
            out bool autoOpenMemberMenu)
        {
            autoOpenMemberMenu = false;

            // Type field for a static type.
            if (targetProperty.objectReferenceValue == null &&
                !targetProperty.hasMultipleDifferentValues)
            {
                DoTargetTypeFieldGUI(area, targetProperty, memberNameProperty, ref autoOpenMemberMenu);
            }
            else// Object field for an object reference.
            {
                DoTargetObjectFieldGUI(area, targetProperty, ref autoOpenMemberMenu);
            }
        }

        /************************************************************************************************************************/

        private static void DoTargetTypeFieldGUI(
            Rect area,
            SerializedProperty targetProperty,
            SerializedProperty memberNameProperty,
            ref bool autoOpenMemberMenu)
        {
            var methodName = memberNameProperty.stringValue;
            string typeName;

            var lastDot = methodName.LastIndexOf('.');
            if (lastDot >= 0)
            {
                typeName = methodName[..lastDot];
                lastDot++;
                methodName = methodName[lastDot..];
            }
            else typeName = "";

            var color = GUI.color;
            if (Type.GetType(typeName) == null)
                GUI.color = ErrorFieldColor;

            PickerMenu.DrawTypeField(
                area,
                memberNameProperty,
                typeName,
                BoolPref.ShowFullTypeNames,
                TypePickerButtonStyle,
                ref _TypeFieldState,
                GetSuportedTypes,
                setValue: (property, type) =>
                {
                    if (type == null)
                    {
                        property.stringValue = "";

                        Serialization.ForEachTarget(targetProperty, target =>
                        {
                            target.objectReferenceValue = target.serializedObject.targetObject;
                        });
                    }
                    else
                    {
                        property.stringValue = $"{type.AssemblyQualifiedName}.{methodName}";
                    }
                },
                onOpenMenu: () =>
                {
                    targetProperty = targetProperty.Copy();
                });

            HandleTargetFieldDragAndDrop(area, ref autoOpenMemberMenu);

            GUI.color = color;
        }

        /************************************************************************************************************************/

        private static void DoTargetObjectFieldGUI(
            Rect area,
            SerializedProperty targetProperty,
            ref bool autoOpenMemberMenu)
        {
            if (targetProperty.hasMultipleDifferentValues)
                EditorGUI.showMixedValue = true;

            EditorGUI.BeginChangeCheck();

            var oldTarget = targetProperty.objectReferenceValue;
            var target = EditorGUI.ObjectField(area, oldTarget, typeof(Object), true);
            if (EditorGUI.EndChangeCheck())
            {
                SetBestTarget(oldTarget, target, out autoOpenMemberMenu);
            }

            EditorGUI.showMixedValue = false;
        }

        /************************************************************************************************************************/

        private static List<Type> _SupportedTypes;

        private static List<Type> GetSuportedTypes()
        {
            if (_SupportedTypes != null)
                return _SupportedTypes;

            // Gather all types in all currently loaded assemblies.
            _SupportedTypes = new(8192);

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            FilterSupportedAssemblies(assemblies, out var count);

            var tasks = new Task<List<Type>>[count];
            for (int i = 0; i < count; i++)
            {
                var assembly = assemblies[i];
                var task = tasks[i] = new(() => GetSupportedTypes(assembly));
                task.Start();
            }

            var wait = Task.WhenAll(tasks);
            wait.Wait();

            var results = wait.Result;
            for (int i = 0; i < results.Length; i++)
                _SupportedTypes.AddRange(results[i]);

            _SupportedTypes.Sort((a, b) => a.GetNameCS().CompareTo(b.GetNameCS()));
            _SupportedTypes.Insert(0, null);

            GC.Collect();

            return _SupportedTypes;
        }

        /************************************************************************************************************************/

        private static void FilterSupportedAssemblies(Assembly[] assemblies, out int supportedAssemblyCount)
        {
            supportedAssemblyCount = assemblies.Length;

            for (int i = 0; i < supportedAssemblyCount; i++)
            {
                var assembly = assemblies[i];
                if (!assembly.IsDynamic)
                    continue;

                // Overwrite the one we want to remove with the last one.

                supportedAssemblyCount--;
                assemblies[i] = assemblies[supportedAssemblyCount];

                // Leave the last one where it was since the count will no longer include it anyway.

                i--;// We need to re-check this index now that it's swapped.
            }
        }

        /************************************************************************************************************************/

        private static List<Type> GetSupportedTypes(Assembly assembly)
        {
            var types = assembly.GetExportedTypes();
            var supported = new List<Type>(types.Length);
            for (int i = 0; i < types.Length; i++)
            {
                var type = types[i];

                // No Generics (would need a way to pick generic parameters).
                if (type.ContainsGenericParameters)
                    continue;

                // No Enums.
                if (type.IsEnum)
                    continue;

                // No special names.
                if (type.IsSpecialName || !char.IsLetter(type.Name[0]))
                    continue;

                // No Obsoletes.
                if (type.IsDefined(typeof(ObsoleteAttribute), true))
                    continue;

                // No Delegates.
                if (type.BaseType == typeof(MulticastDelegate))
                    continue;

                // Nothing without static members or constructors.
                if (type.GetMembers(UltEventUtils.StaticBindings).Length == 0 &&
                    (type.IsAbstract || type.GetConstructors(UltEventUtils.AnyAccessBindings).Length == 0))
                    continue;

                // The type might still not have any valid members,
                // but at least we've narrowed down the list a lot.

                supported.Add(type);
            }

            return supported;
        }

        /************************************************************************************************************************/

        private static void HandleTargetFieldDragAndDrop(Rect area, ref bool autoOpenMethodMenu)
        {
            // Drag and drop objects into the type field.
            switch (Event.current.type)
            {
                case EventType.Repaint:
                case EventType.DragUpdated:
                    {
                        if (!area.Contains(Event.current.mousePosition))
                            break;

                        var dragging = DragAndDrop.objectReferences;
                        if (dragging != null && dragging.Length == 1)
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        }
                    }
                    break;

                case EventType.DragPerform:
                    {
                        if (!area.Contains(Event.current.mousePosition))
                            break;

                        var dragging = DragAndDrop.objectReferences;
                        if (dragging != null && dragging.Length == 1)
                        {
                            SetBestTarget(
                                DrawerState.Current.TargetProperty.objectReferenceValue,
                                dragging[0],
                                out autoOpenMethodMenu);

                            DragAndDrop.AcceptDrag();
                            GUI.changed = true;
                        }
                    }
                    break;

                default:
                    break;
            }
        }

        /************************************************************************************************************************/

        private static void SetBestTarget(Object oldTarget, Object newTarget, out bool autoOpenMethodMenu)
        {
            // It's more likely that the user intends to target a method on a Component than the GameObject itself so
            // if a GameObject was dropped in, try to select a component with the same type as the old target,
            // otherwise select it's first component after the Transform.
            var gameObject = newTarget as GameObject;
            if (oldTarget is not GameObject && !ReferenceEquals(gameObject, null))
            {
                var oldComponent = oldTarget as Component;
                if (!ReferenceEquals(oldComponent, null))
                {
                    newTarget = gameObject.GetComponent(oldComponent.GetType());
                    if (newTarget != null)
                        goto FoundTarget;
                }

                var components = gameObject.GetComponents<Component>();
                newTarget = components.Length > 1 ? components[1] : components[0];
            }

            FoundTarget:

            SetTarget(newTarget);

            autoOpenMethodMenu =
                BoolPref.AutoOpenMenu &&
                newTarget != null &&
                DrawerState.Current.call.GetMemberSafe() == null;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/

        private static void DoMemberFieldGUI(Rect area, MemberInfo member, bool autoOpenMethodMenu)
        {
            EditorGUI.BeginProperty(area, null, DrawerState.Current.MemberNameProperty);
            {
                if (includeRemoveButton)
                    area.width -= RemoveButtonWidth;

                var color = GUI.color;

                string label;
                if (EditorGUI.showMixedValue)
                {
                    label = "Mixed Values";
                }
                else if (member != null)
                {
                    label = MemberSelectionMenu.GetSignature(member, false);

                    DoGetSetToggleGUI(ref area, member);
                }
                else
                {
                    var methodName = DrawerState.Current.MemberNameProperty.stringValue;

                    PersistentCall.GetMemberDetails(
                        methodName,
                        DrawerState.Current.TargetProperty.objectReferenceValue,
                        out var declaringType,
                        out label);

                    DoMethodNameSuggestionGUI(ref area, declaringType, methodName);

                    GUI.color = ErrorFieldColor;
                }

                if (autoOpenMethodMenu ||
                    (GUI.Button(area, GUIContent.none, PopupButtonStyle) && Event.current.button == 0))
                {
                    MemberSelectionMenu.ShowMenu(area);
                }

                GUI.color = color;

                PopupLabelStyle.fontStyle = DrawerState.Current.MemberNameProperty.prefabOverride
                    ? FontStyle.Bold
                    : FontStyle.Normal;

                GUI.Label(area, label, PopupLabelStyle);
            }
            EditorGUI.EndProperty();
        }

        /************************************************************************************************************************/

        private static float _GetSetWidth;

        private static float GetSetWidth
        {
            get
            {
                if (_GetSetWidth <= 0)
                {
                    ArgumentLabel.text = "Get";
                    GUI.skin.button.CalcMinMaxWidth(ArgumentLabel, out var _, out var width);

                    ArgumentLabel.text = "Set";
                    GUI.skin.button.CalcMinMaxWidth(ArgumentLabel, out _, out _GetSetWidth);

                    if (_GetSetWidth < width)
                        _GetSetWidth = width;
                }

                return _GetSetWidth;
            }
        }

        /************************************************************************************************************************/

        private static void DoGetSetToggleGUI(ref Rect area, MemberInfo member)
        {
            if (member is MethodBase method)
                DoGetSetToggleGUI(ref area, method);
            else if (member is FieldInfo field)
                DoGetSetToggleGUI(ref area, field);
        }

        /************************************************************************************************************************/

        private static void DoGetSetToggleGUI(ref Rect area, MethodBase method)
        {
            // Check if the method name starts with "get_" or "set_".
            // Check the underscore first since it's hopefully the rarest so it can break out early.

            var name = method.Name;
            if (name.Length <= 4 || name[3] != '_' || name[2] != 't' || name[1] != 'e')
                return;

            var first = name[0];
            var isGet = first == 'g';
            var isSet = first == 's';
            if (!isGet && !isSet)
                return;

            var methodName = (isGet ? "set_" : "get_") + name[4..];
            var oppositePropertyMethod = method.DeclaringType.GetMethod(methodName, UltEventUtils.AnyAccessBindings);
            if (oppositePropertyMethod == null ||
                (isGet && !MemberSelectionMenu.IsSupported(method.GetReturnType())))
                return;

            area.width -= GetSetWidth + Padding;

            var buttonArea = new Rect(
                area.x + area.width + Padding,
                area.y,
                GetSetWidth,
                area.height);

            if (GUI.Button(buttonArea, isGet ? "Get" : "Set"))
            {
                var cachedState = new DrawerState();
                cachedState.CopyFrom(DrawerState.Current);

                EditorApplication.delayCall += () =>
                {
                    DrawerState.Current.CopyFrom(cachedState);

                    SetMethod(oppositePropertyMethod);

                    DrawerState.Current.Clear();

                    InternalEditorUtility.RepaintAllViews();
                };
            }
        }

        /************************************************************************************************************************/

        private static void DoGetSetToggleGUI(ref Rect area, FieldInfo field)
        {
            var call = DrawerState.Current.Event.PersistentCallsList[DrawerState.Current.callIndex];
            var isGet = call.IsGetter;

            // Can't change to Set if the field type is unsupported.
            if (isGet && !MemberSelectionMenu.IsSupported(field.FieldType))
                return;

            area.width -= GetSetWidth + Padding;

            var buttonArea = new Rect(
                area.x + area.width + Padding,
                area.y,
                GetSetWidth,
                area.height);

            if (GUI.Button(buttonArea, isGet ? "Get" : "Set"))
            {
                var cachedState = new DrawerState();
                cachedState.CopyFrom(DrawerState.Current);

                EditorApplication.delayCall += () =>
                {
                    DrawerState.Current.CopyFrom(cachedState);

                    DrawerState.Current.CallProperty.ModifyValues<PersistentCall>(call =>
                    {
                        call?.SetField(field, DrawerState.Current.TargetProperty.objectReferenceValue, !isGet);
                    }, "Set Field");

                    DrawerState.Current.Clear();

                    InternalEditorUtility.RepaintAllViews();
                };
            }
        }

        /************************************************************************************************************************/

        private static void DoMethodNameSuggestionGUI(ref Rect area, Type declaringType, string methodName)
        {
            if (declaringType == null ||
                string.IsNullOrEmpty(methodName))
                return;

            var lastDot = methodName.LastIndexOf('.');
            if (lastDot >= 0)
            {
                lastDot++;
                if (lastDot >= methodName.Length)
                    return;

                methodName = methodName[lastDot..];
            }

            var methods = declaringType.GetMethods(UltEventUtils.AnyAccessBindings);
            if (methods.Length == 0)
                return;

            area.width -= LineHeight + Padding;

            var buttonArea = new Rect(
                area.x + area.width + Padding,
                area.y,
                LineHeight,
                area.height);

            if (GUI.Button(buttonArea, MethodNameSuggestionLabel))
            {
                var cachedState = new DrawerState();
                cachedState.CopyFrom(DrawerState.Current);

                EditorApplication.delayCall += () =>
                {
                    DrawerState.Current.CopyFrom(cachedState);

                    var bestMethod = methods[0];
                    var bestDistance = UltEventUtils.CalculateLevenshteinDistance(methodName, bestMethod.Name);

                    var i = 1;
                    for (; i < methods.Length; i++)
                    {
                        var method = methods[i];
                        var distance = UltEventUtils.CalculateLevenshteinDistance(methodName, method.Name);

                        if (bestDistance > distance)
                        {
                            bestDistance = distance;
                            bestMethod = method;
                        }
                    }

                    SetMethod(bestMethod);

                    DrawerState.Current.Clear();

                    InternalEditorUtility.RepaintAllViews();
                };
            }
        }

        /************************************************************************************************************************/

        public static void SetTarget(Object target)
        {
            DrawerState.Current.TargetProperty.objectReferenceValue = target;
            DrawerState.Current.TargetProperty.serializedObject.ApplyModifiedProperties();

            if (target == null ||
                DrawerState.Current.call.GetMemberSafe() == null)
            {
                SetMethod(null);
            }
        }

        /************************************************************************************************************************/

        public static void SetMethod(MethodInfo methodInfo)
        {
            DrawerState.Current.CallProperty.ModifyValues<PersistentCall>(call =>
            {
                call?.SetMethod(methodInfo, DrawerState.Current.TargetProperty.objectReferenceValue);
            }, "Set Method");
        }

        /************************************************************************************************************************/
        #region Remove Button
        /************************************************************************************************************************/

        public const float RemoveButtonWidth = 18;

        public static bool includeRemoveButton;

        /************************************************************************************************************************/

        public static bool DoRemoveButtonGUI(Rect rowArea)
        {
            includeRemoveButton = false;

            rowArea.xMin = rowArea.xMax - RemoveButtonWidth + 2;
            rowArea.height = LineHeight + 2;

            return GUI.Button(
                rowArea,
                ReorderableList.defaultBehaviours.iconToolbarMinus,
                ReorderableList.defaultBehaviours.preButton);
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif