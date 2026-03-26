// Achievement.cs
using System.Threading.Tasks;
using UnityEngine;

public class Achievement : MonoBehaviour
{
    public string achievementId;

    protected bool performTracking = true;
    protected virtual float TrackingLoopInterval => 2;

    protected virtual void Start()
    {
        TrackingLoopCaller();
    }

    private async void TrackingLoopCaller()
    {
        while (true)
        {
            if (!performTracking)
                return;

            await Task.Delay((int)(TrackingLoopInterval * 1000));
            TrackingLoop();
        }
    }

    protected virtual void TrackingLoop() { }
}