using Unity.Entities;
using UnityEngine;
using System;
using System.Threading.Tasks;

/// <summary>
/// Example implementation of TaskSystem that creates new tasks periodically
/// based on configurable conditions.
/// </summary>
public partial class ExamplePeriodicTaskSystem : TaskSystem {
    // Component to identify our specific task type
    public struct PeriodicTaskFlag : IComponentData { }

    // Configuration
    public float TaskIntervalSeconds = 5f;
    private float _lastTaskTime;
    
    // Whether to allow multiple tasks to run at once
    protected override bool AllowMultipleTasks => true;

    protected override ComponentType FlagType => ComponentType.ReadWrite<PeriodicTaskFlag>();

    protected override ComponentType[] RequiredForUpdate => Array.Empty<ComponentType>();

    protected override async void OnTaskComplete(EntityManager em, Entity entity) {
        Debug.Log($"Task completed for entity {entity.Index}");
        // Add any completion logic here
    }

    protected override void OnTaskFailed(EntityManager em, Entity entity, AggregateException exception) {
        Debug.LogError($"Task failed for entity {entity.Index}: {exception.Message}");
        // Add any failure handling logic here
    }

    protected override bool Setup(EntityManager em, Entity entity, Task task) {
        // Example async work
        task.Value = DoSomeAsyncWork();
        return true;
    }

    protected override void OnSystemUpdate() {
        // Update our timer
        if (Time.ElapsedTime - _lastTaskTime >= TaskIntervalSeconds) {
            _lastTaskTime = (float)Time.ElapsedTime;
            // This will trigger CreateNewTask() if CanCreateNewTask() is true
            ShouldCreateNewTaskNow = true;
        }

        // Do any other system-specific update work here
    }

    private bool ShouldCreateNewTaskNow;
    protected override bool ShouldCreateNewTask() {
        if (ShouldCreateNewTaskNow) {
            ShouldCreateNewTaskNow = false;
            return true;
        }
        return false;
    }

    private async Task DoSomeAsyncWork() {
        // Example async work - replace with actual task logic
        await Task.Delay(TimeSpan.FromSeconds(UnityEngine.Random.Range(1f, 3f)));
    }
}
