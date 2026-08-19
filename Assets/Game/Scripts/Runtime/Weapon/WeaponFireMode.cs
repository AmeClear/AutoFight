using Sirenix.OdinInspector;

/// <summary>
/// 武器开火模式。
/// </summary>
public enum WeaponFireMode
{
    [LabelText("单发")]
    SemiAuto = 0,

    [LabelText("连发")]
    FullAuto = 1
}
