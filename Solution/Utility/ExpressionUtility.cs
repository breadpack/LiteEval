using System.Collections.Concurrent;
using System.Collections.Generic;

namespace LiteEval.Utility {
    public static class ExpressionUtility<T> {
        private static ConcurrentBag<List<T>>  _pool      = new();
        private static ConcurrentBag<Stack<T>> _stackPool = new();

        public static List<T> Rent() {
            if (_pool.TryTake(out var list)) {
                return list;
            }

            return new();
        }

        public static void Return(List<T> list) {
            list.Clear();
            _pool.Add(list);
        }


        public static Stack<T> RentStack() {
            if (_stackPool.TryTake(out var list)) {
                return list;
            }

            return new();
        }

        public static void ReturnStack(Stack<T> list) {
            list.Clear();
            _stackPool.Add(list);
        }
    }
}