// UltEvents // https://kybernetik.com.au/ultevents // Copyright 2021-2024 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

namespace UltEvents.Editor
{
    /// <summary>[Editor-Only] Manages the GUI state used when drawing events.</summary>
    internal sealed class DrawerState
    {
        /************************************************************************************************************************/

        /// <summary>The currently active state.</summary>
        public static readonly DrawerState Current = new();

        /************************************************************************************************************************/

        /// <summary>The <see cref="SerializedProperty"/> for the event currently being drawn.</summary>
        public SerializedProperty EventProperty { get; private set; }

        /// <summary>The event currently being drawn.</summary>
        public UltEventBase Event { get; private set; }

        /************************************************************************************************************************/

        /// <summary>The <see cref="SerializedProperty"/> for the call currently being drawn.</summary>
        public SerializedProperty CallProperty { get; private set; }

        /// <summary>The <see cref="SerializedProperty"/> for the target of the call currently being drawn.</summary>
        public SerializedProperty TargetProperty { get; private set; }

        /// <summary>The <see cref="SerializedProperty"/> for the method name of the call currently being drawn.</summary>
        public SerializedProperty MemberNameProperty { get; private set; }

        /// <summary>The <see cref="SerializedProperty"/> for the persistent arguments array of the call currently being drawn.</summary>
        public SerializedProperty PersistentArgumentsProperty { get; private set; }

        /// <summary>The index of the call currently being drawn.</summary>
        public int callIndex = -1;

        /// <summary>The call currently being drawn.</summary>
        public PersistentCall call;

        /// <summary>The parameters of the call currently being drawn.</summary>
        public ParameterInfo[] callParameters;

        /// <summary>The call currently being drawn.</summary>
        public FieldInfo currentField;

        /// <summary>The index of the parameter currently being drawn.</summary>
        public int parameterIndex;

        /************************************************************************************************************************/

        /// <summary>If true, each call will be stored so that subsequent calls can link to their return value.</summary>
        public bool CachePreviousCalls { get; private set; }

        /// <summary>The calls of the current event that come before the current call currently being drawn.</summary>
        private readonly List<PersistentCall> PreviousCalls = new();

        /// <summary>The <see cref="PersistentCall.Member"/>s of the event currently being drawn.</summary>
        private readonly List<MemberInfo> PersistentMemberCache = new();

        /************************************************************************************************************************/

        /// <summary>The parameter currently being drawn.</summary>
        public ParameterInfo CurrentParameter
            => callParameters != null
            ? callParameters[parameterIndex]
            : null;

        /// <summary>The type of the parameter currently being drawn.</summary>
        public Type CurrentParameterType
            => callParameters != null
            ? callParameters[parameterIndex].ParameterType
            : currentField?.FieldType;

        /************************************************************************************************************************/

        /// <summary>Caches the event from the specified property and returns true as long as it is not null.</summary>
        public bool TryBeginEvent(SerializedProperty eventProperty)
        {
            Event = eventProperty.GetValue<UltEventBase>();
            if (Event == null)
                return false;

            EventProperty = eventProperty;
            return true;
        }

        /// <summary>Cancels out a call to <see cref="TryBeginEvent"/>.</summary>
        public void EndEvent()
        {
            EventProperty = null;
            Event = null;
        }

        /************************************************************************************************************************/

        /// <summary>Starts caching calls so that subsequent calls can link to earlier return values.</summary>
        public void BeginCache()
        {
            CacheLinkedArguments();
            CachePreviousCalls = true;
        }

        /// <summary>Cancels out a call to <see cref="EndCache"/>.</summary>
        public void EndCache()
        {
            CachePreviousCalls = false;
            PreviousCalls.Clear();
        }

        /************************************************************************************************************************/

        /// <summary>Caches the call from the specified property.</summary>
        public void BeginCall(SerializedProperty callProperty)
        {
            CallProperty = callProperty;

            TargetProperty = GetTargetProperty(callProperty);
            MemberNameProperty = GetMemberNameProperty(callProperty);
            PersistentArgumentsProperty = GetPersistentArgumentsProperty(callProperty);

            call = GetCall(callProperty);
        }

        /// <summary>Cancels out a call to <see cref="BeginCall"/>.</summary>
        public void EndCall()
        {
            if (CachePreviousCalls)
                PreviousCalls.Add(call);

            call = null;
        }

        /************************************************************************************************************************/

        /// <summary>Returns the property encapsulating the <see cref="PersistentCall.Target"/>.</summary>
        public static SerializedProperty GetTargetProperty(SerializedProperty callProperty)
            => callProperty.FindPropertyRelative(Names.PersistentCall.Target);

        /// <summary>Returns the property encapsulating the <see cref="PersistentCall.MethodName"/>.</summary>
        public static SerializedProperty GetMemberNameProperty(SerializedProperty callProperty)
            => callProperty.FindPropertyRelative(Names.PersistentCall.MemberName);

        /// <summary>Returns the property encapsulating the <see cref="PersistentCall.PersistentArguments"/>.</summary>
        public static SerializedProperty GetPersistentArgumentsProperty(SerializedProperty callProperty)
            => callProperty.FindPropertyRelative(Names.PersistentCall.PersistentArguments);

        /// <summary>Returns the call encapsulated by the specified property.</summary>
        public static PersistentCall GetCall(SerializedProperty callProperty)
            => callProperty.GetValue<PersistentCall>();

        /************************************************************************************************************************/
        #region Linked Argument Cache
        /************************************************************************************************************************/

        /// <summary>Stores all the persistent methods in the current event.</summary>
        public void CacheLinkedArguments()
        {
            PersistentMemberCache.Clear();

            if (Event == null || Event._PersistentCalls == null)
                return;

            for (int i = 0; i < Event._PersistentCalls.Count; i++)
                PersistentMemberCache.Add(Event._PersistentCalls[i]?.GetMemberSafe());
        }

        /************************************************************************************************************************/

        /// <summary>Ensures that any linked parameters remain linked to the correct target index.</summary>
        /// <remarks>Call this after the call list is reordered or a call is removed.</remarks>
        public void UpdateLinkedArguments()
        {
            if (Event == null ||
                PersistentMemberCache.Count == 0)
                return;

            for (int i = 0; i < Event._PersistentCalls.Count; i++)
            {
                var call = Event._PersistentCalls[i];
                if (call == null)
                    continue;

                for (int j = 0; j < call._PersistentArguments.Length; j++)
                {
                    var argument = call._PersistentArguments[j];
                    if (argument == null || argument._Type != PersistentArgumentType.ReturnValue)
                        continue;

                    var linkedMember = PersistentMemberCache[argument.ReturnedValueIndex];

                    if (argument.ReturnedValueIndex < Event._PersistentCalls.Count)
                    {
                        var linkedCall = Event._PersistentCalls[argument.ReturnedValueIndex];
                        if (linkedMember == linkedCall?.GetMemberSafe())
                            continue;
                    }

                    var index = IndexOfMember(linkedMember);
                    if (index >= 0)
                        argument.ReturnedValueIndex = index;
                }
            }

            PersistentMemberCache.Clear();
        }

        /************************************************************************************************************************/

        /// <summary>Returns the index of the persistent call that targets the specified `method` or -1 if there is none.</summary>
        public int IndexOfMember(MemberInfo member)
        {
            for (int i = 0; i < Event._PersistentCalls.Count; i++)
                if (Event._PersistentCalls[i]?.GetMemberSafe() == member)
                    return i;

            return -1;
        }

        /************************************************************************************************************************/

        /// <summary>Returns the member cached from the persistent call at the specified `index`.</summary>
        public MemberInfo GetLinkedMember(int index)
            => index >= 0 && index < PersistentMemberCache.Count
            ? PersistentMemberCache[index]
            : null;

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Previous Call Cache
        /************************************************************************************************************************/

        static DrawerState()
        {
            PersistentArgument.TryGetLinkable =
                (Type type, out int linkIndex, out PersistentArgumentType linkType)
                => Current.TryGetLinkable(type, out linkIndex, out linkType);
        }

        /// <summary>Tries to get the details of the a parameter or return value of the specified `type`.</summary>
        public bool TryGetLinkable(Type type, out int linkIndex, out PersistentArgumentType linkType)
        {
            if (type != null && Event != null)
            {
                // Parameters.
                var parameterTypes = Event.ParameterTypes;
                for (int i = 0; i < parameterTypes.Length; i++)
                {
                    if (type.IsAssignableFrom(parameterTypes[i]))
                    {
                        linkIndex = i;
                        linkType = PersistentArgumentType.Parameter;
                        return true;
                    }
                }

                // Return Values.
                for (int i = 0; i < PreviousCalls.Count; i++)
                {
                    var returnType = PreviousCalls[i].GetReturnType();
                    if (returnType == null)
                        continue;

                    if (type.IsAssignableFrom(returnType))
                    {
                        linkIndex = i;
                        linkType = PersistentArgumentType.ReturnValue;
                        return true;
                    }
                }
            }

            linkIndex = -1;
            linkType = PersistentArgumentType.None;
            return false;
        }

        /************************************************************************************************************************/

        /// <summary>Tries to get the details of the a parameter or return value of the current parameter type.</summary>
        public bool TryGetLinkable(out int linkIndex, out PersistentArgumentType linkType)
            => TryGetLinkable(CurrentParameterType, out linkIndex, out linkType);

        /************************************************************************************************************************/

        /// <summary>The number of persistent calls that came earlier in the current event.</summary>
        public int PreviousCallCount
            => PreviousCalls.Count;

        /************************************************************************************************************************/

        /// <summary>Returns the persistent call at the specified index in the current event.</summary>
        public PersistentCall GetPreviousCall(int index)
            => index >= 0 && index < PreviousCalls.Count
            ? PreviousCalls[index]
            : null;

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/

        /// <summary>Copies the contents of the `other` state to overwrite this one.</summary>
        public void CopyFrom(DrawerState other)
        {
            EventProperty = other.EventProperty;
            Event = other.Event;

            CallProperty = other.CallProperty;
            TargetProperty = other.TargetProperty;
            MemberNameProperty = other.MemberNameProperty;
            PersistentArgumentsProperty = other.PersistentArgumentsProperty;

            callIndex = other.callIndex;
            call = other.call;
            callParameters = other.callParameters;
            parameterIndex = other.parameterIndex;

            PreviousCalls.Clear();
            PreviousCalls.AddRange(other.PreviousCalls);
        }

        /************************************************************************************************************************/

        /// <summary>Clears all the details of this state.</summary>
        public void Clear()
        {
            EventProperty = null;
            Event = null;

            CallProperty = null;
            TargetProperty = null;
            MemberNameProperty = null;
            PersistentArgumentsProperty = null;

            callIndex = -1;
            call = null;
            callParameters = null;
            parameterIndex = 0;

            PreviousCalls.Clear();
        }

        /************************************************************************************************************************/
    }
}

#endif
