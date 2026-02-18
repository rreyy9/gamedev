/// <summary>
/// Abstract base class for all enemy states.
/// 
/// Each concrete state (Idle, Chase, Dead, etc.) inherits from this and
/// implements the three lifecycle methods. The state machine in EnemyController
/// calls these methods — states never call each other directly.
/// 
/// States never hold references to the next state themselves; they signal
/// a transition by calling controller.ChangeState(new SomeState(controller)).
/// </summary>
public abstract class EnemyState
{
    // Protected reference so every state can access the controller and its data.
    protected EnemyController Controller { get; private set; }

    protected EnemyState(EnemyController controller)
    {
        Controller = controller;
    }

    /// <summary>Called once when this state becomes active.</summary>
    public abstract void Enter();

    /// <summary>Called every frame while this state is active (from EnemyController.Update).</summary>
    public abstract void Tick();

    /// <summary>Called once when this state is about to be replaced by another state.</summary>
    public abstract void Exit();
}
