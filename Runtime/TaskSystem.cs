using System;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public abstract partial class TaskSystem : SystemBase
{

    private EntityQuery taskQuery;

    protected abstract void OnTaskComplete(EntityManager em, Entity entity, Task result);
    protected abstract void OnTaskFailed(EntityManager em, Entity entity, AggregateException exception);

    //abstract field derivers must implement which says whether to auto create a task on start running
    protected abstract bool AutoCreateTask { get; }

    // New: Abstract method for derived system's update logic
    protected abstract void OnSystemUpdate();

    protected abstract ComponentType FlagType { get; }
    protected abstract ComponentType[] RequireForUpdate { get; }

    protected abstract Task Setup(EntityManager em, Entity entity);

    sealed protected override void OnCreate()
    {
        foreach (var c in RequireForUpdate)
        {
            RequireForUpdate(GetEntityQuery(RequireForUpdate));
        }

        // Create the EntityQuery
        taskQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(TaskComponent), FlagType }
        });
    }

    protected void CreateNewTask()
    {
        var e = EntityManager.CreateEntity();
        EntityManager.AddComponentData(e, new TaskComponent
        {
            Value = Setup(EntityManager, e)
        });

        EntityManager.AddComponent(e, FlagType);
    }

    public class TaskComponent : IComponentData, IEnableableComponent
    {
        public Task Value;
    }

    // Seal OnUpdate to prevent derived systems from overriding
    protected override void OnUpdate()
    {
        // Handle completed tasks
        using var entities = taskQuery.ToEntityArray(Allocator.TempJob);

        foreach (var e in entities)
        {
            var item = EntityManager.GetComponentData<TaskComponent>(e);
            if (!item.Value.IsCompleted)
                continue;

            if (item.Value.IsFaulted || item.Value.IsCanceled || !item.Value.IsCompletedSuccessfully)
            {
                OnTaskFailed(EntityManager, e, item.Value.Exception);
                EntityManager.SetComponentEnabled<TaskComponent>(e, false);
                continue;
            }

            EntityManager.RemoveComponent<TaskComponent>(e);

            //Try and cast from a Task to a Task<T> if possible, safely
            OnTaskComplete(EntityManager, e, item.Value);
        }

        // Let the derived system do its update
        OnSystemUpdate();

    }

    protected sealed override void OnStartRunning()
    {
        if (AutoCreateTask)
            CreateNewTask();
    }
}