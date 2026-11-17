using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SMPro.SkillTree.EditorTools
{
    [CustomPropertyDrawer(typeof(TypePickerAttribute))]
    public class TypePickerDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            EditorGUI.GetPropertyHeight(property, label, true);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            var buttonRect = new Rect(
                position.x + EditorGUIUtility.labelWidth,
                position.y,
                position.width - EditorGUIUtility.labelWidth,
                EditorGUIUtility.singleLineHeight);

            if (EditorGUI.DropdownButton(buttonRect, new GUIContent(CurrentTypeName(property)), FocusType.Keyboard))
                ShowMenu(property, BaseType());

            EditorGUI.PropertyField(position, property, label, true);
        }

        Type BaseType()
        {
            Type type = fieldInfo.FieldType;
            if (type.IsArray) return type.GetElementType();
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) return type.GetGenericArguments()[0];
            return type;
        }

        static string CurrentTypeName(SerializedProperty property)
        {
            string fullName = property.managedReferenceFullTypename;
            if (string.IsNullOrEmpty(fullName)) return "None";
            string typeName = fullName.Substring(fullName.LastIndexOfAny(new[] { ' ', '.' }) + 1);
            return ObjectNames.NicifyVariableName(typeName);
        }

        static void ShowMenu(SerializedProperty property, Type baseType)
        {
            SerializedObject serializedObject = property.serializedObject;
            string path = property.propertyPath;
            object current = property.managedReferenceValue;

            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("None"), current == null, () => Assign(serializedObject, path, null));

            foreach (Type type in TypeCache.GetTypesDerivedFrom(baseType).Where(IsPickable).OrderBy(MenuPath))
            {
                Type picked = type;
                menu.AddItem(new GUIContent(MenuPath(type)), current?.GetType() == type, () => Assign(serializedObject, path, picked));
            }

            menu.ShowAsContext();
        }

        static void Assign(SerializedObject serializedObject, string path, Type type)
        {
            serializedObject.Update();
            SerializedProperty property = serializedObject.FindProperty(path);
            property.managedReferenceValue = type == null ? null : Activator.CreateInstance(type);
            serializedObject.ApplyModifiedProperties();
        }

        static bool IsPickable(Type type) =>
            !type.IsAbstract &&
            !type.IsInterface &&
            !type.IsGenericTypeDefinition &&
            type.IsDefined(typeof(SerializableAttribute), false) &&
            !typeof(UnityEngine.Object).IsAssignableFrom(type) &&
            type.GetConstructor(Type.EmptyTypes) != null;

        static string MenuPath(Type type) =>
            type.GetCustomAttribute<SkillEffectMenuAttribute>()?.Path ?? ObjectNames.NicifyVariableName(type.Name);
    }
}
