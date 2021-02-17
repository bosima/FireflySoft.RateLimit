using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StackExchange.Redis;
using System.Threading.Tasks;
using System.Threading;

namespace FireflySoft.RateLimit.Core
{
    /// <summary>
    /// Redis Storage
    /// </summary>
    public class RedisStorage : IRateLimitStorage
    {
        private readonly ConnectionMultiplexer _redisClient;

        private readonly RedisLuaScript _fixedWindowIncrementLuaScript;

        private readonly RedisLuaScript _slidingWindowIncrementLuaScript;

        private readonly RedisLuaScript _leakyBucketIncrementLuaScript;

        private readonly RedisLuaScript _tokenBucketDecrementLuaScript;

        /// <summary>
        /// Create a new instance
        /// </summary>
        /// <param name="redisClient"></param>
        public RedisStorage(ConnectionMultiplexer redisClient)
        {
            _redisClient = redisClient;

            _fixedWindowIncrementLuaScript = new RedisLuaScript(_redisClient, "Src-IncrWithExpireSec",
                @"local ret={}
                local lock_key=KEYS[1] .. '-lock'
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
                local current
                current = redis.call('get',KEYS[1])
                if current~=false then
                    current = tonumber(current)
                    if current>=limit_number then
                        check_result=true
                    else
                        redis.call('incrby',KEYS[1],amount)
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
                local period_expire_ms=tonumber(ARGV[2])*2
                local period_ms=tonumber(ARGV[3])
                local period_number=tonumber(ARGV[4])
                local current_time=tonumber(ARGV[5])
                local limit_number=tonumber(ARGV[6])
                local lock_seconds=tonumber(ARGV[7])
                local current_period
                local current_period_key
                local start_time=redis.call('get',st_key)
                if(start_time==false)
                then
                    start_time=current_time
                    current_period=start_time+period_ms-1
                    current_period_key=KEYS[1] .. '-' .. current_period
                    redis.call('set',st_key,start_time)
                    redis.call('set',current_period_key,amount,'PX',period_expire_ms)
                    ret[2]=amount
                    return ret
                end

                start_time=tonumber(start_time)
                local past_ms=current_time-start_time
                local past_period_number=past_ms/period_ms
                local past_period_number_floor=math.floor(past_period_number)
                local past_period_number_ceiling=math.ceil(past_period_number)

                local past_period_number_fixed=past_period_number_floor
                if (past_period_number_ceiling > past_period_number_floor)
                then
                    past_period_number_fixed = past_period_number_ceiling
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
                if ret[2] > limit_number then
                    if lock_seconds>0 then 
                        redis.call('set',lock_key,'1','EX',lock_seconds,'NX')
                    end
                    ret[1]=1
                    return ret
                end

                local current_amount
                current_amount = redis.call('incrby',current_period_key,amount)
                current_amount = tonumber(current_amount)
                if current_amount == amount then
                    redis.call('PEXPIRE',current_period_key,period_expire_ms)
                end

                return ret");

            _leakyBucketIncrementLuaScript = new RedisLuaScript(_redisClient, "Src-IncrWithLeakyBucket",
                @"local ret={}
                local lock_key=KEYS[1] .. '-lock'
                local lock_val=redis.call('get',lock_key)
                if lock_val == '1' then
                    ret[1]=1
                    ret[2]=-1
                    return ret;
                end
                ret[1]=0
                local st_key= KEYS[1] .. '-st'
                local amount=tonumber(ARGV[1])
                local capacity=tonumber(ARGV[2])
                local outflow_unit=tonumber(ARGV[3])
                local outflow_quantity_per_unit=tonumber(ARGV[4])
                local current_time=tonumber(ARGV[5])
                local lock_seconds=tonumber(ARGV[6])
                local last_time
                last_time=redis.call('get',st_key)
                if(last_time==false)
                then
                    redis.call('mset',KEYS[1],amount,st_key,current_time)
                    ret[2]=amount
                    return ret
                end
                
                local current_value = redis.call('get',KEYS[1])
                current_value = tonumber(current_value)
                last_time=tonumber(last_time)
                local past_time=current_time-last_time
                local last_time_changed=0
                if(past_time<outflow_unit)
                then
                    current_value=current_value+amount
                else
                    local past_outflow_unit_quantity = past_time/outflow_unit
                    past_outflow_unit_quantity=math.floor(past_outflow_unit_quantity)
                    last_time=last_time+past_outflow_unit_quantity*outflow_unit
                    last_time_changed=1
                    local past_outflow_quantity=past_outflow_unit_quantity*outflow_quantity_per_unit
                    local new_value=current_value-past_outflow_quantity+amount
                    if(new_value<=0)
                    then
                        current_value=amount
                    else
                        current_value=new_value
                    end
                end

                ret[2]=current_value
                if(current_value>capacity)
                then
                    if lock_seconds>0 then
                        redis.call('set',lock_key,'1','EX',lock_seconds,'NX')
                    end
                    ret[1]=1
                    return ret
                end

                if last_time_changed==1 then
                    redis.call('mset',KEYS[1],current_value,st_key,last_time)
                else
                    redis.call('set',KEYS[1],current_value)
                end
                return ret");

            _tokenBucketDecrementLuaScript = new RedisLuaScript(_redisClient, "Src-DecrWithTokenBucket",
                @"local ret={}
                local lock_key=KEYS[1] .. '-lock'
                local lock_val=redis.call('get',lock_key)
                if lock_val == '1' then
                    ret[1]=1
                    ret[2]=-1
                    return ret;
                end
                ret[1]=0
                local st_key= KEYS[1] .. '-st'
                local amount=tonumber(ARGV[1])
                local capacity=tonumber(ARGV[2])
                local inflow_unit=tonumber(ARGV[3])
                local inflow_quantity_per_unit=tonumber(ARGV[4])
                local current_time=tonumber(ARGV[5])
                local lock_seconds=tonumber(ARGV[6])
                local bucket_amount=0
                local last_time=redis.call('get',st_key)
                if(last_time==false)
                then
                    bucket_amount = capacity - amount;
                    redis.call('mset',KEYS[1],bucket_amount,st_key,current_time)
                    ret[2]=bucket_amount
                    return ret
                end
                
                local current_value = redis.call('get',KEYS[1])
                current_value = tonumber(current_value)
                last_time=tonumber(last_time)
                local last_time_changed=0
                local past_time=current_time-last_time
                if(past_time<inflow_unit)
                then
                    bucket_amount=current_value-amount
                else
                    local past_inflow_unit_quantity = past_time/inflow_unit
                    past_inflow_unit_quantity=math.floor(past_inflow_unit_quantity)
                    last_time=last_time+past_inflow_unit_quantity*inflow_unit
                    last_time_changed=1
                    local past_inflow_quantity=past_inflow_unit_quantity*inflow_quantity_per_unit
                    bucket_amount=current_value+past_inflow_quantity-amount
                end

                if(bucket_amount>=capacity)
                then
                    bucket_amount=capacity-amount
                end
                ret[2]=bucket_amount

                if(bucket_amount<0)
                then
                    if lock_seconds>0 then
                        redis.call('set',lock_key,'1','EX',lock_seconds,'NX')
                    end
                    ret[1]=1
                    return ret
                end

                if last_time_changed==1 then
                    redis.call('mset',KEYS[1],bucket_amount,st_key,last_time)
                else
                    redis.call('set',KEYS[1],bucket_amount)
                end
                return ret");
        }

        /// <summary>
        /// Increase the count value of the rate limit target. When the target does not exist, create it first and increase the specified value, then set its expiration time.
        /// </summary>
        /// <param name="target">The target</param>
        /// <param name="amount">amount of increase</param>
        /// <param name="expireTimeSpan">The expiration time is set when the target is created</param>
        /// <param name="limitNumber">The number of rate limit</param>
        /// <param name="lockSeconds">The number of seconds locked after triggering rate limiting. 0 means not locked</param>
        /// <returns>amount of requests</returns>
        public Tuple<bool, long> FixedWindowIncrement(string target, long amount, TimeSpan expireTimeSpan, int limitNumber, int lockSeconds)
        {
            var ret = (long[])EvaluateScript(_fixedWindowIncrementLuaScript,
                new RedisKey[] { target },
                new RedisValue[] { amount, (long)expireTimeSpan.TotalMilliseconds, limitNumber, lockSeconds });

            return new Tuple<bool, long>(ret[0] == 0 ? false : true, ret[1]);
        }

        /// <summary>
        /// Increase the count value of the rate limit target. When the target does not exist, create it first and increase the specified value, then set its expiration time.
        /// </summary>
        /// <param name="target">The target</param>
        /// <param name="amount">amount of increase</param>
        /// <param name="expireTimeSpan">The expiration time is set when the target is created</param>
        /// <param name="limitNumber">The number of rate limit</param>
        /// <param name="lockSeconds">The number of seconds locked after triggering rate limiting. 0 means not locked</param>
        /// <returns>amount of requests</returns>
        public async Task<Tuple<bool, long>> FixedWindowIncrementAsync(string target, long amount, TimeSpan expireTimeSpan, int limitNumber, int lockSeconds)
        {
            var ret = (long[])await EvaluateScriptAsync(_fixedWindowIncrementLuaScript,
                new RedisKey[] { target },
                new RedisValue[] { amount, (long)expireTimeSpan.TotalMilliseconds, limitNumber, lockSeconds });
            return new Tuple<bool, long>(ret[0] == 0 ? false : true, ret[1]);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="amount"></param>
        /// <param name="statWindow"></param>
        /// <param name="statPeriod"></param>
        /// <param name="periodNumber"></param>
        /// <param name="limitNumber">The number of rate limit</param>
        /// <param name="lockSeconds">The number of seconds locked after triggering rate limiting. 0 means not locked</param>
        /// <returns></returns>
        public Tuple<bool, long> SlidingWindowIncrement(string target, long amount, TimeSpan statWindow, TimeSpan statPeriod, int periodNumber, int limitNumber, int lockSeconds)
        {
            var currentTime = GetCurrentTime();

            var ret = (long[])EvaluateScript(_slidingWindowIncrementLuaScript,
                 new RedisKey[] { target },
                 new RedisValue[] { amount, (long)statWindow.TotalMilliseconds, (long)statPeriod.TotalMilliseconds, periodNumber, currentTime, limitNumber, lockSeconds });

            return new Tuple<bool, long>(ret[0] == 0 ? false : true, ret[1]);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="amount"></param>
        /// <param name="statWindow"></param>
        /// <param name="statPeriod"></param>
        /// <param name="periodNumber"></param>
        /// <param name="limitNumber">The number of rate limit</param>
        /// <param name="lockSeconds">The number of seconds locked after triggering rate limiting. 0 means not locked</param>
        /// <returns></returns>
        public async Task<Tuple<bool, long>> SlidingWindowIncrementAsync(string target, long amount, TimeSpan statWindow, TimeSpan statPeriod, int periodNumber, int limitNumber, int lockSeconds)
        {
            var currentTime = await GetCurrentTimeAsync();

            var ret = (long[])await EvaluateScriptAsync(_slidingWindowIncrementLuaScript,
                 new RedisKey[] { target },
                 new RedisValue[] { amount, (long)statWindow.TotalMilliseconds, (long)statPeriod.TotalMilliseconds, periodNumber, currentTime, limitNumber, lockSeconds });

            return new Tuple<bool, long>(ret[0] == 0 ? false : true, ret[1]);
        }

        /// <summary>
        /// Increase the count value of the rate limit target for leaky bucket algorithm.
        /// </summary>
        /// <param name="target">The target</param>
        /// <param name="amount">amount of increase</param>
        /// <param name="capacity">The capacity of leaky bucket</param>
        /// <param name="outflowUnit">The time unit of outflow from the leaky bucket</param>
        /// <param name="outflowQuantityPerUnit">The outflow quantity per unit time</param>
        /// <param name="lockSeconds">The number of seconds locked after triggering rate limiting. 0 means not locked</param>
        /// <returns>Amount of request in the bucket</returns>
        public Tuple<bool, long> LeakyBucketIncrement(string target, long amount, long capacity, int outflowUnit, int outflowQuantityPerUnit, int lockSeconds)
        {
            // can not call redis TIME command in script
            var currentTime = GetCurrentTime();

            var ret = (long[])EvaluateScript(_leakyBucketIncrementLuaScript, new RedisKey[] { target },
                new RedisValue[] { amount, capacity, outflowUnit, outflowQuantityPerUnit, currentTime, lockSeconds });
            return new Tuple<bool, long>(ret[0] == 0 ? false : true, ret[1]);
        }

        /// <summary>
        /// Increase the count value of the rate limit target for leaky bucket algorithm.
        /// </summary>
        /// <param name="target">The target</param>
        /// <param name="amount">amount of increase</param>
        /// <param name="capacity">The capacity of leaky bucket</param>
        /// <param name="outflowUnit">The time unit of outflow from the leaky bucket</param>
        /// <param name="outflowQuantityPerUnit">The outflow quantity per unit time</param>
        /// <param name="lockSeconds">The number of seconds locked after triggering rate limiting. 0 means not locked</param>
        /// <returns>Amount of request in the bucket</returns>
        public async Task<Tuple<bool, long>> LeakyBucketIncrementAsync(string target, long amount, long capacity, int outflowUnit, int outflowQuantityPerUnit, int lockSeconds)
        {
            // can not call redis TIME command in script
            var currentTime = await GetCurrentTimeAsync();

            var ret = (long[])await EvaluateScriptAsync(_leakyBucketIncrementLuaScript, new RedisKey[] { target },
                new RedisValue[] { amount, capacity, outflowUnit, outflowQuantityPerUnit, currentTime, lockSeconds });
            return new Tuple<bool, long>(ret[0] == 0 ? false : true, ret[1]);
        }

        /// <summary>
        /// Decrease the count value of the rate limit target for token bucket algorithm.
        /// </summary>
        /// <param name="target">The target</param>
        /// <param name="amount">amount of decrease</param>
        /// <param name="capacity">The capacity of token bucket</param>
        /// <param name="inflowUnit">The time unit of inflow to the bucket bucket</param>
        /// <param name="inflowQuantityPerUnit">The inflow quantity per unit time</param>
        /// <param name="lockSeconds">The number of seconds locked after triggering rate limiting. 0 means not locked</param>
        /// <returns>Amount of token in the bucket</returns>
        public Tuple<bool, long> TokenBucketDecrement(string target, long amount, long capacity, int inflowUnit, int inflowQuantityPerUnit, int lockSeconds)
        {
            // can not call redis TIME command in script
            var currentTime = GetCurrentTime();

            var ret = (long[])EvaluateScript(_tokenBucketDecrementLuaScript, new RedisKey[] { target },
                new RedisValue[] { amount, capacity, inflowUnit, inflowQuantityPerUnit, currentTime, lockSeconds });
            return new Tuple<bool, long>(ret[0] == 0 ? false : true, ret[1]);
        }

        /// <summary>
        /// Decrease the count value of the rate limit target for token bucket algorithm.
        /// </summary>
        /// <param name="target">The target</param>
        /// <param name="amount">amount of decrease</param>
        /// <param name="capacity">The capacity of token bucket</param>
        /// <param name="inflowUnit">The time unit of inflow to the bucket bucket</param>
        /// <param name="inflowQuantityPerUnit">The inflow quantity per unit time</param>
        /// <param name="lockSeconds">The number of seconds locked after triggering rate limiting. 0 means not locked</param>
        /// <returns>Amount of token in the bucket</returns>
        public async Task<Tuple<bool, long>> TokenBucketDecrementAsync(string target, long amount, long capacity, int inflowUnit, int inflowQuantityPerUnit, int lockSeconds)
        {
            // can not call redis TIME command in script
            var currentTime = await GetCurrentTimeAsync();

            var ret = (long[])await EvaluateScriptAsync(_tokenBucketDecrementLuaScript, new RedisKey[] { target },
                new RedisValue[] { amount, capacity, inflowUnit, inflowQuantityPerUnit, currentTime, lockSeconds });
            return new Tuple<bool, long>(ret[0] == 0 ? false : true, ret[1]);
        }

        /// <summary>
        /// Get the current unified time
        /// </summary>
        /// <returns></returns>
        private long GetCurrentTime()
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
        /// Get the current unified time
        /// </summary>
        /// <returns></returns>
        private async Task<long> GetCurrentTimeAsync()
        {
            var endPoints = _redisClient.GetEndPoints();
            foreach (var endPoint in endPoints)
            {
                var server = _redisClient.GetServer(endPoint);
                if (server.IsConnected)
                {
                    var redisTime = await server.TimeAsync();
                    DateTimeOffset utcTime = redisTime;
                    return utcTime.ToUnixTimeMilliseconds();
                }
            }

            return 0;
        }

        /// <summary>
        /// Lock the rate limit target until the expiration time, when triggering the rate limit rule.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="expireTimeSpan"></param>
        private void TryLock(string target, TimeSpan expireTimeSpan)
        {
            IDatabase database = _redisClient.GetDatabase();
            database.StringSet($"{target}-lock", 1, expireTimeSpan, when: When.NotExists);
        }

        private async Task<RedisResult> EvaluateScriptAsync(RedisLuaScript luaScript, RedisKey[] keys, RedisValue[] values)
        {
            byte[] sha1 = await luaScript.LoadAsync();
            IDatabase dataBase = _redisClient.GetDatabase();
            return await dataBase.ScriptEvaluateAsync(sha1, keys, values);
        }

        private RedisResult EvaluateScript(RedisLuaScript luaScript, RedisKey[] keys, RedisValue[] values)
        {
            byte[] sha1 = luaScript.Load();
            IDatabase dataBase = _redisClient.GetDatabase();
            return dataBase.ScriptEvaluate(sha1, keys, values);
        }

        private class RedisLuaScript
        {
            private readonly ConnectionMultiplexer _redisClient;

            private readonly static SemaphoreSlim _loadLock = new SemaphoreSlim(1, 1);

            public RedisLuaScript(ConnectionMultiplexer redisClient, string name, string script)
            {
                _redisClient = redisClient;
                Name = name;
                Script = script;
            }

            public string Name { get; private set; }

            public string Script { get; private set; }

            public byte[] SHA1 { get; private set; }

            public async Task<byte[]> LoadAsync()
            {
                if (SHA1 == null)
                {
                    await _loadLock.WaitAsync();

                    try
                    {
                        if (SHA1 == null)
                        {
                            var sha1 = CalcLuaSha1(Script);

                            var endPoints = _redisClient.GetEndPoints();
                            foreach (var endpoint in endPoints)
                            {
                                var server = _redisClient.GetServer(endpoint);
                                if (server.IsConnected)
                                {
                                    bool exists = await server.ScriptExistsAsync(sha1);
                                    if (!exists)
                                    {
                                        await server.ScriptLoadAsync(Script);
                                    }
                                }
                            }

                            SHA1 = sha1;
                        }
                    }
                    finally
                    {
                        _loadLock.Release();
                    }
                }

                return SHA1;
            }

            public byte[] Load()
            {
                if (SHA1 == null)
                {
                    _loadLock.WaitAsync();
                    try
                    {
                        if (SHA1 == null)
                        {
                            var sha1 = CalcLuaSha1(Script);

                            var endPoints = _redisClient.GetEndPoints();
                            Array.ForEach(endPoints, endpoint =>
                            {
                                var server = _redisClient.GetServer(_redisClient.GetEndPoints()[0]);
                                if (server.IsConnected)
                                {
                                    if (!server.ScriptExists(sha1))
                                    {
                                        server.ScriptLoad(Script);
                                    }
                                }
                            });

                            SHA1 = sha1;
                        }
                    }
                    finally
                    {
                        _loadLock.Release();
                    }
                }

                return SHA1;
            }

            private byte[] CalcLuaSha1(string luaScript)
            {
                SHA1 sha1 = new SHA1CryptoServiceProvider();
                var bytesSha1In = Encoding.Default.GetBytes(luaScript);
                return sha1.ComputeHash(bytesSha1In);
            }
        }
    }
}
