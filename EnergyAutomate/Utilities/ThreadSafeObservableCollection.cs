using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

public class ThreadSafeObservableCollection<T> : ObservableCollection<T>
{
    #region Fields

    public readonly Lock _syncRoot = new Lock();
    private readonly Dictionary<object, Action> _callbacks = [];

    #endregion Fields

    #region Public Constructors

    public ThreadSafeObservableCollection() : base() { }

    public ThreadSafeObservableCollection(IEnumerable<T> collection) : base(collection) { }

    public ThreadSafeObservableCollection(List<T> list) : base(list) { }

    #endregion Public Constructors

    #region Public Methods

    public new void Add(T item)
    {
        lock (_syncRoot)
        {
            base.Add(item);
        }
    }

    public new void Clear()
    {
        lock (_syncRoot)
        {
            base.Clear();
        }
    }

    public new void Insert(int index, T item)
    {
        lock (_syncRoot)
        {
            base.Insert(index, item);
        }
    }

    public new void Remove(T item)
    {
        lock (_syncRoot)
        {
            base.Remove(item);
        }
    }

    public new void RemoveAt(int index)
    {
        lock (_syncRoot)
        {
            base.RemoveAt(index);
        }
    }

    public void RegisterOnCollectionChanged(object sender, Action callback)
    {
        _callbacks[sender] = callback;
    }

    public void UnRegisterOnCollectionChanged(object sender)
    {
        _callbacks.Remove(sender);
    }

    #endregion Public Methods

    #region Protected Methods

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        lock (_syncRoot)
        {
            base.OnCollectionChanged(e);
            if (e.NewItems != null)
            {
                foreach (var callback in _callbacks)
                {
                    callback.Value.Invoke();
                }
            }
        }
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        lock (_syncRoot)
        {
            base.OnPropertyChanged(e);
        }
    }

    #endregion Protected Methods
}
