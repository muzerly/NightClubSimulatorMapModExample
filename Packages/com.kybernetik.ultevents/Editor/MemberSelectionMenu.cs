// UltEvents // https://kybernetik.com.au/ultevents // Copyright 2021-2024 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UltEvents.Editor
{
    /// <summary>[Editor-Only]
    /// Manages the construction of menus for selecting methods for <see cref="PersistentCall"/>s.
    /// </summary>
    internal static class MemberSelectionMenu
    {
        /************************************************************************************************************************/
        #region Fields
        /************************************************************************************************************************/

        /// <summary>
        /// The drawer state from when the menu was opened which needs to be restored when a method is selected because
        /// menu items are executed after the frame finishes and the drawer state is cleared.
        /// </summary>
        private static readonly DrawerState
            CachedState = new();

        private static AdvancedDropdownState
            _DropdownState;

        private static readonly StringBuilder
            LabelBuilder = new();

        // These fields should really be passed around as parameters,
        // but they make all the method signatures annoyingly long
        // so it's easier to just have them here.
        private static MemberInfo _CurrentMember;
        private static BindingFlags _Bindings;
        private static PickerMenu _Menu;

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Entry Point
        /************************************************************************************************************************/

        /// <summary>Opens the menu near the specified `area`.</summary>
        public static void ShowMenu(Rect area)
        {
            CachedState.CopyFrom(DrawerState.Current);

            _CurrentMember = CachedState.call.GetMemberSafe();
            _Bindings = GetBindingFlags();

            _Menu = new(ref _DropdownState, "Pick a Field or Method", BoolPref.ContextMenuStyle);

            BoolPref.AddDisplayOptions(_Menu);

            var targets = GetObjectReferences(CachedState.TargetProperty, out var targetObjects);

            AddCoreItems(targets);

            // Populate the main contents of the menu.
            {
                if (targets == null)
                {
                    var serializedMethodName = CachedState.MemberNameProperty.stringValue;
                    PersistentCall.GetMemberDetails(
                        serializedMethodName,
                        null,
                        out var declaringType,
                        out var methodName);

                    // If we have no target, but do have a type, populate the menu with that type's statics.
                    if (declaringType != null)
                    {
                        PopulateMenuWithStatics(targetObjects, declaringType);

                        goto ShowMenu;
                    }
                    else// If we have no type either, pretend the inspected objects are the targets.
                    {
                        targets = targetObjects;
                    }
                }

                // Ensure that all targets share the same type.
                var firstTarget = ValidateTargetsAndGetFirst(targets);
                if (firstTarget == null)
                {
                    targets = targetObjects;
                    firstTarget = targets[0];
                }

                // Add menu items according to the type of the target.
                if (firstTarget is GameObject)
                    PopulateMenuForGameObject("", false, targets);
                else if (firstTarget is Component)
                    PopulateMenuForComponent(targets);
                else
                    PopulateMenuForObject(targets);
            }

            ShowMenu:

            _Menu.ShowContext(area);

            GC.Collect();
        }

        /************************************************************************************************************************/

        private static BindingFlags GetBindingFlags()
        {
            var bindings = BindingFlags.Public | BindingFlags.Instance;

            if (BoolPref.ShowNonPublicMethods)
                bindings |= BindingFlags.NonPublic;

            if (BoolPref.ShowStaticMethods)
                bindings |= BindingFlags.Static;

            return bindings;
        }

        /************************************************************************************************************************/

        private static void AddCoreItems(Object[] targets)
        {
            AddNullItem(targets);
            AddStaticItem(targets);

            _Menu.AddSeparator("");
        }

        /************************************************************************************************************************/

        private static void AddNullItem(Object[] targets)
        {
            _Menu.AddItem("Null", _CurrentMember == null, () =>
            {
                DrawerState.Current.CopyFrom(CachedState);

                if (targets != null)
                {
                    PersistentCallDrawer.SetMethod(null);
                }
                else
                {
                    // For a static method, remove the method name but keep the declaring type.
                    var methodName = CachedState.MemberNameProperty.stringValue;
                    var lastDot = methodName.LastIndexOf('.');
                    if (lastDot < 0)
                        CachedState.MemberNameProperty.stringValue = null;
                    else
                        CachedState.MemberNameProperty.stringValue = methodName[..(lastDot + 1)];

                    CachedState.PersistentArgumentsProperty.arraySize = 0;

                    CachedState.MemberNameProperty.serializedObject.ApplyModifiedProperties();
                }

                DrawerState.Current.Clear();
            });
        }

        /************************************************************************************************************************/

        private static void AddStaticItem(Object[] targets)
        {

            var isStatic =
                (_CurrentMember is MethodBase method && method.IsStatic) ||
                (_CurrentMember is FieldInfo field && field.IsStatic);

            if (targets != null && !isStatic)
            {
                _Menu.AddItem("Static Member", isStatic, () =>
                {
                    DrawerState.Current.CopyFrom(CachedState);

                    PersistentCallDrawer.SetTarget(null);

                    DrawerState.Current.Clear();
                });
            }
        }

        /************************************************************************************************************************/

        private static Object[] GetObjectReferences(SerializedProperty property, out Object[] targetObjects)
        {
            targetObjects = property.serializedObject.targetObjects;

            if (property.hasMultipleDifferentValues)
            {
                var references = new Object[targetObjects.Length];
                for (int i = 0; i < references.Length; i++)
                {
                    using (var serializedObject = new SerializedObject(targetObjects[i]))
                    {
                        references[i] = serializedObject.FindProperty(property.propertyPath).objectReferenceValue;
                    }
                }
                return references;
            }
            else
            {
                var target = property.objectReferenceValue;
                return target != null
                    ? new Object[] { target }
                    : null;
            }

        }

        /************************************************************************************************************************/

        private static Object ValidateTargetsAndGetFirst(Object[] targets)
        {
            var firstTarget = targets[0];
            if (firstTarget == null)
                return null;

            var targetType = firstTarget.GetType();

            // Make sure all targets have the exact same type.
            // Unfortunately supporting inheritance would be more complicated.

            var i = 1;
            for (; i < targets.Length; i++)
            {
                var obj = targets[i];
                if (obj == null || obj.GetType() != targetType)
                {
                    return null;
                }
            }

            return firstTarget;
        }

        /************************************************************************************************************************/

        private static T[] GetRelatedObjects<T>(Object[] objects, Func<Object, T> getRelatedObject)
        {
            var relatedObjects = new T[objects.Length];

            for (int i = 0; i < relatedObjects.Length; i++)
            {
                relatedObjects[i] = getRelatedObject(objects[i]);
            }

            return relatedObjects;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Populate for Objects
        /************************************************************************************************************************/

        private static void PopulateMenuWithStatics(Object[] targets, Type type)
        {
            var firstTarget = targets[0];
            var component = firstTarget as Component;
            if (!ReferenceEquals(component, null))
            {
                var gameObjects = GetRelatedObjects(targets, (target) => (target as Component).gameObject);
                PopulateMenuForGameObject("", true, gameObjects);
            }
            else
            {
                PopulateMenuForObject(firstTarget.GetType().GetNameCS(BoolPref.ShowFullTypeNames) + " ->/", targets);
            }

            _Menu.AddSeparator("");

            var bindings = BindingFlags.Static | BindingFlags.Public;
            if (BoolPref.ShowNonPublicMethods)
                bindings |= BindingFlags.NonPublic;

            PopulateMenuWithMembers(type, bindings, "", null);
        }

        /************************************************************************************************************************/

        private static void PopulateMenuForGameObject(string prefix, bool putGameObjectInSubMenu, Object[] targets)
        {
            var header = prefix + "Selected GameObject and its Components";

            var gameObjectPrefix = prefix;
            if (putGameObjectInSubMenu)
            {
                _Menu.AddDisabledItem(header);
                gameObjectPrefix += "GameObject ->/";
            }

            PopulateMenuForObject(gameObjectPrefix, targets);

            if (!putGameObjectInSubMenu)
            {
                _Menu.AddSeparator(prefix);
                _Menu.AddDisabledItem(header);
            }

            var gameObjects = GetRelatedObjects(targets, (target) => target as GameObject);
            PopulateMenuForComponents(prefix, gameObjects);
        }

        /************************************************************************************************************************/

        private static void PopulateMenuForComponents(string prefix, GameObject[] gameObjects)
        {
            var firstGameObject = gameObjects[0];
            var components = firstGameObject.GetComponents<Component>();

            for (int i = 0; i < components.Length; i++)
            {
                var component = components[i];

                var targets = new Object[gameObjects.Length];
                targets[0] = component;

                var typeIndex = GetComponentTypeIndex(component, components, out var type);

                GetComponent(firstGameObject, type, typeIndex, out var minTypeCount, out var unused);

                var j = 1;
                for (; j < gameObjects.Length; j++)
                {
                    GetComponent(gameObjects[j], type, typeIndex, out var typeCount, out var targetComponent);
                    if (typeCount <= typeIndex)
                        goto NextComponent;

                    targets[j] = targetComponent;

                    if (minTypeCount > typeCount)
                        minTypeCount = typeCount;
                }

                var name = type.GetNameCS(BoolPref.ShowFullTypeNames) + " ->/";

                if (minTypeCount > 1)
                    name = UltEventUtils.GetPlacementName(typeIndex) + " " + name;

                PopulateMenuForObject(prefix + name, targets);
            }

            NextComponent:;
        }

        private static int GetComponentTypeIndex(Component component, Component[] components, out Type type)
        {
            type = component.GetType();

            var count = 0;

            for (int i = 0; i < components.Length; i++)
            {
                var c = components[i];
                if (c == component)
                    break;
                else if (c.GetType() == type)
                    count++;
            }

            return count;
        }

        private static void GetComponent(GameObject gameObject, Type type, int targetIndex, out int numberOfComponentsOfType, out Component targetComponent)
        {
            numberOfComponentsOfType = 0;
            targetComponent = null;

            var components = gameObject.GetComponents(type);
            for (int i = 0; i < components.Length; i++)
            {
                var component = components[i];
                if (component.GetType() == type)
                {
                    if (numberOfComponentsOfType == targetIndex)
                        targetComponent = component;

                    numberOfComponentsOfType++;
                }
            }
        }

        /************************************************************************************************************************/

        private static void PopulateMenuForComponent(Object[] targets)
        {
            var gameObjects = GetRelatedObjects(targets, (target) => (target as Component).gameObject);

            PopulateMenuForGameObject("", true, gameObjects);
            _Menu.AddSeparator("");

            PopulateMenuForObject(targets);
        }

        /************************************************************************************************************************/

        private static void PopulateMenuForObject(Object[] targets)
            => PopulateMenuForObject("", targets);

        private static void PopulateMenuForObject(string prefix, Object[] targets)
            => PopulateMenuWithMembers(targets[0].GetType(), _Bindings, prefix, targets);

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Populate for Types
        /************************************************************************************************************************/

        private static void PopulateMenuWithMembers(Type type, BindingFlags bindings, string prefix, Object[] targets)
        {
            var members = GetSortedMembers(type, bindings);
            var previousDeclaringType = type;

            var firstSeparator = true;
            var firstField = true;
            var firstProperty = true;
            var firstMethod = true;
            var firstBaseType = true;
            var nameMatchesNextMethod = false;

            var i = 0;
            while (i < members.Count)
            {
                var member = GetNextSupportedMember(members, ref i, out var parameters, out var getter);

                GotMember:

                if (member == null)
                    return;

                i++;

                if (BoolPref.SubMenuForEachBaseType)
                {
                    if (firstBaseType && member.DeclaringType != type)
                    {
                        if (firstSeparator)
                            firstSeparator = false;
                        else
                            _Menu.AddSeparator(prefix);

                        var baseTypesOf = "Base Types of " + type.GetNameCS();
                        if (BoolPref.SubMenuForBaseTypes)
                        {
                            prefix += baseTypesOf + " ->/";
                        }
                        else
                        {
                            _Menu.AddDisabledItem(prefix + baseTypesOf);
                        }
                        firstField = false;
                        firstProperty = false;
                        firstMethod = false;
                        firstBaseType = false;
                    }

                    if (previousDeclaringType != member.DeclaringType)
                    {
                        previousDeclaringType = member.DeclaringType;
                        firstField = true;
                        firstProperty = true;
                        firstMethod = true;
                        firstSeparator = true;
                    }
                }

                switch (member)
                {
                    case FieldInfo field:
                        AppendGroupHeader(
                            prefix, "Fields in ", member.DeclaringType, type, ref firstField, ref firstSeparator);

                        AddSelectFieldItem(prefix, targets, type, field);
                        continue;

                    case PropertyInfo property:
                        AppendGroupHeader(
                            prefix, "Properties in ", member.DeclaringType, type, ref firstProperty, ref firstSeparator);

                        AddSelectPropertyItem(prefix, targets, type, property, getter);
                        continue;

                    case MethodBase method:
                        {
                            AppendGroupHeader(
                                prefix, "Methods in ", member.DeclaringType, type, ref firstMethod, ref firstSeparator);

                            // Check if the method name matched the previous or next method to group them.
                            if (BoolPref.GroupMethodOverloads)
                            {
                                var nameMatchedPreviousMethod = nameMatchesNextMethod;

                                var nextMember = GetNextSupportedMember(
                                    members, ref i, out var nextParameters, out var nextGetter);

                                nameMatchesNextMethod = nextMember != null && method.Name == nextMember.Name;

                                if (nameMatchedPreviousMethod || nameMatchesNextMethod)
                                {
                                    AddSelectMethodItem(prefix, targets, type, true, method, parameters);

                                    if (i < members.Count)
                                    {
                                        member = nextMember;
                                        parameters = nextParameters;
                                        getter = nextGetter;
                                        goto GotMember;
                                    }
                                    else
                                    {
                                        return;
                                    }
                                }
                            }

                            // Otherwise just build the label normally.
                            AddSelectMethodItem(prefix, targets, type, false, method, parameters);
                            break;
                        }
                }
            }
        }

        /************************************************************************************************************************/

        private static void AppendGroupHeader(string prefix, string name, Type declaringType, Type currentType, ref bool firstInGroup, ref bool firstSeparator)
        {
            if (firstInGroup)
            {
                LabelBuilder.Length = 0;
                LabelBuilder.Append(prefix);

                if (BoolPref.SubMenuForEachBaseType && declaringType != currentType)
                    AppendDeclaringTypeSubMenu(LabelBuilder, declaringType, currentType);

                if (firstSeparator)
                    firstSeparator = false;
                else
                    _Menu.AddSeparator(LabelBuilder.ToString());

                LabelBuilder.Append(name);

                if (BoolPref.SubMenuForEachBaseType)
                    LabelBuilder.Append(declaringType.GetNameCS());
                else
                    LabelBuilder.Append(currentType.GetNameCS());

                _Menu.AddDisabledItem(LabelBuilder.ToString());
                firstInGroup = false;
            }
        }

        private static void AppendDeclaringTypeSubMenu(StringBuilder text, Type declaringType, Type currentType)
        {
            if (BoolPref.SubMenuForEachBaseType)
            {
                if (BoolPref.SubMenuForRootBaseType || declaringType != currentType)
                {
                    text.Append(declaringType.GetNameCS());
                    text.Append(" ->/");
                }
            }
        }

        /************************************************************************************************************************/

        private static void AddSelectFieldItem(
            string prefix,
            Object[] targets,
            Type currentType,
            FieldInfo field)
        {
            LabelBuilder.Length = 0;
            LabelBuilder.Append(prefix);

            // Declaring Type.
            AppendDeclaringTypeSubMenu(LabelBuilder, field.DeclaringType, currentType);

            // Non-Public Grouping.
            if (BoolPref.GroupNonPublicMethods && !field.IsPublic)
                LabelBuilder.Append("Non-Public Fields ->/");

            // Property Type and Name.
            LabelBuilder.Append(field.FieldType.GetNameCS(BoolPref.ShowFullTypeNames));
            LabelBuilder.Append(' ');
            LabelBuilder.Append(field.Name);

            var label = LabelBuilder.ToString();
            AddSetCallItem(label, field, targets);
        }

        /************************************************************************************************************************/

        private static void AddSelectPropertyItem(
            string prefix,
            Object[] targets,
            Type currentType,
            PropertyInfo property,
            MethodInfo getter)
        {
            var defaultMethod = getter;

            MethodInfo setter = null;
            if (IsSupported(property.PropertyType))
            {
                setter = property.GetSetMethod(true);
                if (setter != null)
                    defaultMethod = setter;
            }

            LabelBuilder.Length = 0;
            LabelBuilder.Append(prefix);

            // Declaring Type.
            AppendDeclaringTypeSubMenu(LabelBuilder, property.DeclaringType, currentType);

            // Non-Public Grouping.
            if (BoolPref.GroupNonPublicMethods && !IsPublic(property))
                LabelBuilder.Append("Non-Public Properties ->/");

            // Property Type and Name.
            LabelBuilder.Append(property.PropertyType.GetNameCS(BoolPref.ShowFullTypeNames));
            LabelBuilder.Append(' ');
            LabelBuilder.Append(property.Name);

            // Get and Set.
            LabelBuilder.Append(" { ");
            if (getter != null) LabelBuilder.Append("get; ");
            if (setter != null) LabelBuilder.Append("set; ");
            LabelBuilder.Append('}');

            var label = LabelBuilder.ToString();
            AddSetCallItem(label, defaultMethod, targets);
        }

        /************************************************************************************************************************/

        private static void AddSelectMethodItem(
            string prefix,
            Object[] targets,
            Type currentType,
            bool methodNameSubMenu,
            MethodBase method,
            ParameterInfo[] parameters)
        {
            LabelBuilder.Length = 0;
            LabelBuilder.Append(prefix);

            // Declaring Type.
            AppendDeclaringTypeSubMenu(LabelBuilder, method.DeclaringType, currentType);

            // Non-Public Grouping.
            if (BoolPref.GroupNonPublicMethods && !method.IsPublic)
                LabelBuilder.Append("Non-Public Methods ->/");

            // Overload Grouping.
            if (methodNameSubMenu)
            {
                LabelBuilder
                    .Append(method.GetReturnType().GetNameCS(BoolPref.ShowFullTypeNames))
                    .Append(' ')
                    .Append(method.Name)
                    .Append(" ->/");

                AppendParameters(LabelBuilder, method.GetParameters(), true, true);
            }
            else// Regular Method Signature.
            {
                LabelBuilder.Append(GetMethodSignature(method, parameters, true));
            }

            var label = LabelBuilder.ToString();

            AddSetCallItem(label, method, targets);
        }

        /************************************************************************************************************************/

        private static void AddSetCallItem(string label, FieldInfo field, Object[] targets)
        {
            _Menu.AddItem(
                label,
                field == _CurrentMember,
                () =>
                {
                    DrawerState.Current.CopyFrom(CachedState);

                    var i = 0;
                    CachedState.CallProperty.ModifyValues<PersistentCall>((call) =>
                    {
                        var target = targets != null ? targets[i % targets.Length] : null;
                        call.SetField(field, target, true);
                        i++;
                    }, "Set Persistent Call");

                    DrawerState.Current.Clear();
                });
        }

        private static void AddSetCallItem(string label, MethodBase method, Object[] targets)
        {
            _Menu.AddItem(
                label,
                method == _CurrentMember,
                () =>
                {
                    DrawerState.Current.CopyFrom(CachedState);

                    var i = 0;
                    CachedState.CallProperty.ModifyValues<PersistentCall>((call) =>
                    {
                        var target = targets != null ? targets[i % targets.Length] : null;
                        call.SetMethod(method, target);
                        i++;
                    }, "Set Persistent Call");

                    DrawerState.Current.Clear();
                });
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Member Gathering
        /************************************************************************************************************************/

        private static readonly Dictionary<BindingFlags, Dictionary<Type, List<MemberInfo>>>
            MemberCache = new();

        internal static void ClearMemberCache()
            => MemberCache.Clear();

        /************************************************************************************************************************/

        private static List<MemberInfo> GetSortedMembers(Type type, BindingFlags bindings)
        {
            // Get the cache for the specified bindings.
            if (!MemberCache.TryGetValue(bindings, out var memberCache))
            {
                memberCache = new();
                MemberCache.Add(bindings, memberCache);
            }

            // If the members for the specified type aren't cached for those bindings, gather and sort them.
            if (!memberCache.TryGetValue(type, out var members))
            {
                var fields = type.GetFields(bindings);
                var properties = type.GetProperties(bindings);
                var methods = type.GetMethods(bindings);

                // When gathering static members, also include constructors.
                var constructors = ((bindings & BindingFlags.Static) == BindingFlags.Static) ?
                    type.GetConstructors((bindings & ~BindingFlags.Static) | BindingFlags.Instance) :
                    null;

                var capacity = properties.Length + methods.Length;
                if (constructors != null)
                    capacity += constructors.Length;

                members = new(capacity);
                members.AddRange(fields);
                members.AddRange(properties);
                if (constructors != null)
                    members.AddRange(constructors);
                members.AddRange(methods);

                // If the bindings include static, add static members from each base type.
                if ((bindings & BindingFlags.Static) == BindingFlags.Static && type.BaseType != null)
                {
                    members.AddRange(GetSortedMembers(type.BaseType, bindings & ~BindingFlags.Instance));
                }

                UltEventUtils.StableInsertionSort(members, CompareMembers);

                memberCache.Add(type, members);
            }

            return members;
        }

        /************************************************************************************************************************/

        private static int CompareMembers(MemberInfo a, MemberInfo b)
        {
            if (BoolPref.SubMenuForEachBaseType)
            {
                var result = CompareChildBeforeBase(a.DeclaringType, b.DeclaringType);
                if (result != 0)
                    return result;
            }

            // Fields.
            if (a is FieldInfo)
            {
                if (b is not FieldInfo)
                    return -1;
            }
            else
            {
                if (b is FieldInfo)
                    return 1;
            }

            // Properties.
            if (a is PropertyInfo)
            {
                if (b is not PropertyInfo)
                    return -1;
            }
            else
            {
                if (b is PropertyInfo)
                    return 1;
            }

            // Methods.

            // Non-Public Sub-Menu.
            if (BoolPref.GroupNonPublicMethods)
            {
                if (IsPublic(a))
                {
                    if (!IsPublic(b))
                        return -1;
                }
                else
                {
                    if (IsPublic(b))
                        return 1;
                }
            }

            // Compare names.
            return a.Name.CompareTo(b.Name);
        }

        /************************************************************************************************************************/

        private static int CompareChildBeforeBase(Type a, Type b)
        {
            if (a == b)
                return 0;

            while (true)
            {
                a = a.BaseType;

                if (a == null)
                    return 1;

                if (a == b)
                    return -1;
            }
        }

        /************************************************************************************************************************/

        private static readonly Dictionary<MemberInfo, bool>
            MemberToIsPublic = new();

        private static bool IsPublic(MemberInfo member)
        {
            if (!MemberToIsPublic.TryGetValue(member, out var isPublic))
            {
                switch (member.MemberType)
                {
                    case MemberTypes.Field:
                        isPublic = (member as FieldInfo).IsPublic;
                        break;

                    case MemberTypes.Property:
                        isPublic =
                            (member as PropertyInfo).GetGetMethod() != null ||
                            (member as PropertyInfo).GetSetMethod() != null;
                        break;

                    case MemberTypes.Constructor:
                    case MemberTypes.Method:
                        isPublic = (member as MethodBase).IsPublic;
                        break;

                    default:
                        throw new ArgumentException("Unhandled member type", "member");
                }

                MemberToIsPublic.Add(member, isPublic);
            }

            return isPublic;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Supported Checks
        /************************************************************************************************************************/

        private static bool IsSupported(FieldInfo field)
            => !field.IsSpecialName
            && !field.IsDefined(typeof(ObsoleteAttribute), true)
            && IsSupported(field.FieldType);

        private static bool IsSupported(PropertyInfo property, out MethodInfo getter)
        {
            if (property.IsSpecialName ||
                property.IsDefined(typeof(ObsoleteAttribute), true))// Obsolete.
            {
                getter = null;
                return false;
            }

            getter = property.GetGetMethod(true);
            if (getter == null && !IsSupported(property.PropertyType))
                return false;

            return true;
        }

        private static bool IsSupported(MethodBase method, out ParameterInfo[] parameters)
        {
            if (method.IsGenericMethod ||
                (method.IsSpecialName && (!method.IsConstructor || method.IsStatic)) ||
                method.Name.Contains("<") ||
                method.IsDefined(typeof(ObsoleteAttribute), true))
            {
                parameters = null;
                return false;
            }

            // Most UnityEngine.Object types shouldn't be constructed directly.
            if (method.IsConstructor)
            {
                if (typeof(Component).IsAssignableFrom(method.DeclaringType) ||
                    typeof(ScriptableObject).IsAssignableFrom(method.DeclaringType))
                {
                    parameters = null;
                    return false;
                }
            }

            parameters = method.GetParameters();
            if (!IsSupported(parameters))
                return false;

            return true;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns true if the specified `type` can be represented by a <see cref="PersistentArgument"/>.
        /// </summary>
        public static bool IsSupported(Type type)
            => PersistentCall.IsSupportedNative(type)
            || DrawerState.Current.TryGetLinkable(type, out var linkIndex, out var linkType);

        /// <summary>
        /// Returns true if the type of each of the `parameters` can be represented by a <see cref="PersistentArgument"/>.
        /// </summary>
        public static bool IsSupported(ParameterInfo[] parameters)
        {
            for (int i = 0; i < parameters.Length; i++)
                if (!IsSupported(parameters[i].ParameterType))
                    return false;

            return true;
        }

        /************************************************************************************************************************/

        private static MemberInfo GetNextSupportedMember(
            List<MemberInfo> members,
            ref int startIndex,
            out ParameterInfo[] parameters,
            out MethodInfo getter)
        {
            while (startIndex < members.Count)
            {
                var member = members[startIndex];

                switch (member)
                {
                    case FieldInfo field:
                        if (IsSupported(field))
                        {
                            parameters = null;
                            getter = null;
                            return member;
                        }
                        break;

                    case PropertyInfo property:
                        if (IsSupported(property, out getter))
                        {
                            parameters = null;
                            return member;
                        }
                        break;

                    case MethodBase method:
                        if (IsSupported(method, out parameters))
                        {
                            getter = null;
                            return member;
                        }
                        break;
                }

                startIndex++;
            }

            parameters = null;
            getter = null;
            return null;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Signatures
        /************************************************************************************************************************/

        private static readonly Dictionary<MemberInfo, string>
            SignaturesWithParameters = new(),
            SignaturesWithoutParameters = new();
        private static readonly StringBuilder
            SignatureBuilder = new();

        /************************************************************************************************************************/

        public static string GetSignature(MemberInfo member, bool includeParameterNames)
        {
            if (member == null)
                return null;

            var signatureCache = includeParameterNames
                ? SignaturesWithParameters
                : SignaturesWithoutParameters;

            if (!signatureCache.TryGetValue(member, out var signature))
            {
                if (member is MethodBase method)
                    signature = BuildSignature(method, method.GetParameters(), includeParameterNames);
                else if (member is FieldInfo field)
                    signature = BuildSignature(field);
                else
                    signature = "Unhandled Member Type: " + member.GetType().Name;

                signatureCache.Add(member, signature);
            }

            return signature;
        }

        /************************************************************************************************************************/

        public static string GetMethodSignature(
            MethodBase method,
            ParameterInfo[] parameters,
            bool includeParameterNames)
        {
            if (method == null)
                return null;

            var signatureCache = includeParameterNames
                ? SignaturesWithParameters
                : SignaturesWithoutParameters;

            if (!signatureCache.TryGetValue(method, out var signature))
            {
                signature = BuildSignature(method, parameters, includeParameterNames);
                signatureCache.Add(method, signature);
            }

            return signature;
        }

        /************************************************************************************************************************/

        private static string BuildSignature(
            MethodBase method,
            ParameterInfo[] parameters,
            bool includeParameterNames)
        {
            SignatureBuilder.Length = 0;

            SignatureBuilder.Append(method.GetReturnType().GetNameCS(false));
            SignatureBuilder.Append(' ');

            SignatureBuilder.Append(method.Name);

            SignatureBuilder.Append(' ');
            AppendParameters(SignatureBuilder, parameters, true, includeParameterNames);

            var signature = SignatureBuilder.ToString();
            SignatureBuilder.Length = 0;

            return signature;
        }

        private static string BuildSignature(
            FieldInfo field)
        {
            SignatureBuilder.Length = 0;

            SignatureBuilder.Append(field.FieldType.GetNameCS(false));
            SignatureBuilder.Append(' ');
            SignatureBuilder.Append(field.Name);

            var signature = SignatureBuilder.ToString();
            SignatureBuilder.Length = 0;

            return signature;
        }

        /************************************************************************************************************************/

        public static void AppendParameters(
            StringBuilder text,
            ParameterInfo[] parameters,
            bool includeTypes,
            bool includeNames)
        {
            text.Append('(');

            for (int i = 0; i < parameters.Length; i++)
            {
                if (i > 0)
                    text.Append(", ");

                var parameter = parameters[i];

                if (includeTypes)
                {
                    text.Append(parameter.ParameterType.GetNameCS(false));

                    if (includeNames)
                        text.Append(' ');
                }

                if (includeNames)
                    text.Append(parameter.Name);
            }

            text.Append(')');
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif
