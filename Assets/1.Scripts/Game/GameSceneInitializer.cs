using CommonLib;
using UnityEngine;

/// <summary>
/// Creates application-wide services in one deterministic place before any scene loads.
/// </summary>
public static class GameSceneInitializer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeApplicationServices()
    {
        _ = MainThreadDispatcher.Instance;
        _ = ServerProfile.Instance;
        _ = ClientServerHandler.Instance;

        if (Object.FindAnyObjectByType<ApplicationLifecycleManager>() == null)
        {
            var lifecycleObject = new GameObject(nameof(ApplicationLifecycleManager));
            lifecycleObject.AddComponent<ApplicationLifecycleManager>();
        }
    }
}
