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

//    Portions of this file are derived from the .NET Foundation source file located at:
//    https://github.com/aspnet/Options/blob/dev/src/Microsoft.Extensions.Options/OptionsFactory.cs

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace Finbuckle.MultiTenant.Options
{
    internal class MultiTenantOptionsFactory<TOptions, TTenantInfo>
        where TOptions : class, new()
        where TTenantInfo : class, ITenantInfo, new()
    {
        private readonly IEnumerable<IConfigureOptions<TOptions>> configures;
        private readonly Action<TOptions, TTenantInfo> tenantConfig;
        private readonly IEnumerable<IPostConfigureOptions<TOptions>> postConfigures;

        /// <summary>
        /// Initializes a new instance with the specified options configurations.
        /// </summary>
        /// <param name="setups">The configuration actions to run.</param>
        /// <param name="postConfigures">The initialization actions to run.</param>
        public MultiTenantOptionsFactory(IEnumerable<IConfigureOptions<TOptions>> setups, IEnumerable<IPostConfigureOptions<TOptions>> postConfigures, Action<TOptions, TTenantInfo> tenantConfig)
        {
            configures = setups;
            this.tenantConfig = tenantConfig;
            this.postConfigures = postConfigures;
        }

        public TOptions Create(TTenantInfo tenantInfo, string name)
        {
            var options = new TOptions();
            foreach (var setup in configures)
            {
                if (setup is IConfigureNamedOptions<TOptions> namedSetup)
                {
                    namedSetup.Configure(name, options);
                }
                else if (name == Microsoft.Extensions.Options.Options.DefaultName)
                {
                    setup.Configure(options);
                }
            }

            if(tenantInfo != null)
                tenantConfig(options, tenantInfo);

            foreach (var post in postConfigures)
            {
                post.PostConfigure(name, options);
            }
            return options;
        }
    }
}
