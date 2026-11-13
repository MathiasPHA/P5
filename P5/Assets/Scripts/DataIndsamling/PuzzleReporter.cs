using UnityEngine;

// Sends puzzle events to the host's SessionLogger. Works from both the PC and the VR side.
// Put one on a puzzle, pick which puzzle it is, and hook its methods to the puzzle's
// UnityEvents in the inspector, e.g.:
//   PhoneMaze.onWallHit  -> ReportFail      PhoneMaze.onSolved  -> ReportSolved
//   SafeLock.onUnlocked  -> ReportSolved
public class PuzzleReporter : MonoBehaviour
{
    [SerializeField] PuzzleId puzzle;

    // Optional. Without it, a puzzle's time starts when the previous puzzle was solved.
    public void ReportStart() => Send(PuzzleEvent.Start);
    public void ReportFail() => Send(PuzzleEvent.Fail);
    public void ReportSolved() => Send(PuzzleEvent.Solved);

    void Send(PuzzleEvent evt)
    {
        if (SharedState.Instance == null)
        {
            Debug.LogWarning($"[PuzzleReporter] SharedState missing, {puzzle} {evt} was not logged");
            return;
        }
        SharedState.Instance.ReportPuzzleEventRpc(puzzle, evt);
    }
}
