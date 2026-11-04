using Facebook.Unity;
using UnityEngine;

public sealed class FacebookSdkCompatibilitySample : MonoBehaviour
{
    private string status = "Waiting to initialize Facebook SDK";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateSample()
    {
        if (Application.isBatchMode)
        {
            return;
        }

        var sample = new GameObject("FacebookSdkCompatibilitySample");
        Object.DontDestroyOnLoad(sample);
        sample.AddComponent<FacebookSdkCompatibilitySample>();
    }

    private void Start()
    {
        this.status = "Calling FB.Init()";
        FB.Init(
            "123456789",
            "test-client-token",
            onInitComplete: () =>
            {
                this.status = FB.IsInitialized
                    ? "Facebook SDK initialized in the Unity Editor backend"
                    : "Facebook SDK init callback ran, but initialization failed";
            });
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(24, 24, 640, 120), GUI.skin.box);
        GUILayout.Label("Facebook SDK Unity 7 compatibility sample");
        GUILayout.Label(this.status);
        GUILayout.Label("Automated API and reload tests run through the companion Editor scripts.");
        GUILayout.EndArea();
    }
}
