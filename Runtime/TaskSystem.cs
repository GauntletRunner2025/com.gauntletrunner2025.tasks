using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public abstract partial class TaskSystem : SystemBase {

    private EntityQuery taskQuery;

    protected abstract void OnTaskComplete(EntityManager em, Entity entity);
    protected abstract void OnTaskFailed(EntityManager em, Entity entity, AggregateException exception);
    
    // New: Abstract method for derived system's update logic
    protected abstract void OnSystemUpdate();

    protected abstract ComponentType FlagType { get; }
    protected abstract ComponentType[] RequiredForUpdate { get; }

    protected abstract Task Setup(EntityManager em, Entity entity, Task task);
    
    protected virtual bool ShouldCreateNewTask() => false;

    sealed protected override void OnCreate() {
        foreach (var c in RequiredForUpdate) {
            RequireForUpdate(GetEntityQuery(RequiredForUpdate));
        }

        // Create the EntityQuery
        taskQuery = GetEntityQuery(new EntityQueryDesc {
            All = new ComponentType[] { typeof(Task), FlagType }
        });
    }

    protected virtual Task CreateNewTask() {
        var task = new Task(); // Default task created
        var e = EntityManager.CreateEntity();
        task = Setup(EntityManager, e, task); // Update task to whatever Setup returns
        EntityManager.AddComponentData(e, task); // Add the task to the entity
        EntityManager.AddComponent(e, FlagType);
        return task; // Return the created task without evaluation
    }

    protected class Task : IComponentData, IEnableableComponent {
        public System.Threading.Tasks.Task Value;
    }

    // Seal OnUpdate to prevent derived systems from overriding
    protected override void OnUpdate() {
        // Handle completed tasks
        using var entities = taskQuery.ToEntityArray(Allocator.TempJob);
        
        foreach (var e in entities) {
            var item = EntityManager.GetComponentData<Task>(e);
            if (!item.Value.IsCompleted)
                continue;

            if (item.Value.IsFaulted || item.Value.IsCanceled || !item.Value.IsCompletedSuccessfully) {
                OnTaskFailed(EntityManager, e, item.Value.Exception);
                EntityManager.SetComponentEnabled<Task>(e, false);
                continue;
            }

            EntityManager.RemoveComponent<Task>(e);
            OnTaskComplete(EntityManager, e);
        }

        // Let the derived system do its update
        OnSystemUpdate();

        // Check if we should create a new task
        if (ShouldCreateNewTask()) {
            CreateNewTask();
        }
    }

    protected sealed override void OnStartRunning() {
        CreateNewTask();
    }
}