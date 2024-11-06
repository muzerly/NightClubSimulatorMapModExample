// UltEvents // https://kybernetik.com.au/ultevents // Copyright 2021-2024 Kybernetik //

using System;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace UltEvents
{
    /// <summary>
    /// Encapsulates a delegate so it can be serialized for <see cref="UltEventBase"/>.
    /// </summary>
    [Serializable]
    public sealed class PersistentCall
#if UNITY_EDITOR
        : ISerializationCallbackReceiver
#endif
    {
        /************************************************************************************************************************/
        #region Fields and Properties
        /************************************************************************************************************************/

        [SerializeField]
        private Object _Target;

        /// <summary>The object on which the persistent method is called.</summary>
        public Object Target => _Target;

        /************************************************************************************************************************/

        /// <summary>The character used at the end of a <see cref="MemberName"/> to refer to a field.</summary>
        public const char FieldNameSuffix = '=';

        [SerializeField]
        [FormerlySerializedAs("_MethodName")]
        private string _MemberName;

        /// <summary>The name of the <see cref="Method"/> or <see cref="Field"/>.</summary>
        /// <remarks>
        /// If the member is static, this value will start with the assembly qualified name of its declaring type.
        /// <para></para>
        /// If the member is a field, this value will end with the <see cref="FieldNameSuffix"/>.
        /// </remarks>
        public string MemberName => _MemberName;

        /************************************************************************************************************************/

        [SerializeField]
        internal PersistentArgument[] _PersistentArguments = NoArguments;

        /// <summary>The arguments which are passed to the method when it is invoked.</summary>
        public PersistentArgument[] PersistentArguments => _PersistentArguments;

        /************************************************************************************************************************/

        [NonSerialized]
        internal MethodBase _Method;

        /// <summary>The method which this call encapsulates.</summary>
        public MethodBase Method
        {
            get
            {
                if (_Method != null)
                    return _Method;

                GetMemberDetails(out var declaringType, out var methodName);
                if (declaringType == null || string.IsNullOrEmpty(methodName))
                    return null;

                var argumentCount = _PersistentArguments.Length;
                var parameters = ArrayCache<Type>.GetTempArray(argumentCount);
                for (int i = 0; i < argumentCount; i++)
                {
                    parameters[i] = _PersistentArguments[i].SystemType;
                }

                if (methodName == "ctor")
                    _Method = declaringType.GetConstructor(UltEventUtils.AnyAccessBindings, null, parameters, null);
                else
                    _Method = declaringType.GetMethod(methodName, UltEventUtils.AnyAccessBindings, null, parameters, null);

                return _Method;
            }
        }

        /// <summary>Gets the <see cref="Method"/> and blocks any exceptions (which causes it to return null).</summary>
        internal MethodBase GetMethodSafe()
        {
            try { return Method; }
            catch { return null; }
        }

        /************************************************************************************************************************/

        [NonSerialized]
        internal FieldInfo _Field;

        /// <summary>The field which this call encapsulates.</summary>
        /// <remarks>
        /// If there are any <see cref="PersistentArguments"/>, this call will set the field using the first one.
        /// Otherwise, this call will get the field.
        /// </remarks>
        public FieldInfo Field
        {
            get
            {
                if (_Field != null)
                    return _Field;

                GetMemberDetails(out var declaringType, out var fieldName);
                if (declaringType == null || string.IsNullOrEmpty(fieldName))
                    return null;

                fieldName = fieldName[..^1];// Remove the FieldNameSuffix.
                _Field = declaringType.GetField(fieldName, UltEventUtils.AnyAccessBindings);

                return _Field;
            }
        }

        /************************************************************************************************************************/

        /// <summary>Does this call get or set a <see cref="Field"/> instead of invoking a <see cref="Method"/>?</summary>
        public bool IsField
            => !string.IsNullOrEmpty(_MemberName)
            && _MemberName[^1] == FieldNameSuffix;

        /// <summary>Will this call get the value of the <see cref="Field"/> (otherwise set)?</summary>
        /// <remarks>
        /// This property will also return true for parameterless methods
        /// so you may need to check <see cref="IsField"/> as well.
        /// </remarks>
        public bool IsGetter
            => _PersistentArguments == null
            || _PersistentArguments.Length == 0;

        /// <summary>The <see cref="Field"/> or <see cref="Method"/> depending on <see cref="IsField"/>.</summary>
        public MemberInfo Member
            => IsField ? Field : Method;

        /// <summary>Gets the <see cref="Member"/> and blocks any exceptions (which causes it to return null).</summary>
        internal MemberInfo GetMemberSafe()
        {
            try { return Member; }
            catch { return null; }
        }

        /// <summary>Gets the <see cref="Method"/> return type or <see cref="Field"/> type if <see cref="IsGetter"/>.</summary>
        internal Type GetReturnType()
        {
            return IsField ?
                IsGetter ? Field.FieldType : null :
                GetMethodSafe()?.GetReturnType();
        }

        /************************************************************************************************************************/
#if UNITY_EDITOR
        /************************************************************************************************************************/

        // Always clear the cached members in the editor in case any fields
        // have been directly modified by Inspector or Undo operations.
        void ISerializationCallbackReceiver.OnBeforeSerialize() => ClearCache();
        void ISerializationCallbackReceiver.OnAfterDeserialize() => ClearCache();

        private void ClearCache()
        {
            _Method = null;
            _Field = null;

            for (int i = 0; i < _PersistentArguments.Length; i++)
            {
                _PersistentArguments[i].ClearCache();
            }
        }

        /************************************************************************************************************************/
#endif
        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/

        /// <summary>Constructs a new <see cref="PersistentCall"/> with default values.</summary>
        public PersistentCall() { }

        /// <summary>Constructs a new <see cref="PersistentCall"/> to serialize the specified `method`.</summary>
        public PersistentCall(MethodInfo method, Object target)
            => SetMethod(method, target);

        /// <summary>Constructs a new <see cref="PersistentCall"/> to serialize the specified `method`.</summary>
        public PersistentCall(Delegate method)
            => SetMethod(method);

        /// <summary>Constructs a new <see cref="PersistentCall"/> to serialize the specified `method`.</summary>
        public PersistentCall(Action method)
            => SetMethod(method);

        /************************************************************************************************************************/

        /// <summary>Sets the <see cref="Method"/>.</summary>
        public void SetMethod(MethodBase method, Object target)
        {
            _Field = null;

            _Method = method;
            _Target = target;

            if (method != null)
            {
                if (method.IsStatic || method.IsConstructor)
                {
                    _MemberName = UltEventUtils.GetFullyQualifiedName(method);
                    _Target = null;
                }
                else _MemberName = method.Name;

                var parameters = method.GetParameters();

                if (_PersistentArguments == null || _PersistentArguments.Length != parameters.Length)
                {
                    _PersistentArguments = NewArgumentArray(parameters.Length);
                }

                for (int i = 0; i < _PersistentArguments.Length; i++)
                {
                    var parameter = parameters[i];
                    var persistentArgument = _PersistentArguments[i];

                    persistentArgument.SystemType = parameter.ParameterType;

                    switch (persistentArgument.Type)
                    {
                        case PersistentArgumentType.Parameter:
                        case PersistentArgumentType.ReturnValue:
                            break;
                        default:
                            if ((parameter.Attributes & ParameterAttributes.HasDefault) == ParameterAttributes.HasDefault)
                            {
                                persistentArgument.Value = parameter.DefaultValue;
                            }
                            break;
                    }
                }
            }
            else
            {
                _MemberName = null;
                _PersistentArguments = NoArguments;
            }
        }

        /// <summary>Sets the <see cref="Method"/>.</summary>
        public void SetMethod(Delegate method)
        {
            if (method.Target == null)
            {
                SetMethod(method.Method, null);
            }
            else
            {
                var target = method.Target as Object;
                if (target != null)
                    SetMethod(method.Method, target);
                else
                    throw new InvalidOperationException("SetMethod failed because action.Target is not a UnityEngine.Object.");
            }
        }

        /// <summary>Sets the delegate which this call encapsulates.</summary>
        public void SetMethod(Action method)
            => SetMethod((Delegate)method);

        /************************************************************************************************************************/

        /// <summary>
        /// Sets the <see cref="Field"/>.
        /// If not `isGetter`, this method will return the first <see cref="PersistentArgument"/>
        /// where the value to set will be stored.
        /// </summary>
        public PersistentArgument SetField(FieldInfo field, Object target, bool isGetter)
        {
            _Method = null;

            _Field = field;
            _Target = target;

            if (field == null)
            {
                _MemberName = null;
                _PersistentArguments = NoArguments;
                return null;
            }

            // Name.

            if (field.IsStatic)
            {
                _MemberName = UltEventUtils.GetFullyQualifiedName(field);
                _Target = null;
            }
            else _MemberName = field.Name;

            _MemberName += FieldNameSuffix;

            // Arguments.

            var argumentCount = isGetter ? 0 : 1;
            if (_PersistentArguments == null ||
                _PersistentArguments.Length != argumentCount)
                _PersistentArguments = NewArgumentArray(argumentCount);

            if (isGetter)
                return null;

            var argument = _PersistentArguments[0];
            argument.SystemType = field.FieldType;
            return argument;
        }

        /************************************************************************************************************************/

        private static readonly PersistentArgument[]
            NoArguments = new PersistentArgument[0];

        private static PersistentArgument[] NewArgumentArray(int length)
        {
            if (length == 0)
                return NoArguments;

            var array = new PersistentArgument[length];

            for (int i = 0; i < length; i++)
                array[i] = new();

            return array;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// If <see cref="IsField"/>, gets or sets the <see cref="Field"/>, else invokes the <see cref="Method"/>.
        /// </summary>
        public object Invoke()
        {
            // Field.
            if (IsField)
            {
                var field = Field;
                if (field == null)
                {
                    Debug.LogWarning(GetFailedInvokeMessage("Field"));
                    return null;
                }

                if (IsGetter)
                    return field.GetValue(_Target);

                field.SetValue(_Target, _PersistentArguments[0].Value);

                UltEventBase.UpdateLinkedValueOffsets();
                return null;
            }

            // Method.
            var method = Method;
            if (method == null)
            {
                Debug.LogWarning(GetFailedInvokeMessage("Method"));
                return null;
            }

            object[] parameters;
            if (_PersistentArguments != null && _PersistentArguments.Length > 0)
            {
                parameters = ArrayCache<object>.GetTempArray(_PersistentArguments.Length);
                for (int i = 0; i < parameters.Length; i++)
                {
                    parameters[i] = _PersistentArguments[i].Value;
                }
            }
            else parameters = null;

            UltEventBase.UpdateLinkedValueOffsets();

            if (method.IsConstructor)
                return ((ConstructorInfo)method).Invoke(parameters);

            return method.Invoke(_Target, parameters);
        }

        /************************************************************************************************************************/

        private string GetFailedInvokeMessage(string memberType)
        {
            var message = $"Attempted to Invoke a PersistentCall which couldn't find it's {memberType}: ";

            if (Target != null)
                message += $"{Target.GetType().GetNameCS(true)}.";

            message += $"{MemberName}.";

#if !UNITY_EDITOR
            message +=
                " If this is only happening in Runtime Builds, Unity may be stripping the target method: " + 
                "https://docs.unity3d.com/Manual/ManagedCodeStripping.html";
#endif

            return message;
        }

        /************************************************************************************************************************/

        /// <summary>Sets the value of the first persistent argument.</summary>
        public void SetArguments(object argument0)
        {
            PersistentArguments[0].Value = argument0;
        }

        /// <summary>Sets the value of the first and second persistent arguments.</summary>
        public void SetArguments(object argument0, object argument1)
        {
            PersistentArguments[0].Value = argument0;
            PersistentArguments[1].Value = argument1;
        }

        /// <summary>Sets the value of the first, second, and third persistent arguments.</summary>
        public void SetArguments(object argument0, object argument1, object argument2)
        {
            PersistentArguments[0].Value = argument0;
            PersistentArguments[1].Value = argument1;
            PersistentArguments[2].Value = argument2;
        }

        /// <summary>Sets the value of the first, second, third, and fourth persistent arguments.</summary>
        public void SetArguments(object argument0, object argument1, object argument2, object argument3)
        {
            PersistentArguments[0].Value = argument0;
            PersistentArguments[1].Value = argument1;
            PersistentArguments[2].Value = argument2;
            PersistentArguments[3].Value = argument3;
        }

        /************************************************************************************************************************/

        internal void GetMemberDetails(out Type declaringType, out string memberName)
        {
#if UNITY_EDITOR
            // If you think this looks retarded, that's because it is.

            // Sometimes Unity ends up with an old reference to an object where the reference thinks
            // it has been destroyed even though it hasn't and it still has a value Instance ID.

            // So we just get a new reference using that ID.

            if (_Target == null && !ReferenceEquals(_Target, null))
                _Target = UnityEditor.EditorUtility.InstanceIDToObject(_Target.GetInstanceID());
#endif

            GetMemberDetails(_MemberName, _Target, out declaringType, out memberName);
        }

        internal static void GetMemberDetails(
            string serializedMemberName,
            Object target,
            out Type declaringType,
            out string memberName)
        {
            if (string.IsNullOrEmpty(serializedMemberName))
            {
                declaringType = null;
                memberName = null;
                return;
            }

            if (target == null)
            {
                var lastDot = serializedMemberName.LastIndexOf('.');
                if (lastDot < 0)
                {
                    declaringType = null;
                    memberName = serializedMemberName;
                }
                else
                {
                    declaringType = ReflectionCache.GetType(serializedMemberName[..lastDot]);
                    lastDot++;
                    memberName = serializedMemberName[lastDot..];
                }
            }
            else
            {
                declaringType = target.GetType();
                memberName = serializedMemberName;
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns true if the specified `type` can be represented by a non-linked <see cref="PersistentArgument"/>.
        /// </summary>
        public static bool IsSupportedNative(Type type)
        {
            return
                type == typeof(bool) ||
                type == typeof(string) ||
                type == typeof(int) ||
                (type.IsEnum && Enum.GetUnderlyingType(type) == typeof(int)) ||
                type == typeof(float) ||
                type == typeof(Vector2) ||
                type == typeof(Vector3) ||
                type == typeof(Vector4) ||
                type == typeof(Quaternion) ||
                type == typeof(Color) ||
                type == typeof(Color32) ||
                type == typeof(Rect) ||
                type == typeof(Object) || type.IsSubclassOf(typeof(Object));
        }

        /// <summary>
        /// Returns true if the type of each of the `parameters` can be represented by a non-linked <see cref="PersistentArgument"/>.
        /// </summary>
        public static bool IsSupportedNative(ParameterInfo[] parameters)
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                if (!IsSupportedNative(parameters[i].ParameterType))
                    return false;
            }

            return true;
        }

        /************************************************************************************************************************/

        /// <summary>Copies the contents of the `target` call to this call.</summary>
        public void CopyFrom(PersistentCall target)
        {
            _Target = target._Target;
            _MemberName = target._MemberName;
            _Method = target._Method;
            _Field = target._Field;

            _PersistentArguments = new PersistentArgument[target._PersistentArguments.Length];
            for (int i = 0; i < _PersistentArguments.Length; i++)
            {
                _PersistentArguments[i] = target._PersistentArguments[i].Clone();
            }
        }

        /************************************************************************************************************************/

        /// <summary>Returns a description of this call.</summary>
        public override string ToString()
        {
            var text = new StringBuilder();
            ToString(text);
            return text.ToString();
        }

        /// <summary>Appends a description of this call.</summary>
        public void ToString(StringBuilder text)
        {
            text.Append("PersistentCall: MethodName=");
            text.Append(_MemberName);
            text.Append(", Target=");
            text.Append(_Target != null ? _Target.ToString() : "null");
            text.Append(", PersistentArguments=");
            UltEventUtils.AppendDeepToString(text, _PersistentArguments.GetEnumerator(), "\n        ");
        }

        /************************************************************************************************************************/
    }
}
