using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;

public class AlignPlayerOnLoad : MonoBehaviour
{
    [SerializeField] private XROrigin xrOrigin;
    [SerializeField] private Transform spawnPoint;   // assign per scene, or find by tag

    private IEnumerator Start()
    {
        // Head pose isn't valid on the first frame(s) after load
        yield return null;
        yield return new WaitForEndOfFrame();

        Align();
    }

    public void Align()
    {
        Vector3 flatForward = Vector3.ProjectOnPlane(spawnPoint.forward, Vector3.up).normalized;

        // Rotates the rig so the camera's forward matches (yaw only)
        xrOrigin.MatchOriginUpCameraForward(Vector3.up, flatForward);

        // Moves the rig so the camera (head) sits over the spawn point, not the rig's centre
        Vector3 target = spawnPoint.position;
        target.y = xrOrigin.Camera.transform.position.y; // keep the real head height
        xrOrigin.MoveCameraToWorldLocation(target);
    }
}