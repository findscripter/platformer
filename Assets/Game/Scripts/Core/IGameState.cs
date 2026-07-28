public interface IGameState
{
    void Enter();
    void Update(float deltaTime);
    void FixedUpdate(float fixedDeltaTime);
    void Exit();
}
