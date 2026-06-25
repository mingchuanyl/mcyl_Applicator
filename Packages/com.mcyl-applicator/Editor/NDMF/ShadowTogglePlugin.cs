using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects; // 修复：添加 VRC 参数所需的命名空间
using nadena.dev.ndmf;
using MyCustomPlugins; // 替换为你的命名空间

// 注册 NDMF 插件
[assembly: ExportsPlugin(typeof(ShadowTogglePlugin))]

namespace MyCustomPlugins
{
    public class ShadowTogglePlugin : Plugin<ShadowTogglePlugin>
    {
        public override string QualifiedName => "com.yourname.shadowtoggle";
        public override string DisplayName => "Auto Shadow Toggle Generator";

        protected override void Configure()
        {
            // 在 Generating 阶段执行，此时可以安全地生成和注入动画
            InPhase(BuildPhase.Generating).Run("Generate Shadow Toggle", ctx =>
            {
                var config = ctx.AvatarRootObject.GetComponent<ShadowToggleConfig>();
                if (config == null) return; // 如果模型没有挂载配置组件，则跳过

                var descriptor = ctx.AvatarRootObject.GetComponent<VRCAvatarDescriptor>();
                if (descriptor == null) return;

                // 1. 获取 FX Controller
                AnimatorController fxController = GetFXController(descriptor);
                if (fxController == null)
                {
                    Debug.LogWarning("[ShadowToggle] 未找到有效的 FX Controller，跳过生成。");
                    return;
                }

                // 2. 创建开/关的动画片段 (AnimationClip)
                AnimationClip clipOn = new AnimationClip { name = $"Shadow_ON_{config.parameterName}" };
                AnimationClip clipOff = new AnimationClip { name = $"Shadow_OFF_{config.parameterName}" };

                // 3. 遍历模型下所有 Renderer 并寻找带有 _UseShadow 的材质
                var renderers = ctx.AvatarRootObject.GetComponentsInChildren<Renderer>(true);
                bool hasAnyShadowMaterial = false;

                foreach (var renderer in renderers)
                {
                    // 计算相对路径
                    var path = AnimationUtility.CalculateTransformPath(renderer.transform, ctx.AvatarRootObject.transform);
                    var materials = renderer.sharedMaterials;

                    for (int i = 0; i < materials.Length; i++)
                    {
                        var mat = materials[i];
                        if (mat != null && mat.HasProperty("_UseShadow"))
                        {
                            hasAnyShadowMaterial = true;
                            // Unity Animation 系统中，多材质插槽的属性绑定后缀为 [索引]
                            string propName = i == 0 ? "material._UseShadow" : $"material[{i}]._UseShadow";

                            var binding = EditorCurveBinding.FloatCurve(path, renderer.GetType(), propName);

                            // VRChat 动画建议使用两帧来确保稳定触发
                            AnimationCurve curveOn = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f / 60f, 1f));
                            AnimationCurve curveOff = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f / 60f, 0f));

                            AnimationUtility.SetEditorCurve(clipOn, binding, curveOn);
                            AnimationUtility.SetEditorCurve(clipOff, binding, curveOff);
                        }
                    }
                }

                if (!hasAnyShadowMaterial)
                {
                    Debug.Log("[ShadowToggle] 模型中未找到包含 _UseShadow 属性的材质，不进行修改。");
                    return;
                }

                // 修复：删除了原本错误的 ctx.AssetRegistry.RegisterAsset 代码
                // 动画片段直接赋值给状态机后，NDMF 和 VRCSDK 会自动在构建时持久化它们。

                // 4. 注入到 FX Controller 动画机中
                InjectIntoAnimator(ctx, fxController, config, clipOn, clipOff);
                
                // 5. 自动将参数注入到 VRC Expressions Parameters 中
                InjectIntoVRCParameters(descriptor, config);
            });
        }

        private AnimatorController GetFXController(VRCAvatarDescriptor descriptor)
        {
            foreach (var layer in descriptor.baseAnimationLayers)
            {
                if (layer.type == VRCAvatarDescriptor.AnimLayerType.FX && layer.animatorController != null)
                {
                    return layer.animatorController as AnimatorController;
                }
            }
            return null;
        }

        private void InjectIntoAnimator(BuildContext ctx, AnimatorController fx, ShadowToggleConfig config, AnimationClip on, AnimationClip off)
        {
            string paramName = config.parameterName;

            // 添加 Animator Parameter
            if (!fx.parameters.Any(p => p.name == paramName))
            {
                fx.AddParameter(paramName, AnimatorControllerParameterType.Int);
            }

            // 添加新 Layer
            string layerName = "ShadowToggle_" + paramName;
            fx.AddLayer(layerName);
            
            var layers = fx.layers;
            var layer = layers.Last();
            layer.defaultWeight = 1f;
            fx.layers = layers;

            var sm = layer.stateMachine;

            // 创建状态
            var stateOn = sm.AddState("Shadow ON", new Vector3(300, 100, 0));
            stateOn.motion = on;

            var stateOff = sm.AddState("Shadow OFF", new Vector3(300, 200, 0));
            stateOff.motion = off;

            // 设置默认状态
            sm.defaultState = config.defaultValue == 1 ? stateOn : stateOff;

            // AnyState 转换逻辑
            var toOn = sm.AddAnyStateTransition(stateOn);
            toOn.AddCondition(AnimatorConditionMode.Equals, 1, paramName);
            toOn.duration = 0f;
            toOn.canTransitionToSelf = false;

            var toOff = sm.AddAnyStateTransition(stateOff);
            toOff.AddCondition(AnimatorConditionMode.NotEqual, 1, paramName);
            toOff.duration = 0f;
            toOff.canTransitionToSelf = false;
        }

        private void InjectIntoVRCParameters(VRCAvatarDescriptor descriptor, ShadowToggleConfig config)
        {
            if (descriptor.expressionParameters != null)
            {
                var p = descriptor.expressionParameters;
                if (!p.parameters.Any(x => x.name == config.parameterName))
                {
                    var newParams = p.parameters.ToList();
                    newParams.Add(new VRCExpressionParameters.Parameter()
                    {
                        name = config.parameterName,
                        valueType = VRCExpressionParameters.ValueType.Int,
                        defaultValue = config.defaultValue,
                        saved = true
                    });
                    p.parameters = newParams.ToArray();
                }
            }
        }
    }
}