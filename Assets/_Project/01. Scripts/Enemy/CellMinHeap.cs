namespace ExplodeIt.Enemies
{
    // 다익스트라용 최소 힙. Unity의 .NET 프로필에는 PriorityQueue가 없고,
    // 재계산마다 할당하지 않도록 배열을 한 번만 만들어 재사용한다.
    // 우선순위 갱신 대신 중복 삽입을 허용하고, 꺼낼 때 이미 더 짧은 거리가 확정된 항목은 호출 쪽에서 버린다.
    public class CellMinHeap
    {
        private readonly int[] _cells;
        private readonly float[] _priorities;
        private int _count;

        public int Count => _count;

        public CellMinHeap(int capacity)
        {
            _cells = new int[capacity];
            _priorities = new float[capacity];
        }

        public void Clear()
        {
            _count = 0;
        }

        public void Push(int cell, float priority)
        {
            int index = _count++;
            _cells[index] = cell;
            _priorities[index] = priority;

            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (_priorities[parent] <= _priorities[index])
                {
                    break;
                }

                Swap(index, parent);
                index = parent;
            }
        }

        public int Pop(out float priority)
        {
            int cell = _cells[0];
            priority = _priorities[0];

            _count--;
            _cells[0] = _cells[_count];
            _priorities[0] = _priorities[_count];

            int index = 0;
            while (true)
            {
                int left = index * 2 + 1;
                if (left >= _count)
                {
                    break;
                }

                int right = left + 1;
                int smallest = right < _count && _priorities[right] < _priorities[left] ? right : left;
                if (_priorities[index] <= _priorities[smallest])
                {
                    break;
                }

                Swap(index, smallest);
                index = smallest;
            }

            return cell;
        }

        private void Swap(int a, int b)
        {
            int cell = _cells[a];
            _cells[a] = _cells[b];
            _cells[b] = cell;

            float priority = _priorities[a];
            _priorities[a] = _priorities[b];
            _priorities[b] = priority;
        }
    }
}
