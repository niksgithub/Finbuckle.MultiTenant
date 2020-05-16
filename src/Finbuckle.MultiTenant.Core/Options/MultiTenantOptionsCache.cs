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
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Finbuckle.MultiTenant.Options
{
    public class MultiTenantOptionsCache<TOptions> : IOptionsMonitorCache<TOptions>
        where TOptions : class, new()
    {
        private readonly ConcurrentDictionary<string, IOptionsMonitorCache<TOptions>> map = new ConcurrentDictionary<string, IOptionsMonitorCache<TOptions>>();
        private readonly IOptionsMonitorCache<TOptions> noTenantCache = new OptionsCache<TOptions>();
        private readonly IEnumerable<IOptionsChangeTokenSource<TOptions>> sources;
        private readonly List<IDisposable> registrations;
        private event Action<string> onChange;

        public MultiTenantOptionsCache(IEnumerable<IOptionsChangeTokenSource<TOptions>> sources)
        {
            this.sources = sources;

            foreach (var source in sources)
            {
                var registration = ChangeToken.OnChange(() => source.GetChangeToken(),
                                                        (name) => TryRemove(name ?? Microsoft.Extensions.Options.Options.DefaultName),
                                                        source.Name);

                registrations.Add(registration);
            }
        }

        public void Clear(string tenantId)
        {
            if (tenantId == null)
                noTenantCache.Clear();
            else
            {
                var cache = map.GetOrAdd(tenantId, new OptionsCache<TOptions>());
                cache.Clear();
            }
        }

        public void Clear()
        {
            noTenantCache.Clear();
            foreach (var cache in map.Values)
                cache.Clear();
        }

        public TOptions GetOrAdd(string tenantId, string name, Func<TOptions> createOptions)
        {
            if (createOptions == null)
            {
                throw new ArgumentNullException(nameof(createOptions));
            }

            name = name ?? Microsoft.Extensions.Options.Options.DefaultName;

            IOptionsMonitorCache<TOptions> cache;
            if(tenantId == null)
                cache = noTenantCache;
            else
                cache = map.GetOrAdd(tenantId, new OptionsCache<TOptions>());

            return cache.GetOrAdd(name, createOptions);
        }

        public TOptions GetOrAdd(string name, Func<TOptions> createOptions)
        {
            throw new NotImplementedException();
        }

        public bool TryAdd(string tenantId, string name, TOptions options)
        {
            name = name ?? Microsoft.Extensions.Options.Options.DefaultName;

            IOptionsMonitorCache<TOptions> cache;
            if(tenantId == null)
                cache = noTenantCache;
            else
                cache = map.GetOrAdd(tenantId, new OptionsCache<TOptions>());

            return cache.TryAdd(name, options);
        }

        public bool TryAdd(string name, TOptions options)
        {
            throw new NotImplementedException();
        }

        public bool TryRemove(string tenantId, string name)
        {
            IOptionsMonitorCache<TOptions> cache;
            if(tenantId == null)
                cache = noTenantCache;
            else
                cache = map.GetOrAdd(tenantId, new OptionsCache<TOptions>());

            return cache.TryRemove(name);
        }

        public bool TryRemove(string name)
        {
            var result = true;

            if(!noTenantCache.TryRemove(name))
                result = false;

            foreach (var cache in map.Values)
                if (!cache.TryRemove(name))
                    result = false;

            return result;
        }
    }
}