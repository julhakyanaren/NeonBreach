public interface IEnemyController
{
    public float MoveSpeed { get; set; }
    public float RotationSpeed { get; set; }
    public float DetectionRadius { get; set; }

    public void FindTarget();
    public void UpdateMovementDirection();
    public void RotateToTarget();
    public void MoveToTarget();
    public void SetDeadState(bool deadState);
}
