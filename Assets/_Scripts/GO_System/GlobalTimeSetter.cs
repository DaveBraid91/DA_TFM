using UnityEngine;

public class GlobalTimeSetter : MonoBehaviour
{
    private static readonly int GlobalTimeId = Shader.PropertyToID("_GlobalTime");

    private void Update()
    {
        Shader.SetGlobalFloat(GlobalTimeId, Time.time);
    }
}