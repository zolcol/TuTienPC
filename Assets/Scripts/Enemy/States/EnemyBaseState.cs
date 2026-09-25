using TopDownGame.StateMachine;

namespace TopDownGame.Enemy
{
    public abstract class EnemyBaseState : IState
    {
        protected EnemyController enemy;
        protected TopDownGame.StateMachine.StateMachine stateMachine;

        public EnemyBaseState(EnemyController enemy, TopDownGame.StateMachine.StateMachine stateMachine)
        {
            this.enemy = enemy;
            this.stateMachine = stateMachine;
        }

        public virtual void Enter() { }
        public virtual void Update() { }
        public virtual void PhysicsUpdate() { }
        public virtual void Exit() { }
    }
}
