using System;
using System.Linq;
using FireflySoft.RateLimit.Core.Rule;
using FireflySoft.RateLimit.Core.Time;

namespace FireflySoft.RateLimit.Core.InProcessAlgorithm
{
    /// <summary>
    /// The sliding window item
    /// </summary>
    public struct SlidingWindowPeriod
    {
        /// <summary>
        /// The key
        /// </summary>
        public long Key;

        /// <summary>
        /// The count value
        /// </summary>
        public long CountValue;
    }

    /// <summary>
    /// Define a sliding window in memory
    /// </summary>
    public class MemorySlidingWindow
    {
        readonly int _length;
        readonly SlidingWindowPeriod[] _queue;
        readonly SlidingWindowRule _rule;
        readonly long _statPeriodMilliseconds;
        long _startPeriod;

        int _head = 0;
        int _tail = 0;

        /// <summary>
        /// Create a new instance of MemorySlidingWindow
        /// </summary>
        /// <param name="rule"></param>
        public MemorySlidingWindow(SlidingWindowRule rule)
        {
            _length = rule.PeriodNumber;
            _queue = new SlidingWindowPeriod[_length];
            _rule = rule;
            _statPeriodMilliseconds = (long)rule.StatPeriod.TotalMilliseconds;
        }

        /// <summary>
        /// Increment the 'CountValue' of the specified period
        /// </summary>
        /// <param name="periodIndex">The index of specified period in time window</param>
        /// <param name="amount"></param>
        /// <returns>The count value of the current period after increment</returns>
        public long IncreamentPeriod(int periodIndex, int amount)
        {
            _queue[periodIndex].CountValue += amount;
            return _queue[periodIndex].CountValue;
        }

        /// <summary>
        /// Gets the count value of the sliding window
        /// </summary>
        /// <returns></returns>
        public long GetCount()
        {
            return _queue.Sum(d => d.CountValue);
        }

        /// <summary>
        /// Gets the period index of the current time in the sliding window
        /// </summary>
        /// <returns></returns>
        public int LoadPeriod(long currentMilliseconds)
        {
            var currentPeriodResult = GetCurrentPeriod(currentMilliseconds);
            var currentPeriod = currentPeriodResult.Item1;
            var pastPeriods = currentPeriodResult.Item2;

            var tailPeriod = _queue[_tail];

            // first use sliding window
            if (_tail == _head && tailPeriod.Key == 0)
            {
                var firstPeriod = new SlidingWindowPeriod()
                {
                    Key = currentPeriod,
                    CountValue = 0
                };
                _queue[_tail] = firstPeriod;
                return _tail;
            }

            // The current period is exactly corresponding to the tail of the queue
            if (currentPeriod == tailPeriod.Key)
            {
                return _tail;
            }

            // In the case of high concurrency, the previous period may be obtained
            // It is simply considered as the previous one
            if (currentPeriod < tailPeriod.Key)
            {
                int index = _tail;
                index--;
                if (index < 0) index += _length;
                return index;
            }

            // if 'currentPeriod' greater than the last period, we need create new period
            CreatePastPeriod(pastPeriods, tailPeriod);
            return _tail;
        }

        private void CreatePastPeriod(int pastPeriods, SlidingWindowPeriod lastPeriod)
        {
            for (int i = 1; i <= pastPeriods; i++)
            {
                var newPeriod = new SlidingWindowPeriod()
                {
                    Key = lastPeriod.Key + _statPeriodMilliseconds * i,
                    CountValue = 0
                };

                _tail++;
                if (_tail == _length) _tail = 0;

                // this is a circular queue
                if (_tail <= _head)
                {
                    _head++;
                    if (_head == _length) _head = 0;
                }
                _queue[_tail] = newPeriod;
            }
        }

        private Tuple<long, int> GetCurrentPeriod(long currentMilliseconds)
        {
            long currentPeriod = 0;
            int pastPeriods = 0;

            if (_startPeriod == 0)
            {
                var startTimeMilliseconds = AlgorithmStartTime.ToSpecifiedTypeTime(currentMilliseconds, _rule.StatWindow, _rule.StartTimeType);
                _startPeriod = startTimeMilliseconds + _statPeriodMilliseconds - 1;
                currentPeriod = _startPeriod;
            }
            else
            {
                var tailPeriod = _queue[_tail].Key;
                var pastMilliseconds = currentMilliseconds - tailPeriod;
                if (pastMilliseconds <= 0)
                {
                    currentPeriod = tailPeriod;
                }
                else
                {
                    pastPeriods = (int)Math.Ceiling(pastMilliseconds / (double)_statPeriodMilliseconds);
                    currentPeriod = tailPeriod + pastPeriods * _statPeriodMilliseconds;
                }
            }

            return new Tuple<long, int>(currentPeriod, pastPeriods);
        }
    }
}