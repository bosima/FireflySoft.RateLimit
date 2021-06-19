using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FireflySoft.RateLimit.Core.Rule;
using FireflySoft.RateLimit.Core.Time;
using StackExchange.Redis;

namespace FireflySoft.RateLimit.Core.RedisAlgorithm
{
    /// <summary>
    /// 
    /// </summary>
    public class RedisSlidingWindowAlgorithm : BaseRedisAlgorithm
    {
        private readonly RedisLuaScript _slidingWindowIncrementLuaScript;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rules"></param>
        /// <param name="redisClient"></param>
        /// <param name="timeProvider"></param>
        /// <param name="updatable"></param>
        public RedisSlidingWindowAlgorithm(IEnumerable<SlidingWindowRule> rules, ConnectionMultiplexer redisClient = null, ITimeProvider timeProvider = null, bool updatable = false)
        : base(rules, redisClient, timeProvider, updatable)
        {
            _slidingWindowIncrementLuaScript = new RedisLuaScript(_redisClient, "Src-IncrWithExpireSec",
                @"local ret={}
                local lock_key=KEYS[1] .. '-lock'
                local lock_val=redis.call('get',lock_key)
                if lock_val == '1' then
                    ret[1]=1
                    ret[2]=-1
                    return ret;
                end
                ret[1]=0
                local st_key=KEYS[1] .. '-st'
                local amount=tonumber(ARGV[1])
                local period_expire_ms=tonumber(ARGV[2])
                local period_ms=tonumber(ARGV[3])
                local period_number=tonumber(ARGV[4])
                local current_time=tonumber(ARGV[5])
                local cal_start_time=tonumber(ARGV[6])
                local limit_number=tonumber(ARGV[7])
                local lock_seconds=tonumber(ARGV[8])
                local key_expire_time=period_expire_ms+10
                local current_period
                local current_period_key
                local start_time=redis.call('get',st_key)
                if(start_time==false)
                then
                    start_time=cal_start_time
                    current_period=start_time+period_ms-1
                    current_period_key=KEYS[1] .. '-' .. current_period
                    redis.call('set',st_key,start_time,'PX',key_expire_time)
                    redis.call('set',current_period_key,amount,'PX',period_expire_ms)
                    ret[2]=amount
                    return ret
                end

                start_time=tonumber(start_time)
                local past_ms=current_time-start_time
                local past_period_number=past_ms/period_ms
                local past_period_number_floor=math.floor(past_period_number)
                local past_period_number_ceil=math.ceil(past_period_number)

                local past_period_number_fixed=past_period_number_floor
                if (past_period_number_ceil > past_period_number_floor)
                then
                    past_period_number_fixed = past_period_number_ceil
                end
                if past_period_number_fixed==0
                then
                    past_period_number_fixed=1
                end
                current_period=start_time + past_period_number_fixed * period_ms - 1
                current_period_key=KEYS[1] .. '-' .. current_period

                local periods={current_period_key}
                for i=1,period_number-1,1 do
                    periods[i+1] = KEYS[1] .. '-' .. (current_period - period_ms * i)
                end
                local periods_amount=0
                local periods_amount_array=redis.call('mget',unpack(periods))
                for key,value in ipairs(periods_amount_array) do
                    if(value~=false)
                    then
                        periods_amount=periods_amount+value
                    end
                end

                ret[2]=amount+periods_amount
                if (limit_number>=0 and ret[2]>limit_number) then
                    if lock_seconds>0 then 
                        redis.call('set',lock_key,'1','EX',lock_seconds,'NX')
                    end
                    ret[1]=1
                    ret[2]=periods_amount
                    return ret
                end

                local current_amount
                current_amount = redis.call('incrby',current_period_key,amount)
                current_amount = tonumber(current_amount)
                if current_amount == amount then
                    redis.call('PEXPIRE',current_period_key,period_expire_ms)
                end

                return ret");
        }

        /// <summary>
        /// check single rule for target
        /// </summary>
        /// <param name="target"></param>
        /// <param name="rule"></param>
        /// <returns></returns>
        protected override RuleCheckResult CheckSingleRule(string target, RateLimitRule rule)
        {
            var currentRule = rule as SlidingWindowRule;
            var amount = 1;

            var currentTime = _timeProvider.GetCurrentUtcMilliseconds();
            var startTime = AlgorithmStartTime.ToSpecifiedTypeTime(currentTime, currentRule.StatWindow, currentRule.StartTimeType);
            long expireMilliseconds = ((long)currentRule.StatWindow.TotalMilliseconds) * 2;
            long periodMilliseconds = (long)currentRule.StatPeriod.TotalMilliseconds;

            var ret = (long[])EvaluateScript(_slidingWindowIncrementLuaScript,
                 new RedisKey[] { target },
                 new RedisValue[] { amount, expireMilliseconds, periodMilliseconds, currentRule.PeriodNumber, currentTime, startTime, currentRule.LimitNumber, currentRule.LockSeconds });
            return new RuleCheckResult()
            {
                IsLimit = ret[0] == 0 ? false : true,
                Target = target,
                Count = ret[1],
                Rule = rule
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="rule"></param>
        /// <returns></returns>
        protected override async Task<RuleCheckResult> CheckSingleRuleAsync(string target, RateLimitRule rule)
        {
            var currentRule = rule as SlidingWindowRule;
            var amount = 1;

            var currentTime = await _timeProvider.GetCurrentUtcMillisecondsAsync();
            var startTime = AlgorithmStartTime.ToSpecifiedTypeTime(currentTime, currentRule.StatWindow, currentRule.StartTimeType);
            long expireMilliseconds = ((long)currentRule.StatWindow.TotalMilliseconds) * 2;
            long periodMilliseconds = (long)currentRule.StatPeriod.TotalMilliseconds;

            var ret = (long[])await EvaluateScriptAsync(_slidingWindowIncrementLuaScript,
                 new RedisKey[] { target },
                 new RedisValue[] { amount, expireMilliseconds, periodMilliseconds, currentRule.PeriodNumber, currentTime, startTime, currentRule.LimitNumber, currentRule.LockSeconds });
            return new RuleCheckResult()
            {
                IsLimit = ret[0] == 0 ? false : true,
                Target = target,
                Count = ret[1],
                Rule = rule
            };
        }
    }
}