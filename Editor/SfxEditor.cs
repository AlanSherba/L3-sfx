using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Less3.TypeTree.Editor;
using System;

[CustomEditor(typeof(Sfx))]
public class SfxEditor : Editor
{
    private Sfx sfx;
    private VisualElement modulesContainer;

    public override VisualElement CreateInspectorGUI()
    {
        sfx = (Sfx)target;

        var root = new VisualElement();

        // Clips property
        var clipsProperty = serializedObject.FindProperty("clips");
        var clipsField = new PropertyField(clipsProperty);
        clipsField.Bind(serializedObject);
        root.Add(clipsField);

        // Spacer
        root.Add(new VisualElement { style = { height = 10 } });

        // Effect Modules header
        var header = new Label("Effect Modules");
        header.style.unityFontStyleAndWeight = FontStyle.Bold;
        header.style.marginTop = 10;
        header.style.marginBottom = 5;
        root.Add(header);

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

            // Module container
            var moduleBox = new VisualElement();
            moduleBox.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.3f);
            moduleBox.style.borderTopLeftRadius = 4;
            moduleBox.style.borderTopRightRadius = 4;
            moduleBox.style.borderBottomLeftRadius = 4;
            moduleBox.style.borderBottomRightRadius = 4;
            moduleBox.style.paddingTop = 6;
            moduleBox.style.paddingBottom = 6;
            moduleBox.style.paddingLeft = 8;
            moduleBox.style.paddingRight = 8;
            moduleBox.style.marginBottom = 4;

            // Header row
            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.justifyContent = Justify.SpaceBetween;
            headerRow.style.alignItems = Align.Center;

            // Enabled toggle + name
            var moduleSerializedObject = new SerializedObject(module);
            var enabledProperty = moduleSerializedObject.FindProperty("enabled");
            var enabledToggle = new Toggle();
            enabledToggle.value = enabledProperty.boolValue;
            enabledToggle.RegisterValueChangedCallback(evt =>
            {
                moduleSerializedObject.Update();
                enabledProperty.boolValue = evt.newValue;
                moduleSerializedObject.ApplyModifiedProperties();
            });

            var nameLabel = new Label(module.GetType().Name);
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLabel.style.marginLeft = 4;

            var leftGroup = new VisualElement();
            leftGroup.style.flexDirection = FlexDirection.Row;
            leftGroup.style.alignItems = Align.Center;
            leftGroup.Add(enabledToggle);
            leftGroup.Add(nameLabel);

            // Remove button
            var removeButton = new Button(() =>
            {
                RemoveModule(capturedIndex);
                RebuildModulesList();
            });
            removeButton.text = "X";
            removeButton.style.width = 24;
            removeButton.style.height = 20;

            headerRow.Add(leftGroup);
            headerRow.Add(removeButton);
            moduleBox.Add(headerRow);

            // Module properties (excluding 'enabled' and 'm_Script')
            var prop = moduleSerializedObject.GetIterator();
            prop.NextVisible(true); // Skip m_Script
            while (prop.NextVisible(false))
            {
                if (prop.name != "enabled")
                {
                    var field = new PropertyField(prop);
                    field.Bind(moduleSerializedObject);
                    field.style.marginTop = 4;
                    moduleBox.Add(field);
                }
            }

            modulesContainer.Add(moduleBox);
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
