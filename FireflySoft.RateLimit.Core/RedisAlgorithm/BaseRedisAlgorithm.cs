using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FireflySoft.RateLimit.Core.Rule;
using FireflySoft.RateLimit.Core.Time;
using StackExchange.Redis;

namespace FireflySoft.RateLimit.Core.RedisAlgorithm
{
    /// <summary>
    /// The base class for redis algorithm
    /// </summary>
    public abstract class BaseRedisAlgorithm : BaseAlgorithm
    {
        /// <summary>
        /// current redis client
        /// </summary>
        protected readonly ConnectionMultiplexer _redisClient;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rules"></param>
        /// <param name="redisClient">An instance of StackExchange.Redis.ConnectionMultiplexer.</param>
        /// <param name="timeProvider"></param>
        /// <param name="updatable">Used to identify whether the rule can be updated.</param>
        public BaseRedisAlgorithm(IEnumerable<RateLimitRule> rules, ConnectionMultiplexer redisClient = null, ITimeProvider timeProvider = null, bool updatable = false)
        : base(rules, timeProvider, updatable)
        {
            _redisClient = redisClient;
            if (_redisClient == null)
            {
                _redisClient = StackExchange.Redis.ConnectionMultiplexer.Connect("127.0.0.1");
            }
        }

        /// <summary>
        /// async evaluate lua script
        /// </summary>
        /// <param name="luaScript"></param>
        /// <param name="keys"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        protected async Task<RedisResult> EvaluateScriptAsync(RedisLuaScript luaScript, RedisKey[] keys, RedisValue[] values)
        {
            RedisServerException _lastException = null;

            for (int attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    byte[] sha1 = await luaScript.LoadAsync().ConfigureAwait(false);
                    IDatabase dataBase = _redisClient.GetDatabase();
                    return await dataBase.ScriptEvaluateAsync(sha1, keys, values).ConfigureAwait(false);
                }
                catch (RedisServerException exception)
                {
                    _lastException = exception;

                    // If the database gets reset, the script can end up cleared. This will force it to be reloaded
                    if (exception.Message.Contains("NOSCRIPT"))
                    {
                        luaScript.ResetLoadStatus();
                        continue;
                    }
                }
            }

            throw _lastException;
        }

        /// <summary>
        /// evaluate lua script
        /// </summary>
        /// <param name="luaScript"></param>
        /// <param name="keys"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        protected RedisResult EvaluateScript(RedisLuaScript luaScript, RedisKey[] keys, RedisValue[] values)
        {
            RedisServerException _lastException = null;

            for (int attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    byte[] sha1 = luaScript.Load();
                    IDatabase dataBase = _redisClient.GetDatabase();
                    return dataBase.ScriptEvaluate(sha1, keys, values);
                }
                catch (RedisServerException exception)
                {
                    _lastException = exception;

                    // If the database gets reset, the script can end up cleared. This will force it to be reloaded
                    if (exception.Message.Contains("NOSCRIPT"))
                    {
                        luaScript.ResetLoadStatus();
                        continue;
                    }
                }
            }

            throw _lastException;
        }

        /// <summary>
        /// Define the operation mechanism for Lua script
        /// </summary>
        protected class RedisLuaScript
        {
            private byte[] _sha1;
            private readonly ConnectionMultiplexer _redisClient;
            private readonly static SemaphoreSlim _loadLock = new SemaphoreSlim(1, 1);
            private DateTimeOffset _reloadTs;
            private readonly object _reloadLocker = new object();

            /// <summary>
            /// Create a new instace
            /// </summary>
            /// <param name="redisClient"></param>
            /// <param name="name"></param>
            /// <param name="script"></param>
            public RedisLuaScript(ConnectionMultiplexer redisClient, string name, string script)
            {
                _redisClient = redisClient;
                _reloadTs = DateTimeOffset.MinValue;
                Name = name;
                Script = script;
            }

            /// <summary>
            /// The name of script
            /// </summary>
            /// <value></value>
            public string Name { get; private set; }

            /// <summary>
            /// The content of script
            /// </summary>
            /// <value></value>
            public string Script { get; private set; }

            /// <summary>
            /// Async load script in the redis server
            /// </summary>
            /// <returns></returns>
            public async Task<byte[]> LoadAsync()
            {
                if (_sha1 == null)
                {
                    await _loadLock.WaitAsync().ConfigureAwait(false);

                    try
                    {
                        if (_sha1 == null)
                        {
                            var tmpSHA1 = CalcLuaSHA1();

                            var endPoints = _redisClient.GetEndPoints();
                            foreach (var endpoint in endPoints)
                            {
                                var server = _redisClient.GetServer(endpoint);
                                if (server.IsConnected)
                                {
                                    bool exists = await server.ScriptExistsAsync(tmpSHA1).ConfigureAwait(false);
                                    if (!exists)
                                    {
                                        await server.ScriptLoadAsync(Script).ConfigureAwait(false);
                                    }
                                }
                            }

                            // When reset load status, other threads may change '_sha1' to null, so 'tmpSHA1' is returned
                            _sha1 = tmpSHA1;
                            return tmpSHA1;
                        }
                    }
                    finally
                    {
                        _loadLock.Release();
                    }
                }

                return _sha1;
            }

            /// <summary>
            /// Load script in the redis server
            /// </summary>
            /// <returns></returns>
            public byte[] Load()
            {
                if (_sha1 == null)
                {
                    _loadLock.Wait();
                    try
                    {
                        if (_sha1 == null)
                        {
                            var tmpSHA1 = CalcLuaSHA1();

                            var endPoints = _redisClient.GetEndPoints();
                            Array.ForEach(endPoints, endpoint =>
                            {
                                var server = _redisClient.GetServer(endpoint);
                                if (server.IsConnected)
                                {
                                    if (!server.ScriptExists(tmpSHA1))
                                    {
                                        server.ScriptLoad(Script);
                                    }
                                }
                            });

                            // When reset load status, other threads may change '_sha1' to null, so 'tmpSHA1' is returned
                            _sha1 = tmpSHA1;
                            return tmpSHA1;
                        }
                    }
                    finally
                    {
                        _loadLock.Release();
                    }
                }

                return _sha1;
            }

            /// <summary>
            /// Reset the load status of the script, forcing it to load again next time.
            /// </summary>
            internal void ResetLoadStatus()
            {
                lock (_reloadLocker)
                {
                    var now = DateTimeOffset.Now;
                    if (now.Subtract(_reloadTs).TotalMilliseconds > 1000)
                    {
                        _sha1 = null;
                        _reloadTs = now;
                    }
                }
            }

            private byte[] CalcLuaSHA1()
            {
                using (SHA1 sha1Service = new SHA1CryptoServiceProvider())
                {
                    return sha1Service.ComputeHash(Encoding.Default.GetBytes(Script));
                }
            }
        }
    }
}