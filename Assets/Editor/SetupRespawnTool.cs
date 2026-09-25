using UnityEditor;
using UnityEngine;
using YourGame.Gameplay.Core;

namespace YourGame.Editor
{
    public class SetupRespawnTool
    {
        [MenuItem("Tools/2D-Gaviola/Setup/3. Setup Respawn and Camera")]
        public static void Setup()
        {
            // 1. Camera Follow
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                CameraFollow cf = mainCam.GetComponent<CameraFollow>();
                if (cf == null) cf = mainCam.gameObject.AddComponent<CameraFollow>();
                
                GameObject player = GameObject.Find("Player");
                if (player != null)
                {
                    cf.SetTarget(player.transform);
                }
            }

            // 2. Respawn Manager & Spawn Point
            GameObject respawnMgrGo = GameObject.Find("RespawnManager");
            if (respawnMgrGo == null)
            {
                respawnMgrGo = new GameObject("RespawnManager");
                RespawnManager rm = respawnMgrGo.AddComponent<RespawnManager>();
                
                GameObject spawnPoint = new GameObject("SpawnPoint");
                spawnPoint.transform.SetParent(respawnMgrGo.transform);
                
                GameObject player = GameObject.Find("Player");
                if (player != null)
                {
                    spawnPoint.transform.position = player.transform.position;
                }

                SerializedObject so = new SerializedObject(rm);
                so.FindProperty("_spawnPoint").objectReferenceValue = spawnPoint.transform;
                so.ApplyModifiedProperties();
            }

            // 3. KillZone
            GameObject killZoneGo = GameObject.Find("KillZone");
            if (killZoneGo == null)
            {
                killZoneGo = new GameObject("KillZone");
                killZoneGo.transform.position = new Vector3(0, -15f, 0);
                killZoneGo.AddComponent<KillZone>();
                
                BoxCollider2D bc = killZoneGo.AddComponent<BoxCollider2D>();
                bc.isTrigger = true;
                bc.size = new Vector2(500f, 10f); // Massive box below level
            }

            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            Debug.Log("[SetupRespawnTool] Setup complete (Camera Follow, RespawnManager, KillZone).");
        }
    }
}
