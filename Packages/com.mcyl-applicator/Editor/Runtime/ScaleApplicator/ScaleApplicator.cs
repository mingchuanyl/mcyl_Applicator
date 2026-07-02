using UnityEngine;
using System.Collections.Generic;
using VRC.SDKBase;

namespace VRCNDMFPlugins.ScaleApplicator
{
    [AddComponentMenu("NDMF/Scale Applicator")]
    [DisallowMultipleComponent]
    [Icon("Assets/ScaleApplicator/Icons/ScaleIcon.png")] // 自定义图标路径
    public class ScaleApplicator : MonoBehaviour, IEditorOnly
    {
        [Header("缩放设置")]
        [Tooltip("XYZ 轴的独立缩放倍数 (例如：X=1, Y=2, Z=1 表示仅将高度拉伸一倍)")]
        public Vector3 scaleValue = Vector3.one; // 默认为 (1, 1, 1)

        [Header("目标对象")]
        [Tooltip("需要被缩放的游戏对象列表")]
        public List<GameObject> targetObjects = new List<GameObject>();

        // --- 以下为预览状态数据，对用户隐藏，但由脚本调用 ---
        [HideInInspector] public bool isPreviewing = false;
        [HideInInspector] public List<Vector3> originalScales = new List<Vector3>();
    }
}