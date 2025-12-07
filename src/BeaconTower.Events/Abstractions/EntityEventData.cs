namespace BeaconTower.Events.Abstractions;

/// <summary>
/// Data payload for entity created events.
/// </summary>
/// <typeparam name="T">The type of the entity.</typeparam>
public record EntityCreatedData<T>
{
    /// <summary>
    /// Gets or sets the newly created entity.
    /// </summary>
    public required T Entity { get; init; }
}

/// <summary>
/// Data payload for entity updated events.
/// </summary>
/// <typeparam name="T">The type of the entity.</typeparam>
public record EntityUpdatedData<T>
{
    /// <summary>
    /// Gets or sets the entity state before the update.
    /// </summary>
    public required T OldEntity { get; init; }

    /// <summary>
    /// Gets or sets the entity state after the update.
    /// </summary>
    public required T NewEntity { get; init; }

    /// <summary>
    /// Gets or sets the names of properties that changed during the update.
    /// </summary>
    public IReadOnlyList<string> ChangedFields { get; init; } = [];
}

/// <summary>
/// Data payload for entity deleted events.
/// </summary>
/// <typeparam name="T">The type of the entity.</typeparam>
public record EntityDeletedData<T>
{
    /// <summary>
    /// Gets or sets the deleted entity.
    /// </summary>
    public required T Entity { get; init; }
}
