using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Reflection;
using Eremite;
using StockAlert.Infrastructure.Plugin;
using StockAlert.Game;
using StockAlert.Game.Discovery;
using StockAlert.Game.Hooks;

namespace StockAlert.Game.Hooks
{
    internal class StockAlertGameReadyHook : MonoBehaviour
    {
        private void Awake()
        {
            Plugin.Log("StockAlertGameReadyHook created, persistent, ID=" + GetInstanceID());
            DontDestroyOnLoad(this.gameObject);

            SceneManager.sceneLoaded += OnSceneLoaded;
            Plugin.Log("Subscribed to sceneLoaded event");
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Plugin.Log($"OnSceneLoaded called on ID={GetInstanceID()} for scene {scene.name}");

            if (scene.name != "Game")
                return;

            Plugin.Log("Gameplay scene detected, searching for GameMB...");

            foreach (var root in scene.GetRootGameObjects())
            {
                var game = root.GetComponentInChildren<GameMB>();
                if (game != null)
                {
                    Plugin.Log("Found REAL GameMB in gameplay scene");
                    StartCoroutine(WaitForGameServices(game));
                    return;
                }
            }

            Plugin.Log("Gameplay scene loaded but GameMB not found yet.");
        }

        private IEnumerator WaitForGameServices(GameMB game)
        {
            Plugin.Log("Waiting for GameServices...");

            var fi = typeof(GameMB).GetField("GameServices",
                BindingFlags.NonPublic | BindingFlags.Instance);

            while (fi.GetValue(game) == null)
            {
                Plugin.Log("GameServices still null...");
                yield return new WaitForSeconds(0.5f);
            }

            Plugin.Log("GameServices READY — firing callback!");
            Plugin.Instance.OnGameReady();
        }
    }
}
