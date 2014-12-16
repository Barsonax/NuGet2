﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.V2
{
    public class V2Utilities
    {
        public static bool IsV2(PackageSource source)
        {
            var url = new Uri(source.Url);
            if (url.IsFile || url.IsUnc)
            {
                return true;
            }

            using (var client = new Data.DataClient())
            {
                var result = client.GetFile(url);
                if (result == null)
                {
                    return false;
                }

                var raw = result.Result.Value<string>("raw");
                if (raw != null && raw.IndexOf("Packages", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    return true;
                }

                return false;
            }
        }

        public static IPackageRepository GetV2SourceRepository(PackageSource source, string host)
        {           
            IPackageRepository repo = new PackageRepositoryFactory().CreateRepository(source.Url);
            LocalPackageRepository _lprepo = repo as LocalPackageRepository;
            if (_lprepo != null)
                return _lprepo;
            string _userAgent = UserAgentUtil.GetUserAgent("NuGet.Client.Interop", host);
            var events = repo as IHttpClientEvents;
            if (events != null)
            {
                events.SendingRequest += (sender, args) =>
                {
                    var httpReq = args.Request as HttpWebRequest;
                    if (httpReq != null)
                    {
                        httpReq.UserAgent = _userAgent;
                    }
                };               
            }
            return repo;
        }

    }
}
