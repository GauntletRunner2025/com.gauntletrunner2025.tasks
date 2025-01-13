using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public abstract partial class TaskSystem : SystemBase
{
    protected abstract void OnTaskComplete(EntityCommandBuffer ecb, Entity entity);
    protected abstract void OnTaskFailed(EntityCommandBuffer ecb, Entity entity, AggregateException exception);

    protected class Task : IComponentData, IEnableableComponent
    {
        public System.Threading.Tasks.Task Value;
    }

    protected override void OnUpdate()
    {
        using EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
        foreach (var (item, entity) in SystemAPI
            .Query<Task>()
            .WithEntityAccess())
        {
            if (!item.Value.IsCompleted)
                continue;

            if (item.Value.IsFaulted || item.Value.IsCanceled || !item.Value.IsCompletedSuccessfully)
            {
                OnTaskFailed(ecb, entity, item.Value.Exception);
                ecb.SetComponentEnabled<Task>(entity, false);
                continue;
            }

            ecb.RemoveComponent<Task>(entity);

            Debug.Log($"[{this.GetType().Name}] Task completed");
            OnTaskComplete(ecb, entity);

        }

        ecb.Playback(EntityManager);

    }

}