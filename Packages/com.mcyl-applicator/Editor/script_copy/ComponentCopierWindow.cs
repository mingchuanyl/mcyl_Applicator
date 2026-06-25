using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;

public class ComponentCopierWindow : EditorWindow
{
    [Serializable]
    private class SlotData
    {
        public GameObject targetObject;
        public bool isActive = true;
        public string objectPath; // 用于存储对象路径，方便调试
    }

    [SerializeField]
    private GameObject leftRootObject;
    [SerializeField]
    private GameObject rightRootObject;
    [SerializeField]
    private List<SlotData> leftSlots = new List<SlotData>();
    [SerializeField]
    private List<SlotData> rightSlots = new List<SlotData>();

    private Vector2 scrollPosition;
    private int slotCount = 5;
    private bool showAdvanced = false;
    private float columnWidth = 300f;
    private float scrollViewHeight = 400f;
    private Vector2[] columnScrollPositions = new Vector2[2]; // 用于每列的独立滚动
    private bool autoExpandSlots = true; // 是否自动扩展插槽

    [MenuItem("Tools/组件复制器")]
    public static void ShowWindow()
    {
        GetWindow<ComponentCopierWindow>("组件复制器", true);
    }

    private void OnEnable()
    {
        minSize = new Vector2(850, 650);
    }

    private void OnGUI()
    {
        DrawHeader();
        
        EditorGUILayout.BeginVertical();
        {
            DrawConfigurationSection();
            EditorGUILayout.Space(10);
            DrawSlotsSection();
            EditorGUILayout.Space(10);
            DrawActionButtons();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawHeader()
    {
        GUILayout.BeginHorizontal();
        {
            GUILayout.FlexibleSpace();
            GUILayout.Label("组件复制器", EditorStyles.boldLabel, GUILayout.Height(30));
            GUILayout.FlexibleSpace();
        }
        GUILayout.EndHorizontal();
        
        EditorGUILayout.HelpBox("将左侧对象的组件复制到右侧对应位置的对象上\nTransform组件不会被复制", MessageType.Info);
        EditorGUILayout.Space(5);
    }

    private void DrawConfigurationSection()
    {
        EditorGUILayout.LabelField("配置选项", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(GUI.skin.box);
        {
            // 插槽数量设置
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("插槽数量:", GUILayout.Width(80));
                int newSlotCount = EditorGUILayout.IntSlider(slotCount, 1, 50);
                if (newSlotCount != slotCount)
                {
                    slotCount = newSlotCount;
                    ResizeLists();
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(slotCount.ToString(), EditorStyles.boldLabel, GUILayout.Width(30));
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // 列宽设置
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("列宽:", GUILayout.Width(80));
                columnWidth = EditorGUILayout.Slider(columnWidth, 250f, 450f);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // 高度设置
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("列表高度:", GUILayout.Width(80));
                scrollViewHeight = EditorGUILayout.Slider(scrollViewHeight, 200f, 600f);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            // 根对象设置
            EditorGUILayout.LabelField("根对象设置", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.BeginVertical();
                {
                    EditorGUILayout.LabelField("左侧根对象");
                    leftRootObject = EditorGUILayout.ObjectField(
                        leftRootObject,
                        typeof(GameObject),
                        true,
                        GUILayout.Height(30)) as GameObject;
                }
                EditorGUILayout.EndVertical();
                
                GUILayout.Space(20);
                
                EditorGUILayout.BeginVertical();
                {
                    EditorGUILayout.LabelField("右侧根对象");
                    rightRootObject = EditorGUILayout.ObjectField(
                        rightRootObject,
                        typeof(GameObject),
                        true,
                        GUILayout.Height(30)) as GameObject;
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // 高级选项
            showAdvanced = EditorGUILayout.Foldout(showAdvanced, "高级选项", true);
            if (showAdvanced)
            {
                EditorGUI.indentLevel++;
                autoExpandSlots = EditorGUILayout.Toggle("自动扩展插槽", autoExpandSlots);
                EditorGUILayout.HelpBox("自动扩展插槽: 当扫描到的对象超过当前插槽数量时自动增加插槽", MessageType.Info);
                EditorGUI.indentLevel--;
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawSlotsSection()
    {
        EditorGUILayout.LabelField("对象插槽", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        {
            // 左侧对象列
            DrawSlotColumn(0, "左侧对象", leftSlots, new Color(0.2f, 0.6f, 0.8f, 0.2f));
            
            // 中间操作按钮区域
            GUILayout.BeginVertical(GUILayout.Width(60));
            {
                GUILayout.FlexibleSpace();
                
                // 操作按钮
                if (GUILayout.Button("←\n复制\n→", GUILayout.Height(80), GUILayout.Width(50)))
                {
                    CopyComponents();
                }
                
                GUILayout.Space(20);
                
                if (GUILayout.Button("扫描", GUILayout.Height(30), GUILayout.Width(50)))
                {
                    ScanChildObjects();
                }
                
                if (GUILayout.Button("清空", GUILayout.Height(30), GUILayout.Width(50)))
                {
                    ClearAllSlots();
                }
                
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndVertical();
            
            // 右侧对象列
            DrawSlotColumn(1, "右侧对象", rightSlots, new Color(0.8f, 0.4f, 0.2f, 0.2f));
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawSlotColumn(int columnIndex, string title, List<SlotData> slots, Color bgColor)
    {
        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        Texture2D bgTex = new Texture2D(1, 1);
        bgTex.SetPixel(0, 0, bgColor);
        bgTex.Apply();
        boxStyle.normal.background = bgTex;
        
        EditorGUILayout.BeginVertical(boxStyle, GUILayout.Width(columnWidth), GUILayout.MinHeight(scrollViewHeight));
        {
            // 标题区域
            EditorGUILayout.BeginHorizontal(GUI.skin.box);
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label(title, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();
            
            // 插槽列表区域 - 使用独立的滚动视图
            columnScrollPositions[columnIndex] = EditorGUILayout.BeginScrollView(
                columnScrollPositions[columnIndex], 
                GUILayout.Height(scrollViewHeight - 50));
            {
                for (int i = 0; i < slots.Count; i++)
                {
                    DrawSlotItem(slots[i], i + 1);
                }
                
                // 添加空白空间以确保滚动正常工作
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndScrollView();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawSlotItem(SlotData slot, int slotNumber)
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);
        {
            // 插槽标题行
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label($"插槽 {slotNumber}", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                slot.isActive = EditorGUILayout.Toggle("", slot.isActive, GUILayout.Width(20));
            }
            EditorGUILayout.EndHorizontal();
            
            // 对象字段
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("对象:", GUILayout.Width(40));
                slot.targetObject = EditorGUILayout.ObjectField(
                    slot.targetObject,
                    typeof(GameObject),
                    true,
                    GUILayout.Height(25)) as GameObject;
            }
            EditorGUILayout.EndHorizontal();
            
            // 显示对象信息
            if (slot.targetObject != null)
            {
                // 显示对象路径
                if (!string.IsNullOrEmpty(slot.objectPath))
                {
                    EditorGUILayout.LabelField($"路径: {slot.objectPath}", EditorStyles.miniLabel);
                }
                
                // 显示组件信息
                EditorGUILayout.BeginVertical(GUI.skin.box);
                {
                    var components = slot.targetObject.GetComponents<Component>();
                    int nonTransformCount = components.Count(c => !(c is Transform));
                    
                    EditorGUILayout.LabelField($"组件: {nonTransformCount}个", EditorStyles.miniBoldLabel);
                    
                    // 显示组件列表
                    if (nonTransformCount > 0)
                    {
                        EditorGUI.indentLevel++;
                        int count = 0;
                        foreach (var component in components)
                        {
                            if (!(component is Transform))
                            {
                                EditorGUILayout.LabelField($"• {component.GetType().Name}", EditorStyles.miniLabel);
                                count++;
                                if (count >= 3) // 最多显示3个
                                {
                                    if (nonTransformCount > 3)
                                    {
                                        EditorGUILayout.LabelField($"...还有{nonTransformCount - 3}个", EditorStyles.miniLabel);
                                    }
                                    break;
                                }
                            }
                        }
                        EditorGUI.indentLevel--;
                    }
                }
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.HelpBox("空插槽", MessageType.None);
            }
        }
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(3);
    }

    private void DrawActionButtons()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);
        {
            // 主要操作按钮
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("扫描所有子对象", GUILayout.Height(35)))
                {
                    ScanChildObjects();
                }
                
                GUI.enabled = HasValidPairs();
                if (GUILayout.Button("一键复制组件", GUILayout.Height(35)))
                {
                    CopyComponents();
                }
                GUI.enabled = true;
                
                if (GUILayout.Button("清空所有插槽", GUILayout.Height(35)))
                {
                    ClearAllSlots();
                }
                
                if (GUILayout.Button("交换左右列", GUILayout.Height(35)))
                {
                    SwapColumns();
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // 插槽管理
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label("插槽管理:", GUILayout.Width(70));
                
                if (GUILayout.Button("+ 添加5个插槽"))
                {
                    AddSlots(5);
                }
                
                if (GUILayout.Button("- 移除5个插槽"))
                {
                    RemoveSlots(5);
                }
                
                if (GUILayout.Button("重置所有设置"))
                {
                    ResetToDefault();
                }
                
                GUILayout.FlexibleSpace();
                
                // 状态显示
                int activePairs = CountActivePairs();
                int filledSlots = CountFilledSlots();
                GUILayout.Label($"已填充: {filledSlots}/{slotCount*2}", EditorStyles.boldLabel);
                GUILayout.Space(10);
                GUILayout.Label($"激活配对: {activePairs}", EditorStyles.boldLabel);
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
    }

    private void ResizeLists()
    {
        while (leftSlots.Count < slotCount)
        {
            leftSlots.Add(new SlotData());
        }
        while (leftSlots.Count > slotCount)
        {
            leftSlots.RemoveAt(leftSlots.Count - 1);
        }
        
        while (rightSlots.Count < slotCount)
        {
            rightSlots.Add(new SlotData());
        }
        while (rightSlots.Count > slotCount)
        {
            rightSlots.RemoveAt(rightSlots.Count - 1);
        }
    }

    private void AddSlots(int count)
    {
        slotCount += count;
        if (slotCount > 100) slotCount = 100;
        ResizeLists();
    }

    private void RemoveSlots(int count)
    {
        slotCount -= count;
        if (slotCount < 1) slotCount = 1;
        ResizeLists();
    }

    private void ResetToDefault()
    {
        if (EditorUtility.DisplayDialog("确认重置", 
            "确定要将所有设置重置为默认值吗？", "确定", "取消"))
        {
            slotCount = 5;
            columnWidth = 300f;
            scrollViewHeight = 400f;
            leftRootObject = null;
            rightRootObject = null;
            leftSlots.Clear();
            rightSlots.Clear();
            ResizeLists();
        }
    }

    private void SwapColumns()
    {
        // 交换根对象
        GameObject tempRoot = leftRootObject;
        leftRootObject = rightRootObject;
        rightRootObject = tempRoot;
        
        // 交换插槽列表
        List<SlotData> tempSlots = new List<SlotData>(leftSlots);
        leftSlots = rightSlots;
        rightSlots = tempSlots;
        
        // 交换滚动位置
        Vector2 tempScroll = columnScrollPositions[0];
        columnScrollPositions[0] = columnScrollPositions[1];
        columnScrollPositions[1] = tempScroll;
    }

    private void ScanChildObjects()
    {
        if (leftRootObject == null && rightRootObject == null)
        {
            EditorUtility.DisplayDialog("错误", "请先指定左侧和/或右侧的根对象", "确定");
            return;
        }
        
        // 递归收集所有子对象
        Dictionary<string, List<GameObject>> leftChildren = new Dictionary<string, List<GameObject>>();
        Dictionary<string, List<GameObject>> rightChildren = new Dictionary<string, List<GameObject>>();
        
        if (leftRootObject != null)
        {
            CollectAllChildren(leftRootObject.transform, "", leftChildren);
        }
        
        if (rightRootObject != null)
        {
            CollectAllChildren(rightRootObject.transform, "", rightChildren);
        }
        
        // 计算匹配对象数量
        int matchedCount = 0;
        
        // 计算需要的插槽数量
        if (leftRootObject != null && rightRootObject != null)
        {
            // 两侧都有根对象，计算匹配数量
            foreach (var leftKvp in leftChildren)
            {
                if (rightChildren.ContainsKey(leftKvp.Key))
                {
                    matchedCount += Mathf.Min(leftKvp.Value.Count, rightChildren[leftKvp.Key].Count);
                }
            }
        }
        else
        {
            // 只有一侧有根对象，使用该侧的所有子对象数量
            var children = leftRootObject != null ? leftChildren : rightChildren;
            matchedCount = children.Values.Sum(list => list.Count);
        }
        
        // 检查是否需要扩展插槽
        if (autoExpandSlots && matchedCount > slotCount)
        {
            int oldSlotCount = slotCount;
            slotCount = matchedCount;
            ResizeLists();
            Debug.Log($"自动扩展插槽: {oldSlotCount} → {slotCount}");
        }
        
        // 清空现有插槽
        for (int i = 0; i < leftSlots.Count; i++)
        {
            leftSlots[i].targetObject = null;
            leftSlots[i].objectPath = null;
        }
        for (int i = 0; i < rightSlots.Count; i++)
        {
            rightSlots[i].targetObject = null;
            rightSlots[i].objectPath = null;
        }
        
        // 匹配同名对象并填充插槽
        int slotIndex = 0;
        int actualMatched = 0;
        
        if (leftRootObject == null || rightRootObject == null)
        {
            // 只有一侧有根对象
            var children = leftRootObject != null ? leftChildren : rightChildren;
            
            foreach (var kvp in children)
            {
                foreach (var child in kvp.Value)
                {
                    if (slotIndex >= slotCount) break;
                    
                    if (leftRootObject != null)
                    {
                        leftSlots[slotIndex].targetObject = child;
                        leftSlots[slotIndex].objectPath = GetRelativePath(leftRootObject.transform, child.transform);
                    }
                    else
                    {
                        rightSlots[slotIndex].targetObject = child;
                        rightSlots[slotIndex].objectPath = GetRelativePath(rightRootObject.transform, child.transform);
                    }
                    
                    slotIndex++;
                    actualMatched++;
                }
                if (slotIndex >= slotCount) break;
            }
        }
        else
        {
            // 两侧都有根对象，匹配同名
            foreach (var leftKvp in leftChildren)
            {
                string name = leftKvp.Key;
                
                if (rightChildren.ContainsKey(name))
                {
                    var leftObjects = leftKvp.Value;
                    var rightObjects = rightChildren[name];
                    
                    int minCount = Mathf.Min(leftObjects.Count, rightObjects.Count);
                    
                    for (int i = 0; i < minCount; i++)
                    {
                        if (slotIndex >= slotCount) break;
                        
                        leftSlots[slotIndex].targetObject = leftObjects[i];
                        leftSlots[slotIndex].objectPath = GetRelativePath(leftRootObject.transform, leftObjects[i].transform);
                        
                        rightSlots[slotIndex].targetObject = rightObjects[i];
                        rightSlots[slotIndex].objectPath = GetRelativePath(rightRootObject.transform, rightObjects[i].transform);
                        
                        slotIndex++;
                        actualMatched++;
                    }
                }
                
                if (slotIndex >= slotCount) break;
            }
        }
        
        // 显示结果
        string message = "";
        if (actualMatched == 0)
        {
            message = "未找到匹配的子对象";
        }
        else
        {
            message = $"成功匹配 {actualMatched} 对对象";
            
            // 显示一些统计信息
            int leftTotal = leftChildren.Values.Sum(list => list.Count);
            int rightTotal = rightChildren.Values.Sum(list => list.Count);
            
            if (leftTotal > 0 || rightTotal > 0)
            {
                message += $"\n\n左侧: {leftTotal} 个子对象";
                message += $"\n右侧: {rightTotal} 个子对象";
                
                if (leftTotal > 0 && rightTotal > 0)
                {
                    int uniqueLeftNames = leftChildren.Count;
                    int uniqueRightNames = rightChildren.Count;
                    int commonNames = leftChildren.Keys.Intersect(rightChildren.Keys).Count();
                    
                    message += $"\n左侧唯一名称: {uniqueLeftNames}";
                    message += $"\n右侧唯一名称: {uniqueRightNames}";
                    message += $"\n共同名称: {commonNames}";
                }
            }
            
            if (autoExpandSlots && slotCount > 5)
            {
                message += $"\n\n插槽已自动扩展至 {slotCount} 个";
            }
        }
        
        EditorUtility.DisplayDialog("扫描完成", message, "确定");
        Repaint();
    }
    
    // 递归收集所有子对象
    private void CollectAllChildren(Transform parent, string currentPath, Dictionary<string, List<GameObject>> result)
    {
        foreach (Transform child in parent)
        {
            string childPath = string.IsNullOrEmpty(currentPath) ? child.name : $"{currentPath}/{child.name}";
            
            if (!result.ContainsKey(child.name))
            {
                result[child.name] = new List<GameObject>();
            }
            result[child.name].Add(child.gameObject);
            
            // 递归收集子对象的子对象
            if (child.childCount > 0)
            {
                CollectAllChildren(child, childPath, result);
            }
        }
    }
    
    // 获取相对路径
    private string GetRelativePath(Transform root, Transform target)
    {
        if (root == null || target == null) return "";
        
        List<string> pathParts = new List<string>();
        Transform current = target;
        
        while (current != null && current != root)
        {
            pathParts.Insert(0, current.name);
            current = current.parent;
        }
        
        if (current == root)
        {
            return string.Join("/", pathParts);
        }
        
        return "N/A";
    }

    private bool HasValidPairs()
    {
        for (int i = 0; i < Mathf.Min(leftSlots.Count, rightSlots.Count); i++)
        {
            if (leftSlots[i].targetObject != null && 
                rightSlots[i].targetObject != null && 
                leftSlots[i].isActive)
            {
                return true;
            }
        }
        return false;
    }

    private int CountActivePairs()
    {
        int count = 0;
        for (int i = 0; i < Mathf.Min(leftSlots.Count, rightSlots.Count); i++)
        {
            if (leftSlots[i].targetObject != null && 
                rightSlots[i].targetObject != null && 
                leftSlots[i].isActive)
            {
                count++;
            }
        }
        return count;
    }

    private int CountFilledSlots()
    {
        int count = 0;
        foreach (var slot in leftSlots)
        {
            if (slot.targetObject != null) count++;
        }
        foreach (var slot in rightSlots)
        {
            if (slot.targetObject != null) count++;
        }
        return count;
    }

    private void CopyComponents()
    {
        int successCount = 0;
        int totalCopied = 0;
        List<string> logMessages = new List<string>();
        
        // 收集所有需要修改的右侧对象
        List<GameObject> targetsToModify = new List<GameObject>();
        for (int i = 0; i < Mathf.Min(leftSlots.Count, rightSlots.Count); i++)
        {
            if (leftSlots[i].isActive && 
                leftSlots[i].targetObject != null && 
                rightSlots[i].targetObject != null)
            {
                targetsToModify.Add(rightSlots[i].targetObject);
            }
        }
        
        if (targetsToModify.Count > 0)
        {
            Undo.RecordObjects(targetsToModify.ToArray(), "复制组件");
        }
        
        for (int i = 0; i < Mathf.Min(leftSlots.Count, rightSlots.Count); i++)
        {
            if (!leftSlots[i].isActive || 
                leftSlots[i].targetObject == null || 
                rightSlots[i].targetObject == null)
            {
                continue;
            }
            
            GameObject source = leftSlots[i].targetObject;
            GameObject target = rightSlots[i].targetObject;
            
            // 获取所有组件（除了Transform）
            var components = source.GetComponents<Component>()
                .Where(c => !(c is Transform) && c != null);
            
            int copied = 0;
            foreach (var sourceComponent in components)
            {
                var type = sourceComponent.GetType();
                
                try
                {
                    var existingComponent = target.GetComponent(type);
                    
                    if (existingComponent == null)
                    {
                        // 添加新组件
                        var newComponent = Undo.AddComponent(target, type);
                        EditorUtility.CopySerialized(sourceComponent, newComponent);
                        copied++;
                        totalCopied++;
                    }
                    else
                    {
                        // 更新现有组件
                        Undo.RecordObject(existingComponent, "更新组件");
                        EditorUtility.CopySerialized(sourceComponent, existingComponent);
                        copied++;
                        totalCopied++;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"无法复制组件 {type.Name}: {e.Message}");
                }
            }
            
            if (copied > 0)
            {
                successCount++;
                logMessages.Add($"✓ 插槽 {i+1}: {source.name} → {target.name} ({copied}个组件)");
                EditorUtility.SetDirty(target);
            }
        }
        
        // 显示结果
        if (successCount > 0)
        {
            string message = $"成功复制 {totalCopied} 个组件，涉及 {successCount} 对对象\n\n";
            message += string.Join("\n", logMessages);
            
            EditorUtility.DisplayDialog("复制完成", message, "确定");
            
            // 在控制台输出详细信息
            Debug.Log("=== 组件复制结果 ===");
            foreach (var msg in logMessages)
            {
                Debug.Log(msg);
            }
            Debug.Log($"总计: {totalCopied}个组件");
        }
        else
        {
            EditorUtility.DisplayDialog("复制完成", 
                "没有组件被复制（可能没有合适的组件或所有组件都已存在）", "确定");
        }
    }

    private void ClearAllSlots()
    {
        if (EditorUtility.DisplayDialog("确认清空", 
            "确定要清空所有插槽中的对象吗？", "清空", "取消"))
        {
            for (int i = 0; i < leftSlots.Count; i++)
            {
                leftSlots[i].targetObject = null;
                leftSlots[i].objectPath = null;
            }
            for (int i = 0; i < rightSlots.Count; i++)
            {
                rightSlots[i].targetObject = null;
                rightSlots[i].objectPath = null;
            }
            Repaint();
        }
    }
}