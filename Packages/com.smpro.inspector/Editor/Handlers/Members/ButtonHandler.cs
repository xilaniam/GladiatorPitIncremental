using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace SMPro.Inspector.EditorTools
{
    [AttributeHandler(typeof(ButtonAttribute))]
    public sealed class ButtonHandler : FieldHandler<ButtonAttribute>
    {
        readonly List<IValueField> arguments = new();
        MethodInfo method;
        ParameterInfo[] parameters = Array.Empty<ParameterInfo>();
        bool invocable;

        public string Text => string.IsNullOrEmpty(Attribute.Label) ? Property.Label : Attribute.Label;

        protected override void OnInitialize()
        {
            method = Property.Member?.Info as MethodInfo;
            if (method == null)
            {
                ReportProblem("Button: only methods can be buttons");
                return;
            }
            parameters = method.GetParameters();
            invocable = !method.ContainsGenericParameters;
            foreach (ParameterInfo parameter in parameters)
            {
                if (!parameter.ParameterType.IsByRef && ValueFields.FieldValueType(parameter.ParameterType) != null) continue;
                ReportProblem($"Button: parameter '{parameter.Name}' of type {parameter.ParameterType.Name} is not supported");
                invocable = false;
            }
        }

        public override VisualElement CreateField()
        {
            var button = new Button(Invoke) { text = Text, name = "sm-button:" + Property.Path };
            button.AddToClassList("sm-button");
            button.AddToClassList("sm-button--" + Attribute.Size.ToString().ToLowerInvariant());
            button.SetEnabled(invocable);
            if (parameters.Length == 0 || !invocable) return button;

            var box = new VisualElement();
            box.AddToClassList("sm-button-box");
            foreach (ParameterInfo parameter in parameters)
            {
                IValueField field = ValueFields.Create(parameter.ParameterType, ObjectNames.NicifyVariableName(parameter.Name));
                field.BoxedValue = InitialValue(parameter);
                field.Element.name = "sm-argument:" + parameter.Name;
                arguments.Add(field);
                box.Add(field.Element);
            }
            box.Add(button);
            return box;
        }

        static object InitialValue(ParameterInfo parameter)
        {
            Type type = parameter.ParameterType;
            if (!parameter.HasDefaultValue || parameter.DefaultValue == null) return type.IsEnum ? Enum.ToObject(type, 0) : ValueFields.DefaultFor(type);
            return type.IsEnum && !(parameter.DefaultValue is Enum) ? Enum.ToObject(type, parameter.DefaultValue) : parameter.DefaultValue;
        }

        public void Invoke()
        {
            if (!invocable) return;
            object[] values = Arguments();
            if (method.IsStatic)
            {
                Call(null, values);
                return;
            }

            Object[] targets = Property.Targets;
            Undo.RecordObjects(targets, Text);
            for (int i = 0; i < Property.TargetCount; i++)
            {
                object owner = Property.GetOwner(i);
                if (owner != null) Call(owner, values);
            }
            foreach (Object target in targets)
            {
                if (target == null) continue;
                EditorUtility.SetDirty(target);
                if (PrefabUtility.IsPartOfPrefabInstance(target)) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            }
        }

        object[] Arguments()
        {
            var values = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
                values[i] = i < arguments.Count ? arguments[i].BoxedValue : InitialValue(parameters[i]);
            return values;
        }

        void Call(object owner, object[] values)
        {
            try
            {
                method.Invoke(owner, values);
            }
            catch (TargetInvocationException exception)
            {
                Debug.LogException(exception.InnerException ?? exception);
            }
            catch (ArgumentException exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
