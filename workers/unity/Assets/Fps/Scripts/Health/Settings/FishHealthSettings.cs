using UnityEngine;
using System.Collections.Generic;
using System;

public enum EFishType
{
    NORMAL,
    SPEED,
    OCTOPUS
}


namespace Fps.Health
{
    public static class FishHealthSettings
    {
        public static Dictionary<EFishType, float> FishHealthDic = new Dictionary<EFishType, float>()
        {
            {EFishType.NORMAL, 80 },
            {EFishType.SPEED, 40 },
            {EFishType.OCTOPUS, 600 }
        };

    }
}


