//    Copyright 2018-2020 Andrew White
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//        http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Core;
using Finbuckle.MultiTenant.Options;
using Microsoft.Extensions.Options;
using Xunit;

public class MultiTenantOptionsCacheShould
{
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("name")]
    public void AddNamedOptionsForTenantIdOnlyOnAdd(string name)
    {
        var ti = new TenantInfo { Id = "test-id-123" };
        var cache = new MultiTenantOptionsCache<TestOptions>(Enumerable.Empty<IOptionsChangeTokenSource<TestOptions>>());

        var options = new TestOptions();

        // Add new options.
        var result = cache.TryAdd(ti.Id, name, options);
        Assert.True(result);

        // Fail adding options under same name.
        result = cache.TryAdd(ti.Id, name, options);
        Assert.False(result);

        // Change the tenant id and confirm options can be added again.
        ti.Id = "diff_id";
        result = cache.TryAdd(ti.Id, name, options);
        Assert.True(result);
    }

    [Fact]
    public void HandleNullTenantIdOnAdd()
    {
        var cache = new MultiTenantOptionsCache<TestOptions>(Enumerable.Empty<IOptionsChangeTokenSource<TestOptions>>());
        var options = new TestOptions();

        var result = cache.TryAdd(null, "", options);

        Assert.True(result);
    }

    [Fact]
    public void ThrowOnDefaultAdd()
    {
        var cache = new MultiTenantOptionsCache<TestOptions>(Enumerable.Empty<IOptionsChangeTokenSource<TestOptions>>());
        var options = new TestOptions();
        Assert.Throws<NotImplementedException>(() => cache.TryAdd("", options));
    }

    [Fact]
    public void HandleNullTenantIdOnGetOrAdd()
    {
        var cache = new MultiTenantOptionsCache<TestOptions>(Enumerable.Empty<IOptionsChangeTokenSource<TestOptions>>());
        var options = new TestOptions();

        var result = cache.GetOrAdd(null, "", () => options);
        Assert.NotNull(result);
    }

    [Fact]
    public void ThrowOnDefaultGetOrAdd()
    {
        var cache = new MultiTenantOptionsCache<TestOptions>(Enumerable.Empty<IOptionsChangeTokenSource<TestOptions>>());
        var options = new TestOptions();
        Assert.Throws<NotImplementedException>(() => cache.GetOrAdd("", () => options));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("name")]
    public void GetOrAddNamedOptionForTenantIdOnly(string name)
    {
        var ti = new TenantInfo { Id = "test-id-123"};
        var cache = new MultiTenantOptionsCache<TestOptions>(Enumerable.Empty<IOptionsChangeTokenSource<TestOptions>>());
        var options = new TestOptions();
        var options2 = new TestOptions();

        // Add new options.
        var result = cache.GetOrAdd(ti.Id, name, () => options);
        Assert.Same(options, result);

        // Get the existing options if exists.
        result = cache.GetOrAdd(ti.Id, name, () => options2);
        Assert.NotSame(options2, result);

        // Confirm different tenant on same object is an add (ie it didn't exist there).
        ti.Id = "diff_id";
        result = cache.GetOrAdd(ti.Id, name, () => options2);
        Assert.Same(options2, result);
    }

    [Fact]
    public void ThrowsIfGetOrAddFactoryIsNull()
    {
        var cache = new MultiTenantOptionsCache<TestOptions>(Enumerable.Empty<IOptionsChangeTokenSource<TestOptions>>());

        Assert.Throws<ArgumentNullException>(() => cache.GetOrAdd(null, null, null));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("name")]
    public void RemoveForAllOnDefaultRemove(string name)
    {
        var ti = new TenantInfo { Id = "test-id-123" };
        var cache = new MultiTenantOptionsCache<TestOptions>(Enumerable.Empty<IOptionsChangeTokenSource<TestOptions>>());
        var options = new TestOptions();

        // Add new options.
        var result = cache.TryAdd(ti.Id, name, options);
        Assert.True(result);

        // Add under a different tenant.
        ti.Id = "diff_id";
        result = cache.TryAdd(ti.Id, name, options);
        Assert.True(result);
        result = cache.TryAdd(ti.Id, "diffname", options);
        Assert.True(result);

        // Add under no tenant.
        result = cache.TryAdd(null, name, options);
        Assert.True(result);
        result = cache.TryAdd(null, "diffname", options);
        Assert.True(result);

        // Remove named options
        result = cache.TryRemove(name);
        Assert.True(result);
        
        var tenantCache = (ConcurrentDictionary<string, IOptionsMonitorCache<TestOptions>>)cache.GetType().
            GetField("map", BindingFlags.NonPublic | BindingFlags.Instance).
            GetValue(cache);

        dynamic tenantInternalCache = tenantCache[ti.Id].GetType().GetField("_cache", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(tenantCache[ti.Id]);

        // Assert named options removed and other options on tenant left as-is.
        Assert.False(tenantInternalCache.Keys.Contains(name ?? ""));
        Assert.True(tenantInternalCache.Keys.Contains("diffname"));

        // Assert other tenant also affected.
        ti.Id = "test-id-123";
        tenantInternalCache = tenantCache[ti.Id].GetType().GetField("_cache", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(tenantCache[ti.Id]);
        Assert.False(tenantInternalCache.Keys.Contains(name ?? ""));

        // Assert no tenant also affected
        var noTenantCache = cache.GetType()
                                 .GetField("noTenantCache", BindingFlags.NonPublic | BindingFlags.Instance)
                                 .GetValue(cache);
        dynamic internalCache = noTenantCache.GetType()
                                             .GetField("_cache", BindingFlags.NonPublic | BindingFlags.Instance)
                                             .GetValue(noTenantCache);
        Assert.False(internalCache.Keys.Contains(name ?? ""));
        Assert.True(internalCache.Keys.Contains("diffname"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("name")]
    public void HandleNullTenantIdForRemove(string name)
    {
        var ti = new TenantInfo { Id = "test-id-123" };
        var cache = new MultiTenantOptionsCache<TestOptions>(Enumerable.Empty<IOptionsChangeTokenSource<TestOptions>>());
        var options = new TestOptions();

        // Add new options.
        var result = cache.TryAdd(ti.Id, name, options);
        Assert.True(result);

        // Add under a different tenant.
        ti.Id = "diff_id";
        result = cache.TryAdd(ti.Id, name, options);
        Assert.True(result);
        result = cache.TryAdd(ti.Id, "diffname", options);
        Assert.True(result);

        // Add under no tenant.
        result = cache.TryAdd(null, name, options);
        Assert.True(result);
        result = cache.TryAdd(null, "diffname", options);
        Assert.True(result);

        // Remove named options for no tenant.
        result = cache.TryRemove(null, name);
        Assert.True(result);
        var tenantCache = (ConcurrentDictionary<string, IOptionsMonitorCache<TestOptions>>)cache.GetType().
            GetField("map", BindingFlags.NonPublic | BindingFlags.Instance).
            GetValue(cache);

        dynamic tenantInternalCache = tenantCache[ti.Id].GetType().GetField("_cache", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(tenantCache[ti.Id]);

        // Assert named options removed and other options on tenant left as-is.
        Assert.True(tenantInternalCache.Keys.Contains(name ?? ""));
        Assert.True(tenantInternalCache.Keys.Contains("diffname"));

        // Assert other tenant not affected.
        ti.Id = "test-id-123";
        tenantInternalCache = tenantCache[ti.Id].GetType().GetField("_cache", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(tenantCache[ti.Id]);
        Assert.True(tenantInternalCache.ContainsKey(name ?? ""));

        // Assert no tenant option removed
        var noTenantCache = cache.GetType()
                                 .GetField("noTenantCache", BindingFlags.NonPublic | BindingFlags.Instance)
                                 .GetValue(cache);
        dynamic internalCache = noTenantCache.GetType()
                                             .GetField("_cache", BindingFlags.NonPublic | BindingFlags.Instance)
                                             .GetValue(noTenantCache);
        Assert.False(internalCache.Keys.Contains(name ?? ""));
        Assert.True(internalCache.Keys.Contains("diffname"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("name")]
    public void RemoveNamedOptionsForTenantIdOnly(string name)
    {
        var ti = new TenantInfo { Id = "test-id-123" };
        var cache = new MultiTenantOptionsCache<TestOptions>(Enumerable.Empty<IOptionsChangeTokenSource<TestOptions>>());
        var options = new TestOptions();

        // Add new options.
        var result = cache.TryAdd(ti.Id, name, options);
        Assert.True(result);

        // Add under a different tenant.
        ti.Id = "diff_id";
        result = cache.TryAdd(ti.Id, name, options);
        Assert.True(result);
        result = cache.TryAdd(ti.Id, "diffname", options);
        Assert.True(result);

        // Remove named options for current tenant.
        result = cache.TryRemove(ti.Id,name);
        Assert.True(result);
        var tenantCache = (ConcurrentDictionary<string, IOptionsMonitorCache<TestOptions>>)cache.GetType().
            GetField("map", BindingFlags.NonPublic | BindingFlags.Instance).
            GetValue(cache);

        dynamic tenantInternalCache = tenantCache[ti.Id].GetType().GetField("_cache", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(tenantCache[ti.Id]);

        // Assert named options removed and other options on tenant left as-is.
        Assert.False(tenantInternalCache.Keys.Contains(name ?? ""));
        Assert.True(tenantInternalCache.Keys.Contains("diffname"));

        // Assert other tenant not affected.
        ti.Id = "test-id-123";
        tenantInternalCache = tenantCache[ti.Id].GetType().GetField("_cache", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(tenantCache[ti.Id]);
        Assert.True(tenantInternalCache.ContainsKey(name ?? ""));
    }

    [Fact]
    public void ClearOptionsForTenantIdOnly()
    {
        var ti = new TenantInfo { Id = "test-id-123" };
        var cache = new MultiTenantOptionsCache<TestOptions>(Enumerable.Empty<IOptionsChangeTokenSource<TestOptions>>());

        var options = new TestOptions();

        // Add new options.
        var result = cache.TryAdd(ti.Id, "", options);
        Assert.True(result);

        // Add under a different tenant.
        ti.Id = "diff_id";
        result = cache.TryAdd(ti.Id, "", options);
        Assert.True(result);

        // Clear options on first tenant.
        cache.Clear("test-id-123");

        // Assert options cleared on this tenant.
        var tenantCache = (ConcurrentDictionary<string, IOptionsMonitorCache<TestOptions>>)cache.GetType().
            GetField("map", BindingFlags.NonPublic | BindingFlags.Instance).
            GetValue(cache);

        dynamic tenantInternalCache = tenantCache[ti.Id].GetType().GetField("_cache", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(tenantCache["test-id-123"]);
        Assert.True(tenantInternalCache.IsEmpty);

        // Assert options still exist on other tenant.
        tenantInternalCache = tenantCache["diff_id"].GetType().GetField("_cache", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(tenantCache["diff_id"]);
        Assert.False(tenantInternalCache.IsEmpty);
    }

    [Fact]
    public void HandleNullTenantIdOnClear()
    {
        var cache = new MultiTenantOptionsCache<TestOptions>(Enumerable.Empty<IOptionsChangeTokenSource<TestOptions>>());
        var options = new TestOptions();
        cache.TryAdd(null, null, options);

        cache.Clear(null);

        var result = cache.GetOrAdd(null, null, () => new TestOptions());
        Assert.NotSame(result, options);
    }

    [Fact]
    public void ClearAllOptionsForClear()
    {
        var ti = new TenantInfo { Id = "test-id-123" };
        var cache = new MultiTenantOptionsCache<TestOptions>(Enumerable.Empty<IOptionsChangeTokenSource<TestOptions>>());
        var options = new TestOptions();

        // Add new options.
        var result = cache.TryAdd(ti.Id, null, options);
        Assert.True(result);

        // Add under a different tenant.
        ti.Id = "diff_id";
        result = cache.TryAdd(ti.Id, null, options);
        Assert.True(result);

        // Add under null tenant.
        result = cache.TryAdd(null, null, options);
        Assert.True(result);

        // Clear all options.
        cache.Clear();

        var tenantCaches = (ConcurrentDictionary<string, IOptionsMonitorCache<TestOptions>>)cache.GetType().
            GetField("map", BindingFlags.NonPublic | BindingFlags.Instance).
            GetValue(cache);

        // Assert options cleared on this tenant.
        dynamic tenantInternalCache = tenantCaches[ti.Id].GetType().GetField("_cache", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(tenantCaches[ti.Id]);
        Assert.True(tenantInternalCache.IsEmpty);

        // Assert options cleared on other tenant.
        ti.Id = "diff_id";
        tenantInternalCache = tenantCaches[ti.Id].GetType().GetField("_cache", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(tenantCaches[ti.Id]);
        Assert.True(tenantInternalCache.IsEmpty);

        // Assert cleared for null tenant
        var noTenantCache = cache.GetType()
                                     .GetField("noTenantCache", BindingFlags.NonPublic | BindingFlags.Instance)
                                     .GetValue(cache);
        dynamic internalCache = noTenantCache.GetType()
                                             .GetField("_cache", BindingFlags.NonPublic | BindingFlags.Instance)
                                             .GetValue(noTenantCache);
        Assert.True(internalCache.IsEmpty);
    }
}