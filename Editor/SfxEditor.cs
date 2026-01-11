using System;
using Less3.TypeTree.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(Sfx))]
public class SfxEditor : Editor
{
    private Sfx sfx;
    private VisualElement modulesContainer;

    public VisualTreeAsset rootAsset;
    public VisualTreeAsset moduleElementAsset;

    public override VisualElement CreateInspectorGUI()
    {
        sfx = (Sfx)target;

        var root = rootAsset.CloneTree();
        var clipsContainer = root.Q<VisualElement>("ClipsContainer");

        // Clips property
        var clipsProperty = serializedObject.FindProperty("clips");
        var clipsField = new PropertyField(clipsProperty);
        clipsField.Bind(serializedObject);
        clipsContainer.Add(clipsField);

        // Modules container
        modulesContainer = new VisualElement();
        root.Add(modulesContainer);

        RebuildModulesList();

        // Add Module button
        var addButton = new Button();
        addButton.clicked += () =>
        {
            L3TypeTreeWindow.OpenForType(typeof(Sfx), addButton.worldBound.position, (type) =>
            {
                AddModule(type);
                RebuildModulesList();
            });
        };
        addButton.focusable = false;
        addButton.text = "Add Module";
        addButton.style.height = 32;
        addButton.style.marginTop = 24;
        addButton.style.marginLeft = 12;
        addButton.style.marginRight = 12;
        root.Add(addButton);

        return root;
    }

    private void RebuildModulesList()
    {
        modulesContainer.Clear();
        CleanupNullModules();

        var effectModulesProperty = serializedObject.FindProperty("effectModules");

        for (int i = 0; i < effectModulesProperty.arraySize; i++)
        {
            var moduleProperty = effectModulesProperty.GetArrayElementAtIndex(i);
            var module = moduleProperty.objectReferenceValue as SfxEffectModule;

            if (module == null)
                continue;

            int capturedIndex = i;

            var moduleSerializedObject = new SerializedObject(module);

            var moduleElement = moduleElementAsset.CloneTree();
            var inspectorContainer = moduleElement.Q<VisualElement>("InspectorContainer");

            moduleElement.Q<Label>("ModuleName").text = module.displayName;

            var enabledProperty = moduleSerializedObject.FindProperty("enabled");
            var toggle = moduleElement.Q<Toggle>("Toggle");
            toggle.value = module.enabled;
            inspectorContainer.SetEnabled(module.enabled);
            toggle.RegisterValueChangedCallback(evt =>
            {
                moduleSerializedObject.Update();
                enabledProperty.boolValue = evt.newValue;
                moduleSerializedObject.ApplyModifiedProperties();
                inspectorContainer.SetEnabled(evt.newValue);
            });

            var moveUpButton = moduleElement.Q<Button>("MoveUp");
            moveUpButton.clicked += () =>
            {
                //TODO MoveModuleUp(capturedIndex);
                RebuildModulesList();
            };
            var moveDownButton = moduleElement.Q<Button>("MoveDown");
            moveDownButton.clicked += () =>
            {
                //TODO MoveModuleDown(capturedIndex);
                RebuildModulesList();
            };

            var removeButton = moduleElement.Q<Button>("Delete");
            removeButton.clicked += () =>
            {
                RemoveModule(capturedIndex);
                RebuildModulesList();
            };


            // Module properties (excluding 'enabled' and 'm_Script')
            var prop = moduleSerializedObject.GetIterator();
            prop.NextVisible(true); // Skip m_Script
            while (prop.NextVisible(false))
            {
                if (prop.name != "enabled")
                {
                    var field = new PropertyField(prop);
                    field.Bind(moduleSerializedObject);
                    field.style.marginTop = 0;
                    inspectorContainer.Add(field);
                }
            }

            modulesContainer.Add(moduleElement);
        }
    }

    private void AddModule(Type moduleType)
    {
        var newModule = ScriptableObject.CreateInstance(moduleType) as SfxEffectModule;
        newModule.name = moduleType.Name;

        AssetDatabase.AddObjectToAsset(newModule, sfx);

        serializedObject.Update();
        var effectModulesProperty = serializedObject.FindProperty("effectModules");
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

        var effectModulesProperty = serializedObject.FindProperty("effectModules");
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
        serializedObject.Update();
        var effectModulesProperty = serializedObject.FindProperty("effectModules");

        for (int i = effectModulesProperty.arraySize - 1; i >= 0; i--)
        {
            if (effectModulesProperty.GetArrayElementAtIndex(i).objectReferenceValue == null)
            {
                effectModulesProperty.DeleteArrayElementAtIndex(i);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
