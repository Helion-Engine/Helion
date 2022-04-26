using Helion.Util.Assertion;
using System;

namespace Helion.World.Entities;

// Represents a weak reference to an entity.
// Used for handling references that can be disposed.
// For example if a monster has a lost soul as a target it will eventually be completely disposed from the game.
// When the lost soul is disposed it will set all the weak references to null so that monster no longer has a reference to this disposed entity.
public class WeakEntity
{
    public static readonly WeakEntity Default = new(null);
    private static WeakEntity?[] WeakEntities = new WeakEntity?[1024];

    public Entity? Entity;

    private WeakEntity(Entity? entity)
    {
        Entity = entity;
    }

    public static WeakEntity GetReference(Entity? entity)
    {
        if (entity == null)
            return Default;

        if (entity.Id >= WeakEntities.Length)
        {
            WeakEntity?[] newEntities = new WeakEntity?[Math.Max(WeakEntities.Length * 2, entity.Id * 2)];
            Array.Copy(WeakEntities, newEntities, WeakEntities.Length);
            WeakEntities = newEntities;
        }

        var weakEntity = WeakEntities[entity.Id];
        if (weakEntity == null)
        {
            weakEntity = new WeakEntity(entity);
            WeakEntities[entity.Id] = weakEntity;
            return weakEntity;
        }

        // Safety check to make sure references are not held over from a different world.
        Assert.Precondition(!(weakEntity.Entity != null && weakEntity.Entity != entity), "Weak reference entity incorrectly set.");
        Assert.Precondition(!ReferenceEquals(weakEntity, Default), "Default static instance set in array.");
        weakEntity.Entity = entity;
        return weakEntity;
    }

    public static void DisposeEntity(Entity entity)
    {
        if (entity.Id >= WeakEntities.Length)
            return;

        var weakEntity = WeakEntities[entity.Id];
        if (weakEntity == null)
            return;

        weakEntity.Entity = null;
    }
}