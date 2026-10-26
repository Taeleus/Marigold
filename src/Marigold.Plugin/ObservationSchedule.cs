using System.Collections.Generic;

namespace Marigold
{
    internal sealed class ObservationSchedule<T>
    {
        private readonly Queue<T> queue = new Queue<T>();
        private readonly HashSet<T> queued = new HashSet<T>();
        private IList<T> sweep = new T[0];
        private int cursor;

        public bool Ready { get; private set; }

        public void Reset(IList<T> items)
        {
            sweep = items;
            cursor = 0;
            Ready = items.Count == 0;
            queue.Clear();
            queued.Clear();
        }

        public void Mark(T item)
        {
            if (queued.Count < 4096 && queued.Add(item))
                queue.Enqueue(item);
        }

        public List<T> Take(int budget)
        {
            var result = new List<T>();
            int priority = budget / 2;
            while (queue.Count > 0 && result.Count < priority)
            {
                var item = queue.Dequeue();
                queued.Remove(item);
                result.Add(item);
            }

            int remaining = System.Math.Min(budget - result.Count, sweep.Count);
            while (remaining-- > 0)
            {
                result.Add(sweep[cursor++]);
                if (cursor >= sweep.Count)
                {
                    cursor = 0;
                    Ready = true;
                }
            }

            return result;
        }
    }
}
