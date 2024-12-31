using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AutoGuru.KeyValuePush.Tests
{
    public class DefaultDictionaryBuilderTests
    {
        private readonly DefaultDictionaryBuilder _builder;

        public DefaultDictionaryBuilderTests()
        {
            _builder = new DefaultDictionaryBuilder();
        }

        [Fact]
        public void TryAdd_ShouldAddKeyValuePair_WhenKeyDoesNotExist()
        {
            var dict = new Dictionary<string, string>();
            var key = "key1";
            var value = "value1";

            DefaultDictionaryBuilder.TryAdd(dict, key, value);

            Assert.Single(dict);
            Assert.Equal(value, dict[key]);
        }

        [Fact]
        public void TryAdd_ShouldNotThrow_WhenKeyExistsWithSameValue()
        {
            var dict = new Dictionary<string, string> { { "key1", "value1" } };
            var key = "key1";
            var value = "value1";

            DefaultDictionaryBuilder.TryAdd(dict, key, value);

            Assert.Single(dict);
            Assert.Equal(value, dict[key]);
        }

        [Fact]
        public void TryAdd_ShouldThrowException_WhenKeyExistsWithDifferentValue()
        {
            var dict = new Dictionary<string, string> { { "key1", "value1" } };
            var key = "key1";
            var value = "differentValue";

            var exception = Assert.Throws<Exception>(() => DefaultDictionaryBuilder.TryAdd(dict, key, value));
            Assert.Equal("Duplicate key of 'key1' with a different value detected.", exception.Message);
        }

        [Fact]
        public async Task BuildAsync_ShouldThrowException_WhenFileAccessFails()
        {
            var path = "RestrictedFiles";
            Directory.CreateDirectory(path);
            var filePath = Path.Combine(path, "file1.txt");
            File.WriteAllText(filePath, "Content1");

            File.SetAttributes(filePath, FileAttributes.ReadOnly);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _builder.BuildAsync(path, "*.txt", SearchOption.TopDirectoryOnly, false, CancellationToken.None));

            File.SetAttributes(filePath, FileAttributes.Normal);
            Directory.Delete(path, true);
        }

        [Fact]
        public async Task BuildAsync_ShouldRespectCancellationToken()
        {
            var path = "TestFiles";
            Directory.CreateDirectory(path);
            File.WriteAllText(Path.Combine(path, "file1.txt"), "Content1");

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                _builder.BuildAsync(path, "*.txt", SearchOption.TopDirectoryOnly, false, cts.Token));

            Directory.Delete(path, true);
        }

        [Fact]
        public async Task BuildAsync_ShouldHandleDuplicateKeysInJsonFiles()
        {
            var path = "TestFiles";
            Directory.CreateDirectory(path);
            var jsonContent = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                { "Key1", "Value1" },
                { "Key1", "Value2" } // Duplicate key
            });
            File.WriteAllText(Path.Combine(path, "file1.json"), jsonContent);

            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _builder.BuildAsync(path, "*.json", SearchOption.TopDirectoryOnly, true, CancellationToken.None));

            Assert.Contains("Duplicate key of 'Key1'", exception.Message);
            Directory.Delete(path, true);
        }

        [Fact]
        public async Task BuildAsync_ShouldIgnoreFilesWithNonJsonExtensions()
        {
            var path = "TestFiles";
            Directory.CreateDirectory(path);
            File.WriteAllText(Path.Combine(path, "file1.unsupported"), "Unsupported Content");

            var result = await _builder.BuildAsync(path, "*.*", SearchOption.TopDirectoryOnly, false, CancellationToken.None);

            Assert.Empty(result);
            Directory.Delete(path, true);
        }

        [Fact]
        public async Task BuildAsync_ShouldCombineJsonAndTextFiles()
        {
            var path = "TestFiles";
            Directory.CreateDirectory(path);
            var jsonContent = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                { "Key1", "Value1" },
                { "Key2", "Value2" }
            });
            File.WriteAllText(Path.Combine(path, "file1.json"), jsonContent);
            File.WriteAllText(Path.Combine(path, "file2.txt"), "TextContent");

            var result = await _builder.BuildAsync(path, "*.*", SearchOption.TopDirectoryOnly, true, CancellationToken.None);

            Assert.Equal(3, result.Count);
            Assert.Equal("Value1", result["Key1"]);
            Assert.Equal("Value2", result["Key2"]);
            Assert.Equal("TextContent", result["file2"]);
            Directory.Delete(path, true);
        }
    }
}
