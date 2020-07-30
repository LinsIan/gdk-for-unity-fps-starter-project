using System.Collections;
using System.Collections.Generic;
using Improbable.Gdk.Core;
using Unity.Entities;
using UnityEngine;


namespace Fps.UI
{
    public struct ScoreData
    {
        public string Name;
        public float Score;
    }

    public class ScoreDataComparer : IComparer<ScoreData>
    {
        int IComparer<ScoreData>.Compare(ScoreData x, ScoreData y)
        {
            if (x.Score > y.Score) return -1;
            if (x.Score < y.Score) return 1;
            return 0;
        }
    }

    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class RankingSystem : ComponentSystem
    {
        private const float UpdateFreq = 1f;

        private WorkerSystem workerSystem;
        private CommandSystem commandSystem;
        private ComponentUpdateSystem componentUpdateSystem;
        private InGameScreenManager inGameScreenManager;
        private float timer;

        protected override void OnCreate()
        {
            base.OnCreate();

            workerSystem = World.GetExistingSystem<WorkerSystem>();
            commandSystem = World.GetExistingSystem<CommandSystem>();
            componentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();

            var uiManager = GameObject.FindGameObjectWithTag("OnScreenUI");
            if (uiManager == null)
            {
                throw new MissingReferenceException("Missing reference to the UI manager.");
            }

            inGameScreenManager = uiManager.GetComponentInChildren<InGameScreenManager>(true);
            if (inGameScreenManager == null)
            {
                throw new MissingReferenceException("Missing reference to the in game screen manager.");
            }

            timer = 0;
        }

        protected override void OnUpdate()
        {
            timer += Time.DeltaTime;
            if(timer >= UpdateFreq)
            {
                UpdateRankingUI();
                timer = 0;
            }
        }

        private void UpdateRankingUI()
        {
            var rankingNameList = inGameScreenManager.RankingNameList;
            var rankingScoreList = inGameScreenManager.RankingScoreList;
            var scoreDataList = new List<ScoreData>();

            var comparer = new ScoreDataComparer();

            EntityQuery query = GetEntityQuery(
                ComponentType.ReadOnly<ScoreComponent.Component>()
            );
            
            Entities.With(query).ForEach((ref ScoreComponent.Component score) =>
            {
                scoreDataList.Add(new ScoreData { Name = score.Name, Score = score.Score });
            });

            scoreDataList.Sort(comparer);

            int count = 0;
            
            foreach(var scoreData in scoreDataList)
            {
                rankingNameList[count].text = "#" + (count + 1) + "    " + scoreData.Name;
                rankingScoreList[count].text = "Score:  " + scoreData.Score.ToString();
                ++count;
                if (count == 10) break;
            }

            if (count < 10)
            {
                for(; count<10; count++)
                {
                    rankingNameList[count].text = "";
                    rankingScoreList[count].text = "";
                }
            }
        }
    }
}

