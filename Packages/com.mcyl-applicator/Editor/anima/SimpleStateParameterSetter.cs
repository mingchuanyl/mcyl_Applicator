using UnityEngine;
using System;

[System.Serializable]
public class SimpleParameterSetting
{
    public string parameterName;
    public ParameterType parameterType;
    public bool boolValue;
    public int intValue;
    public float floatValue;
    
    public enum ParameterType
    {
        Bool,
        Int,
        Float
    }
}

public class SimpleStateParameterSetter : StateMachineBehaviour
{
    [SerializeField] private SimpleParameterSetting[] parameterSettings;
    
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        foreach (var setting in parameterSettings)
        {
            if (string.IsNullOrEmpty(setting.parameterName))
                continue;
                
            switch (setting.parameterType)
            {
                case SimpleParameterSetting.ParameterType.Bool:
                    animator.SetBool(setting.parameterName, setting.boolValue);
                    break;
                    
                case SimpleParameterSetting.ParameterType.Int:
                    animator.SetInteger(setting.parameterName, setting.intValue);
                    break;
                    
                case SimpleParameterSetting.ParameterType.Float:
                    animator.SetFloat(setting.parameterName, setting.floatValue);
                    break;
            }
        }
    }
}