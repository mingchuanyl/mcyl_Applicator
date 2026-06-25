using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;

public class 形态键传输工具 : EditorWindow
{
    private GameObject 源对象;
    private GameObject 目标对象;
    private Vector2 滚动位置;
    private Dictionary<string, float> 源形态键值 = new Dictionary<string, float>();
    private bool[] 选中的形态键;
    private string 搜索过滤 = "";
    private bool 全选 = false;
    private float 传输速度 = 1.0f;
    private Vector2 ARKit滚动位置;
    private List<string> 缺失的ARKit形态键 = new List<string>();
    private bool 显示ARKit检查结果 = false;

    // ARKit标准52个形态键名称
    private readonly string[] ARKit标准形态键 = new string[]
    {
        // 眉毛区域
        "browDown_L", "browDown_R",
        "browInnerUp", "browOuterUp_L", "browOuterUp_R",
        
        // 眼睛区域
        "eyeBlink_L", "eyeBlink_R",
        "eyeSquint_L", "eyeSquint_R",
        "eyeWide_L", "eyeWide_R",
        "eyeLookUp_L", "eyeLookUp_R",
        "eyeLookDown_L", "eyeLookDown_R",
        "eyeLookIn_L", "eyeLookIn_R",
        "eyeLookOut_L", "eyeLookOut_R",
        
        // 鼻子区域
        "noseSneer_L", "noseSneer_R",
        
        // 脸颊区域
        "cheekPuff", "cheekSquint_L", "cheekSquint_R",
        
        // 嘴巴区域 - 基础
        "mouthClose", "mouthFunnel", "mouthPucker",
        "mouthLeft", "mouthRight",
        "mouthSmile_L", "mouthSmile_R",
        "mouthFrown_L", "mouthFrown_R",
        "mouthDimple_L", "mouthDimple_R",
        "mouthStretch_L", "mouthStretch_R",
        "mouthRollLower", "mouthRollUpper",
        "mouthShrugLower", "mouthShrugUpper",
        "mouthPress_L", "mouthPress_R",
        "mouthLowerDown_L", "mouthLowerDown_R",
        "mouthUpperUp_L", "mouthUpperUp_R",
        
        // 嘴巴区域 - 复合
        "jawOpen", "jawForward", "jawLeft", "jawRight",
        "jawChew",
        
        // 舌头区域
        "tongueOut", "tongueUp", "tongueDown", "tongueLeft", "tongueRight",
        "tongueRoll", "tongueBendDown", "tongueCurlUp", "tongueSquish",
        "tongueFlat",
        
        // 嘴唇矫正器
        "mouthUpperDeepen_L", "mouthUpperDeepen_R"
    };

    // 将菜单项放到子菜单中，避免冲突
    [MenuItem("tools/形态键传输工具")]
    public static void 显示窗口()
    {
        GetWindow<形态键传输工具>("形态键传输工具");
    }

    void OnGUI()
    {
        GUILayout.Label("🎭 形态键传输工具", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 源对象和目标对象选择
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        源对象 = (GameObject)EditorGUILayout.ObjectField("源对象", 源对象, typeof(GameObject), true);
        目标对象 = (GameObject)EditorGUILayout.ObjectField("目标对象", 目标对象, typeof(GameObject), true);
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space();
        
        // 搜索过滤
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("🔍 搜索:", GUILayout.Width(60));
        搜索过滤 = EditorGUILayout.TextField(搜索过滤);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();

        if (源对象 != null)
        {
            // 获取源对象的形态键
            SkinnedMeshRenderer 源渲染器 = 源对象.GetComponent<SkinnedMeshRenderer>();
            
            if (源渲染器 != null && 源渲染器.sharedMesh != null)
            {
                Mesh 源网格 = 源渲染器.sharedMesh;
                int 形态键数量 = 源网格.blendShapeCount;
                
                GUILayout.Label($"📊 源对象形态键: {形态键数量}个", EditorStyles.boldLabel);
                
                // 初始化选择数组
                if (选中的形态键 == null || 选中的形态键.Length != 形态键数量)
                {
                    选中的形态键 = new bool[形态键数量];
                }
                
                // 按钮区域
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("✅ 全选", GUILayout.Width(80)))
                {
                    全选 = true;
                    for (int i = 0; i < 选中的形态键.Length; i++)
                    {
                        选中的形态键[i] = true;
                    }
                }
                
                if (GUILayout.Button("❌ 取消全选", GUILayout.Width(90)))
                {
                    全选 = false;
                    for (int i = 0; i < 选中的形态键.Length; i++)
                    {
                        选中的形态键[i] = false;
                    }
                }
                
                if (GUILayout.Button("🔄 ARKit检查", GUILayout.Width(100)))
                {
                    检查ARKit形态键(源渲染器);
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space();
                
                // 获取当前值按钮
                if (GUILayout.Button("📥 获取当前形态键值"))
                {
                    获取当前形态键值(源渲染器);
                }
                
                EditorGUILayout.Space();
                
                // 传输速度控制
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("传输速度:", GUILayout.Width(80));
                传输速度 = EditorGUILayout.Slider(传输速度, 0.1f, 2.0f);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space();
                
                // 显示形态键列表
                滚动位置 = EditorGUILayout.BeginScrollView(滚动位置, GUILayout.Height(250));
                
                int 显示数量 = 0;
                for (int i = 0; i < 形态键数量; i++)
                {
                    string 形态键名称 = 源网格.GetBlendShapeName(i);
                    
                    // 应用搜索过滤
                    if (!string.IsNullOrEmpty(搜索过滤) && 
                        !形态键名称.ToLower().Contains(搜索过滤.ToLower()))
                    {
                        continue;
                    }
                    
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    
                    // 获取当前值
                    float 当前值 = 0f;
                    if (源形态键值.ContainsKey(形态键名称))
                    {
                        当前值 = 源形态键值[形态键名称];
                    }
                    
                    // 如果是ARKit标准形态键，添加标记
                    bool 是ARKit形态键 = System.Array.Exists(ARKit标准形态键, x => x == 形态键名称);
                    if (是ARKit形态键)
                    {
                        GUI.color = new Color(0.2f, 0.8f, 0.2f); // 绿色标记
                    }
                    
                    // 显示名称和值
                    GUILayout.Label($"{形态键名称}: {当前值:F2}", GUILayout.Width(250));
                    
                    // 恢复颜色
                    if (是ARKit形态键)
                    {
                        GUI.color = Color.white;
                    }
                    
                    // 选择复选框
                    选中的形态键[i] = EditorGUILayout.Toggle(选中的形态键[i], GUILayout.Width(20));
                    
                    // 值滑块
                    if (源形态键值.ContainsKey(形态键名称))
                    {
                        源形态键值[形态键名称] = EditorGUILayout.Slider(源形态键值[形态键名称], 0f, 100f);
                    }
                    
                    EditorGUILayout.EndHorizontal();
                    显示数量++;
                }
                
                if (显示数量 == 0 && !string.IsNullOrEmpty(搜索过滤))
                {
                    GUILayout.Label("没有找到匹配的形态键。", EditorStyles.centeredGreyMiniLabel);
                }
                
                EditorGUILayout.EndScrollView();
                
                EditorGUILayout.Space();
                
                // 传输按钮区域
                EditorGUI.BeginDisabledGroup(目标对象 == null);
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("📋 复制所有值", GUILayout.Height(35)))
                {
                    复制所有形态键值(源渲染器);
                }
                
                if (GUILayout.Button("📤 粘贴选中项", GUILayout.Height(35)))
                {
                    粘贴选中的形态键值(源渲染器);
                }
                
                if (GUILayout.Button("🚀 一键复制粘贴", GUILayout.Height(35)))
                {
                    复制并粘贴所有形态键();
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUI.EndDisabledGroup();
                
                // 状态信息
                if (目标对象 != null)
                {
                    SkinnedMeshRenderer 目标渲染器 = 目标对象.GetComponent<SkinnedMeshRenderer>();
                    if (目标渲染器 != null && 目标渲染器.sharedMesh != null)
                    {
                        EditorGUILayout.HelpBox($"目标对象有 {目标渲染器.sharedMesh.blendShapeCount} 个形态键", MessageType.Info);
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("❌ 源对象没有SkinnedMeshRenderer组件或没有形态键。", MessageType.Warning);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("👈 请先选择一个源对象。", MessageType.Info);
        }
        
        EditorGUILayout.Space();
        
        // 额外功能区域
        EditorGUILayout.LabelField("🔧 额外功能", EditorStyles.boldLabel);
        
        EditorGUI.BeginDisabledGroup(源对象 == null || 目标对象 == null);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🔍 对比形态键名称"))
        {
            对比形态键名称();
        }
        
        if (GUILayout.Button("🔄 重置所有值为0"))
        {
            重置所有形态键为0();
        }
        
        if (GUILayout.Button("📋 检查ARKit兼容性"))
        {
            if (源对象 != null)
            {
                SkinnedMeshRenderer 渲染器 = 源对象.GetComponent<SkinnedMeshRenderer>();
                if (渲染器 != null)
                {
                    检查ARKit形态键(渲染器);
                }
            }
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUI.EndDisabledGroup();
        
        // 显示ARKit检查结果
        if (显示ARKit检查结果)
        {
            显示ARKit检查结果面板();
        }
    }
    
    // 获取当前形态键值
    private void 获取当前形态键值(SkinnedMeshRenderer 渲染器)
    {
        源形态键值.Clear();
        Mesh 网格 = 渲染器.sharedMesh;
        
        for (int i = 0; i < 网格.blendShapeCount; i++)
        {
            string 形态键名称 = 网格.GetBlendShapeName(i);
            float 值 = 渲染器.GetBlendShapeWeight(i);
            源形态键值[形态键名称] = 值;
        }
        
        EditorUtility.DisplayDialog("成功", $"已获取 {网格.blendShapeCount} 个形态键的当前值。", "确定");
    }
    
    // 复制所有形态键值
    private void 复制所有形态键值(SkinnedMeshRenderer 源渲染器)
    {
        获取当前形态键值(源渲染器);
        Debug.Log("所有形态键值已复制。");
    }
    
    // 粘贴选中的形态键值
    private void 粘贴选中的形态键值(SkinnedMeshRenderer 源渲染器)
    {
        if (目标对象 == null)
        {
            EditorUtility.DisplayDialog("错误", "请先选择一个目标对象。", "确定");
            return;
        }
        
        SkinnedMeshRenderer 目标渲染器 = 目标对象.GetComponent<SkinnedMeshRenderer>();
        if (目标渲染器 == null || 目标渲染器.sharedMesh == null)
        {
            EditorUtility.DisplayDialog("错误", "目标对象没有有效的SkinnedMeshRenderer组件。", "确定");
            return;
        }
        
        Mesh 源网格 = 源渲染器.sharedMesh;
        Mesh 目标网格 = 目标渲染器.sharedMesh;
        int 粘贴数量 = 0;
        int 成功数量 = 0;
        
        for (int i = 0; i < 源网格.blendShapeCount; i++)
        {
            if (i < 选中的形态键.Length && 选中的形态键[i])
            {
                string 形态键名称 = 源网格.GetBlendShapeName(i);
                int 目标索引 = 获取形态键索引(目标网格, 形态键名称);
                粘贴数量++;
                
                if (目标索引 >= 0 && 源形态键值.ContainsKey(形态键名称))
                {
                    float 值 = 源形态键值[形态键名称] * 传输速度;
                    目标渲染器.SetBlendShapeWeight(目标索引, Mathf.Clamp(值, 0f, 100f));
                    成功数量++;
                }
            }
        }
        
        string 结果信息 = $"尝试粘贴 {粘贴数量} 个形态键，成功 {成功数量} 个。";
        if (成功数量 < 粘贴数量)
        {
            结果信息 += $"\n{粘贴数量 - 成功数量} 个形态键在目标对象中未找到。";
        }
        
        Debug.Log($"已粘贴 {成功数量} 个形态键值到目标对象。");
        EditorUtility.DisplayDialog("完成", 结果信息, "确定");
    }
    
    // 复制并粘贴所有形态键
    private void 复制并粘贴所有形态键()
    {
        if (源对象 == null || 目标对象 == null)
            return;
            
        SkinnedMeshRenderer 源渲染器 = 源对象.GetComponent<SkinnedMeshRenderer>();
        SkinnedMeshRenderer 目标渲染器 = 目标对象.GetComponent<SkinnedMeshRenderer>();
        
        if (源渲染器 == null || 目标渲染器 == null)
            return;
            
        获取当前形态键值(源渲染器);
        粘贴选中的形态键值(源渲染器);
    }
    
    // 对比形态键名称
    private void 对比形态键名称()
    {
        if (源对象 == null || 目标对象 == null)
            return;
            
        SkinnedMeshRenderer 源渲染器 = 源对象.GetComponent<SkinnedMeshRenderer>();
        SkinnedMeshRenderer 目标渲染器 = 目标对象.GetComponent<SkinnedMeshRenderer>();
        
        if (源渲染器 == null || 目标渲染器 == null)
            return;
            
        Mesh 源网格 = 源渲染器.sharedMesh;
        Mesh 目标网格 = 目标渲染器.sharedMesh;
        
        List<string> 匹配的名称 = new List<string>();
        List<string> 源独有名称 = new List<string>();
        List<string> 目标独有名称 = new List<string>();
        
        // 收集源对象所有形态键名称
        Dictionary<string, bool> 源形态键字典 = new Dictionary<string, bool>();
        for (int i = 0; i < 源网格.blendShapeCount; i++)
        {
            string 名称 = 源网格.GetBlendShapeName(i);
            源形态键字典[名称] = true;
        }
        
        // 收集目标对象所有形态键名称
        Dictionary<string, bool> 目标形态键字典 = new Dictionary<string, bool>();
        for (int i = 0; i < 目标网格.blendShapeCount; i++)
        {
            string 名称 = 目标网格.GetBlendShapeName(i);
            目标形态键字典[名称] = true;
        }
        
        // 找出匹配的名称
        foreach (var 键值对 in 源形态键字典)
        {
            if (目标形态键字典.ContainsKey(键值对.Key))
            {
                匹配的名称.Add(键值对.Key);
            }
            else
            {
                源独有名称.Add(键值对.Key);
            }
        }
        
        // 找出目标独有的名称
        foreach (var 键值对 in 目标形态键字典)
        {
            if (!源形态键字典.ContainsKey(键值对.Key))
            {
                目标独有名称.Add(键值对.Key);
            }
        }
        
        StringBuilder 结果文本 = new StringBuilder();
        结果文本.AppendLine($"🔍 形态键名称对比结果:");
        结果文本.AppendLine($"📊 匹配的形态键: {匹配的名称.Count}个");
        结果文本.AppendLine($"📤 源对象独有: {源独有名称.Count}个");
        结果文本.AppendLine($"🎯 目标对象独有: {目标独有名称.Count}个");
        结果文本.AppendLine();
        
        if (匹配的名称.Count > 0)
        {
            结果文本.AppendLine("✅ 匹配的形态键:");
            foreach (string 名称 in 匹配的名称)
            {
                结果文本.AppendLine($"   • {名称}");
            }
            结果文本.AppendLine();
        }
        
        if (源独有名称.Count > 0)
        {
            结果文本.AppendLine("⚠️ 源对象独有的形态键:");
            foreach (string 名称 in 源独有名称)
            {
                结果文本.AppendLine($"   • {名称}");
            }
            结果文本.AppendLine();
        }
        
        if (目标独有名称.Count > 0)
        {
            结果文本.AppendLine("⚠️ 目标对象独有的形态键:");
            foreach (string 名称 in 目标独有名称)
            {
                结果文本.AppendLine($"   • {名称}");
            }
        }
        
        EditorUtility.DisplayDialog("形态键对比结果", 结果文本.ToString(), "确定");
    }
    
    // 重置所有形态键为0
    private void 重置所有形态键为0()
    {
        if (目标对象 == null)
            return;
            
        SkinnedMeshRenderer 目标渲染器 = 目标对象.GetComponent<SkinnedMeshRenderer>();
        if (目标渲染器 == null)
            return;
            
        Mesh 目标网格 = 目标渲染器.sharedMesh;
        for (int i = 0; i < 目标网格.blendShapeCount; i++)
        {
            目标渲染器.SetBlendShapeWeight(i, 0f);
        }
        
        Debug.Log("已重置所有形态键值为0。");
        EditorUtility.DisplayDialog("完成", "所有形态键值已重置为0。", "确定");
    }
    
    // 检查ARKit形态键
    private void 检查ARKit形态键(SkinnedMeshRenderer 渲染器)
    {
        if (渲染器 == null || 渲染器.sharedMesh == null)
        {
            EditorUtility.DisplayDialog("错误", "对象没有有效的网格。", "确定");
            return;
        }
        
        Mesh 网格 = 渲染器.sharedMesh;
        缺失的ARKit形态键.Clear();
        
        // 收集对象的所有形态键名称
        HashSet<string> 现有形态键 = new HashSet<string>();
        for (int i = 0; i < 网格.blendShapeCount; i++)
        {
            现有形态键.Add(网格.GetBlendShapeName(i));
        }
        
        // 检查ARKit标准形态键
        foreach (string arkit形态键 in ARKit标准形态键)
        {
            if (!现有形态键.Contains(arkit形态键))
            {
                缺失的ARKit形态键.Add(arkit形态键);
            }
        }
        
        // 统计结果
        int 现有ARKit数量 = ARKit标准形态键.Length - 缺失的ARKit形态键.Count;
        float 兼容率 = (float)现有ARKit数量 / ARKit标准形态键.Length * 100f;
        
        StringBuilder 结果文本 = new StringBuilder();
        结果文本.AppendLine($"🎭 ARKit形态键兼容性检查");
        结果文本.AppendLine($"📊 总标准形态键: {ARKit标准形态键.Length}个");
        结果文本.AppendLine($"✅ 已实现的ARKit形态键: {现有ARKit数量}个");
        结果文本.AppendLine($"❌ 缺失的ARKit形态键: {缺失的ARKit形态键.Count}个");
        结果文本.AppendLine($"📈 ARKit兼容率: {兼容率:F1}%");
        结果文本.AppendLine();
        
        if (缺失的ARKit形态键.Count > 0)
        {
            结果文本.AppendLine("🔍 缺失的ARKit形态键列表:");
            foreach (string 缺失键 in 缺失的ARKit形态键)
            {
                // 根据形态键名称分类显示
                if (缺失键.Contains("brow"))
                {
                    结果文本.AppendLine($"   [眉毛] {缺失键}");
                }
                else if (缺失键.Contains("eye"))
                {
                    结果文本.AppendLine($"   [眼睛] {缺失键}");
                }
                else if (缺失键.Contains("nose"))
                {
                    结果文本.AppendLine($"   [鼻子] {缺失键}");
                }
                else if (缺失键.Contains("cheek"))
                {
                    结果文本.AppendLine($"   [脸颊] {缺失键}");
                }
                else if (缺失键.Contains("mouth") || 缺失键.Contains("jaw"))
                {
                    结果文本.AppendLine($"   [嘴巴] {缺失键}");
                }
                else if (缺失键.Contains("tongue"))
                {
                    结果文本.AppendLine($"   [舌头] {缺失键}");
                }
                else
                {
                    结果文本.AppendLine($"   [其他] {缺失键}");
                }
            }
        }
        else
        {
            结果文本.AppendLine("🎉 完美兼容ARKit所有52个标准形态键！");
        }
        
        显示ARKit检查结果 = true;
        EditorUtility.DisplayDialog("ARKit兼容性检查", 结果文本.ToString(), "确定");
    }
    
    // 显示ARKit检查结果面板
    private void 显示ARKit检查结果面板()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("🎭 ARKit兼容性检查结果", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox($"缺失 {缺失的ARKit形态键.Count} 个ARKit标准形态键", 
            缺失的ARKit形态键.Count > 0 ? MessageType.Warning : MessageType.Info);
        
        ARKit滚动位置 = EditorGUILayout.BeginScrollView(ARKit滚动位置, GUILayout.Height(200));
        
        foreach (string 形态键 in ARKit标准形态键)
        {
            bool 存在 = !缺失的ARKit形态键.Contains(形态键);
            
            EditorGUILayout.BeginHorizontal();
            
            // 根据存在状态显示不同的图标
            GUILayout.Label(存在 ? "✅" : "❌", GUILayout.Width(20));
            
            // 根据形态键类型显示不同的颜色
            if (形态键.Contains("brow"))
            {
                GUI.color = new Color(0.8f, 0.6f, 0.4f); // 棕色
            }
            else if (形态键.Contains("eye"))
            {
                GUI.color = new Color(0.2f, 0.6f, 0.8f); // 蓝色
            }
            else if (形态键.Contains("mouth") || 形态键.Contains("jaw"))
            {
                GUI.color = new Color(0.8f, 0.3f, 0.3f); // 红色
            }
            else if (形态键.Contains("tongue"))
            {
                GUI.color = new Color(0.8f, 0.2f, 0.6f); // 粉色
            }
            else
            {
                GUI.color = Color.white;
            }
            
            GUILayout.Label(形态键, GUILayout.Width(150));
            
            // 恢复颜色
            GUI.color = Color.white;
            
            // 显示状态
            GUILayout.Label(存在 ? "已实现" : "缺失", GUILayout.Width(60));
            
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndScrollView();
        
        // 导出报告按钮
        if (GUILayout.Button("📄 导出ARKit兼容性报告"))
        {
            导出ARKit兼容性报告();
        }
    }
    
    // 导出ARKit兼容性报告
    private void 导出ARKit兼容性报告()
    {
        string 路径 = EditorUtility.SaveFilePanel("保存ARKit兼容性报告", "", "ARKit兼容性报告.txt", "txt");
        
        if (!string.IsNullOrEmpty(路径))
        {
            StringBuilder 报告 = new StringBuilder();
            报告.AppendLine("🎭 ARKit形态键兼容性报告");
            报告.AppendLine("=".PadRight(50, '='));
            报告.AppendLine($"生成时间: {System.DateTime.Now}");
            报告.AppendLine($"对象名称: {源对象?.name ?? "未指定"}");
            报告.AppendLine();
            报告.AppendLine("📊 统计信息:");
            报告.AppendLine($"   总标准形态键: {ARKit标准形态键.Length}个");
            报告.AppendLine($"   已实现的ARKit形态键: {ARKit标准形态键.Length - 缺失的ARKit形态键.Count}个");
            报告.AppendLine($"   缺失的ARKit形态键: {缺失的ARKit形态键.Count}个");
            报告.AppendLine($"   兼容率: {(float)(ARKit标准形态键.Length - 缺失的ARKit形态键.Count) / ARKit标准形态键.Length * 100:F1}%");
            报告.AppendLine();
            报告.AppendLine("🔍 详细列表:");
            报告.AppendLine();
            
            // 按类别分组显示
            Dictionary<string, List<string>> 分类形态键 = new Dictionary<string, List<string>>();
            
            foreach (string 形态键 in ARKit标准形态键)
            {
                string 类别 = "其他";
                
                if (形态键.Contains("brow")) 类别 = "眉毛";
                else if (形态键.Contains("eye")) 类别 = "眼睛";
                else if (形态键.Contains("nose")) 类别 = "鼻子";
                else if (形态键.Contains("cheek")) 类别 = "脸颊";
                else if (形态键.Contains("mouth") || 形态键.Contains("jaw")) 类别 = "嘴巴";
                else if (形态键.Contains("tongue")) 类别 = "舌头";
                
                if (!分类形态键.ContainsKey(类别))
                {
                    分类形态键[类别] = new List<string>();
                }
                分类形态键[类别].Add(形态键);
            }
            
            foreach (var 分类 in 分类形态键)
            {
                报告.AppendLine($"{分类.Key}区域 ({分类.Value.Count}个):");
                报告.AppendLine("-".PadRight(50, '-'));
                
                foreach (string 形态键 in 分类.Value)
                {
                    bool 存在 = !缺失的ARKit形态键.Contains(形态键);
                    报告.AppendLine($"  {(存在 ? "✅" : "❌")} {形态键} - {(存在 ? "已实现" : "缺失")}");
                }
                报告.AppendLine();
            }
            
            System.IO.File.WriteAllText(路径, 报告.ToString());
            EditorUtility.DisplayDialog("成功", $"ARKit兼容性报告已保存到:\n{路径}", "确定");
        }
    }
    
    // 获取形态键索引
    private int 获取形态键索引(Mesh 网格, string 形态键名称)
    {
        for (int i = 0; i < 网格.blendShapeCount; i++)
        {
            if (网格.GetBlendShapeName(i) == 形态键名称)
            {
                return i;
            }
        }
        return -1;
    }
}