using GAS.Runtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

[CreateAssetMenu(fileName = "NewRangeCue", menuName = "GAS/Cue/CueRange")]
public class CueRange : GameplayCueDurational
{
    public int rangeId;
    public RangeProfile rangeProfile;
    public Vector3 center;

    public override GameplayCueDurationalSpec CreateSpec(GameplayCueParameters parameters)
    {
        return new CueRangeSpec(this, parameters);
    }
}
public class CueRangeSpec : GameplayCueDurationalSpec<CueRange>
{
    private int _rangeHandle = -1;

    public CueRangeSpec(CueRange cue, GameplayCueParameters parameters) : base(cue, parameters)
    {
    }

    public override void OnAdd()
    {
        if (cue.rangeProfile == null)
        {
            Debug.LogError($"[CueRange] rangeProfile is null on cue asset: {cue.name}");
            return;
        }

        Transform parent = Owner != null ? Owner.transform : null;
        _rangeHandle = RangeSystemManager.Instance.CreateExpandingRange(cue.rangeProfile, cue.center, parent);
    }

    public override void OnGameplayEffectActivate()
    {
    }

    public override void OnGameplayEffectDeactivate()
    {
    }

    public override void OnRemove()
    {
        if (_rangeHandle < 0) return;

        RangeSystemManager.Instance.RemoveRange(_rangeHandle);
        _rangeHandle = -1;
    }

    public override void OnTick()
    {
        
    }
}
