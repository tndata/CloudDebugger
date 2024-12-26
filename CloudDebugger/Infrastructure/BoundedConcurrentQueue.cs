using System.Collections;
using System.Collections.Concurrent;

namespace CloudDebugger.Infrastructure;

/// <summary>
/// A thread-safe bounded queue that implements ICollection<T>.
/// 
/// This class is used for the in-memory OpenTelemetry exporter
/// 
/// This class uses a ConcurrentQueue<T> as its underlying storage to maintain FIFO (First-In-First-Out) order. 
/// When the maximum capacity is reached, the oldest items in the queue are automatically discarded to make room 
/// for new items. 
/// 
/// Features:
/// - Thread-safe: Uses a lock to synchronize Add and Clear operations.
/// - FIFO ordering: Items are dequeued in the same order they were added.
/// - Bounded capacity: Automatically removes oldest items when the queue size exceeds the specified limit.
/// - ICollection<T> implementation: Provides compatibility with APIs expecting ICollection<T>, 
///   including Add, Contains, CopyTo, and enumeration.
/// 
/// Limitations:
/// - Does not support explicit removal of specific items (Remove method throws NotSupportedException).
/// 
/// Use Cases:
/// - Maintaining a rolling buffer of the latest N items (e.g., logs, metrics, recent events).
/// - Thread-safe access to a limited collection of data in concurrent environments.
/// 
/// Example:
/// var boundedCollection = new BoundedConcurrentQueue<int>(5); // Max 5 items
/// boundedCollection.Add(1);
/// boundedCollection.Add(2);
/// boundedCollection.Add(3);
/// boundedCollection.Add(4);
/// boundedCollection.Add(5);
/// boundedCollection.Add(6); // Oldest item (1) will be discarded
/// Console.WriteLine(string.Join(", ", boundedCollection)); // Outputs: 2, 3, 4, 5, 6
/// </summary>
/// <typeparam name="T">The type of elements in the collection.</typeparam>
public class BoundedConcurrentQueue<T> : ICollection<T>
{
    private readonly ConcurrentQueue<T> _queue = new();
    private readonly int _maxCapacity;
    private readonly Lock _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="BoundedConcurrentQueue{T}"/> class with the specified maximum capacity.
    /// </summary>
    /// <param name="maxCapacity">The maximum number of items the queue can hold. Must be greater than zero.</param>
    public BoundedConcurrentQueue(int maxCapacity)
    {
        if (maxCapacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxCapacity), "Capacity must be greater than zero.");

        _maxCapacity = maxCapacity;
    }

    /// <summary>
    /// Gets the number of items in the queue.
    /// </summary>
    public int Count => _queue.Count;

    /// <summary>
    /// Gets a value indicating whether the collection is read-only. Always returns false.
    /// </summary>
    public bool IsReadOnly => false;

    /// <summary>
    /// Adds an item to the queue. If the maximum capacity is exceeded, the oldest item is removed.
    /// </summary>
    /// <param name="item">The item to add to the queue.</param>
    public void Add(T item)
    {
        lock (_lock)
        {
            _queue.Enqueue(item);

            // Remove oldest item if capacity is exceeded
            while (_queue.Count > _maxCapacity)
            {
                _queue.TryDequeue(out _);
            }
        }
    }

    /// <summary>
    /// Throws NotSupportedException because explicit removal of specific items is not supported.
    /// </summary>
    /// <param name="item">The item to remove.</param>
    /// <returns>Always throws an exception.</returns>
    public bool Remove(T item)
    {
        throw new NotSupportedException("Remove operation is not supported in this collection.");
    }

    /// <summary>
    /// Clears all items from the queue.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _queue.Clear();
        }
    }

    /// <summary>
    /// Determines whether the queue contains a specific item.
    /// </summary>
    /// <param name="item">The item to locate in the queue.</param>
    /// <returns>true if the item is found; otherwise, false.</returns>
    public bool Contains(T item)
    {
        return _queue.Contains(item);
    }

    /// <summary>
    /// Copies the elements of the queue to an array, starting at a particular array index.
    /// </summary>
    /// <param name="array">The array to copy elements to.</param>
    /// <param name="arrayIndex">The zero-based index in the array at which copying begins.</param>
    public void CopyTo(T[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(nameof(array));

        foreach (var item in _queue)
        {
            array[arrayIndex++] = item;
        }
    }

    /// <summary>
    /// Returns an enumerator that iterates through the queue.
    /// </summary>
    /// <returns>An enumerator for the queue.</returns>
    public IEnumerator<T> GetEnumerator()
    {
        return _queue.GetEnumerator();
    }

    /// <summary>
    /// Returns an enumerator that iterates through the queue (non-generic version).
    /// </summary>
    /// <returns>An enumerator for the queue.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
