using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;

public class GameObject名称搜索替换工具 : EditorWindow
{
    private GameObject 父级对象;
    private string 搜索文本 = "";
    private string 替换文本 = "";
    private bool 搜索子对象 = true;
    private bool 区分大小写 = false;
    private bool 使用正则表达式 = false;
    private List<GameObject> 搜索结果 = new List<GameObject>();
    private Vector2 滚动位置;
    private string 状态信息 = "";
    private bool 显示预览 = false;
    private Dictionary<GameObject, string> 原始名称缓存 = new Dictionary<GameObject, string>();
    private string 排除关键词 = "";
    private bool 仅搜索激活对象 = true;

    // 在"工具/对象管理"子菜单中添加
    [MenuItem("tools/游戏对象名称搜索替换")]
    public static void 显示窗口()
    {
        GetWindow<GameObject名称搜索替换工具>("名称搜索替换");
    }

    void OnGUI()
    {
        GUILayout.Label("🔍 游戏对象名称搜索替换工具", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 设置区域
        EditorGUILayout.LabelField("⚙️ 搜索设置", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        父级对象 = (GameObject)EditorGUILayout.ObjectField("父级对象", 父级对象, typeof(GameObject), true);
        
        EditorGUILayout.Space();
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("搜索文本:", GUILayout.Width(80));
        搜索文本 = EditorGUILayout.TextField(搜索文本);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("替换为:", GUILayout.Width(80));
        替换文本 = EditorGUILayout.TextField(替换文本);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        EditorGUILayout.BeginHorizontal();
        搜索子对象 = EditorGUILayout.Toggle("搜索子对象", 搜索子对象, GUILayout.Width(150));
        区分大小写 = EditorGUILayout.Toggle("区分大小写", 区分大小写, GUILayout.Width(150));
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        使用正则表达式 = EditorGUILayout.Toggle("使用正则表达式", 使用正则表达式, GUILayout.Width(150));
        仅搜索激活对象 = EditorGUILayout.Toggle("仅搜索激活对象", 仅搜索激活对象, GUILayout.Width(150));
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("排除关键词:", GUILayout.Width(80));
        排除关键词 = EditorGUILayout.TextField(排除关键词);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space();

        // 控制按钮区域
        EditorGUILayout.BeginHorizontal();
        
        GUI.enabled = !string.IsNullOrEmpty(搜索文本) && 父级对象 != null;
        if (GUILayout.Button("🔍 搜索", GUILayout.Height(30)))
        {
            执行搜索();
        }
        
        GUI.enabled = 搜索结果.Count > 0;
        if (GUILayout.Button("🔄 预览替换", GUILayout.Height(30)))
        {
            显示预览 = true;
            生成预览();
        }
        
        if (GUILayout.Button("✅ 执行替换", GUILayout.Height(30)))
        {
            执行替换();
        }
        
        GUI.enabled = true;
        
        if (GUILayout.Button("🗑️ 清空", GUILayout.Height(30)))
        {
            清空结果();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();

        // 状态信息
        if (!string.IsNullOrEmpty(状态信息))
        {
            EditorGUILayout.HelpBox(状态信息, MessageType.Info);
        }
        
        EditorGUILayout.Space();

        // 搜索结果区域
        if (搜索结果.Count > 0)
        {
            GUILayout.Label($"📊 搜索结果: {搜索结果.Count} 个对象", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("全选", GUILayout.Width(60)))
            {
                全选结果();
            }
            if (GUILayout.Button("反选", GUILayout.Width(60)))
            {
                反选结果();
            }
            if (GUILayout.Button("选择所有结果", GUILayout.Width(100)))
            {
                选择所有结果();
            }
            EditorGUILayout.EndHorizontal();
            
            滚动位置 = EditorGUILayout.BeginScrollView(滚动位置, GUILayout.Height(300));
            
            foreach (var 对象 in 搜索结果)
            {
                if (对象 == null) continue;
                
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                
                // 选择复选框
                bool 选中 = EditorGUILayout.Toggle(原始名称缓存.ContainsKey(对象), GUILayout.Width(20));
                if (选中 && !原始名称缓存.ContainsKey(对象))
                {
                    原始名称缓存[对象] = 对象.name;
                }
                else if (!选中 && 原始名称缓存.ContainsKey(对象))
                {
                    原始名称缓存.Remove(对象);
                }
                
                // 对象名称显示
                EditorGUILayout.BeginVertical(GUILayout.Width(300));
                if (显示预览 && 原始名称缓存.ContainsKey(对象))
                {
                    string 新名称 = 生成新名称(原始名称缓存[对象]);
                    GUILayout.Label($"{原始名称缓存[对象]} → {新名称}", EditorStyles.miniLabel);
                }
                else
                {
                    GUILayout.Label(对象.name, EditorStyles.miniLabel);
                }
                
                // 路径显示
                GUILayout.Label(生成对象路径(对象), EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.EndVertical();
                
                // 按钮
                if (GUILayout.Button("定位", GUILayout.Width(50)))
                {
                    EditorGUIUtility.PingObject(对象);
                    Selection.activeGameObject = 对象;
                }
                
                if (GUILayout.Button("重命名", GUILayout.Width(60)))
                {
                    单独重命名(对象);
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
            
            // 批量操作
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            
            GUI.enabled = 原始名称缓存.Count > 0;
            if (GUILayout.Button($"重命名选中 ({原始名称缓存.Count})", GUILayout.Height(30)))
            {
                重命名选中项();
            }
            
            if (GUILayout.Button("导出列表", GUILayout.Height(30)))
            {
                导出结果列表();
            }
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
        }
        
        // 帮助信息
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("💡 使用提示", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "1. 将父级对象拖入插槽，会自动搜索其所有子对象\n" +
            "2. 支持使用正则表达式进行高级搜索\n" +
            "3. 可以预览替换结果后再执行\n" +
            "4. 使用排除关键词过滤不需要的对象",
            MessageType.None
        );
        
        // 正则表达式示例
        if (使用正则表达式)
        {
            EditorGUILayout.HelpBox(
                "正则表达式示例:\n" +
                "• ^Enemy_  匹配以Enemy_开头的名称\n" +
                "• _\\d+$   匹配以_数字结尾的名称\n" +
                "• Cube.*  匹配包含Cube的名称\n" +
                "• (A|B)  匹配A或B",
                MessageType.Info
            );
        }
    }

    // 执行搜索
    private void 执行搜索()
    {
        搜索结果.Clear();
        原始名称缓存.Clear();
        状态信息 = "";
        
        if (父级对象 == null)
        {
            状态信息 = "❌ 请先选择父级对象";
            return;
        }
        
        if (string.IsNullOrEmpty(搜索文本))
        {
            状态信息 = "❌ 请输入搜索文本";
            return;
        }
        
        List<GameObject> 所有对象 = new List<GameObject>();
        
        if (搜索子对象)
        {
            // 获取所有子对象
            Transform[] 所有子变换 = 父级对象.GetComponentsInChildren<Transform>(true);
            foreach (Transform 子变换 in 所有子变换)
            {
                所有对象.Add(子变换.gameObject);
            }
        }
        else
        {
            所有对象.Add(父级对象);
        }
        
        int 找到数量 = 0;
        int 排除数量 = 0;
        
        foreach (GameObject 对象 in 所有对象)
        {
            if (对象 == null) continue;
            
            // 检查对象是否激活
            if (仅搜索激活对象 && !对象.activeInHierarchy)
                continue;
            
            // 检查排除关键词
            if (!string.IsNullOrEmpty(排除关键词) && 
                对象.name.Contains(排除关键词))
            {
                排除数量++;
                continue;
            }
            
            bool 匹配 = false;
            
            if (使用正则表达式)
            {
                try
                {
                    var 选项 = 区分大小写 ? System.Text.RegularExpressions.RegexOptions.None : 
                                           System.Text.RegularExpressions.RegexOptions.IgnoreCase;
                    var 正则 = new System.Text.RegularExpressions.Regex(搜索文本, 选项);
                    匹配 = 正则.IsMatch(对象.name);
                }
                catch (System.Exception e)
                {
                    状态信息 = $"❌ 正则表达式错误: {e.Message}";
                    return;
                }
            }
            else
            {
                if (区分大小写)
                {
                    匹配 = 对象.name.Contains(搜索文本);
                }
                else
                {
                    匹配 = 对象.name.ToLower().Contains(搜索文本.ToLower());
                }
            }
            
            if (匹配)
            {
                搜索结果.Add(对象);
                找到数量++;
            }
        }
        
        状态信息 = $"✅ 搜索完成: 找到 {找到数量} 个对象";
        if (排除数量 > 0)
        {
            状态信息 += $", 排除 {排除数量} 个";
        }
    }

    // 生成对象路径
    private string 生成对象路径(GameObject 对象)
    {
        StringBuilder 路径 = new StringBuilder();
        Transform 当前变换 = 对象.transform;
        
        while (当前变换 != null)
        {
            if (路径.Length > 0)
                路径.Insert(0, "/");
            路径.Insert(0, 当前变换.name);
            
            当前变换 = 当前变换.parent;
            if (当前变换 == 父级对象?.transform)
                break;
        }
        
        return 路径.ToString();
    }

    // 生成新名称
    private string 生成新名称(string 原始名称)
    {
        if (使用正则表达式)
        {
            try
            {
                var 选项 = 区分大小写 ? System.Text.RegularExpressions.RegexOptions.None : 
                                       System.Text.RegularExpressions.RegexOptions.IgnoreCase;
                var 正则 = new System.Text.RegularExpressions.Regex(搜索文本, 选项);
                return 正则.Replace(原始名称, 替换文本);
            }
            catch
            {
                return 原始名称;
            }
        }
        else
        {
            if (区分大小写)
            {
                return 原始名称.Replace(搜索文本, 替换文本);
            }
            else
            {
                // 不区分大小写的替换
                int 索引 = 原始名称.ToLower().IndexOf(搜索文本.ToLower());
                if (索引 >= 0)
                {
                    string 替换部分 = 原始名称.Substring(索引, 搜索文本.Length);
                    return 原始名称.Replace(替换部分, 替换文本);
                }
                return 原始名称;
            }
        }
    }

    // 生成预览
    private void 生成预览()
    {
        原始名称缓存.Clear();
        foreach (var 对象 in 搜索结果)
        {
            原始名称缓存[对象] = 对象.name;
        }
        状态信息 = $"🔄 预览模式: {搜索结果.Count} 个对象将被修改";
    }

    // 执行替换
    private void 执行替换()
    {
        if (string.IsNullOrEmpty(替换文本))
        {
            状态信息 = "❌ 请输入替换文本";
            return;
        }
        
        int 成功数量 = 0;
        
        Undo.RecordObjects(搜索结果.ToArray(), "批量重命名游戏对象");
        
        foreach (var 对象 in 搜索结果)
        {
            if (对象 == null) continue;
            
            string 新名称 = 生成新名称(对象.name);
            if (对象.name != 新名称)
            {
                对象.name = 新名称;
                成功数量++;
                EditorUtility.SetDirty(对象);
            }
        }
        
        状态信息 = $"✅ 替换完成: {成功数量} 个对象已重命名";
        搜索结果.Clear();
        原始名称缓存.Clear();
    }

    // 单独重命名
    private void 单独重命名(GameObject 对象)
    {
        if (string.IsNullOrEmpty(替换文本))
        {
            状态信息 = "❌ 请输入替换文本";
            return;
        }
        
        Undo.RecordObject(对象, "重命名游戏对象");
        string 新名称 = 生成新名称(对象.name);
        对象.name = 新名称;
        EditorUtility.SetDirty(对象);
        
        状态信息 = $"✅ 已重命名: {对象.name}";
    }

    // 重命名选中项
    private void 重命名选中项()
    {
        if (原始名称缓存.Count == 0) return;
        
        int 成功数量 = 0;
        var 待处理对象 = new List<GameObject>(原始名称缓存.Keys);
        
        Undo.RecordObjects(待处理对象.ToArray(), "重命名选中游戏对象");
        
        foreach (var 对象 in 待处理对象)
        {
            if (对象 == null) continue;
            
            string 新名称 = 生成新名称(原始名称缓存[对象]);
            if (对象.name != 新名称)
            {
                对象.name = 新名称;
                成功数量++;
                EditorUtility.SetDirty(对象);
            }
        }
        
        状态信息 = $"✅ 已重命名 {成功数量} 个选中对象";
        原始名称缓存.Clear();
    }

    // 全选结果
    private void 全选结果()
    {
        foreach (var 对象 in 搜索结果)
        {
            if (!原始名称缓存.ContainsKey(对象))
            {
                原始名称缓存[对象] = 对象.name;
            }
        }
    }

    // 反选结果
    private void 反选结果()
    {
        var 新选中 = new Dictionary<GameObject, string>();
        foreach (var 对象 in 搜索结果)
        {
            if (!原始名称缓存.ContainsKey(对象))
            {
                新选中[对象] = 对象.name;
            }
        }
        原始名称缓存 = 新选中;
    }

    // 选择所有结果
    private void 选择所有结果()
    {
        Selection.objects = 搜索结果.ToArray();
        状态信息 = $"✅ 已选中 {搜索结果.Count} 个对象";
    }

    // 清空结果
    private void 清空结果()
    {
        搜索结果.Clear();
        原始名称缓存.Clear();
        状态信息 = "已清空搜索结果";
    }

    // 导出结果列表
    private void 导出结果列表()
    {
        string 路径 = EditorUtility.SaveFilePanel("导出结果列表", "", "游戏对象列表.txt", "txt");
        
        if (!string.IsNullOrEmpty(路径))
        {
            StringBuilder 内容 = new StringBuilder();
            内容.AppendLine("游戏对象名称列表");
            内容.AppendLine("=".PadRight(50, '='));
            内容.AppendLine($"生成时间: {System.DateTime.Now}");
            内容.AppendLine($"父级对象: {父级对象?.name ?? "无"}");
            内容.AppendLine($"搜索条件: {搜索文本}");
            内容.AppendLine($"找到数量: {搜索结果.Count}");
            内容.AppendLine();
            
            int 序号 = 1;
            foreach (var 对象 in 搜索结果)
            {
                内容.AppendLine($"{序号}. {对象.name}");
                内容.AppendLine($"   路径: {生成对象路径(对象)}");
                内容.AppendLine($"   类型: {对象.GetType().Name}");
                if (显示预览 && 原始名称缓存.ContainsKey(对象))
                {
                    string 新名称 = 生成新名称(原始名称缓存[对象]);
                    内容.AppendLine($"   新名称: {新名称}");
                }
                内容.AppendLine();
                序号++;
            }
            
            System.IO.File.WriteAllText(路径, 内容.ToString(), Encoding.UTF8);
            EditorUtility.DisplayDialog("导出成功", $"已导出到:\n{路径}", "确定");
        }
    }
}