# FireflySoft.RateLimit
A .net rate limit class library, support .net framework and .net core.

## 功能特点
*不仅可用于WebAPI限流，亦可应用于任何需要业务限流的场景。
*提供四种限流算法：固定窗口、滑动窗口、漏桶、令牌桶。
*支持分布式服务限流。
*支持触发限流后锁定一段时间。
*支持灵活的限流目标设定和规则匹配。
*支持扩展自己的算法和持久化方式。
*同时支持.net framework和.net core。

## 项目说明
|项目|说明|
|FireflySoft.RateLmit.Core|限流算法、规则、持久化等核心程序。
|FireflySoft.RateLmit.Core.Sample|使用FireflySoft.RateLmit的演示程序，可快速迁移到自己的项目中。
|FireflySoft.RateLimit.AspNet|基于.NET Framework的ASP.NET限流中间件。
|FireflySoft.RateLimit.AspNet.Sample|使用FireflySoft.RateLimit.AspNet的演示程序。
|FireflySoft.RateLimit.AspNetCore|基于.NET Core的ASP.NET Core限流中间件。
|FireflySoft.RateLimit.AspNetCore.Sample|使用FireflySoft.RateLimit.AspNetCore的演示程序。






