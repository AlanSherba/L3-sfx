using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class sfxtester : MonoBehaviour
{
    public Sfx sfx;

}
#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(sfxtester))]
public class sfxtesterEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        sfxtester tester = (sfxtester)target;
        if (UnityEditor.EditorApplication.isPlaying)
        {
            if (GUILayout.Button("Play Sfx"))
            {
                if (tester.sfx != null)
                    SfxManager.Play(tester.sfx);
            }
        }
        else
        {
            GUILayout.Label("Enter Play Mode to use Play Sfx");
        }
    }
}
#endif
