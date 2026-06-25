using UnityEngine;
using VRC.SDKBase;

// 将此脚本挂载到 Avatar 根目录
[AddComponentMenu("NDMF Plugins/Shadow Toggle Config")]
public class ShadowToggleConfig : MonoBehaviour, IEditorOnly
{
    [Tooltip("生成的动画参数名称，0代表关，1代表开")]
    public string parameterName = "UseShadowToggle";
    
    [Tooltip("默认状态：开启(1) 还是 关闭(0)")]
    public int defaultValue = 1;
}