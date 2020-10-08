using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using StackExchange.Redis;

namespace FireflySoft.RateLimit.Core
{
    /// <summary>
    /// Redis存储
    /// </summary>
    public class RedisStorage : IRateLimitStorage
    {
        /// <summary>
        /// Redis客户端
        /// </summary>
        private readonly ConnectionMultiplexer _redisClient;

        /// <summary>
        /// 服务端已加载的Lua脚本
        /// </summary>
        private readonly ConcurrentDictionary<string, Lazy<byte[]>> _loadedLuaScriptsOnServer = new ConcurrentDictionary<string, Lazy<byte[]>>();

        /// <summary>
        /// 初始化Redis存储的一个新实例
        /// </summary>
        /// <param name="redisClient"></param>
        public RedisStorage(ConnectionMultiplexer redisClient)
        {
            _redisClient = redisClient;
        }

        public bool CheckIsLocked(string target)
        {
            if (Get(target) == "1")
            {
                return true;
            }

            return false;
        }

        public long Increment(string target, long amount, TimeSpan expireTimeSpan)
        {
            return Increment(target, amount, (int)expireTimeSpan.TotalSeconds);
        }

        public void Lock(string target, TimeSpan expireTimeSpan)
        {
            Set(target, "1", expireTimeSpan);
        }

        /// <summary>
        /// 范型数据的新增或修改
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="value">value</param>
        /// <param name="expireTimeSpan">过期时间</param>
        /// <returns>true or false</returns>
        private bool Set(string key, string value, TimeSpan expireTimeSpan)
        {
            IDatabase database = _redisClient.GetDatabase();
            return database.StringSet(key, value, expireTimeSpan);
        }

        /// <summary>
        /// 范型数据的查询
        /// </summary>
        /// <param name="key"></param>
        /// <returns>value</returns>
        private string Get(string key)
        {
            IDatabase database = _redisClient.GetDatabase();
            return database.StringGet(key);
        }

        /// <summary>
        /// 数值增加指定的数量，并在创建时设置过期秒数
        /// </summary>
        /// <param name="key">要操作数据的Key</param>
        /// <param name="amount">要增加的数量</param>
        /// <param name="expireSeconds">kv创建时设置的过期秒数</param>
        /// <returns>增加后的数值</returns>
        private long Increment(string key, long amount, int expireSeconds)
        {
            string luaScript = @"local amount=tonumber(ARGV[1])
                                local current
                                current = redis.call('incrby',KEYS[1],amount)
                                current = tonumber(current)
                                if current == amount then
                                    redis.call('EXPIRE',KEYS[1], ARGV[2])
                                end
                                return current";

            return (int)EvaluateScript("Src-IncrWithExpireSec", luaScript, new RedisKey[] { key },
                new RedisValue[] { amount, expireSeconds });

        }

        /// <summary>
        /// 执行指定lua脚本
        /// </summary>
        /// <param name="scriptName"></param>
        /// <param name="luaScript"></param>
        /// <param name="keys"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public RedisResult EvaluateScript(string scriptName, string luaScript, RedisKey[] keys, RedisValue[] values)
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
