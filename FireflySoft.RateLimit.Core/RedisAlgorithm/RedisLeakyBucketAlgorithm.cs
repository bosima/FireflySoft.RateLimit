using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FireflySoft.RateLimit.Core.Rule;
using FireflySoft.RateLimit.Core.Time;
using StackExchange.Redis;

namespace FireflySoft.RateLimit.Core.RedisAlgorithm
{
    /// <summary>
    /// Redis Leaky Bucket Algorithm
    /// </summary>
    public class RedisLeakyBucketAlgorithm : BaseRedisAlgorithm
    {
        private readonly RedisLuaScript _leakyBucketIncrementLuaScript;

        /// <summary>
        /// Create a new instance
        /// </summary>
        /// <param name="rules">The rate limit rules</param>
        /// <param name="redisClient">The redis client</param>
        /// <param name="timeProvider">The provider of current time</param>
        /// <param name="updatable">If rules can be updated</param>
        public RedisLeakyBucketAlgorithm(IEnumerable<LeakyBucketRule> rules, ConnectionMultiplexer redisClient = null, ITimeProvider timeProvider = null, bool updatable = false)
        : base(rules, redisClient, timeProvider, updatable)
        {
            _leakyBucketIncrementLuaScript = new RedisLuaScript(_redisClient, "Src-IncrWithLeakyBucket",
                @"local ret={}
                local cl_key='{' .. KEYS[1] .. '}'
                local lock_key=cl_key .. '-lock'
                local lock_val=redis.call('get',lock_key)
                if lock_val=='1' then
                    ret[1]=1
                    ret[2]=-1
                    ret[3]=-1
                    return ret;
                end
                ret[1]=0
                local st_key=cl_key .. '-st'
                local amount=tonumber(ARGV[1])
                local capacity=tonumber(ARGV[2])
                local outflow_unit=tonumber(ARGV[3])
                local outflow_quantity_per_unit=tonumber(ARGV[4])
                local key_expire_time=(math.ceil(capacity/outflow_quantity_per_unit)+1)*outflow_unit
                local current_time=tonumber(ARGV[5])
                local start_time=tonumber(ARGV[6])
                local lock_seconds=tonumber(ARGV[7])
                local last_time=redis.call('get',st_key)
                if(last_time==false)
                then
                    redis.call('set',KEYS[1],amount,'PX',key_expire_time)
                    redis.call('set',st_key,start_time,'PX',key_expire_time)
                    ret[2]=amount
                    ret[3]=0
                    return ret
                end
                local current_value = redis.call('get',KEYS[1])
                current_value = tonumber(current_value)
                last_time=tonumber(last_time)
                local past_time=current_time-last_time
                local last_time_changed=0
                local wait=0
                if(past_time<outflow_unit)
                then
                    current_value=current_value+amount
                    if(current_value<=capacity+outflow_quantity_per_unit)
                    then
                        local current_unit_rest_time = outflow_unit - past_time
                        if(current_value>outflow_quantity_per_unit)
                        then
                            local batch_number = math.ceil(current_value/outflow_quantity_per_unit) - 1
                            if (batch_number == 1)
                            then
                                wait = current_unit_rest_time;
                            else
                                wait = outflow_unit * (batch_number - 1) + current_unit_rest_time;
                            end
                        end
                    else
                        if lock_seconds>0 then
                            redis.call('set',lock_key,'1','EX',lock_seconds,'NX')
                        end
                        ret[1]=1
                        ret[2]=current_value-amount
                        ret[3]=-1
                        return ret
                    end
                else
                    local past_outflow_unit_quantity = math.floor(past_time/outflow_unit)
                    last_time=last_time+past_outflow_unit_quantity*outflow_unit
                    last_time_changed=1
                    if (current_value < outflow_quantity_per_unit)
                    then
                        current_value = amount
                        wait = 0
                    else
                        local past_outflow_quantity=past_outflow_unit_quantity*outflow_quantity_per_unit
                        local new_value=current_value-past_outflow_quantity+amount
                        if(new_value<=0)
                        then
                            current_value=amount
                        else
                            current_value=new_value
                        end

                        local current_unit_rest_time = outflow_unit - (current_time - last_time)
                        if(current_value>outflow_quantity_per_unit)
                        then
                            local batch_number = math.ceil(current_value/outflow_quantity_per_unit) - 1
                            if (batch_number == 1)
                            then
                                wait = current_unit_rest_time;
                            else
                                wait = outflow_unit * (batch_number - 1) + current_unit_rest_time;
                            end
                        end
                    end
                end

                if last_time_changed==1 then
                    redis.call('set',KEYS[1],current_value,'PX',key_expire_time)
                    redis.call('set',st_key,last_time,'PX',key_expire_time)
                else
                    redis.call('set',KEYS[1],current_value,'PX',key_expire_time)
                end

                ret[2]=current_value
                ret[3]=wait
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
            throw new NotSupportedException();
        }

        /// <summary>
        /// check single rule for target
        /// </summary>
        /// <param name="target"></param>
        /// <param name="rule"></param>
        /// <returns></returns>
        protected override RuleCheckResult CheckSingleRule(string target, RateLimitRule rule)
        {
            var currentRule = rule as LeakyBucketRule;
            var amount = 1;

            var outflowUnit = currentRule.OutflowUnit.TotalMilliseconds;
            var currentTime = _timeProvider.GetCurrentUtcMilliseconds();
            var startTime = AlgorithmStartTime.ToSpecifiedTypeTime(currentTime, TimeSpan.FromMilliseconds(outflowUnit), currentRule.StartTimeType);

            var ret = (long[])EvaluateScript(_leakyBucketIncrementLuaScript, new RedisKey[] { target },
                new RedisValue[] { amount, currentRule.Capacity, outflowUnit, currentRule.OutflowQuantityPerUnit, currentTime, startTime, currentRule.LockSeconds });
            return new RuleCheckResult()
            {
                IsLimit = ret[0] == 0 ? false : true,
                Target = target,
                Count = ret[1],
                Rule = rule,
                Wait = ret[2],
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
            var currentRule = rule as LeakyBucketRule;
            var amount = 1;

            // can not call redis TIME command in script
            var outflowUnit = currentRule.OutflowUnit.TotalMilliseconds;
            var currentTime = await _timeProvider.GetCurrentUtcMillisecondsAsync();
            var startTime = AlgorithmStartTime.ToSpecifiedTypeTime(currentTime, TimeSpan.FromMilliseconds(outflowUnit), currentRule.StartTimeType);

            var ret = (long[])await EvaluateScriptAsync(_leakyBucketIncrementLuaScript, new RedisKey[] { target },
                new RedisValue[] { amount, currentRule.Capacity, outflowUnit, currentRule.OutflowQuantityPerUnit, currentTime, startTime, currentRule.LockSeconds });
            return new RuleCheckResult()
            {
                IsLimit = ret[0] == 0 ? false : true,
                Target = target,
                Count = ret[1],
                Rule = rule,
                Wait = ret[2],
            };
        }
    }
}