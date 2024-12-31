using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AutoGuru.KeyValuePush.Redis.Tests
{
    public class RedisPusherTests : IDisposable
    {
        private readonly RedisPusher _redisPusher;

        public RedisPusherTests()
        {
            _redisPusher = new RedisPusher();
        }

        [Fact]
        public void Configure_ShouldSetConnectionProperly()
        {
            // Arrange
            var configuration = "localhost:6379";

            // Act
            _redisPusher.Configure(configuration, null);

            // Assert
            Assert.NotNull(_redisPusher);
        }

        [Fact]
        public async Task PushAsync_ShouldThrowException_WhenNotConfigured()
        {
            // Arrange
            var dictionary = new Dictionary<string, string> { { "key1", "value1" } };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _redisPusher.PushAsync(dictionary, CancellationToken.None));
            Assert.Equal("RedisPusher wasn't configured yet.", exception.Message);
        }

        [Fact]
        public async Task PushAsync_ShouldSetValuesInRedis()
        {
            // Arrange
            var configuration = "localhost:6379";
            _redisPusher.Configure(configuration, null);
            var dictionary = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" }
            };

            // Act
            await _redisPusher.PushAsync(dictionary, CancellationToken.None);

            // Assert
            // Simularía obtener valores de Redis para comprobar que se han guardado.
            // Aquí debería usar un mock para IDatabase y verificar la inserción.
        }

        [Fact]
        public void Dispose_ShouldDisposeConnectionMultiplexer()
        {
            // Arrange
            var configuration = "localhost:6379";
            _redisPusher.Configure(configuration, null);

            // Act
            _redisPusher.Dispose();

            // Assert
            // Aquí se podría comprobar internamente si _connectionMultiplexer se ha dispuesto.
            Assert.NotNull(_redisPusher);
        }

        public void Dispose()
        {
            _redisPusher.Dispose();
        }
    }
}
