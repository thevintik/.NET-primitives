using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SystemExt.CollectionsExt.GenericExt
{
    public class PoolFullyReservedException : Exception
    {
    }

    public class ResourcePool<TResource>
        where TResource : new()
    {
        private readonly Int32 _size;
        private readonly Stack<Slot> _pool 
            = new Stack<Slot>();
        private readonly Stack<Slot> _reserved
            = new Stack<Slot>();
        private readonly Queue<TaskCompletionSource<Slot>> _takeSlotQueue 
            = new Queue<TaskCompletionSource<Slot>>();

        public class Slot : IDisposable
        {
            private readonly ResourcePool<TResource> _pool;
            private bool _shouldBeReturned = true;
            private readonly TResource _resource;

            public Slot(ResourcePool<TResource> pool)
            {
                this._pool = pool;
                this._resource = new TResource();
            }

            public TResource Resource
                => this._resource;

            public void Reserve()
            {
                this._shouldBeReturned = false;
                this._pool.Reserve(this);
            }

            public void Dispose()
            {
                if (this._shouldBeReturned)
                {
                    this._pool.Add(this);
                }
            }
        }

        public ResourcePool(Int32 size)
        {
            this._size = size;
            for(var i =0; i < size; i++)
            {
                this.Add(new Slot(this));
            }
        }

        private void Add(Slot slot)
        {
            lock (this._takeSlotQueue)
            {
                if (this._takeSlotQueue.Count > 0)
                {
                    this._takeSlotQueue.Dequeue().SetResult(slot);
                }
                else
                {
                    this._pool.Push(slot);
                }
            }
        }

        private void Reserve(Slot slot)
        {
            lock (this._takeSlotQueue)
            {
                this._reserved.Push(slot);

                if (this._reserved.Count >= this._size)
                {
                    foreach (var awaiter in this._takeSlotQueue)
                    {
                        awaiter.SetException(new PoolFullyReservedException());
                    }
                }
            }
        }

        public async Task<Slot> TakeSlot()
        {
            var awaiter = new TaskCompletionSource<Slot>();
            lock (this._takeSlotQueue)
            {
                if (this._reserved.Count >= this._size)
                {
                    awaiter.SetException(new PoolFullyReservedException());
                }
                else if (this._takeSlotQueue.Count == 0
                    && this._pool.Count > 0)
                {
                    awaiter.SetResult(this._pool.Pop());
                }
                else
                {
                    this._takeSlotQueue.Enqueue(awaiter);
                }
            }

            return await awaiter.Task;
        }
    }
}
