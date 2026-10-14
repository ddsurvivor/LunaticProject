using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static class BattleAuthoringValidation
{
    private static readonly List<string> passed = new List<string>();
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        passed.Add("PASS " + message);
    }

    [MenuItem("Tests/Battle/Validate Authoring Prefabs")]
    public static void Run()
    {
        passed.Clear();
        var previousScene = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
            Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        Vector3 origin = new Vector3(2000, 0, 2000);
        NavMeshData groundData = null;
        NavMeshDataInstance ground = default;
        try
        {
            var obstacle = Instantiate<BattleObstacle>("障碍物掩体");
            obstacle.transform.position = origin + new Vector3(0, 9, 0);
            obstacle.transform.localScale = new Vector3(2, 3, 4);
            obstacle.size = new Vector3(6, 3, 2);
            obstacle.SnapToGround();
            var box = obstacle.GetComponent<BoxCollider>();
            var navObstacle = obstacle.GetComponent<NavMeshObstacle>();
            Check(obstacle.transform.position.y == 0 && obstacle.transform.lossyScale == Vector3.one,
                "Obstacle snaps to Y=0 and rejects Transform resizing");
            Check(box.size == obstacle.size && box.center == Vector3.up * 1.5f &&
                navObstacle.size == box.size && navObstacle.center == box.center && navObstacle.carving,
                "Obstacle collider and carved navigation dimensions agree");
            Check(obstacle.GetComponentInChildren<SpriteRenderer>().sprite != null && !box.isTrigger,
                "Obstacle prefab has a visible sprite and a solid collider");
            Check(!obstacle.GetComponent<CaverSlot>().isFull, "Cover mode is compatible with existing cover detection");
            obstacle.providesCover = false;
            obstacle.Refresh();
            Check(obstacle.GetComponent<CaverSlot>().isFull, "Obstacle-only mode cannot grant cover bonuses");
            UnityEngine.Object.DestroyImmediate(obstacle.gameObject);

            var platform = Instantiate<BattlePlatform>("高层平台");
            platform.transform.position = origin;
            platform.height = 3;
            platform.Refresh();
            Check(platform.NavigationReady && NavMesh.SamplePosition(origin + Vector3.up * 3, out var topHit, 0.3f, NavMesh.AllAreas),
                "Platform generates real walkable NavMesh at its height");
            Check(platform.GetComponent<BoxCollider>().bounds.max.y == 3 && platform.transform.Find("Top").localPosition.y > 3,
                "Platform collider and top image follow height");
            platform.height = 6;
            platform.Refresh();
            Check(NavMesh.SamplePosition(origin + Vector3.up * 6, out topHit, 0.3f, NavMesh.AllAreas) &&
                !NavMesh.SamplePosition(origin + Vector3.up * 3, out _, 0.3f, NavMesh.AllAreas),
                "Changing platform height replaces old navigation instead of leaving duplicate surfaces");
            platform.transform.position += Vector3.right * 15;
            platform.Refresh();
            Check(NavMesh.SamplePosition(platform.transform.position + Vector3.up * 6, out _, 0.3f, NavMesh.AllAreas) &&
                !NavMesh.SamplePosition(origin + Vector3.up * 6, out _, 0.3f, NavMesh.AllAreas),
                "Moving platform moves its navigation");
            platform.enabled = false;
            Check(!platform.NavigationReady, "Disabling platform removes generated navigation");
            UnityEngine.Object.DestroyImmediate(platform.gameObject);

            var settings = NavMesh.GetSettingsByID(0);
            settings.minRegionArea = 0;
            groundData = NavMeshBuilder.BuildNavMeshData(settings, new List<NavMeshBuildSource> {
                new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box,
                    transform = Matrix4x4.TRS(Vector3.down * 0.1f, Quaternion.identity, Vector3.one),
                    size = new Vector3(40, 0.2f, 40), area = 0 }
            }, new Bounds(Vector3.zero, new Vector3(45, 10, 45)), Vector3.zero, Quaternion.identity);
            ground = NavMesh.AddNavMeshData(groundData, origin, Quaternion.identity);
            var groundObject = new GameObject("ValidationGround", typeof(BoxCollider));
            groundObject.transform.position = origin + Vector3.down * 0.1f;
            groundObject.GetComponent<BoxCollider>().size = new Vector3(40, 0.2f, 40);
            var area1 = Instantiate<BattleDeploymentArea>("角色出生区域");
            area1.transform.position = origin;
            area1.size = new Vector2(8, 6);
            area1.transform.rotation = Quaternion.Euler(0, 45, 0);
            area1.Refresh();
            var area2 = Instantiate<BattleDeploymentArea>("角色出生区域");
            area2.transform.position = origin + Vector3.right * 12;
            area2.Refresh();
            var areas = new[] { area1, area2 };
            Check(area1.Contains(origin, 0.5f) && !area1.Contains(origin + new Vector3(8, 0, 0), 0.5f),
                "Rotated deployment rectangle accepts center and rejects outside points");
            Check(!area1.Contains(origin + Vector3.up * 3), "Deployment bounds reject another floor");
            Check(!area1.Contains(origin + area1.transform.right * 3.8f, 0.5f), "Whole pawn footprint must fit inside deployment bounds");
            var pawn = Pawn(origin + Vector3.back * 12);
            Physics.SyncTransforms();
            Check(BattleDeploymentController.TryDestination(pawn, origin, areas, out _, out _),
                "Deployment accepts valid navigation in first region");
            Check(BattleDeploymentController.TryDestination(pawn, area2.transform.position, areas, out _, out _),
                "Deployment accepts a second region in the same scene");
            Check(!BattleDeploymentController.TryDestination(pawn, origin + Vector3.forward * 10, areas, out _, out _),
                "Deployment rejects navigable positions outside every region");
            var occupied = Pawn(origin);
            Physics.SyncTransforms();
            Check(!BattleDeploymentController.TryDestination(pawn, origin, areas, out _, out _),
                "Deployment rejects overlapping another pawn");
            UnityEngine.Object.DestroyImmediate(occupied.gameObject);
            var blockingBox = new GameObject("BlockingBox", typeof(BoxCollider));
            blockingBox.transform.position = origin + Vector3.up;
            blockingBox.GetComponent<BoxCollider>().size = Vector3.one * 2;
            Physics.SyncTransforms();
            Check(!BattleDeploymentController.TryDestination(pawn, origin, areas, out _, out _),
                "Deployment rejects solid obstacles even before navigation carving catches up");
            Check(area1.GetComponent<Collider>() == null, "Deployment markers do not block physics or navigation");
            area1.SetDeploymentVisible(false);
            Check(!area1.GetComponentInChildren<SpriteRenderer>().enabled, "Deployment marker can hide when combat starts");
        }
        finally
        {
            if (ground.valid) ground.Remove();
            if (groundData != null) UnityEngine.Object.DestroyImmediate(groundData);
            if (Application.isBatchMode) EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            else EditorSceneManager.CloseScene(scene, true);
            if (previousScene.IsValid()) SceneManager.SetActiveScene(previousScene);
            Directory.CreateDirectory("obj/BattleAuthoringValidation");
            File.WriteAllLines("obj/BattleAuthoringValidation/results.txt", passed);
        }
        Debug.Log("BattleAuthoringValidation: " + passed.Count + " checks passed.");
    }

    private static T Instantiate<T>(string name) where T : Component
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BattleAuthoringPrefabBuilder.Folder + "/" + name + ".prefab");
        if (prefab == null) throw new Exception("Missing prefab: " + name);
        return ((GameObject)PrefabUtility.InstantiatePrefab(prefab)).GetComponent<T>();
    }

    private static PieceController Pawn(Vector3 position)
    {
        var go = new GameObject("ValidationPawn");
        go.transform.position = position;
        var agent = go.AddComponent<NavMeshAgent>();
        agent.radius = 0.5f;
        agent.height = 2;
        var collider = go.AddComponent<CapsuleCollider>();
        collider.radius = 0.5f;
        collider.height = 2;
        collider.center = Vector3.up;
        return go.AddComponent<PieceController>();
    }
}
