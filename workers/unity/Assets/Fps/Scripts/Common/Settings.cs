using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//public enum EFishType
//{
//    NORMAL,
//    SPEED,
//    OCTOPUS
//}


namespace Fps
{
    public static class FishSettings
    {
        public static Dictionary<EFishType, float> FishHealthDic = new Dictionary<EFishType, float>()
        {
            {EFishType.NORMAL, 80 },
            {EFishType.SPEED, 40 },
            {EFishType.OCTOPUS, 600 }
        };

        public static Dictionary<EFishType, float> FishScoreDic = new Dictionary<EFishType, float>()
        {
            {EFishType.NORMAL, 50 },
            {EFishType.SPEED, 100 },
            {EFishType.OCTOPUS, 500 }
        };

        public static Dictionary<EFishType, float> FishRespawnTimeDic = new Dictionary<EFishType, float>()
        {
            {EFishType.NORMAL, 5 },
            {EFishType.SPEED, 7.5f },
            {EFishType.OCTOPUS, 15 }
        };

        public static Dictionary<EFishType, float> FishOffsetYDic = new Dictionary<EFishType, float>()
        {
            {EFishType.NORMAL, 0.5f },
            {EFishType.SPEED, 0.25f },
            {EFishType.OCTOPUS, 2.5f }
        };
    }

    public static class PlayerSettings
    {
        public const float PlayerScore = 150;
    }
}
