using UnityEngine;
using UnityEditor;
using VRCNDMFPlugins.ScaleApplicator;

namespace VRCNDMFPlugins.ScaleApplicator.Editor
{
    // 绑定到 ScaleApplicator 组件
    [CustomEditor(typeof(ScaleApplicator))]
    public class ScaleApplicatorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            ScaleApplicator applicator = (ScaleApplicator)target;

            // 绘制默认的变量面板 (scaleValue, targetObjects)
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("编辑器预览工具", EditorStyles.boldLabel);

            // 排版：将按钮放在同一行
            GUILayout.BeginHorizontal();
            
            // 绘制“预览”按钮
            GUI.enabled = !applicator.isPreviewing && applicator.targetObjects.Count > 0;
            if (GUILayout.Button("预览缩放 (Preview)"))
            {
                ApplyPreview(applicator);
            }

            // 绘制“取消”按钮
            GUI.enabled = applicator.isPreviewing;
            if (GUILayout.Button("取消预览 (Revert)"))
            {
                RevertPreview(applicator);
            }

            GUILayout.EndHorizontal();

            GUI.enabled = true; // 恢复GUI默认状态

            // 给用户的温馨提示
            if (applicator.isPreviewing)
            {
                EditorGUILayout.HelpBox("当前处于预览模式。\n注意：如果修改了缩放值或目标列表，请先【取消预览】再重新预览以查看最新效果。(NDMF 上传时会自动处理，无需手动取消)", MessageType.Info);
            }
        }

        private void ApplyPreview(ScaleApplicator applicator)
        {
            // 允许使用 Ctrl+Z 撤销
            Undo.RecordObject(applicator, "Preview Scale Applicator");
            
            applicator.originalScales.Clear();
            
            foreach (var obj in applicator.targetObjects)
            {
                if (obj != null)
                {
                    Undo.RecordObject(obj.transform, "Preview Scale Object");
                    // 记录原始缩放值
                    applicator.originalScales.Add(obj.transform.localScale);
                    // 应用新的 XYZ 缩放 (使用 Vector3.Scale 进行分量相乘)
                    obj.transform.localScale = Vector3.Scale(obj.transform.localScale, applicator.scaleValue);
                }
                else
                {
                    applicator.originalScales.Add(Vector3.one); // 防止列表出现 null 错位
                }
            }
            
            applicator.isPreviewing = true;
            // 标记预制体发生改变，确保保存场景时状态不会丢失
            PrefabUtility.RecordPrefabInstancePropertyModifications(applicator);
        }

        private void RevertPreview(ScaleApplicator applicator)
        {
            Undo.RecordObject(applicator, "Revert Scale Applicator");

            for (int i = 0; i < applicator.targetObjects.Count; i++)
            {
                var obj = applicator.targetObjects[i];
                // 确保索引没有越界且对象不为空
                if (obj != null && i < applicator.originalScales.Count)
                {
                    Undo.RecordObject(obj.transform, "Revert Scale Object");
                    // 恢复原始缩放值
                    obj.transform.localScale = applicator.originalScales[i];
                }
            }

            applicator.originalScales.Clear();
            applicator.isPreviewing = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(applicator);
        }
    }
}