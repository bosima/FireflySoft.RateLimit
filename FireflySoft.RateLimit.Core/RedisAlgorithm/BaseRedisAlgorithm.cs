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
        /// Create a new instance
        /// </summary>
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
                    byte[] sha1 = await luaScript.LoadAsync();
                    IDatabase dataBase = _redisClient.GetDatabase();
                    return await dataBase.ScriptEvaluateAsync(sha1, keys, values);
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
            private readonly ConnectionMultiplexer _redisClient;

            private readonly static SemaphoreSlim _loadLock = new SemaphoreSlim(1, 1);

            /// <summary>
            /// Create a new instace
            /// </summary>
            /// <param name="redisClient"></param>
            /// <param name="name"></param>
            /// <param name="script"></param>
            public RedisLuaScript(ConnectionMultiplexer redisClient, string name, string script)
            {
                _redisClient = redisClient;
                Name = name;
                Script = script;
            }

            private RedisLuaScript(string name, string script)
            {
                this.Name = name;
                this.Script = script;
            }

            /// <summary>
            /// The name
            /// </summary>
            /// <value></value>
            public string Name { get; private set; }

            /// <summary>
            /// The script content
            /// </summary>
            /// <value></value>
            public string Script { get; private set; }

            /// <summary>
            /// The script SHA1 
            /// </summary>
            /// <value></value>
            public byte[] SHA1 { get; private set; }

            /// <summary>
            /// Async load script in the redis server
            /// </summary>
            /// <returns></returns>
            public async System.Threading.Tasks.Task<byte[]> LoadAsync()
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

            /// <summary>
            /// Load script in the redis server
            /// </summary>
            /// <returns></returns>
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
                                var server = _redisClient.GetServer(endpoint);
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

            /// <summary>
            /// Reset the load status of the script, forcing it to load again next time.
            /// </summary>
            internal void ResetLoadStatus()
            {
                SHA1 = null;
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