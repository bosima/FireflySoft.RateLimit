using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace FireflySoft.RateLimit.Core
{
    /// <summary>
    /// Redis Storage
    /// </summary>
    public class RedisStorage : IRateLimitStorage
    {
        private readonly ConnectionMultiplexer _redisClient;

        private readonly ConcurrentDictionary<string, Lazy<byte[]>> _loadedLuaScriptsOnServer = new ConcurrentDictionary<string, Lazy<byte[]>>();

        /// <summary>
        /// Create a new instance
        /// </summary>
        /// <param name="redisClient"></param>
        public RedisStorage(ConnectionMultiplexer redisClient)
        {
            _redisClient = redisClient;
        }

        /// <summary>
        /// Lock the rate limit target until the expiration time, when triggering the rate limit rule.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="expireTimeSpan"></param>
        public void TryLock(string target, TimeSpan expireTimeSpan)
        {
            IDatabase database = _redisClient.GetDatabase();
            database.StringSet($"lock-{target}", 1, expireTimeSpan, when: When.NotExists);
        }

        /// <summary>
        /// Check whether the rate limit target is locked
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public bool CheckLocked(string target)
        {
            IDatabase database = _redisClient.GetDatabase();
            long state = (long)database.StringGet($"lock-{target}");

            if (state == 1)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Get the current unified time
        /// </summary>
        /// <returns></returns>
        public long GetCurrentTime()
        {
            var endPoints = _redisClient.GetEndPoints();
            foreach (var endPoint in endPoints)
            {
                var server = _redisClient.GetServer(_redisClient.GetEndPoints()[0]);
                if (server.IsConnected)
                {
                    var redisTime = server.Time();
                    DateTimeOffset utcTime = redisTime;
                    return utcTime.ToUnixTimeMilliseconds();
                }
            }

            return 0;
        }

        /// <summary>
        /// Adds a rate limit target to the storage by using the specified function if the target does not already exist. Returns the new value, or the existing value if the target exists.
        /// </summary>
        /// <param name="target">The target to add.</param>
        /// <param name="retrieveMethod">The function used to generate a value for the target.</param>
        /// <returns></returns>
        public long GetOrAdd(string target, Lazy<long> retrieveMethod)
        {
            IDatabase database = _redisClient.GetDatabase();
            if (!database.StringSet(target, retrieveMethod.Value, when: When.NotExists))
            {
                return (long)database.StringGet(target);
            }
            return retrieveMethod.Value;
        }

        /// <summary>
        /// Gets the sum of the counts of multiple rate limit targets
        /// </summary>
        /// <param name="targets">The targets</param>
        /// <returns></returns>
        public long Sum(IEnumerable<string> targets)
        {
            IDatabase database = _redisClient.GetDatabase();
            var values = database.StringGetAsync(targets.Select(d => (RedisKey)d).ToArray()).ConfigureAwait(false).GetAwaiter().GetResult();
            if (values != null && values.Length > 0)
            {
                return values.Where(d => d.HasValue).Select(d => (long)d).Sum();
            }

            return 0;
        }

        /// <summary>
        /// Increase the count value of the rate limit target. When the target does not exist, create it first and increase the specified value, then set its expiration time.
        /// </summary>
        /// <param name="target">The target</param>
        /// <param name="amount">amount of increase</param>
        /// <param name="expireTimeSpan">The expiration time is set when the target is created</param>
        /// <returns>amount of requests</returns>
        public long SimpleIncrement(string target, long amount, TimeSpan expireTimeSpan)
        {
            var expireSeconds = (int)expireTimeSpan.TotalSeconds;
            if (expireSeconds < 1)
            {
                expireSeconds = 1;
            }

            string luaScript = @"local amount=tonumber(ARGV[1])
                                local current
                                current = redis.call('incrby',KEYS[1],amount)
                                current = tonumber(current)
                                if current == amount then
                                    redis.call('EXPIRE',KEYS[1], ARGV[2])
                                end
                                return current";

            return (long)EvaluateScript("Src-IncrWithExpireSec", luaScript, new RedisKey[] { target },
                new RedisValue[] { amount, expireSeconds });
        }

        /// <summary>
        /// Increase the count value of the rate limit target for leaky bucket algorithm.
        /// </summary>
        /// <param name="target">The target</param>
        /// <param name="amount">amount of increase</param>
        /// <param name="capacity">The capacity of leaky bucket</param>
        /// <param name="outflowUnit">The time unit of outflow from the leaky bucket</param>
        /// <param name="outflowQuantityPerUnit">The outflow quantity per unit time</param>
        /// <returns>Amount of request in the bucket</returns>
        public long LeakyBucketIncrement(string target, long amount, long capacity, int outflowUnit, int outflowQuantityPerUnit)
        {
            // todo: need a global timestamp, but can not call redis TIME command in script
            // GetCurrentTime() may be a good choice
            var currentTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            string luaScript = @"local amount=tonumber(ARGV[1])
                                local capacity=tonumber(ARGV[2])
                                local outflow_unit=tonumber(ARGV[3])
                                local outflow_quantity_per_unit=tonumber(ARGV[4])
                                local current_time=tonumber(ARGV[5])
                                local last_time
                                last_time=redis.call('get',KEYS[2])
                                if(last_time==false)
                                then
                                    redis.call('mset',KEYS[1],amount,KEYS[2],current_time)
                                    return amount
                                end
                                
                                local current_value = redis.call('get',KEYS[1])
                                current_value = tonumber(current_value)
                                last_time=tonumber(last_time)
                                local past_time=current_time-last_time
                                if(past_time<outflow_unit)
                                then
                                    current_value=current_value+amount
                                    redis.call('set',KEYS[1],current_value)
                                    return current_value
                                end

                                local past_outflow_unit_quantity = past_time/outflow_unit
                                past_outflow_unit_quantity=math.floor(past_outflow_unit_quantity)
                                last_time=last_time+past_outflow_unit_quantity*outflow_unit

                                local past_outflow_quantity=past_outflow_unit_quantity*outflow_quantity_per_unit
                                
                                if(current_value>capacity)
                                then
                                    current_value=capacity
                                end

                                local new_value=current_value-past_outflow_quantity+amount
                                if(new_value<=0)
                                then
                                    current_value=amount
                                else
                                    current_value=new_value
                                end
                                
                                redis.call('mset',KEYS[1],current_value,KEYS[2],last_time)
                                return current_value";

            return (long)EvaluateScript("Src-IncrWithLeakyBucket", luaScript, new RedisKey[] { target, "lb_" + target },
                new RedisValue[] { amount, capacity, outflowUnit, outflowQuantityPerUnit, currentTime });
        }

        /// <summary>
        /// Decrease the count value of the rate limit target for token bucket algorithm.
        /// </summary>
        /// <param name="target">The target</param>
        /// <param name="amount">amount of decrease</param>
        /// <param name="capacity">The capacity of token bucket</param>
        /// <param name="inflowUnit">The time unit of inflow to the bucket bucket</param>
        /// <param name="inflowQuantityPerUnit">The inflow quantity per unit time</param>
        /// <returns>Amount of token in the bucket</returns>
        public long TokenBucketDecrement(string target, long amount, long capacity, int inflowUnit, int inflowQuantityPerUnit)
        {
            // todo: need a global timestamp, but can not call redis TIME command in script
            // GetCurrentTime() may be a good choice
            var currentTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            string luaScript = @"local amount=tonumber(ARGV[1])
                                local capacity=tonumber(ARGV[2])
                                local inflow_unit=tonumber(ARGV[3])
                                local inflow_quantity_per_unit=tonumber(ARGV[4])
                                local current_time=tonumber(ARGV[5])
                                local last_time=redis.call('get',KEYS[2])
                                if(last_time==false)
                                then
                                    local bucket_amount = capacity - amount;
                                    redis.call('mset',KEYS[1],bucket_amount,KEYS[2],current_time)
                                    return bucket_amount
                                end
                                
                                local current_value = redis.call('get',KEYS[1])
                                current_value = tonumber(current_value)
                                last_time=tonumber(last_time)
                                local past_time=current_time-last_time
                                if(past_time<inflow_unit)
                                then
                                    current_value=current_value-amount
                                    redis.call('set',KEYS[1],current_value)
                                    return current_value
                                end

                                local past_inflow_unit_quantity = past_time/inflow_unit
                                past_inflow_unit_quantity=math.floor(past_inflow_unit_quantity)
                                last_time=last_time+past_inflow_unit_quantity*inflow_unit

                                local past_inflow_quantity=past_inflow_unit_quantity*inflow_quantity_per_unit
                                if(current_value<0)
                                then
                                    current_value=0
                                end
                                local new_value=current_value+past_inflow_quantity-amount
                                if(new_value>=capacity)
                                then
                                    current_value=capacity-amount
                                else
                                    current_value=new_value
                                end
                                redis.call('mset',KEYS[1],current_value,KEYS[2],last_time)
                                return current_value";

            return (long)EvaluateScript("Src-DecrWithLeakyBucket", luaScript, new RedisKey[] { target, "tb_" + target },
                new RedisValue[] { amount, capacity, inflowUnit, inflowQuantityPerUnit, currentTime });
        }

        private RedisResult EvaluateScript(string scriptName, string luaScript, RedisKey[] keys, RedisValue[] values)
        {
            byte[] sha1 = _loadedLuaScriptsOnServer.GetOrAdd(scriptName, s =>
            {
                return new Lazy<byte[]>(() =>
                {
                    var v = CalcLuaSha1(luaScript);

                    var endPoints = _redisClient.GetEndPoints();
                    Array.ForEach(endPoints, endpoint =>
                    {
                        var server = _redisClient.GetServer(_redisClient.GetEndPoints()[0]);
                        if (server.IsConnected)
                        {
                            if (!server.ScriptExists(v))
                            {
                                server.ScriptLoad(luaScript);
                            }
                        }
                    });

                    return v;
                });
            }).Value;

            IDatabase dataBase = _redisClient.GetDatabase();
            return dataBase.ScriptEvaluate(sha1, keys, values);
        }

        private byte[] CalcLuaSha1(string luaScript)
        {
            SHA1 sha1 = new SHA1CryptoServiceProvider();
            var bytesSha1In = Encoding.Default.GetBytes(luaScript);
            return sha1.ComputeHash(bytesSha1In);
        }
    }
}
