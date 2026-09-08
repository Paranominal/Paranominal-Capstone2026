// Summary:
// Interface for custom movement scripts that can be dropped onto FlyingEnemyBehaviour. Implement this on a MonoBehaviour to define how a flying/non-NavMesh enemy moves.

using UnityEngine;

public interface IEnemyMovement
{
    void MoveTo(Vector3 target, float speed, float stopDistance);
    void Stop();
    bool HasReachedTarget { get; }
}
