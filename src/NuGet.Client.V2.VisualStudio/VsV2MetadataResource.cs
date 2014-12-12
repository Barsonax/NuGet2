﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Client;
using NuGet.Client.VisualStudio.Models;
using NuGet.Client.V2;
using NuGet.Client.BaseTypes;
using NuGet.Versioning;
using System.ComponentModel.Composition;


namespace NuGet.Client.VisualStudio.Repository
{
    [Export(typeof(V2Resource))]
    public class VsV2MetadataResource : V2Resource,IVsMetadataResource
    {
        public VsV2MetadataResource():base(null,null)
        {

        }
        public VsV2MetadataResource(IPackageRepository repo,string host):base(repo,host)
        {

        }
        public override string Description
        {
            get { throw new NotImplementedException(); }
        }

        public VisualStudioUIPackageMetadata GetPackageMetadataForVisualStudioUI(string packageId, NuGetVersion version)
        {
            throw new NotImplementedException();
        }
    }
}
