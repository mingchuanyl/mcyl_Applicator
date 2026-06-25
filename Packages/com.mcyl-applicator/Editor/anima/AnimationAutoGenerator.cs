using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace AnimationTools
{
    /// <summary>
    /// 动画参数类型
    /// </summary>
    public enum ParameterType
    {
        Bool,
        Int
    }

    /// <summary>
    /// 目标对象状态配置
    /// </summary>
    [System.Serializable]
    public class TargetObjectState
    {
        public GameObject targetObject;
        public bool activeOnOn = true;   // 当参数为On时，该对象是否激活
        public bool activeOnOff = false; // 当参数为Off时，该对象是否激活
    }

    /// <summary>
    /// Int状态的目标对象配置
    /// </summary>
    [System.Serializable]
    public class IntStateTargetConfig
    {
        public GameObject targetObject;
        public bool[] activeStates; // 每个Int值对应的激活状态
    }

    /// <summary>
    /// 开关参数配置
    /// </summary>
    [System.Serializable]
    public class SwitchParameter
    {
        public string parameterName = "NewParameter";
        public ParameterType parameterType = ParameterType.Bool;
        public string onStateName = "On";
        public string offStateName = "Off";
        public bool defaultState = true;
        public int defaultIntState = 0;
        public int intValueCount = 2;
        
        // Bool类型：多个目标对象配置
        public List<TargetObjectState> boolTargetObjects = new List<TargetObjectState>();
        
        // Int类型：多个目标对象配置
        public List<IntStateTargetConfig> intTargetObjects = new List<IntStateTargetConfig>();
        
        // 显示配置
        public bool showBoolConfig = true;
        public bool showIntConfig = true;
    }

    /// <summary>
    /// 动画自动生成器
    /// </summary>
    public class AnimationAutoGenerator : EditorWindow
    {
        private GameObject rootObject;
        private AnimatorController controller;
        private List<SwitchParameter> parameters = new List<SwitchParameter>();
        private Vector2 scrollPosition;
        
        // 默认动画状态设置
        private string defaultOnState = "wd on";
        private string defaultOffState = "wd off";
        
        // 样式
        private GUIStyle headerStyle;
        private GUIStyle buttonStyle;
        private GUIStyle boxStyle;
        private GUIStyle foldoutStyle;

        [MenuItem("Tools/Animation Auto Generator")]
        public static void ShowWindow()
        {
            GetWindow<AnimationAutoGenerator>("Animation Generator");
        }

        private void OnEnable()
        {
            InitializeStyles();
            LoadSavedData();
        }

        private void InitializeStyles()
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                margin = new RectOffset(0, 0, 10, 10)
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fixedHeight = 25,
                margin = new RectOffset(5, 5, 5, 5)
            };

            boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(5, 5, 5, 5)
            };

            foldoutStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            DrawHeader();
            DrawRootObjectSection();
            DrawDefaultStatesSection();
            DrawParametersSection();
            
            EditorGUILayout.EndScrollView();
            
            DrawActionButtons();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Animation Auto Generator", headerStyle);
            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox("自动为Animator Controller生成开关参数和动画层。动画文件将保存在控制器相同路径。", MessageType.Info);
            EditorGUILayout.Space(10);
        }

        private void DrawRootObjectSection()
        {
            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("根对象设置", EditorStyles.boldLabel);
            
            rootObject = (GameObject)EditorGUILayout.ObjectField(
                "根对象 (Root)", 
                rootObject, 
                typeof(GameObject), 
                true);
            
            if (rootObject != null)
            {
                var animator = rootObject.GetComponent<Animator>();
                if (animator != null && animator.runtimeAnimatorController != null)
                {
                    controller = animator.runtimeAnimatorController as AnimatorController;
                    if (controller != null)
                    {
                        EditorGUILayout.HelpBox($"找到控制器: {controller.name}", MessageType.Info);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("请为根对象添加Animator组件并指定Animator Controller", MessageType.Warning);
                }
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }

        private void DrawDefaultStatesSection()
        {
            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("默认动画状态名称", EditorStyles.boldLabel);
            
            defaultOnState = EditorGUILayout.TextField("开启状态名", defaultOnState);
            defaultOffState = EditorGUILayout.TextField("关闭状态名", defaultOffState);
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }

        private void DrawParametersSection()
        {
            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("开关参数设置", EditorStyles.boldLabel);
            
            if (GUILayout.Button("添加新参数", buttonStyle))
            {
                parameters.Add(new SwitchParameter());
            }
            
            for (int i = 0; i < parameters.Count; i++)
            {
                DrawParameter(i);
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawParameter(int index)
        {
            var param = parameters[index];
            
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            // 参数头部
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"参数 {index + 1}: {param.parameterName}", 
                EditorStyles.boldLabel, GUILayout.Width(150));
            
            if (GUILayout.Button("×", GUILayout.Width(25)))
            {
                parameters.RemoveAt(index);
                return;
            }
            
            EditorGUILayout.EndHorizontal();
            
            // 参数基础设置
            param.parameterName = EditorGUILayout.TextField("参数名称", param.parameterName);
            param.parameterType = (ParameterType)EditorGUILayout.EnumPopup("参数类型", param.parameterType);
            
            // 根据参数类型显示不同的设置
            if (param.parameterType == ParameterType.Bool)
            {
                DrawBoolParameterSettings(param);
            }
            else
            {
                DrawIntParameterSettings(param);
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void DrawBoolParameterSettings(SwitchParameter param)
        {
            param.defaultState = EditorGUILayout.Toggle("默认开启", param.defaultState);
            param.onStateName = EditorGUILayout.TextField("开启状态名", param.onStateName);
            param.offStateName = EditorGUILayout.TextField("关闭状态名", param.offStateName);
            
            EditorGUILayout.Space(10);
            
            // 多目标对象配置
            param.showBoolConfig = EditorGUILayout.Foldout(param.showBoolConfig, "目标对象配置", foldoutStyle);
            if (param.showBoolConfig)
            {
                EditorGUILayout.HelpBox("设置多个游戏对象在开启和关闭状态下的激活状态", MessageType.Info);
                
                for (int i = 0; i < param.boolTargetObjects.Count; i++)
                {
                    DrawTargetObjectState(i, param.boolTargetObjects[i], param);
                }
                
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                
                if (GUILayout.Button("+ 添加对象", GUILayout.Width(100)))
                {
                    param.boolTargetObjects.Add(new TargetObjectState());
                }
                
                if (param.boolTargetObjects.Count > 0 && GUILayout.Button("清空列表", GUILayout.Width(100)))
                {
                    if (EditorUtility.DisplayDialog("确认", "清空所有目标对象？", "确定", "取消"))
                    {
                        param.boolTargetObjects.Clear();
                    }
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawIntParameterSettings(SwitchParameter param)
        {
            param.intValueCount = EditorGUILayout.IntSlider("状态数量", param.intValueCount, 2, 10);
            param.defaultIntState = EditorGUILayout.IntSlider("默认状态", param.defaultIntState, 0, param.intValueCount - 1);
            
            EditorGUILayout.Space(10);
            
            // 多目标对象配置
            param.showIntConfig = EditorGUILayout.Foldout(param.showIntConfig, "目标对象配置", foldoutStyle);
            if (param.showIntConfig)
            {
                EditorGUILayout.HelpBox($"设置多个游戏对象在 {param.intValueCount} 个状态下的激活情况", MessageType.Info);
                
                // 更新现有配置的状态数量
                foreach (var config in param.intTargetObjects)
                {
                    if (config.activeStates == null || config.activeStates.Length != param.intValueCount)
                    {
                        System.Array.Resize(ref config.activeStates, param.intValueCount);
                        // 默认设置：第一个状态激活，其他状态不激活
                        for (int i = 0; i < param.intValueCount; i++)
                        {
                            config.activeStates[i] = (i == 0);
                        }
                    }
                }
                
                for (int i = 0; i < param.intTargetObjects.Count; i++)
                {
                    DrawIntTargetObjectConfig(i, param.intTargetObjects[i], param);
                }
                
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                
                if (GUILayout.Button("+ 添加对象", GUILayout.Width(100)))
                {
                    var newConfig = new IntStateTargetConfig
                    {
                        activeStates = new bool[param.intValueCount]
                    };
                    for (int i = 0; i < param.intValueCount; i++)
                    {
                        newConfig.activeStates[i] = (i == 0);
                    }
                    param.intTargetObjects.Add(newConfig);
                }
                
                if (param.intTargetObjects.Count > 0 && GUILayout.Button("清空列表", GUILayout.Width(100)))
                {
                    if (EditorUtility.DisplayDialog("确认", "清空所有目标对象？", "确定", "取消"))
                    {
                        param.intTargetObjects.Clear();
                    }
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawTargetObjectState(int index, TargetObjectState targetState, SwitchParameter param)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"对象 {index + 1}", GUILayout.Width(60));
            
            if (GUILayout.Button("×", GUILayout.Width(25)))
            {
                param.boolTargetObjects.RemoveAt(index);
                return;
            }
            
            EditorGUILayout.EndHorizontal();
            
            targetState.targetObject = (GameObject)EditorGUILayout.ObjectField(
                "目标对象", 
                targetState.targetObject, 
                typeof(GameObject), 
                true);
            
            if (targetState.targetObject != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("开关状态:", GUILayout.Width(80));
                EditorGUILayout.LabelField($"开启时: {(targetState.activeOnOn ? "激活" : "关闭")}");
                EditorGUILayout.LabelField($"关闭时: {(targetState.activeOnOff ? "激活" : "关闭")}");
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("开启时:", GUILayout.Width(60));
                targetState.activeOnOn = EditorGUILayout.Toggle("激活", targetState.activeOnOn);
                GUILayout.Label("关闭时:", GUILayout.Width(60));
                targetState.activeOnOff = EditorGUILayout.Toggle("激活", targetState.activeOnOff);
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void DrawIntTargetObjectConfig(int index, IntStateTargetConfig config, SwitchParameter param)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"对象 {index + 1}", GUILayout.Width(60));
            
            if (GUILayout.Button("×", GUILayout.Width(25)))
            {
                param.intTargetObjects.RemoveAt(index);
                return;
            }
            
            EditorGUILayout.EndHorizontal();
            
            config.targetObject = (GameObject)EditorGUILayout.ObjectField(
                "目标对象", 
                config.targetObject, 
                typeof(GameObject), 
                true);
            
            if (config.targetObject != null)
            {
                EditorGUILayout.LabelField("各状态激活情况:");
                
                EditorGUILayout.BeginHorizontal();
                for (int i = 0; i < param.intValueCount; i++)
                {
                    GUILayout.Label($"状态 {i}:", GUILayout.Width(50));
                    config.activeStates[i] = EditorGUILayout.Toggle(config.activeStates[i]);
                }
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.Space(20);
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("保存配置", buttonStyle))
            {
                SaveData();
            }
            
            GUI.enabled = rootObject != null && controller != null && parameters.Count > 0;
            
            if (GUILayout.Button("生成动画", buttonStyle))
            {
                GenerateAnimations();
            }
            
            GUI.enabled = true;
            
            if (GUILayout.Button("清空所有", buttonStyle))
            {
                if (EditorUtility.DisplayDialog("清空确认", "确定要清空所有配置吗？", "确定", "取消"))
                {
                    ClearAll();
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void GenerateAnimations()
        {
            if (rootObject == null)
            {
                EditorUtility.DisplayDialog("错误", "请先指定根对象！", "确定");
                return;
            }
            
            if (controller == null)
            {
                EditorUtility.DisplayDialog("错误", "根对象上没有找到Animator Controller！", "确定");
                return;
            }
            
            // 验证配置
            if (!ValidateConfigurations())
            {
                return;
            }
            
            try
            {
                EditorUtility.DisplayProgressBar("生成动画", "正在生成动画参数和层...", 0);
                
                // 保存控制器路径
                string controllerPath = AssetDatabase.GetAssetPath(controller);
                string controllerFolder = System.IO.Path.GetDirectoryName(controllerPath);
                
                // 清理旧的参数和层
                CleanupExistingParameters();
                
                int totalSteps = parameters.Count;
                
                for (int i = 0; i < parameters.Count; i++)
                {
                    var param = parameters[i];
                    
                    EditorUtility.DisplayProgressBar("生成动画", 
                        $"正在处理参数: {param.parameterName}", 
                        (float)i / totalSteps);
                    
                    if (param.parameterType == ParameterType.Bool)
                    {
                        if (param.boolTargetObjects.Count == 0)
                        {
                            Debug.LogWarning($"Bool参数 {param.parameterName} 没有配置目标对象，跳过");
                            continue;
                        }
                        GenerateBoolAnimation(param, controllerFolder);
                    }
                    else
                    {
                        if (param.intTargetObjects.Count == 0)
                        {
                            Debug.LogWarning($"Int参数 {param.parameterName} 没有配置目标对象，跳过");
                            continue;
                        }
                        GenerateIntAnimation(param, controllerFolder);
                    }
                }
                
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                EditorUtility.DisplayDialog("完成", "动画生成完成！", "确定");
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"生成动画时出错: {e.Message}\n{e.StackTrace}");
                EditorUtility.DisplayDialog("错误", $"生成动画时出错: {e.Message}", "确定");
            }
        }

        private bool ValidateConfigurations()
        {
            foreach (var param in parameters)
            {
                if (param.parameterType == ParameterType.Bool)
                {
                    if (param.boolTargetObjects.Count == 0)
                    {
                        EditorUtility.DisplayDialog("错误", 
                            $"参数 '{param.parameterName}' 没有配置任何目标对象！", "确定");
                        return false;
                    }
                    
                    foreach (var target in param.boolTargetObjects)
                    {
                        if (target.targetObject == null)
                        {
                            EditorUtility.DisplayDialog("错误", 
                                $"参数 '{param.parameterName}' 中存在未分配的目标对象！", "确定");
                            return false;
                        }
                    }
                }
                else
                {
                    if (param.intTargetObjects.Count == 0)
                    {
                        EditorUtility.DisplayDialog("错误", 
                            $"参数 '{param.parameterName}' 没有配置任何目标对象！", "确定");
                        return false;
                    }
                    
                    foreach (var target in param.intTargetObjects)
                    {
                        if (target.targetObject == null)
                        {
                            EditorUtility.DisplayDialog("错误", 
                                $"参数 '{param.parameterName}' 中存在未分配的目标对象！", "确定");
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        private void CleanupExistingParameters()
        {
            // 清理同名参数
            var existingParams = controller.parameters.ToList();
            foreach (var param in parameters)
            {
                var existing = existingParams.Find(p => p.name == param.parameterName);
                if (existing != null)
                {
                    controller.RemoveParameter(existing);
                }
            }
            
            // 清理同名层
            for (int i = controller.layers.Length - 1; i > 0; i--) // 保留 Base Layer
            {
                var layer = controller.layers[i];
                if (parameters.Any(p => p.parameterName == layer.name))
                {
                    controller.RemoveLayer(i);
                }
            }
        }

        private void GenerateBoolAnimation(SwitchParameter param, string controllerFolder)
        {
            // 创建参数
            controller.AddParameter(param.parameterName, AnimatorControllerParameterType.Bool);
            
            // 创建新层
            var layer = new AnimatorControllerLayer
            {
                name = param.parameterName,
                defaultWeight = 1f,
                stateMachine = new AnimatorStateMachine
                {
                    name = param.parameterName,
                    hideFlags = HideFlags.HideInHierarchy
                }
            };
            
            // 保存状态机
            AssetDatabase.AddObjectToAsset(layer.stateMachine, controller);
            
            // 创建动画剪辑
            AnimationClip onClip = CreateBoolAnimationClip(param, true, 
                $"{controller.name}_{param.parameterName}_{param.onStateName}", 
                controllerFolder);
            AnimationClip offClip = CreateBoolAnimationClip(param, false, 
                $"{controller.name}_{param.parameterName}_{param.offStateName}", 
                controllerFolder);
            
            // 创建状态
            var onState = layer.stateMachine.AddState(param.onStateName);
            var offState = layer.stateMachine.AddState(param.offStateName);
            
            onState.motion = onClip;
            offState.motion = offClip;
            
            // 设置默认状态
            layer.stateMachine.defaultState = param.defaultState ? onState : offState;
            
            // 创建转换
            var onToOff = onState.AddTransition(offState);
            var offToOn = offState.AddTransition(onState);
            
            onToOff.AddCondition(AnimatorConditionMode.IfNot, 0, param.parameterName);
            offToOn.AddCondition(AnimatorConditionMode.If, 0, param.parameterName);
            
            // 添加层到控制器
            controller.AddLayer(layer);
        }

        private void GenerateIntAnimation(SwitchParameter param, string controllerFolder)
        {
            // 创建参数
            controller.AddParameter(param.parameterName, AnimatorControllerParameterType.Int);
            
            // 创建新层
            var layer = new AnimatorControllerLayer
            {
                name = param.parameterName,
                defaultWeight = 1f,
                stateMachine = new AnimatorStateMachine
                {
                    name = param.parameterName,
                    hideFlags = HideFlags.HideInHierarchy
                }
            };
            
            // 保存状态机
            AssetDatabase.AddObjectToAsset(layer.stateMachine, controller);
            
            // 创建状态
            AnimatorState[] states = new AnimatorState[param.intValueCount];
            
            for (int i = 0; i < param.intValueCount; i++)
            {
                // 创建动画剪辑
                AnimationClip clip = CreateIntAnimationClip(param, i, 
                    $"{controller.name}_{param.parameterName}_State{i}", 
                    controllerFolder);
                
                // 创建状态
                var state = layer.stateMachine.AddState($"State{i}");
                state.motion = clip;
                states[i] = state;
                
                // 设置默认状态
                if (i == param.defaultIntState)
                {
                    layer.stateMachine.defaultState = state;
                }
            }
            
            // 创建转换
            for (int i = 0; i < param.intValueCount; i++)
            {
                for (int j = 0; j < param.intValueCount; j++)
                {
                    if (i != j)
                    {
                        var transition = states[i].AddTransition(states[j]);
                        transition.AddCondition(AnimatorConditionMode.Equals, j, param.parameterName);
                        transition.exitTime = 0;
                        transition.hasExitTime = false;
                        transition.duration = 0;
                    }
                }
            }
            
            // 添加层到控制器
            controller.AddLayer(layer);
        }

        private AnimationClip CreateBoolAnimationClip(SwitchParameter param, bool isOnState, string clipName, string folderPath)
        {
            string clipPath = $"{folderPath}/{clipName}.anim";
            
            // 检查是否已存在
            AnimationClip existingClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (existingClip != null)
            {
                // 如果要重用现有剪辑，需要先清除旧曲线
                ClearAllCurves(existingClip);
            }
            else
            {
                existingClip = new AnimationClip
                {
                    name = clipName,
                    frameRate = 60
                };
            }
            
            // 为每个目标对象设置曲线
            foreach (var targetState in param.boolTargetObjects)
            {
                if (targetState.targetObject == null) continue;
                
                string relativePath = AnimationUtility.CalculateTransformPath(targetState.targetObject.transform, rootObject.transform);
                if (string.IsNullOrEmpty(relativePath))
                {
                    relativePath = targetState.targetObject.name;
                }
                
                // 根据状态设置激活值
                float activeValue = isOnState ? 
                    (targetState.activeOnOn ? 1 : 0) : 
                    (targetState.activeOnOff ? 1 : 0);
                
                var curve = new AnimationCurve(new Keyframe(0, activeValue));
                existingClip.SetCurve(relativePath, typeof(GameObject), "m_IsActive", curve);
            }
            
            // 如果是新建的剪辑，保存到文件
            if (existingClip != null && AssetDatabase.GetAssetPath(existingClip) == "")
            {
                AssetDatabase.CreateAsset(existingClip, clipPath);
            }
            else
            {
                // 强制保存修改
                EditorUtility.SetDirty(existingClip);
            }
            
            return existingClip;
        }

        private AnimationClip CreateIntAnimationClip(SwitchParameter param, int stateIndex, string clipName, string folderPath)
        {
            string clipPath = $"{folderPath}/{clipName}.anim";
            
            // 检查是否已存在
            AnimationClip existingClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (existingClip != null)
            {
                // 如果要重用现有剪辑，需要先清除旧曲线
                ClearAllCurves(existingClip);
            }
            else
            {
                existingClip = new AnimationClip
                {
                    name = clipName,
                    frameRate = 60
                };
            }
            
            // 为每个目标对象设置曲线
            foreach (var targetConfig in param.intTargetObjects)
            {
                if (targetConfig.targetObject == null) continue;
                if (stateIndex >= targetConfig.activeStates.Length) continue;
                
                string relativePath = AnimationUtility.CalculateTransformPath(targetConfig.targetObject.transform, rootObject.transform);
                if (string.IsNullOrEmpty(relativePath))
                {
                    relativePath = targetConfig.targetObject.name;
                }
                
                // 设置激活值
                float activeValue = targetConfig.activeStates[stateIndex] ? 1 : 0;
                var curve = new AnimationCurve(new Keyframe(0, activeValue));
                existingClip.SetCurve(relativePath, typeof(GameObject), "m_IsActive", curve);
            }
            
            // 如果是新建的剪辑，保存到文件
            if (existingClip != null && AssetDatabase.GetAssetPath(existingClip) == "")
            {
                AssetDatabase.CreateAsset(existingClip, clipPath);
            }
            else
            {
                // 强制保存修改
                EditorUtility.SetDirty(existingClip);
            }
            
            return existingClip;
        }

        private void ClearAllCurves(AnimationClip clip)
        {
            var bindings = AnimationUtility.GetCurveBindings(clip);
            foreach (var binding in bindings)
            {
                clip.SetCurve(binding.path, binding.type, binding.propertyName, null);
            }
        }

        private void SaveData()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "保存配置",
                "AnimationGeneratorConfig",
                "asset",
                "请选择保存配置的位置");
            
            if (!string.IsNullOrEmpty(path))
            {
                var config = ScriptableObject.CreateInstance<GeneratorConfig>();
                config.rootObject = rootObject;
                config.parameters = new List<SwitchParameter>(parameters);
                config.defaultOnState = defaultOnState;
                config.defaultOffState = defaultOffState;
                
                AssetDatabase.CreateAsset(config, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"配置已保存到: {path}");
            }
        }

        private void LoadSavedData()
        {
            string[] guids = AssetDatabase.FindAssets("t:GeneratorConfig");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                var config = AssetDatabase.LoadAssetAtPath<GeneratorConfig>(path);
                if (config != null)
                {
                    rootObject = config.rootObject;
                    parameters = new List<SwitchParameter>(config.parameters);
                    defaultOnState = config.defaultOnState;
                    defaultOffState = config.defaultOffState;
                }
            }
        }

        private void ClearAll()
        {
            rootObject = null;
            controller = null;
            parameters.Clear();
            defaultOnState = "wd on";
            defaultOffState = "wd off";
        }
    }

    /// <summary>
    /// 用于保存配置的ScriptableObject
    /// </summary>
    public class GeneratorConfig : ScriptableObject
    {
        public GameObject rootObject;
        public List<SwitchParameter> parameters = new List<SwitchParameter>();
        public string defaultOnState = "wd on";
        public string defaultOffState = "wd off";
    }
}