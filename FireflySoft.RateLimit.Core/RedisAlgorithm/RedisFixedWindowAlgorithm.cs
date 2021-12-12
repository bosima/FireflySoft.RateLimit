using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FireflySoft.RateLimit.Core.Rule;
using FireflySoft.RateLimit.Core.Time;
using StackExchange.Redis;

namespace FireflySoft.RateLimit.Core.RedisAlgorithm
{
    /// <summary>
    /// Define a redis fixed window algorithm
    /// </summary>
    public class RedisFixedWindowAlgorithm : BaseRedisAlgorithm
    {
        private readonly RedisLuaScript _fixedWindowIncrementLuaScript;
        private readonly RedisLuaScript _fixedWindowPeekLuaScript;

        /// <summary>
        /// create a new instance
        /// </summary>
        /// <param name="rules">The rate limit rules</param>
        /// <param name="redisClient">The redis client</param>
        /// <param name="timeProvider">The time provider</param>
        /// <param name="updatable">If rules can be updated</param>
        public RedisFixedWindowAlgorithm(IEnumerable<FixedWindowRule> rules, ConnectionMultiplexer redisClient = null, ITimeProvider timeProvider = null, bool updatable = false)
        : base(rules, redisClient, timeProvider, updatable)
        {
            _fixedWindowIncrementLuaScript = new RedisLuaScript(_redisClient, "Src-IncrWithExpireSec",
                @"local ret={}
                local cl_key='{' .. KEYS[1] .. '}'
                local lock_key=cl_key .. '-lock'
                local lock_val=redis.call('get',lock_key)
                if lock_val == '1' then
                    ret[1]=1
                    ret[2]=-1
                    return ret;
                end
                ret[1]=0
                local amount=tonumber(ARGV[1])
                local limit_number=tonumber(ARGV[3])
                local lock_seconds=tonumber(ARGV[4])
                local check_result=false
                local current=redis.call('get',KEYS[1])
                if current~=false then
                    current = tonumber(current)
                    if(limit_number>=0 and current>=limit_number) then
                        check_result=true
                    else
                        redis.call('incrby',KEYS[1],amount)
                        current=current+amount
                    end
                else
                    redis.call('set',KEYS[1],amount,'PX',ARGV[2])
                    current=amount
                end
                ret[2]=current
                if check_result then
                    ret[1]=1
                    if lock_seconds>0 then
                        redis.call('set',lock_key,'1','EX',lock_seconds,'NX')
                    end
                end
                return ret");

                _fixedWindowPeekLuaScript = new RedisLuaScript(_redisClient, "Src-PeekIncrWithExpireSec",
                @"local ret={}
                local lock_key=KEYS[1] .. '-lock'
                local lock_val=redis.call('get',lock_key)
                if lock_val == '1' then
                    ret[1]=1
                    ret[2]=-1
                    return ret;
                end
                ret[1]=0
       
                local limit_number=tonumber(ARGV[1])
                local check_result=false
                local current=redis.call('get',KEYS[1])
                if current~=false then
                    current = tonumber(current)
                    if(limit_number>=0 and current>=limit_number) then
                        check_result=true
                    end
                else
                    current=0
                end
                ret[2]=current
               
                return ret");
        }

        /// <summary>
        /// Take a peek at the result of the last processing of the specified target in the specified rule
        /// </summary>
        /// <param name="target"></param>
        /// <param name="rule"></param>
        /// <returns></returns>
        protected override RuleCheckResult PeekSingleRule(string target, RateLimitRule rule)
        {
            var currentRule = rule as FixedWindowRule;

            var ret = (long[])EvaluateScript(_fixedWindowPeekLuaScript,
                new RedisKey[] { target },
                new RedisValue[] { currentRule.LimitNumber });

            return new RuleCheckResult()
            {
                IsLimit = ret[0] == 0 ? false : true,
                Target = target,
                Count = ret[1],
                Rule = rule
            };
        }

        /// <summary>
        /// check single rule for target
        /// </summary>
        /// <param name="target"></param>
        /// <param name="rule"></param>
        /// <returns></returns>
        protected override RuleCheckResult CheckSingleRule(string target, RateLimitRule rule)
        {
            var currentRule = rule as FixedWindowRule;
            var amount = 1;

            // 1. There may also be a millisecond delay in accessing redis. 
            // At this time, some requests cannot be counted to the correct time window, but will be counted to the next time window and will not be missed.
            // 2. In distributed deployment, the time of different machines is not completely synchronized, and there may be several milliseconds difference. 
            // If the time is obtained from a unified address, there is still a certain network access delay.
            // 3. Therefore, when the time window is much longer than these delays or differences, there will be no obvious problems.
            long expireTime = (long)currentRule.StatWindow.TotalMilliseconds;
            if (currentRule.StartTimeType == StartTimeType.FromNaturalPeriodBeign)
            {
                DateTimeOffset now = _timeProvider.GetCurrentUtcTime();
                expireTime = GetExpireTimeFromNaturalPeriodBeign(currentRule.StatWindow, now);
            }

            var ret = (long[])EvaluateScript(_fixedWindowIncrementLuaScript,
                new RedisKey[] { target },
                new RedisValue[] { amount, expireTime, currentRule.LimitNumber, currentRule.LockSeconds });

            return new RuleCheckResult()
            {
                IsLimit = ret[0] == 0 ? false : true,
                Target = target,
                Count = ret[1],
                Rule = rule
            };
        }

        /// <summary>
        /// async check single rule for target
        /// </summary>
        /// <param name="target"></param>
        /// <param name="rule"></param>
        /// <returns></returns>
        protected override async Task<RuleCheckResult> CheckSingleRuleAsync(string target, RateLimitRule rule)
        {
            var currentRule = rule as FixedWindowRule;
            var amount = 1;

            long expireTime = (long)currentRule.StatWindow.TotalMilliseconds;
            if (currentRule.StartTimeType == StartTimeType.FromNaturalPeriodBeign)
            {
                DateTimeOffset now = await _timeProvider.GetCurrentUtcTimeAsync().ConfigureAwait(false);
                expireTime = GetExpireTimeFromNaturalPeriodBeign(currentRule.StatWindow, now);
            }

            var ret = (long[])await EvaluateScriptAsync(_fixedWindowIncrementLuaScript,
                new RedisKey[] { target },
                new RedisValue[] { amount, expireTime, currentRule.LimitNumber, currentRule.LockSeconds }).ConfigureAwait(false);

            return new RuleCheckResult()
            {
                IsLimit = ret[0] == 0 ? false : true,
                Target = target,
                Count = ret[1],
                Rule = rule
            };
        }

        /// <summary>
        /// Get expire time from natural period beign
        /// </summary>
        /// <param name="statWindow"></param>
        /// <param name="now"></param>
        /// <returns></returns>
        protected long GetExpireTimeFromNaturalPeriodBeign(TimeSpan statWindow, DateTimeOffset now)
        {
            DateTimeOffset startTime = AlgorithmStartTime.ToNaturalPeriodBeignTime(now, statWindow);
            DateTimeOffset endTime = startTime.Add(statWindow);
            return (long)endTime.Subtract(now).TotalMilliseconds;
        }
    }
}