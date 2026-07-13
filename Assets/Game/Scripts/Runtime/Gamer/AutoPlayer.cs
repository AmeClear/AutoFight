using GAS.Runtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoPlayer : Actor
{
    private int index;
    private float _autotime;
    public float AutoTime;
    private void Update()
    {
        _autotime += Time.deltaTime;
        if(_autotime>=AutoTime)
        {
            _autotime = 0;
            asc.TryActivateAbility(index++%2==0?GAbilityLib.Atk.Name: GAbilityLib.Def.Name);
        }
    }
}
