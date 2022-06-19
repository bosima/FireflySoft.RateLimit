using System;
using System.Linq;
using System.Runtime.CompilerServices;
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
        int _length;
        SlidingWindowPeriod[] _queue;
        SlidingWindowRule _rule;
        long _statPeriodMilliseconds;
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
        /// Reset when rule changed
        /// </summary>
        /// <param name="rule"></param>
        public void ResetIfRuleChanged(SlidingWindowRule rule)
        {
            if (rule.StatPeriod.Ticks == _rule.StatPeriod.Ticks &&
            rule.StatWindow.Ticks == _rule.StatWindow.Ticks)
            {
                return;
            }

            var newLength = rule.PeriodNumber;
            var newQueue = new SlidingWindowPeriod[newLength];
            var newTail = 0;

            // Only handle the case where 'StatPeriod' has not changed.
            // When StatPeriod changes, simply restart the sliding window.
            // Because 'Period' is the minimum count period,
            // the value of the smaller count period cannot be accurately calculated,
            // so the count value of the new 'Period' cannot be calculated.
            if (rule.StatPeriod.Ticks == _rule.StatPeriod.Ticks)
            {
                var loopIndex = _tail;
                newTail = _length - 1;
                if (rule.StatWindow.Ticks < _rule.StatWindow.Ticks)
                {
                    newTail = newLength - 1;
                }

                for (int i = newTail; i >= 0; i--)
                {
                    newQueue[i] = _queue[loopIndex];
                    loopIndex--;
                    if (loopIndex < 0)
                    {
                        loopIndex = _length - 1;
                    }
                }
            }

            _length = newQueue.Length;
            _queue = newQueue;
            _head = 0;
            _tail = newTail;
            _rule = rule;
        }

        /// <summary>
        /// Increment the 'CountValue' of the specified period
        /// </summary>
        /// <param name="periodIndex">The index of specified period in time window</param>
        /// <param name="amount"></param>
        /// <returns>The count value of the current period after increment</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long IncreamentPeriod(int periodIndex, int amount)
        {
            _queue[periodIndex].CountValue += amount;
            return _queue[periodIndex].CountValue;
        }

        /// <summary>
        /// Gets the count value of the sliding window
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long GetCount()
        {
            return _queue.Sum(d => d.CountValue);
        }

        /// <summary>
        /// Gets the period index of the current time in the sliding window
        /// </summary>
        /// <returns></returns>
        public (int periodIndex, long periodId) LoadPeriod(long currentMilliseconds)
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
                return (_tail, firstPeriod.Key);
            }

            // The current period is exactly corresponding to the tail of the queue
            if (currentPeriod == tailPeriod.Key)
            {
                return (_tail, tailPeriod.Key);
            }

            // In the case of high concurrency, the previous period may be obtained
            // It is simply considered as the previous period
            if (currentPeriod < tailPeriod.Key)
            {
                int index = _tail;
                index--;
                if (index < 0) index += _length;
                return (index, _queue[index].Key);
            }

            // if 'currentPeriod' greater than the last period, we need create new period
            CreatePastPeriod(pastPeriods, tailPeriod);
            return (_tail, _queue[_tail].Key);
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