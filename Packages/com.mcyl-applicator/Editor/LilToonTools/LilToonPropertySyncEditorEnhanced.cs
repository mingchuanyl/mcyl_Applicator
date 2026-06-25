using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace LilToonTools
{
    public class LilToonPropertySyncEditorEnhanced : EditorWindow
    {
        private Material sourceMaterial;
        private List<Material> targetMaterials = new List<Material>();
        private Vector2 targetScrollPosition;
        
        // 窗口滚动
        private Vector2 windowScrollPosition;
        
        // 属性数据
        private Dictionary<string, PropertyInfo> availableProperties = new Dictionary<string, PropertyInfo>();
        private List<string> filteredPropertyNames = new List<string>();
        private Vector2 propertyScrollPosition;
        
        // 过滤和搜索
        private string searchText = "";
        private bool showOnlyDifferent = false;
        private bool showOnlyAvailable = true;
        
        // 分组管理
        private Dictionary<string, bool> groupExpandedStates = new Dictionary<string, bool>();
        private Dictionary<string, List<string>> categorizedProperties = new Dictionary<string, List<string>>();
        
        // 预设管理
        private List<PropertyPreset> propertyPresets = new List<PropertyPreset>();
        private int selectedPresetIndex = -1;
        private string newPresetName = "New Preset";
        
        // 性能优化
        private bool needsRefresh = false;
        private double lastRefreshTime = 0;
        private const double refreshDelay = 0.1f;
        private bool isRefreshing = false;
        
        // 移除标记列表
        private List<int> materialIndicesToRemove = new List<int>();
        
        // 预设文件路径
        private const string PRESET_FOLDER_PATH = "Assets/Script/Editor/LilToonTools/LilToonPropertyPresets";
        
        // 窗口数据保存路径
        private const string WINDOW_DATA_PATH = "Assets/Script/Editor/LilToonTools/WindowData.json";
        
        // 根据图片重新定义的分组（按图片顺序）
        private static readonly Dictionary<string, string[]> defaultPropertyGroups = new Dictionary<string, string[]>
        {
            // 主色 / Alpha 设置
            { "主色/Alpha", new string[] { 
                "_Color", "_MainTex", "_MainColorAdjustMask", 
                "_MainTexHSVG", "_MainGradationTex", "_MainGradationStrength",
                "_UseMain2ndTex", "_UseMain3rdTex",
                // 通用属性名
                "_BaseColor", "_BaseMap", "_Albedo", "_Diffuse", "_MainColor"
            } },
            
            // 阴影
            { "阴影", new string[] { 
                "_UseShadow", "_ShadowStrengthMask", "_ShadowStrength", 
                "_ShadowColorTex", "_ShadowColor", "_ShadowBorder", "_ShadowBlur", 
                "_ShadowNormalStrength", "_ShadowReceive", "_Shadow2ndColorTex", 
                "_Shadow2ndColor", "_Shadow2ndBorder", "_Shadow2ndBlur", 
                "_Shadow2ndNormalStrength", "_Shadow2ndReceive", "_Shadow3rdColorTex", 
                "_Shadow3rdColor", "_ShadowBorderColor", "_ShadowBorderRange", 
                "_ShadowMainStrength", "_ShadowEnvStrength", "lilShadowCasterBias", 
                "_ShadowBlurMask", "_ShadowBorderMask", "_ShadowStrengthMaskLOD", 
                "_ShadowBlurMaskLOD",
                // 通用属性名
                "_Shadow", "_ReceiveShadows"
            } },
            
            // 自发光
            { "自发光", new string[] { 
                "_UseEmission", "_EmissionMap", "_EmissionColor", 
                "_EmissionBlendMask", "_EmissionBlend", "_EmissionBlendMode", 
                "_EmissionBlink", "_EmissionUseGrad", "_EmissionParallaxDepth", 
                "_EmissionFluorescence", "_EmissionMainStrength",
                // 通用属性名
                "_Emission", "_Emissive", "_EmissionStrength", "_EmissionIntensity"
            } },
            
            // 背光
            { "背光", new string[] { 
                "_UseBacklight", "_BacklightColorTex", "_BacklightColor", 
                "_BacklightMainStrength", "_BacklightReceiveShadow", 
                "_BacklightBackfaceMask", "_BacklightNormalStrength", 
                "_BacklightBorder", "_BacklightBlur", "_BacklightDirectivity", 
                "_BacklightViewStrength",
                // 通用属性名
                "_Backlight", "_Translucency", "_Subsurface"
            } },
            
            // 反射
            { "反射", new string[] { 
                "_UseReflection", "_SmoothnessTex", "_Smoothness", 
                "_GSAAStrength", "_MetallicGlossMap", "_Metallic", 
                "_ReflectionColorTex", "_ReflectionColor", "_Reflectance", 
                "_SpecularNormalStrength", "_SpecularBorder", "_SpecularBlur", 
                "_ApplySpecularFA", "_ApplyReflection", "_ReflectionBlendMode",
                // 通用属性名
                "_Reflection", "_Reflect", "_Specular", "_Glossiness", "_Roughness"
            } },
            
            // 边缘光
            { "边缘光", new string[] { 
                "_UseRim", "_RimColorTex", "_RimColor", "_RimMainStrength", 
                "_RimEnableLighting", "_RimShadowMask", "_RimBackfaceMask", 
                "_RimBlendMode", "_RimDirStrength", "_RimDirRange", 
                "_RimBorder", "_RimBlur", "_RimIndirRange", "_RimIndirColor", 
                "_RimIndirBorder", "_RimIndirBlur", "_RimNormalStrength", 
                "_RimFresnelPower", "_RimVRParallaxStrength",
                // 通用属性名
                "_Rim", "_RimLight"
            } },
            
            // 边缘光阴影
            { "边缘光阴影", new string[] { 
                "_UseRimShade", "_RimShadeMask", "_RimShadeColor", 
                "_RimShadeNormalStrength", "_RimShadeBorder", "_RimShadeBlur", 
                "_RimShadeFresnelPower"
            } },
            
            // 轮廓
            { "轮廓", new string[] { 
                "_OutlineTex", "_OutlineColor", "OutlineTexHSVG", 
                "_OutlineLitColor", "_OutlineEnableLighting", "_OutlineWidthMask", 
                "_OutlineWidth", "_OutlineFixWidth", "_OutlineVertexR2Width", 
                "_OutlineDeleteMesh", "_OutlineZBias", "_OutlineDisableInVR", 
                "_OutlineVectorTex", "_OutlineVectorScale", "_OutlineVectorUVMode",
                // 通用属性名
                "_Outline"
            } },
            
            // 闪粉
            { "闪粉", new string[] { 
                "_UseGlitter", "GlitterUVMode", "_GlitterColorTex", 
                "_GlitterColor", "_GlitterMainStrength", "_GlitterEnableLighting", 
                "_GlitterShadowMask", "_GlitterBackfaceMask", "_GlitterApplyShape", 
                "_GlitterParams1", "_GlitterScaleRandomize", "_GlitterSensitivity", 
                "_GlitterParams2", "_GlitterVRParallaxStrength", 
                "_GlitterNormalStrength", "_GlitterPostContrast",
                // 通用属性名
                "_Glitter", "_Sparkle"
            } },
            
            // 渲染设置
            { "渲染设置", new string[] { 
                "_Cull", "_SrcBlend", "_DstBlend", "_ZWrite", "_AlphaToMask", 
                "_Cutoff", "_ZTest", "_ZClip", "_OffsetFactor", "_OffsetUnits",
                // 通用属性名
                "_Blend", "_BlendMode", "_Queue", "_RenderType"
            } },
            
            // UV动画
            { "UV动画", new string[] { 
                "_MainTex_Scroll", "_MainTex_Angle", "_MainTex_UVMode", 
                "_MainTex_Zoom", "_MainTex_UVSec",
                // 通用属性名
                "_UV", "_Scroll", "_Offset", "_Tiling", "_ST"
            } },
            
            // 高级设置
            { "高级设置", new string[] { 
                "_SubPassCutoff", "_AlphaMaskMode", "_FurNoiseMask", 
                "_DistanceFade", "_AudioLink", "_Dither",
                // 通用属性名
                "_BumpScale", "_BumpMap", "_NormalMap", "_Occlusion", "_Parallax", "_DetailMask"
            } }
        };
        
        // 分组显示顺序
        private List<string> groupDisplayOrder = new List<string>
        {
            "主色/Alpha", "阴影", "自发光", "背光", "反射", 
            "边缘光", "边缘光阴影", "轮廓", "闪粉", 
            "渲染设置", "UV动画", "高级设置", "自定义属性"
        };
        
        // 属性类型图标映射
        private static readonly Dictionary<ShaderUtil.ShaderPropertyType, string> propertyTypeIcons = new Dictionary<ShaderUtil.ShaderPropertyType, string>
        {
            { ShaderUtil.ShaderPropertyType.Color, "▣" },
            { ShaderUtil.ShaderPropertyType.Vector, "↑" },
            { ShaderUtil.ShaderPropertyType.Float, "1.0" },
            { ShaderUtil.ShaderPropertyType.Range, "↔" },
            { ShaderUtil.ShaderPropertyType.TexEnv, "▦" }
        };
        
        [MenuItem("Tools/材质/智能材质属性同步工具")]
        public static void ShowWindow()
        {
            var window = GetWindow<LilToonPropertySyncEditorEnhanced>("材质属性同步工具");
            window.minSize = new Vector2(900, 750);
        }
        
        private void OnEnable()
        {
            InitializeGroups();
            EnsurePresetFolderExists();
            LoadPresets();
            LoadWindowData(); // 加载窗口数据
        }
        
        private void OnDisable()
        {
            SaveWindowData(); // 保存窗口数据
        }
        
        private void Update()
        {
            if (needsRefresh && EditorApplication.timeSinceStartup - lastRefreshTime > refreshDelay)
            {
                needsRefresh = false;
                RefreshFilteredProperties();
            }
        }
        
        private void InitializeGroups()
        {
            // 初始化所有分组的展开状态和容器
            foreach (var group in defaultPropertyGroups.Keys)
            {
                groupExpandedStates[group] = false; // 默认收起
                categorizedProperties[group] = new List<string>();
            }
            // 添加自定义分组
            groupExpandedStates["自定义属性"] = false;
            categorizedProperties["自定义属性"] = new List<string>();
        }
        
        private void OnGUI()
        {
            // 清空待移除列表
            materialIndicesToRemove.Clear();
            
            // 整个窗口的垂直滚动条
            windowScrollPosition = EditorGUILayout.BeginScrollView(windowScrollPosition, false, false);
            {
                EditorGUILayout.Space(10);
                
                EditorGUILayout.LabelField("智能材质属性同步工具", EditorStyles.boldLabel);
                EditorGUILayout.Space(5);
                
                DrawSourceMaterialSection();
                
                EditorGUILayout.Space(15);
                
                DrawTargetMaterialsSection();
                
                EditorGUILayout.Space(15);
                
                DrawPresetSection();
                
                EditorGUILayout.Space(15);
                
                if (sourceMaterial == null)
                {
                    EditorGUILayout.HelpBox("请先选择源材质", MessageType.Info);
                    EditorGUILayout.EndScrollView();
                    return;
                }
                
                if (availableProperties.Count == 0)
                {
                    EditorGUILayout.HelpBox("点击扫描按钮分析源材质属性", MessageType.Warning);
                    EditorGUILayout.EndScrollView();
                    return;
                }
                
                if (targetMaterials.Count == 0)
                {
                    EditorGUILayout.HelpBox("请添加至少一个目标材质", MessageType.Info);
                    EditorGUILayout.EndScrollView();
                    return;
                }
                
                DrawFilterSection();
                
                EditorGUILayout.Space(10);
                
                DrawOptimizedPropertyTable();
                
                EditorGUILayout.Space(15);
                
                DrawActionButtons();
            }
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawSourceMaterialSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("源材质分析", EditorStyles.boldLabel);
                
                EditorGUI.BeginChangeCheck();
                sourceMaterial = (Material)EditorGUILayout.ObjectField(
                    "源材质:", 
                    sourceMaterial, 
                    typeof(Material), 
                    false);
                
                if (EditorGUI.EndChangeCheck() && sourceMaterial != null)
                {
                    ScheduleRefresh();
                }
                
                if (sourceMaterial != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("Shader: ", GUILayout.Width(50));
                        EditorGUILayout.SelectableLabel(sourceMaterial.shader?.name ?? "N/A", 
                            EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                    }
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button("使用选中材质", GUILayout.Width(120)))
                        {
                            SetSelectedAsSource();
                        }
                        
                        GUI.enabled = !isRefreshing;
                        if (GUILayout.Button(isRefreshing ? "扫描中..." : "扫描属性", GUILayout.Width(100)))
                        {
                            ScanSourceMaterialProperties();
                        }
                        GUI.enabled = true;
                        
                        if (GUILayout.Button("导出属性", GUILayout.Width(80)))
                        {
                            ExportProperties();
                        }
                        
                        GUILayout.FlexibleSpace();
                        GUILayout.Label($"属性: {availableProperties.Count}", EditorStyles.miniLabel);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndVertical();
        }
        
        private void DrawTargetMaterialsSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("目标材质列表", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("+ 添加槽位", GUILayout.Width(80)))
                    {
                        targetMaterials.Add(null);
                    }
                    
                    if (GUILayout.Button("使用选中材质", GUILayout.Width(120)))
                    {
                        AddSelectedMaterials();
                    }
                    
                    if (GUILayout.Button("使用选中游戏对象材质", GUILayout.Width(150)))
                    {
                        AddMaterialsFromSelectedGameObjects();
                    }
                    
                    if (GUILayout.Button("从文件夹", GUILayout.Width(80)))
                    {
                        AddMaterialsFromFolder();
                    }
                    
                    if (targetMaterials.Count > 0 && GUILayout.Button("清空", GUILayout.Width(60)))
                    {
                        if (EditorUtility.DisplayDialog("确认", "清空所有目标材质吗？", "是", "否"))
                        {
                            targetMaterials.Clear();
                            ScheduleRefresh();
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(5);
                
                targetScrollPosition = EditorGUILayout.BeginScrollView(targetScrollPosition, GUILayout.Height(120));
                {
                    for (int i = 0; i < targetMaterials.Count; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        {
                            EditorGUI.BeginChangeCheck();
                            targetMaterials[i] = (Material)EditorGUILayout.ObjectField(
                                $"目标 {i + 1}:",
                                targetMaterials[i],
                                typeof(Material),
                                false,
                                GUILayout.ExpandWidth(true));
                            
                            if (EditorGUI.EndChangeCheck() && targetMaterials[i] != null)
                            {
                                ScheduleRefresh();
                            }
                            
                            if (targetMaterials[i] != null)
                            {
                                int compatibleCount = GetCompatiblePropertyCount(targetMaterials[i]);
                                float compatibility = availableProperties.Count > 0 ? 
                                    (float)compatibleCount / availableProperties.Count : 0;
                                
                                Color indicatorColor = compatibility > 0.8f ? Color.green : 
                                                    compatibility > 0.5f ? Color.yellow : Color.red;
                                
                                GUI.color = indicatorColor;
                                GUILayout.Label($"{compatibility:P0}", EditorStyles.miniLabel, GUILayout.Width(40));
                                GUI.color = Color.white;
                            }
                            
                            if (GUILayout.Button("×", GUILayout.Width(25)))
                            {
                                // 标记要移除的索引
                                materialIndicesToRemove.Add(i);
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    
                    // 处理标记的移除操作
                    if (materialIndicesToRemove.Count > 0)
                    {
                        // 从大到小排序，避免索引变化问题
                        materialIndicesToRemove.Sort((a, b) => b.CompareTo(a));
                        foreach (int index in materialIndicesToRemove)
                        {
                            if (index >= 0 && index < targetMaterials.Count)
                            {
                                targetMaterials.RemoveAt(index);
                            }
                        }
                        materialIndicesToRemove.Clear();
                        ScheduleRefresh();
                    }
                    
                    if (targetMaterials.Count == 0)
                    {
                        EditorGUILayout.HelpBox("点击上方按钮添加目标材质", MessageType.Info);
                    }
                }
                EditorGUILayout.EndScrollView();
                
                if (targetMaterials.Count > 0)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button("验证兼容性", GUILayout.Width(100)))
                        {
                            ValidateCompatibility();
                        }
                        
                        if (GUILayout.Button("移除不兼容", GUILayout.Width(100)))
                        {
                            RemoveIncompatibleMaterials();
                        }
                        
                        GUILayout.FlexibleSpace();
                        GUILayout.Label($"数量: {targetMaterials.Count}", EditorStyles.miniLabel);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndVertical();
        }
        
        private void DrawPresetSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("属性预设管理", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                {
                    string[] presetNames = propertyPresets.Select(p => p.name).ToArray();
                    if (presetNames.Length == 0) presetNames = new string[] { "无预设" };
                    
                    int oldIndex = selectedPresetIndex;
                    selectedPresetIndex = EditorGUILayout.Popup("预设:", 
                        Mathf.Clamp(selectedPresetIndex, 0, presetNames.Length - 1), 
                        presetNames, GUILayout.Width(300));
                    
                    if (selectedPresetIndex >= 0 && selectedPresetIndex < propertyPresets.Count)
                    {
                        if (GUILayout.Button("应用", GUILayout.Width(60)))
                        {
                            ApplyPreset(propertyPresets[selectedPresetIndex]);
                        }
                        
                        if (GUILayout.Button("删除", GUILayout.Width(60)))
                        {
                            DeletePreset(propertyPresets[selectedPresetIndex]);
                        }
                    }
                    
                    GUILayout.FlexibleSpace();
                    GUILayout.Label($"预设: {propertyPresets.Count}", EditorStyles.miniLabel);
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(5);
                
                EditorGUILayout.BeginHorizontal();
                {
                    newPresetName = EditorGUILayout.TextField("新预设名:", newPresetName, GUILayout.Width(200));
                    
                    if (GUILayout.Button("从当前选择创建", GUILayout.Width(120)))
                    {
                        CreatePresetFromSelection();
                    }
                    
                    if (GUILayout.Button("保存当前预设", GUILayout.Width(100)))
                    {
                        SavePreset();
                    }
                    
                    if (GUILayout.Button("重新加载", GUILayout.Width(80)))
                    {
                        LoadPresets();
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                if (propertyPresets.Count > 0)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("快速应用:", EditorStyles.miniLabel);
                    
                    EditorGUILayout.BeginHorizontal();
                    {
                        foreach (var preset in propertyPresets.Take(5))
                        {
                            if (GUILayout.Button(preset.name, GUILayout.ExpandWidth(false)))
                            {
                                ApplyPreset(preset);
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField($"预设位置: {PRESET_FOLDER_PATH}", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndVertical();
        }
        
        private void DrawFilterSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("属性过滤", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.Label("搜索:", GUILayout.Width(40));
                    string oldSearch = searchText;
                    searchText = EditorGUILayout.TextField(searchText);
                    
                    if (searchText != oldSearch)
                    {
                        ScheduleRefresh();
                    }
                    
                    if (GUILayout.Button("清空", GUILayout.Width(50)))
                    {
                        searchText = "";
                        ScheduleRefresh();
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                {
                    bool oldDiff = showOnlyDifferent;
                    showOnlyDifferent = EditorGUILayout.ToggleLeft("仅显示不同", showOnlyDifferent, GUILayout.Width(100));
                    
                    bool oldAvail = showOnlyAvailable;
                    showOnlyAvailable = EditorGUILayout.ToggleLeft("仅显示可用", showOnlyAvailable, GUILayout.Width(100));
                    
                    if (oldDiff != showOnlyDifferent || oldAvail != showOnlyAvailable)
                    {
                        ScheduleRefresh();
                    }
                    
                    GUILayout.FlexibleSpace();
                    
                    if (GUILayout.Button("全选", GUILayout.Width(60)))
                    {
                        SelectAllProperties(true);
                    }
                    
                    if (GUILayout.Button("全不选", GUILayout.Width(60)))
                    {
                        SelectAllProperties(false);
                    }
                    
                    if (GUILayout.Button("反选", GUILayout.Width(60)))
                    {
                        InvertSelection();
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                // 快速分组选择
                if (availableProperties.Count > 0)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("快速分组选择:", EditorStyles.miniLabel);
                    
                    int columns = 3;
                    int currentCol = 0;
                    EditorGUILayout.BeginHorizontal();
                    {
                        foreach (var group in defaultPropertyGroups.Keys)
                        {
                            if (categorizedProperties.ContainsKey(group))
                            {
                                int count = categorizedProperties[group].Count(p => FilterPropertyByName(p));
                                if (count > 0)
                                {
                                    if (currentCol >= columns)
                                    {
                                        currentCol = 0;
                                        EditorGUILayout.EndHorizontal();
                                        EditorGUILayout.BeginHorizontal();
                                    }
                                    
                                    if (GUILayout.Button($"{group} ({count})", GUILayout.ExpandWidth(false)))
                                    {
                                        ToggleGroupSelection(group);
                                    }
                                    currentCol++;
                                }
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndVertical();
        }
        
        private void DrawOptimizedPropertyTable()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("属性同步配置", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField($"显示: {filteredPropertyNames.Count}/{availableProperties.Count}", 
                        EditorStyles.miniLabel, GUILayout.Width(100));
                }
                EditorGUILayout.EndHorizontal();
                
                propertyScrollPosition = EditorGUILayout.BeginScrollView(propertyScrollPosition, 
                    GUILayout.ExpandHeight(true));
                {
                    bool hasVisibleProperties = false;
                    
                    // 按分组顺序显示
                    foreach (var groupKey in groupDisplayOrder)
                    {
                        if (!categorizedProperties.ContainsKey(groupKey)) continue;
                        
                        var propertiesInGroup = categorizedProperties[groupKey]
                            .Where(propName => availableProperties.ContainsKey(propName))
                            .Where(FilterPropertyByName)
                            .OrderBy(propName => propName)  // 按字母排序
                            .ToList();
                        
                        if (propertiesInGroup.Count == 0)
                            continue;
                        
                        hasVisibleProperties = true;
                        
                        // 分组标题行
                        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                        {
                            // 折叠按钮
                            bool isExpanded = groupExpandedStates.ContainsKey(groupKey) && groupExpandedStates[groupKey];
                            bool newExpanded = EditorGUILayout.Foldout(isExpanded, $"{groupKey} ({propertiesInGroup.Count})", true);
                            if (isExpanded != newExpanded)
                            {
                                groupExpandedStates[groupKey] = newExpanded;
                            }
                            
                            GUILayout.FlexibleSpace();
                            
                            // 分组全选复选框
                            bool anySelected = propertiesInGroup.Any(p => availableProperties[p].selected);
                            bool allSelected = propertiesInGroup.All(p => availableProperties[p].selected);
                            
                            EditorGUI.BeginChangeCheck();
                            bool groupSelected = EditorGUILayout.Toggle(anySelected, GUILayout.Width(20));
                            if (EditorGUI.EndChangeCheck())
                            {
                                foreach (var propName in propertiesInGroup)
                                {
                                    var propInfo = availableProperties[propName];
                                    if (propInfo.isAvailableInAllTargets || !showOnlyAvailable)
                                    {
                                        propInfo.selected = groupSelected;
                                    }
                                }
                            }
                            
                            // 显示选择状态
                            string selectionStatus = allSelected ? "全选" : (anySelected ? "部分" : "无");
                            GUILayout.Label(selectionStatus, EditorStyles.miniLabel, GUILayout.Width(40));
                        }
                        EditorGUILayout.EndHorizontal();
                        
                        // 分组内容
                        if (groupExpandedStates.ContainsKey(groupKey) && groupExpandedStates[groupKey])
                        {
                            foreach (var propName in propertiesInGroup)
                            {
                                DrawPropertyRow(propName);
                            }
                        }
                    }
                    
                    if (!hasVisibleProperties)
                    {
                        EditorGUILayout.HelpBox(
                            string.IsNullOrEmpty(searchText) ? 
                            "没有可显示的属性" : 
                            "没有找到匹配的属性",
                            MessageType.Info);
                    }
                }
                EditorGUILayout.EndScrollView();
                
                // 统计信息
                int selectedCount = availableProperties.Values.Count(p => p.selected);
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.Label($"已选择 {selectedCount} 个属性", EditorStyles.miniLabel);
                    GUILayout.FlexibleSpace();
                    
                    if (GUILayout.Button("选择所有不同", GUILayout.Width(100)))
                    {
                        SelectOnlyDifferentProperties();
                    }
                    
                    if (GUILayout.Button("选择所有兼容", GUILayout.Width(100)))
                    {
                        SelectOnlyCompatibleProperties();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }
        
        private void DrawPropertyRow(string propName)
        {
            if (!availableProperties.ContainsKey(propName)) return;
            
            var propInfo = availableProperties[propName];
            
            // 设置背景色
            if (!propInfo.isAvailableInAllTargets)
            {
                GUI.backgroundColor = new Color(1f, 0.7f, 0.7f, 0.3f);
            }
            else if (propInfo.isDifferent)
            {
                GUI.backgroundColor = new Color(1f, 1f, 0.7f, 0.3f);
            }
            
            EditorGUILayout.BeginHorizontal();
            {
                // 选择复选框
                EditorGUI.BeginDisabledGroup(!propInfo.isAvailableInAllTargets && showOnlyAvailable);
                bool oldSelected = propInfo.selected;
                propInfo.selected = EditorGUILayout.Toggle(propInfo.selected, GUILayout.Width(40));
                if (oldSelected != propInfo.selected)
                {
                    Repaint();
                }
                EditorGUI.EndDisabledGroup();
                
                // 属性名
                EditorGUILayout.LabelField(propName, GUILayout.Width(200));
                
                // 类型
                string typeIcon = propertyTypeIcons.ContainsKey(propInfo.type) ? 
                    propertyTypeIcons[propInfo.type] : "?";
                EditorGUILayout.LabelField(typeIcon, GUILayout.Width(30));
                
                // 源值
                string displayValue = propInfo.sourceValueDisplay;
                if (displayValue.Length > 20) displayValue = displayValue.Substring(0, 20) + "...";
                EditorGUILayout.LabelField(displayValue, GUILayout.Width(150));
                
                // 目标状态
                string statusText = propInfo.isAvailableInAllTargets ? 
                    (propInfo.isDifferent ? "不同" : "相同") : 
                    "缺失";
                Color statusColor = propInfo.isAvailableInAllTargets ? 
                    (propInfo.isDifferent ? Color.yellow : Color.green) : Color.red;
                
                GUI.color = statusColor;
                EditorGUILayout.LabelField(statusText, GUILayout.Width(80));
                GUI.color = Color.white;
                
                // 操作按钮
                EditorGUILayout.BeginHorizontal(GUILayout.Width(100));
                {
                    if (GUILayout.Button("详情", GUILayout.Width(45)))
                    {
                        ShowPropertyDetails(propName);
                    }
                    
                    if (propInfo.isDifferent && propInfo.isAvailableInAllTargets && 
                        GUILayout.Button("同步", GUILayout.Width(45)))
                    {
                        SyncSingleProperty(propName);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndHorizontal();
            
            GUI.backgroundColor = Color.white;
        }
        
        private void DrawActionButtons()
        {
            int selectedCount = availableProperties.Values.Count(p => p.selected);
            int compatibleSelectedCount = availableProperties.Values.Count(p => p.selected && p.isAvailableInAllTargets);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField($"批量操作", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.Label($"目标材质: {targetMaterials.Count}", EditorStyles.miniLabel);
                    GUILayout.Label($"选择属性: {selectedCount}", EditorStyles.miniLabel);
                    GUILayout.Label($"兼容属性: {compatibleSelectedCount}", EditorStyles.miniLabel);
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(5);
                
                EditorGUILayout.BeginHorizontal();
                {
                    GUI.enabled = compatibleSelectedCount > 0;
                    if (GUILayout.Button("预览同步", GUILayout.Height(30)))
                    {
                        PreviewSync();
                    }
                    
                    GUI.color = Color.green;
                    if (GUILayout.Button($"执行同步", GUILayout.Height(30), GUILayout.Width(150)))
                    {
                        ExecuteSync();
                    }
                    GUI.color = Color.white;
                    
                    GUI.enabled = true;
                    
                    if (GUILayout.Button("刷新状态", GUILayout.Height(30)))
                    {
                        RefreshAll();
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(5);
                
                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("同步所有兼容", GUILayout.Height(25)))
                    {
                        SyncAllCompatibleProperties();
                    }
                    
                    if (GUILayout.Button("同步所有不同", GUILayout.Height(25)))
                    {
                        SyncAllDifferentProperties();
                    }
                    
                    if (GUILayout.Button("仅同步颜色", GUILayout.Height(25)))
                    {
                        SyncColorProperties();
                    }
                    
                    if (GUILayout.Button("仅同步纹理", GUILayout.Height(25)))
                    {
                        SyncTextureProperties();
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("生成报告", GUILayout.Height(25)))
                    {
                        GenerateComparisonReport();
                    }
                    
                    if (GUILayout.Button("复制到剪贴板", GUILayout.Height(25)))
                    {
                        CopyPropertiesToClipboard();
                    }
                    
                    if (GUILayout.Button("重置选择", GUILayout.Height(25)))
                    {
                        ResetSelection();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }
        
        #region 窗口数据保存和加载
        
        private void SaveWindowData()
        {
            try
            {
                var windowData = new WindowData
                {
                    sourceMaterialPath = sourceMaterial != null ? AssetDatabase.GetAssetPath(sourceMaterial) : "",
                    targetMaterialPaths = targetMaterials
                        .Where(mat => mat != null)
                        .Select(mat => AssetDatabase.GetAssetPath(mat))
                        .Where(path => !string.IsNullOrEmpty(path))
                        .ToArray()
                };
                
                string json = JsonUtility.ToJson(windowData, true);
                File.WriteAllText(WINDOW_DATA_PATH, json);
                Debug.Log("窗口数据已保存");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"保存窗口数据失败: {e.Message}");
            }
        }
        
        private void LoadWindowData()
        {
            try
            {
                if (!File.Exists(WINDOW_DATA_PATH))
                    return;
                    
                string json = File.ReadAllText(WINDOW_DATA_PATH);
                var windowData = JsonUtility.FromJson<WindowData>(json);
                
                // 加载源材质
                if (!string.IsNullOrEmpty(windowData.sourceMaterialPath))
                {
                    sourceMaterial = AssetDatabase.LoadAssetAtPath<Material>(windowData.sourceMaterialPath);
                }
                
                // 加载目标材质
                targetMaterials.Clear();
                foreach (var path in windowData.targetMaterialPaths)
                {
                    if (!string.IsNullOrEmpty(path))
                    {
                        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                        if (material != null)
                        {
                            targetMaterials.Add(material);
                        }
                    }
                }
                
                Debug.Log("窗口数据已加载");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"加载窗口数据失败: {e.Message}");
            }
        }
        
        #endregion
        
        #region 性能优化方法
        
        private void ScheduleRefresh()
        {
            needsRefresh = true;
            lastRefreshTime = EditorApplication.timeSinceStartup;
        }
        
        private void RefreshAll()
        {
            if (isRefreshing) return;
            
            try
            {
                isRefreshing = true;
                Repaint();
                
                if (availableProperties.Count > 0)
                {
                    UpdatePropertyAvailability();
                }
                
                RefreshFilteredProperties();
                
                Debug.Log("刷新完成");
            }
            finally
            {
                isRefreshing = false;
            }
        }
        
        private void RefreshFilteredProperties()
        {
            if (availableProperties.Count == 0) return;
            
            filteredPropertyNames.Clear();
            
            foreach (var propName in availableProperties.Keys)
            {
                if (FilterPropertyByName(propName))
                {
                    filteredPropertyNames.Add(propName);
                }
            }
            
            Repaint();
        }
        
        private bool FilterPropertyByName(string propertyName)
        {
            if (!availableProperties.ContainsKey(propertyName))
                return false;
            
            var propInfo = availableProperties[propertyName];
            
            if (!string.IsNullOrEmpty(searchText))
            {
                string searchLower = searchText.ToLower();
                if (!propertyName.ToLower().Contains(searchLower) &&
                    !propInfo.sourceValueDisplay.ToLower().Contains(searchLower))
                    return false;
            }
            
            if (showOnlyDifferent && !propInfo.isDifferent)
                return false;
            
            if (showOnlyAvailable && !propInfo.isAvailableInAllTargets)
                return false;
            
            return true;
        }
        
        #endregion
        
        #region 预设管理方法
        
        private void EnsurePresetFolderExists()
        {
            if (!Directory.Exists(PRESET_FOLDER_PATH))
            {
                Directory.CreateDirectory(PRESET_FOLDER_PATH);
                Debug.Log($"创建预设文件夹: {PRESET_FOLDER_PATH}");
            }
        }
        
        private void LoadPresets()
        {
            propertyPresets.Clear();
            
            if (!Directory.Exists(PRESET_FOLDER_PATH))
            {
                EnsurePresetFolderExists();
                return;
            }
            
            string[] presetFiles = Directory.GetFiles(PRESET_FOLDER_PATH, "*.json");
            foreach (string file in presetFiles)
            {
                try
                {
                    string json = File.ReadAllText(file);
                    var preset = JsonUtility.FromJson<PropertyPreset>(json);
                    propertyPresets.Add(preset);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"加载预设失败 {file}: {e.Message}");
                }
            }
            
            propertyPresets = propertyPresets
                .OrderByDescending(p => p.createdTime)
                .ToList();
            
            Debug.Log($"加载了 {propertyPresets.Count} 个预设");
        }
        
        private void SavePreset()
        {
            if (string.IsNullOrEmpty(newPresetName))
            {
                EditorUtility.DisplayDialog("错误", "请输入预设名称", "确定");
                return;
            }
            
            var selectedProps = availableProperties
                .Where(kvp => kvp.Value.selected)
                .Select(kvp => kvp.Key)
                .ToArray();
            
            if (selectedProps.Length == 0)
            {
                EditorUtility.DisplayDialog("提示", "没有选中的属性", "确定");
                return;
            }
            
            var preset = new PropertyPreset
            {
                name = newPresetName,
                selectedProperties = selectedProps,
                createdTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                description = $"包含 {selectedProps.Length} 个属性"
            };
            
            var existing = propertyPresets.Find(p => p.name == newPresetName);
            if (existing != null)
            {
                if (!EditorUtility.DisplayDialog("确认", $"已存在名为 {newPresetName} 的预设，是否覆盖？", "是", "否"))
                {
                    return;
                }
                propertyPresets.Remove(existing);
            }
            
            propertyPresets.Add(preset);
            
            EnsurePresetFolderExists();
            
            string filePath = Path.Combine(PRESET_FOLDER_PATH, $"{newPresetName}.json");
            string json = JsonUtility.ToJson(preset, true);
            File.WriteAllText(filePath, json);
            
            AssetDatabase.Refresh();
            
            LoadPresets();
            
            selectedPresetIndex = propertyPresets.FindIndex(p => p.name == newPresetName);
            
            Debug.Log($"预设已保存: {filePath}");
            EditorUtility.DisplayDialog("完成", $"预设 '{newPresetName}' 已保存", "确定");
            
            newPresetName = "New Preset";
        }
        
        private void ApplyPreset(PropertyPreset preset)
        {
            if (preset == null || preset.selectedProperties == null)
            {
                EditorUtility.DisplayDialog("错误", "预设数据无效", "确定");
                return;
            }
            
            foreach (var propInfo in availableProperties.Values)
            {
                propInfo.selected = false;
            }
            
            int appliedCount = 0;
            foreach (var propName in preset.selectedProperties)
            {
                if (availableProperties.ContainsKey(propName))
                {
                    availableProperties[propName].selected = true;
                    appliedCount++;
                }
            }
            
            RefreshFilteredProperties();
            
            EditorUtility.DisplayDialog("预设应用", 
                $"应用了预设 '{preset.name}'\n({preset.description})\n成功应用 {appliedCount} 个属性", 
                "确定");
        }
        
        private void CreatePresetFromSelection()
        {
            var selectedProps = availableProperties
                .Where(kvp => kvp.Value.selected)
                .Select(kvp => kvp.Key)
                .ToArray();
            
            if (selectedProps.Length == 0)
            {
                EditorUtility.DisplayDialog("提示", "没有选中的属性", "确定");
                return;
            }
            
            newPresetName = $"Preset_{selectedProps.Length}Props_{System.DateTime.Now:MMddHHmm}";
            SavePreset();
        }
        
        private void DeletePreset(PropertyPreset preset)
        {
            if (preset == null) return;
            
            if (EditorUtility.DisplayDialog("确认删除", $"确定删除预设 '{preset.name}' 吗？", "是", "否"))
            {
                string filePath = Path.Combine(PRESET_FOLDER_PATH, $"{preset.name}.json");
                
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    
                    string metaPath = filePath + ".meta";
                    if (File.Exists(metaPath))
                    {
                        File.Delete(metaPath);
                    }
                }
                
                propertyPresets.Remove(preset);
                selectedPresetIndex = -1;
                
                AssetDatabase.Refresh();
                
                EditorUtility.DisplayDialog("完成", $"预设 '{preset.name}' 已删除", "确定");
            }
        }
        
        #endregion
        
        #region UI交互方法
        
        private void SelectAllProperties(bool selected)
        {
            foreach (var propInfo in availableProperties.Values)
            {
                if (propInfo.isAvailableInAllTargets || !showOnlyAvailable)
                {
                    propInfo.selected = selected;
                }
            }
            Repaint();
        }
        
        private void InvertSelection()
        {
            foreach (var propInfo in availableProperties.Values)
            {
                if (propInfo.isAvailableInAllTargets || !showOnlyAvailable)
                {
                    propInfo.selected = !propInfo.selected;
                }
            }
            Repaint();
        }
        
        private void SelectOnlyDifferentProperties()
        {
            foreach (var propInfo in availableProperties.Values)
            {
                propInfo.selected = propInfo.isDifferent && propInfo.isAvailableInAllTargets;
            }
            Repaint();
        }
        
        private void SelectOnlyCompatibleProperties()
        {
            foreach (var propInfo in availableProperties.Values)
            {
                propInfo.selected = propInfo.isAvailableInAllTargets;
            }
            Repaint();
        }
        
        private void ToggleGroupSelection(string groupName)
        {
            if (!categorizedProperties.ContainsKey(groupName)) return;
            
            var propertiesInGroup = categorizedProperties[groupName]
                .Where(propName => availableProperties.ContainsKey(propName))
                .Where(FilterPropertyByName)
                .ToList();
            
            if (propertiesInGroup.Count == 0) return;
            
            bool anySelected = propertiesInGroup.Any(p => availableProperties[p].selected);
            bool newState = !anySelected;
            
            foreach (var propName in propertiesInGroup)
            {
                var propInfo = availableProperties[propName];
                if (propInfo.isAvailableInAllTargets || !showOnlyAvailable)
                {
                    propInfo.selected = newState;
                }
            }
            
            Repaint();
        }
        
        #endregion
        
        #region 材质操作方法
        
        private void SetSelectedAsSource()
        {
            var selected = Selection.activeObject as Material;
            if (selected != null)
            {
                sourceMaterial = selected;
                ScheduleRefresh();
                EditorUtility.DisplayDialog("提示", $"已设置 {selected.name} 为源材质", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("提示", "请选择一个材质", "确定");
            }
        }
        
        private void AddSelectedMaterials()
        {
            bool added = false;
            foreach (var obj in Selection.objects)
            {
                if (obj is Material material && !targetMaterials.Contains(material))
                {
                    targetMaterials.Add(material);
                    added = true;
                }
            }
            
            if (added)
            {
                ScheduleRefresh();
                EditorUtility.DisplayDialog("完成", $"添加了 {Selection.objects.Length} 个材质", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("提示", "请选择至少一个材质", "确定");
            }
        }
        
        private void AddMaterialsFromSelectedGameObjects()
        {
            // 获取选中的所有游戏对象
            var selectedGameObjects = Selection.gameObjects;
            if (selectedGameObjects.Length == 0)
            {
                EditorUtility.DisplayDialog("提示", "请先选择游戏对象", "确定");
                return;
            }
            
            HashSet<Material> uniqueMaterials = new HashSet<Material>();
            int totalFound = 0;
            
            foreach (var go in selectedGameObjects)
            {
                // 获取所有Renderer组件
                var renderers = go.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in renderers)
                {
                    if (renderer == null) continue;
                    
                    var materials = renderer.sharedMaterials;
                    foreach (var material in materials)
                    {
                        if (material != null)
                        {
                            uniqueMaterials.Add(material);
                            totalFound++;
                        }
                    }
                }
                
                // 检查SkinnedMeshRenderer
                var skinnedRenderers = go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                foreach (var renderer in skinnedRenderers)
                {
                    if (renderer == null) continue;
                    
                    var materials = renderer.sharedMaterials;
                    foreach (var material in materials)
                    {
                        if (material != null)
                        {
                            uniqueMaterials.Add(material);
                            totalFound++;
                        }
                    }
                }
            }
            
            // 添加不重复的材质
            int addedCount = 0;
            foreach (var material in uniqueMaterials)
            {
                if (!targetMaterials.Contains(material))
                {
                    targetMaterials.Add(material);
                    addedCount++;
                }
            }
            
            ScheduleRefresh();
            
            string message = $"从 {selectedGameObjects.Length} 个游戏对象中\n";
            message += $"找到 {totalFound} 个材质\n";
            message += $"添加了 {addedCount} 个不重复的材质到列表";
            
            EditorUtility.DisplayDialog("完成", message, "确定");
        }
        
        private void AddMaterialsFromFolder()
        {
            string path = EditorUtility.OpenFolderPanel("选择材质文件夹", "Assets", "");
            if (!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
            {
                string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
                string[] materialPaths = AssetDatabase.FindAssets("t:Material", new[] { relativePath });
                
                int addedCount = 0;
                foreach (string guid in materialPaths.Take(20))
                {
                    string materialPath = AssetDatabase.GUIDToAssetPath(guid);
                    Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    
                    if (material != null && !targetMaterials.Contains(material))
                    {
                        targetMaterials.Add(material);
                        addedCount++;
                    }
                }
                
                ScheduleRefresh();
                EditorUtility.DisplayDialog("完成", $"添加了 {addedCount} 个材质", "确定");
            }
        }
        
        private int GetCompatiblePropertyCount(Material material)
        {
            if (material == null) return 0;
            
            int count = 0;
            foreach (var kvp in availableProperties)
            {
                if (HasProperty(material, kvp.Key, kvp.Value.type))
                {
                    count++;
                }
            }
            return count;
        }
        
        private void ValidateCompatibility()
        {
            StringBuilder report = new StringBuilder();
            report.AppendLine("材质兼容性验证报告:");
            report.AppendLine($"源材质: {sourceMaterial.name}");
            report.AppendLine($"源Shader: {sourceMaterial.shader.name}");
            
            int totalProperties = availableProperties.Count;
            int incompatibleMaterials = 0;
            
            for (int i = targetMaterials.Count - 1; i >= 0; i--)
            {
                var mat = targetMaterials[i];
                if (mat == null) continue;
                
                int compatibleCount = GetCompatiblePropertyCount(mat);
                float compatibility = totalProperties > 0 ? (float)compatibleCount / totalProperties * 100 : 0;
                
                report.AppendLine($"\n{mat.name}:");
                report.AppendLine($"  Shader: {mat.shader.name}");
                report.AppendLine($"  兼容属性: {compatibleCount}/{totalProperties} ({compatibility:F1}%)");
                
                if (compatibility < 50)
                {
                    report.AppendLine($"  ⚠️ 兼容性较低");
                    incompatibleMaterials++;
                }
            }
            
            UpdatePropertyAvailability();
            
            EditorUtility.DisplayDialog("兼容性验证", report.ToString(), "确定");
        }
        
        private void RemoveIncompatibleMaterials()
        {
            int removedCount = 0;
            for (int i = targetMaterials.Count - 1; i >= 0; i--)
            {
                var mat = targetMaterials[i];
                if (mat == null)
                {
                    targetMaterials.RemoveAt(i);
                    removedCount++;
                }
                else
                {
                    int compatibleCount = GetCompatiblePropertyCount(mat);
                    float compatibility = availableProperties.Count > 0 ? 
                        (float)compatibleCount / availableProperties.Count : 0;
                    
                    if (compatibility < 0.3f)
                    {
                        targetMaterials.RemoveAt(i);
                        removedCount++;
                    }
                }
            }
            
            ScheduleRefresh();
            
            EditorUtility.DisplayDialog("完成", $"移除了 {removedCount} 个不兼容材质", "确定");
        }
        
        private void ShowPropertyDetails(string propertyName)
        {
            if (!availableProperties.ContainsKey(propertyName)) return;
            
            var propInfo = availableProperties[propertyName];
            
            string message = $"属性: {propertyName}\n类型: {propInfo.type}\n源值: {propInfo.sourceValueDisplay}";
            EditorUtility.DisplayDialog("属性详情", message, "确定");
        }
        
        private void SyncSingleProperty(string propertyName)
        {
            if (!availableProperties.ContainsKey(propertyName)) return;
            
            var propInfo = availableProperties[propertyName];
            if (!propInfo.isAvailableInAllTargets) return;
            
            int syncedCount = 0;
            foreach (var targetMat in targetMaterials)
            {
                if (targetMat != null && HasProperty(targetMat, propertyName, propInfo.type))
                {
                    Undo.RecordObject(targetMat, $"Sync property {propertyName}");
                    CopyProperty(sourceMaterial, targetMat, propertyName, propInfo.type);
                    EditorUtility.SetDirty(targetMat);
                    syncedCount++;
                }
            }
            
            if (syncedCount > 0)
            {
                ScheduleRefresh();
                EditorUtility.DisplayDialog("完成", $"已同步属性到 {syncedCount} 个材质", "确定");
            }
        }
        
        #endregion
        
        #region 同步操作方法
        
        private void PreviewSync()
        {
            var selectedProps = availableProperties
                .Where(kvp => kvp.Value.selected && kvp.Value.isAvailableInAllTargets)
                .Select(kvp => kvp.Key)
                .ToList();
            
            if (selectedProps.Count == 0)
            {
                EditorUtility.DisplayDialog("预览", "没有选择任何可用的属性！", "确定");
                return;
            }
            
            string message = $"将同步 {selectedProps.Count} 个属性到 {targetMaterials.Count} 个材质";
            if (EditorUtility.DisplayDialog("同步预览", message, "执行同步", "取消"))
            {
                ExecuteSync();
            }
        }
        
        private void ExecuteSync()
        {
            var selectedProps = availableProperties
                .Where(kvp => kvp.Value.selected && kvp.Value.isAvailableInAllTargets)
                .Select(kvp => kvp.Key)
                .ToList();
            
            if (selectedProps.Count == 0)
            {
                EditorUtility.DisplayDialog("错误", "没有选择任何可用的属性！", "确定");
                return;
            }
            
            try
            {
                EditorUtility.DisplayProgressBar("同步材质属性", "正在同步...", 0);
                
                int successMaterials = 0;
                int successProperties = 0;
                
                for (int i = 0; i < targetMaterials.Count; i++)
                {
                    var targetMat = targetMaterials[i];
                    if (targetMat == null) continue;
                    
                    EditorUtility.DisplayProgressBar("同步材质属性", 
                        $"正在处理 {targetMat.name} ({i+1}/{targetMaterials.Count})", 
                        (float)i / targetMaterials.Count);
                    
                    Undo.RecordObject(targetMat, $"Sync properties to {targetMat.name}");
                    
                    int propsSynced = 0;
                    foreach (var propName in selectedProps)
                    {
                        var propInfo = availableProperties[propName];
                        if (HasProperty(targetMat, propName, propInfo.type))
                        {
                            CopyProperty(sourceMaterial, targetMat, propName, propInfo.type);
                            propsSynced++;
                        }
                    }
                    
                    if (propsSynced > 0)
                    {
                        EditorUtility.SetDirty(targetMat);
                        successMaterials++;
                        successProperties += propsSynced;
                    }
                }
                
                EditorUtility.ClearProgressBar();
                
                ScheduleRefresh();
                
                string message = $"同步完成！\n成功同步 {successProperties} 个属性\n到 {successMaterials} 个材质";
                EditorUtility.DisplayDialog("同步完成", message, "确定");
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"同步失败: {e.Message}");
                EditorUtility.DisplayDialog("同步失败", $"发生错误: {e.Message}", "确定");
            }
        }
        
        private void CopyProperty(Material source, Material target, string propertyName, ShaderUtil.ShaderPropertyType type)
        {
            try
            {
                switch (type)
                {
                    case ShaderUtil.ShaderPropertyType.Color:
                        target.SetColor(propertyName, source.GetColor(propertyName));
                        break;
                        
                    case ShaderUtil.ShaderPropertyType.Vector:
                        target.SetVector(propertyName, source.GetVector(propertyName));
                        break;
                        
                    case ShaderUtil.ShaderPropertyType.Float:
                    case ShaderUtil.ShaderPropertyType.Range:
                        target.SetFloat(propertyName, source.GetFloat(propertyName));
                        break;
                        
                    case ShaderUtil.ShaderPropertyType.TexEnv:
                        target.SetTexture(propertyName, source.GetTexture(propertyName));
                        target.SetTextureOffset(propertyName, source.GetTextureOffset(propertyName));
                        target.SetTextureScale(propertyName, source.GetTextureScale(propertyName));
                        break;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"复制属性失败 {propertyName}: {e.Message}");
            }
        }
        
        private void SyncAllCompatibleProperties()
        {
            foreach (var kvp in availableProperties)
            {
                kvp.Value.selected = kvp.Value.isAvailableInAllTargets;
            }
            Repaint();
            ExecuteSync();
        }
        
        private void SyncAllDifferentProperties()
        {
            foreach (var kvp in availableProperties)
            {
                kvp.Value.selected = kvp.Value.isDifferent && kvp.Value.isAvailableInAllTargets;
            }
            Repaint();
            ExecuteSync();
        }
        
        private void SyncColorProperties()
        {
            foreach (var kvp in availableProperties)
            {
                kvp.Value.selected = kvp.Value.type == ShaderUtil.ShaderPropertyType.Color && 
                                   kvp.Value.isAvailableInAllTargets;
            }
            Repaint();
            ExecuteSync();
        }
        
        private void SyncTextureProperties()
        {
            foreach (var kvp in availableProperties)
            {
                kvp.Value.selected = kvp.Value.type == ShaderUtil.ShaderPropertyType.TexEnv && 
                                   kvp.Value.isAvailableInAllTargets;
            }
            Repaint();
            ExecuteSync();
        }
        
        private void ExportProperties()
        {
            StringBuilder export = new StringBuilder();
            export.AppendLine("// 材质属性导出");
            export.AppendLine($"// 源材质: {sourceMaterial.name}");
            export.AppendLine($"// Shader: {sourceMaterial.shader.name}");
            
            foreach (var kvp in availableProperties)
            {
                export.AppendLine($"{kvp.Key}: {kvp.Value.sourceValueDisplay}");
            }
            
            GUIUtility.systemCopyBuffer = export.ToString();
            EditorUtility.DisplayDialog("导出完成", "属性列表已复制到剪贴板", "确定");
        }
        
        private void CopyPropertiesToClipboard()
        {
            var selectedProps = availableProperties
                .Where(kvp => kvp.Value.selected)
                .Select(kvp => kvp.Key)
                .ToList();
            
            if (selectedProps.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "没有选中的属性", "确定");
                return;
            }
            
            StringBuilder clipboard = new StringBuilder();
            clipboard.AppendLine($"选中的属性 ({selectedProps.Count} 个):");
            
            foreach (var propName in selectedProps)
            {
                if (availableProperties.ContainsKey(propName))
                {
                    clipboard.AppendLine(propName);
                }
            }
            
            GUIUtility.systemCopyBuffer = clipboard.ToString();
            EditorUtility.DisplayDialog("完成", "已复制选中的属性到剪贴板", "确定");
        }
        
        private void GenerateComparisonReport()
        {
            if (targetMaterials.Count == 0) return;
            
            int differentCount = 0;
            int missingCount = 0;
            
            foreach (var kvp in availableProperties)
            {
                if (!kvp.Value.isAvailableInAllTargets)
                {
                    missingCount++;
                }
                else if (kvp.Value.isDifferent)
                {
                    differentCount++;
                }
            }
            
            string message = $"属性对比报告:\n总属性: {availableProperties.Count}\n不同属性: {differentCount}\n缺失属性: {missingCount}";
            EditorUtility.DisplayDialog("对比报告", message, "确定");
        }
        
        private void ResetSelection()
        {
            if (EditorUtility.DisplayDialog("确认", "重置所有选择？", "是", "否"))
            {
                foreach (var propInfo in availableProperties.Values)
                {
                    propInfo.selected = false;
                }
                Repaint();
            }
        }
        
        #endregion
        
        #region 属性扫描和管理
        
        private void ScanSourceMaterialProperties()
        {
            if (isRefreshing) return;
            
            try
            {
                isRefreshing = true;
                
                availableProperties.Clear();
                filteredPropertyNames.Clear();
                
                foreach (var list in categorizedProperties.Values)
                {
                    list.Clear();
                }
                
                if (sourceMaterial == null || sourceMaterial.shader == null)
                {
                    Debug.LogWarning("源材质或Shader为空");
                    return;
                }
                
                int propertyCount = ShaderUtil.GetPropertyCount(sourceMaterial.shader);
                
                for (int i = 0; i < propertyCount; i++)
                {
                    if (i % 50 == 0)
                    {
                        EditorUtility.DisplayProgressBar("扫描属性", $"正在扫描属性 {i}/{propertyCount}", 
                            (float)i / propertyCount);
                    }
                    
                    string propertyName = ShaderUtil.GetPropertyName(sourceMaterial.shader, i);
                    var propertyType = ShaderUtil.GetPropertyType(sourceMaterial.shader, i);
                    
                    string sourceValue = GetPropertyValueString(sourceMaterial, propertyName, propertyType);
                    string displayValue = FormatPropertyValue(sourceMaterial, propertyName, propertyType, sourceValue);
                    
                    var propInfo = new PropertyInfo
                    {
                        propertyName = propertyName,
                        type = propertyType,
                        sourceValue = sourceValue,
                        sourceValueDisplay = displayValue,
                        selected = false,
                        isAvailableInAllTargets = true,
                        availableTargetCount = 0,
                        isDifferent = false
                    };
                    
                    availableProperties[propertyName] = propInfo;
                    
                    // 分类属性 - 优化匹配逻辑
                    bool categorized = false;
                    foreach (var group in defaultPropertyGroups)
                    {
                        foreach (var pattern in group.Value)
                        {
                            // 更灵活的匹配：完全匹配、前缀匹配、包含匹配
                            if (propertyName == pattern || 
                                propertyName.StartsWith(pattern) ||
                                pattern.StartsWith("_") && propertyName.Contains(pattern.Substring(1)))
                            {
                                categorizedProperties[group.Key].Add(propertyName);
                                categorized = true;
                                break;
                            }
                        }
                        if (categorized) break;
                    }
                    
                    if (!categorized)
                    {
                        categorizedProperties["自定义属性"].Add(propertyName);
                    }
                }
                
                EditorUtility.ClearProgressBar();
                
                // 对每个分组的属性按字母排序
                foreach (var group in categorizedProperties)
                {
                    group.Value.Sort();
                }
                
                UpdatePropertyAvailability();
                
                RefreshFilteredProperties();
                
                Debug.Log($"成功扫描 {availableProperties.Count} 个属性");
                EditorUtility.DisplayDialog("扫描完成", $"成功扫描 {availableProperties.Count} 个属性", "确定");
            }
            finally
            {
                isRefreshing = false;
                EditorUtility.ClearProgressBar();
            }
        }
        
        private void UpdatePropertyAvailability()
        {
            if (targetMaterials.Count == 0 || availableProperties.Count == 0)
            {
                foreach (var propInfo in availableProperties.Values)
                {
                    propInfo.isAvailableInAllTargets = true;
                    propInfo.availableTargetCount = 0;
                    propInfo.isDifferent = false;
                }
                return;
            }
            
            int totalTargets = targetMaterials.Count;
            
            foreach (var kvp in availableProperties)
            {
                string propName = kvp.Key;
                var propInfo = kvp.Value;
                
                int availableCount = 0;
                bool isDifferent = false;
                
                int checkCount = Mathf.Min(totalTargets, 3);
                for (int i = 0; i < checkCount; i++)
                {
                    var targetMat = targetMaterials[i];
                    if (targetMat != null && HasProperty(targetMat, propName, propInfo.type))
                    {
                        availableCount++;
                        
                        string targetValue = GetPropertyValueString(targetMat, propName, propInfo.type);
                        if (targetValue != propInfo.sourceValue)
                        {
                            isDifferent = true;
                        }
                    }
                }
                
                propInfo.availableTargetCount = (int)(availableCount * ((float)totalTargets / checkCount));
                propInfo.isAvailableInAllTargets = (availableCount == checkCount);
                propInfo.isDifferent = isDifferent;
            }
        }
        
        private string GetPropertyValueString(Material material, string propertyName, ShaderUtil.ShaderPropertyType type)
        {
            if (!material.HasProperty(propertyName))
                return "N/A";
            
            try
            {
                switch (type)
                {
                    case ShaderUtil.ShaderPropertyType.Color:
                        var color = material.GetColor(propertyName);
                        return ColorUtility.ToHtmlStringRGBA(color);
                        
                    case ShaderUtil.ShaderPropertyType.Vector:
                        var vector = material.GetVector(propertyName);
                        return $"{vector.x:F2},{vector.y:F2},{vector.z:F2},{vector.w:F2}";
                        
                    case ShaderUtil.ShaderPropertyType.Float:
                    case ShaderUtil.ShaderPropertyType.Range:
                        return material.GetFloat(propertyName).ToString("F2");
                        
                    case ShaderUtil.ShaderPropertyType.TexEnv:
                        var texture = material.GetTexture(propertyName);
                        return texture != null ? texture.name : "null";
                        
                    default:
                        return "unknown";
                }
            }
            catch
            {
                return "error";
            }
        }
        
        private string FormatPropertyValue(Material material, string propertyName, 
            ShaderUtil.ShaderPropertyType type, string rawValue)
        {
            if (rawValue == "N/A" || rawValue == "error")
                return rawValue;
            
            try
            {
                switch (type)
                {
                    case ShaderUtil.ShaderPropertyType.Color:
                        return $"#{rawValue.Substring(0, 6)}";
                        
                    case ShaderUtil.ShaderPropertyType.Vector:
                        return rawValue;
                        
                    case ShaderUtil.ShaderPropertyType.Float:
                    case ShaderUtil.ShaderPropertyType.Range:
                        return rawValue;
                        
                    case ShaderUtil.ShaderPropertyType.TexEnv:
                        return rawValue == "null" ? "None" : Path.GetFileName(rawValue);
                        
                    default:
                        return rawValue;
                }
            }
            catch
            {
                return rawValue;
            }
        }
        
        private bool HasProperty(Material material, string propertyName, ShaderUtil.ShaderPropertyType expectedType)
        {
            if (material == null || material.shader == null)
                return false;
            
            return material.HasProperty(propertyName);
        }
        
        #endregion
        
        #region 数据类
        
        [System.Serializable]
        private class PropertyInfo
        {
            public string propertyName;
            public ShaderUtil.ShaderPropertyType type;
            public string sourceValue;
            public string sourceValueDisplay;
            public bool selected;
            public bool isAvailableInAllTargets;
            public int availableTargetCount;
            public bool isDifferent;
        }
        
        [System.Serializable]
        private class PropertyPreset
        {
            public string name;
            public string[] selectedProperties;
            public string createdTime;
            public string description;
        }
        
        // 窗口数据类 - 注意不要与PropertyPreset重复
        [System.Serializable]
        private class WindowData
        {
            public string sourceMaterialPath;
            public string[] targetMaterialPaths;
        }
        
        #endregion
    }
}