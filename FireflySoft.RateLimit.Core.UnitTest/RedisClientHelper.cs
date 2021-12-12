public class RedisClientHelper{
    private static StackExchange.Redis.ConnectionMultiplexer _redisClient;
    public static StackExchange.Redis.ConnectionMultiplexer GetClient()
    {
        _redisClient = StackExchange.Redis.ConnectionMultiplexer.Connect("127.0.0.1");
        return _redisClient;
    }
}