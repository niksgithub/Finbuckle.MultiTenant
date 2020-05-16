﻿//    Copyright 2018-2020 Andrew White
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
using System.Linq;
using Microsoft.Extensions.Options;

namespace Finbuckle.MultiTenant.Options
{
    internal class MultiTenantOptionsSnapshotManager<TOptions, TTenantInfo> : OptionsManager<TOptions>
        where TOptions : class, new()
        where TTenantInfo : class, ITenantInfo, new()
    {
        private readonly MultiTenantOptionsFactory<TOptions, TTenantInfo> factory;
        private readonly TTenantInfo tenantInfo;
        private readonly MultiTenantOptionsCache<TOptions> cache = new MultiTenantOptionsCache<TOptions>(Enumerable.Empty<IOptionsChangeTokenSource<TOptions>>()); // Note: this is a private cache

        public MultiTenantOptionsSnapshotManager(MultiTenantOptionsFactory<TOptions, TTenantInfo> factory, TTenantInfo tenantInfo)
            : base(null)
        {
            this.factory = factory;
            this.tenantInfo = tenantInfo;
        }

        public override TOptions Get(string name)
        {
            name = name ?? Microsoft.Extensions.Options.Options.DefaultName;
            return cache.GetOrAdd(tenantInfo.Id, name, () => factory.Create(tenantInfo, name));
        }

        [Obsolete]
        public void Reset()
        {
            cache.Clear(tenantInfo.Id);
        }
    }
}