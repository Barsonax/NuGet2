﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace NuGet.Client.Interop
{
    public static class PackageJsonLd
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static JObject CreatePackageSearchResult(IPackage package, IEnumerable<IPackage> versions, bool hasAdditionalVersions)
        {
            var value = new JObject();
            AddProp(value, Properties.Type, Types.PackageSearchResult);
            AddProp(value, Properties.Id, package.Id);
            AddProp(value, Properties.LatestVersion, package.Version.ToString());
            AddProp(value, Properties.Summary, package.Summary);
            AddProp(value, Properties.IconUrl, package.IconUrl);


            var versionList = new JObject();
            AddProp(versionList, Properties.Type, Types.PackageVersionList);
            AddProp(versionList, Properties.HasAdditionalVersions, hasAdditionalVersions);
            AddProp(versionList, Properties.Packages, versions.Select(v => CreatePackage(v)));
            AddProp(value, Properties.PackageVersionList, versionList);

            return value;
        }

        public static JObject CreatePackage(IPackage version)
        {
            var value = new JObject();
            AddProp(value, Properties.Type, Types.PackageVersion);

            AddProp(value, Properties.Id, version.Id);
            AddProp(value, Properties.Version, version.Version.ToString());
            AddProp(value, Properties.Summary, version.Summary);
            AddProp(value, Properties.Description, version.Description);
            AddProp(value, Properties.Authors, version.Authors);
            AddProp(value, Properties.Owners, version.Owners);
            AddProp(value, Properties.IconUrl, version.IconUrl);
            AddProp(value, Properties.LicenseUrl, version.LicenseUrl);
            AddProp(value, Properties.ProjectUrl, version.ProjectUrl);
            AddProp(value, Properties.Tags, (version.Tags ?? String.Empty).Split(' '));
            AddProp(value, Properties.DownloadCount, version.DownloadCount);
            AddProp(value, Properties.Published, version.Published.HasValue ? version.Published.Value.ToString("O", CultureInfo.InvariantCulture) : null);
            AddProp(value, Properties.RequireLicenseAcceptance, version.RequireLicenseAcceptance);
            AddProp(value, Properties.DependencyGroups, version.DependencySets.Select(set => CreateDependencyGroup(set)));

            var dsPackage = version as DataServicePackage;
            if (dsPackage != null)
            {
                AddProp(value, Properties.NupkgUrl, dsPackage.DownloadUrl);
            }

            return value;
        }

        public static JObject CreateDependencyGroup(PackageDependencySet set)
        {
            var value = new JObject();
            AddProp(value, Properties.Type, Types.DependencyGroup);
            AddProp(value, Properties.TargetFramework, set.TargetFramework == null ? null : set.TargetFramework.FullName);
            AddProp(value, Properties.Dependencies, set.Dependencies.Select(d => CreateDependency(d)));
            return value;
        }

        public static JObject CreateDependency(PackageDependency dependency)
        {
            var value = new JObject();
            AddProp(value, Properties.Type, Types.Dependency);
            AddProp(value, Properties.Id, dependency.Id);
            AddProp(value, Properties.Range, dependency.VersionSpec == null ? null : dependency.VersionSpec.ToString());
            return value;
        }

        public static PackageDependency DependencyFromJson(JObject dependency)
        {
            return new PackageDependency(
                dependency.Value<string>(Properties.Id),
                VersionUtility.ParseVersionSpec(dependency.Value<string>(Properties.Range)));
        }

        public static PackageDependencySet DependencySetFromJson(JObject dependencySet)
        {
            var deps = dependencySet.Value<JArray>(Properties.Dependencies);
            IEnumerable<PackageDependency> depEnum;
            if (deps == null)
            {
                depEnum = Enumerable.Empty<PackageDependency>();
            }
            else
            {
                depEnum = deps.Select(t => DependencyFromJson((JObject)t));
            }

            string fxName = dependencySet.Value<string>(Properties.TargetFramework);

            return new PackageDependencySet(
                String.IsNullOrEmpty(fxName) ? null : new FrameworkName(fxName),
                depEnum);
        }

        public static IPackage PackageFromJson(JObject json)
        {
            return new CoreInteropPackage(json);
        }

        private static void AddProp(JObject obj, string property, JArray content)
        {
            if (content != null && content.Count != 0)
            {
                obj.Add(new JProperty(property, content));
            }
        }

        private static void AddProp(JObject obj, string property, JToken content)
        {
            if (content != null)
            {
                obj.Add(new JProperty(property, content));
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private static void AddProp(JObject obj, string property, object content)
        {
            if (content != null)
            {
                obj.Add(new JProperty(property, content.ToString()));
            }
        }

        // Without this override, a 'string' content parameter falls in to the IEnumerable<T> overload
        // below (T == char), so don't remove it even though it seems useless!
        private static void AddProp(JObject obj, string property, string content)
        {
            if (!String.IsNullOrEmpty(content))
            {
                obj.Add(new JProperty(property, content));
            }
        }

        private static void AddProp<T>(JObject obj, string property, IEnumerable<T> content)
        {
            if (content != null && content.Any())
            {
                obj.Add(new JProperty(property,
                    new JArray(content)));
            }
        }
    }
}
