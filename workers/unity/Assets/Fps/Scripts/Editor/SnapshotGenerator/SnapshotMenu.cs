using System.IO;
using Fps.Config;
using Improbable;
using Improbable.Gdk.Core;
using UnityEditor;
using UnityEngine;

namespace Fps.Editor
{
    public class SnapshotMenu : MonoBehaviour
    {
        private static readonly string DefaultSnapshotPath =
            Path.Combine(Application.dataPath, "../../../snapshots/default.snapshot");

        private static readonly string CloudSnapshotPath =
            Path.Combine(Application.dataPath, "../../../snapshots/cloud.snapshot");

        [MenuItem("SpatialOS/Generate FPS Snapshot")]
        private static void GenerateFpsSnapshot()
        {
            SaveSnapshot(DefaultSnapshotPath, GenerateDefaultSnapshot());
            SaveSnapshot(CloudSnapshotPath, GenerateDefaultSnapshot());
        }

        private static Snapshot GenerateDefaultSnapshot()
        {
            var snapshot = new Snapshot();
            snapshot.AddEntity(FpsEntityTemplates.Spawner(Coordinates.Zero));
            //AddHealthPacks(snapshot);
            return snapshot;
        }

        private static void SaveSnapshot(string path, Snapshot snapshot)
        {
            snapshot.WriteToFile(path);
            Debug.LogFormat("Successfully generated initial world snapshot at {0}", path);
        }

        //測試用功能，補包就是在場景中配置好然後生成Snapshot
        //private static void AddHealthPacks(Snapshot snapshot)
        //{
        //    //生成自訂的 Entity Templates並給初始值
        //    var healthPack = FpsEntityTemplates.HealthPickup(new Vector3(5, 0, 0), 100);
        //    //加到Snapshot
        //    snapshot.AddEntity(healthPack);
        //}
    }
}
