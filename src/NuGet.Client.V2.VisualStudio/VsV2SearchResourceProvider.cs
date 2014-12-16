﻿using NuGet.Client.V2;
using NuGet.Client.VisualStudio.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.V2.VisualStudio
{
    [Export(typeof(IResourceProvider))]
    [ResourceProviderMetadata("VsV2SearchResourceProvider", typeof(IVsSearch))]
    public class VsV2SearchResourceProvider : IResourceProvider
    {
        public bool TryCreateResource(PackageSource source, ref IDictionary<string, object> cache, out Resource resource)
        {
            try
            {
                string host = "TestHost";
                if (V2Utilities.IsV2(source))
                {
                    object repo = null;
                    if (!cache.TryGetValue(source.Url, out repo))
                    {
                        repo = V2Utilities.GetV2SourceRepository(source, host);
                        cache.Add(source.Url, repo);
                    }
                    resource = new VsV2SearchResource((IPackageRepository)repo, host);
                    return true;
                }
                else
                {
                    resource = null;
                    return false;
                }
            }
            catch (Exception)
            {
                resource = null;
                return false; //*TODOs:Do tracing and throw apppropriate exception here.
            }       
        }

        public Resource Create(PackageSource source, ref IDictionary<string, object> cache)
        {
            throw new NotImplementedException();
        }
    }
}
