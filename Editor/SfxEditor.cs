using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;

[CustomEditor(typeof(Sfx))]
public class SfxEditor : Editor
{
    private Sfx sfx;
    private SerializedProperty clipsProperty;
    private SerializedProperty effectModulesProperty;

    private static Type[] cachedModuleTypes;
    private static string[] cachedModuleTypeNames;
    private int selectedModuleIndex = 0;

    private void OnEnable()
    {
        sfx = (Sfx)target;
        clipsProperty = serializedObject.FindProperty("clips");
        effectModulesProperty = serializedObject.FindProperty("effectModules");

        CacheModuleTypes();
    }

    private static void CacheModuleTypes()
    {
        if (cachedModuleTypes != null)
            return;

        var types = new List<Type>();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsSubclassOf(typeof(SfxEffectModule)) && !type.IsAbstract)
                    {
                        types.Add(type);
                    }
                }
            }
            catch (System.Reflection.ReflectionTypeLoadException)
            {
                // Some assemblies may not be loadable
            }
        }

        cachedModuleTypes = types.OrderBy(t => t.Name).ToArray();
        cachedModuleTypeNames = cachedModuleTypes.Select(t => t.Name).ToArray();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(clipsProperty, true);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Effect Modules", EditorStyles.boldLabel);

        CleanupNullModules();
        DrawModulesList();

        EditorGUILayout.Space();
        DrawAddModuleUI();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawModulesList()
    {
        for (int i = 0; i < effectModulesProperty.arraySize; i++)
        {
            var moduleProperty = effectModulesProperty.GetArrayElementAtIndex(i);
            var module = moduleProperty.objectReferenceValue as SfxEffectModule;

            if (module == null)
                continue;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();

            module.enabled = EditorGUILayout.ToggleLeft(
                module.GetType().Name,
                module.enabled,
                EditorStyles.boldLabel
            );

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("X", GUILayout.Width(24)))
            {
                RemoveModule(i);
                break;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel++;
            SerializedObject moduleSerializedObject = new SerializedObject(module);
            moduleSerializedObject.Update();

            SerializedProperty prop = moduleSerializedObject.GetIterator();
            prop.NextVisible(true);
            while (prop.NextVisible(false))
            {
                if (prop.name != "enabled")
                {
                    EditorGUILayout.PropertyField(prop, true);
                }
            }

            moduleSerializedObject.ApplyModifiedProperties();
            EditorGUI.indentLevel--;

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }
    }

    private void DrawAddModuleUI()
    {
        EditorGUILayout.BeginHorizontal();

        if (cachedModuleTypes == null || cachedModuleTypes.Length == 0)
        {
            EditorGUILayout.HelpBox("No SfxEffectModule types found.", MessageType.Info);
        }
        else
        {
            selectedModuleIndex = EditorGUILayout.Popup(
                "Add Module",
                selectedModuleIndex,
                cachedModuleTypeNames
            );

            if (GUILayout.Button("Add", GUILayout.Width(60)))
            {
                AddModule(cachedModuleTypes[selectedModuleIndex]);
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    private void AddModule(Type moduleType)
    {
        var newModule = ScriptableObject.CreateInstance(moduleType) as SfxEffectModule;
        newModule.name = moduleType.Name;

        AssetDatabase.AddObjectToAsset(newModule, sfx);

        serializedObject.Update();
        effectModulesProperty.arraySize++;
        effectModulesProperty.GetArrayElementAtIndex(effectModulesProperty.arraySize - 1)
            .objectReferenceValue = newModule;
        serializedObject.ApplyModifiedProperties();

        EditorUtility.SetDirty(sfx);
        AssetDatabase.SaveAssetIfDirty(sfx);
    }

    private void RemoveModule(int index)
    {
        serializedObject.Update();

        var moduleProperty = effectModulesProperty.GetArrayElementAtIndex(index);
        var module = moduleProperty.objectReferenceValue;

        moduleProperty.objectReferenceValue = null;
        effectModulesProperty.DeleteArrayElementAtIndex(index);

        serializedObject.ApplyModifiedProperties();

        if (module != null)
        {
            ScriptableObject.DestroyImmediate(module, true);
        }

        EditorUtility.SetDirty(sfx);
        AssetDatabase.SaveAssetIfDirty(sfx);
    }

    private void CleanupNullModules()
    {
        for (int i = effectModulesProperty.arraySize - 1; i >= 0; i--)
        {
            if (effectModulesProperty.GetArrayElementAtIndex(i).objectReferenceValue == null)
            {
                effectModulesProperty.DeleteArrayElementAtIndex(i);
            }
        }
    }
}
