using UnityEngine;
using UnityEditor;
using nadena.dev.ndmf;
using VRCNDMFPlugins.ScaleApplicator; 

[assembly: ExportsPlugin(typeof(VRCNDMFPlugins.ScaleApplicator.Editor.ScaleApplicatorPlugin))]

namespace VRCNDMFPlugins.ScaleApplicator.Editor
{
    public class ScaleApplicatorPlugin : Plugin<ScaleApplicatorPlugin>
    {
        public override string DisplayName => "Scale Applicator";
        public override string QualifiedName => "com.vrcndmfplugins.scale-applicator";

        protected override void Configure()
        {
            InPhase(BuildPhase.Transforming)
                .AfterPlugin("nadena.dev.modular-avatar")
                .Run("Apply Scale to Target Objects", ctx =>
                {
                    var applicators = ctx.AvatarRootObject.GetComponentsInChildren<ScaleApplicator>(true);

                    foreach (var applicator in applicators)
                    {
                        if (applicator.targetObjects != null)
                        {
                            for (int i = 0; i < applicator.targetObjects.Count; i++)
                            {
                                var obj = applicator.targetObjects[i];
                                if (obj != null)
                                {
                                    // 默认基准缩放为物体当前的 LocalScale
                                    Vector3 baseScale = obj.transform.localScale;
                                    
                                    // 【防错逻辑】：如果当前组件处于预览状态，说明 localScale 已经被放大过一次了。
                                    // 为了获取准确的计算结果，我们将基准值恢复为记录的原始值。
                                    if (applicator.isPreviewing && i < applicator.originalScales.Count)
                                    {
                                        baseScale = applicator.originalScales[i];
                                    }

                                    // 将 Vector3 的 XYZ 独立缩放值应用到目标上
                                    obj.transform.localScale = Vector3.Scale(baseScale, applicator.scaleValue);
                                }
                            }
                        }
                        
                        // 清理工作
                        Object.DestroyImmediate(applicator);
                    }
                });
        }
    }
}