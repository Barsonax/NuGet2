﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Newtonsoft.Json.Linq;
using NuGet.Client.Resolution;
using System.Diagnostics;
using Resx = NuGet.Client.VisualStudio.UI.Resources;

namespace NuGet.Client.VisualStudio.UI
{
    /// <summary>
    /// Interaction logic for PackageDetail.xaml
    /// </summary>
    public partial class PackageDetailControl : UserControl
    {
        public PackageManagerControl Control { get; set; }

        public PackageDetailControl()
        {
            InitializeComponent();
            this.DataContextChanged += PackageDetailControl_DataContextChanged;
        }

        private void PackageDetailControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is PackageDetailControlModel)
            {
                _root.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                _root.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void Versions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var model = (PackageDetailControlModel)DataContext;
            if (model == null)
            {
                return;
            }

            var v = (VersionForDisplay)_versions.SelectedItem;
            model.SelectVersion(v == null ? null : v.Version);
            UpdateInstallUninstallButton();
        }

        private async void Preview(PackageActionType action)
        {
            try
            {
                var actions = await ResolveActions(action);

                PreviewActions(actions);
            }
            catch (InvalidOperationException ex)
            {
                // TODO: Is this the only reason for this exception???
                MessageBox.Show("Temporary Message! Clean this up!" + Environment.NewLine + ex.Message, "Temporary Message");
            }
        }

        private async Task<IEnumerable<PackageAction>> ResolveActions(PackageActionType action)
        {
            var packageDetail = (PackageDetailControlModel)DataContext;

            Control.SetBusy(true);
            try
            {
                // Create a resolver
                var resolver = new ActionResolver(
                    Control.Sources.ActiveRepository,
                    Control.Target,
                    new ResolutionContext()
                    {
                        DependencyBehavior = packageDetail.SelectedDependencyBehavior.Behavior,
                        AllowPrerelease = false
                    });

                // Resolve actions
                return await resolver.ResolveActionsAsync(packageDetail.Package.Id, packageDetail.Package.Version, action);
            }
            finally
            {
                Control.SetBusy(false);
            }
        }

        private enum PackageStatus
        {
            Unchanged,
            Deleted,
            Added
        }

        private void PreviewActions(
            IEnumerable<PackageAction> actions)
        {
            MessageBox.Show("TODO: Better UI." + Environment.NewLine + String.Join(Environment.NewLine, actions.Select(a => a.ToString())));

            /* !!!
            // Show preview result
            var packageStatus = Control.Target
                .Installed
                .GetInstalledPackageReferences()
                .Select(p => p.Identity)
                .ToDictionary(p => p, _ => PackageStatus.Unchanged);

            foreach (var action in actions)
            {
                if (action.ActionType == PackageActionType.Install)
                {
                    packageStatus[action.PackageName] = PackageStatus.Added;
                }
                else if (action.ActionType == PackageActionType.Uninstall)
                {
                    packageStatus[action.PackageName] = PackageStatus.Deleted;
                }
            }

            var model = new PreviewWindowModel(
                unchanged: packageStatus
                    .Where(v => v.Value == PackageStatus.Unchanged)
                    .Select(v => v.Key),
                deleted: packageStatus
                    .Where(v => v.Value == PackageStatus.Deleted)
                    .Select(v => v.Key),
                added: packageStatus
                    .Where(v => v.Value == PackageStatus.Added)
                    .Select(v => v.Key));

            var w = new PreviewWindow();
            w.DataContext = model;
            w.Owner = Window.GetWindow(Control);
            w.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            w.ShowDialog(); */
        }

        private async void PerformPackageAction(PackageActionType action)
        {
            IEnumerable<PackageAction> actions;
            try
            {
                actions = await ResolveActions(action);
            }
            catch (Exception ex)
            {
                MessageBox.Show("TODO: Show better error." + Environment.NewLine + ex.Message);
                return;
            }

            // show license agreeement
            bool acceptLicense = ShowLicenseAgreement(actions);
            if (!acceptLicense)
            {
                return;
            }

            // This should only be called in cases where there is a single target
            Debug.Assert(Control.Target.TargetProjects.Count() == 1, "PackageDetailControl should only be used when there is only one target project!");
            Debug.Assert(Control.Target is ProjectInstallationTarget, "PackageDetailControl should only be used when there is only one target project!");
            
            // Create the execution context
            var context = new ExecutionContext((ProjectInstallationTarget)Control.Target);

            // Create the executor and execute the actions
            Control.SetBusy(true);
            try
            {
                var executor = new ActionExecutor();

                // TODO: This method takes a logger! Use that to restore the install progress box.
                await executor.ExecuteActionsAsync(actions, context);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                throw;
            }
            finally
            {
                Control.SetBusy(false);
            }

            Control.UpdatePackageStatus();
            UpdatePackageStatus();
        }

        private void UpdateInstallUninstallButton()
        {
            var model = (PackageDetailControlModel)DataContext;
            if (model == null)
            {
                return;
            }

            // This should only be called in cases where there is a single target
            Debug.Assert(Control.Target.TargetProjects.Count() == 1, "PackageDetailControl should only be used when there is only one target project!");

            var isInstalled = Control.Target.TargetProjects.Single().InstalledPackages.IsInstalled(model.Package.Id, model.Package.Version);
            if (isInstalled)
            {
                _dropdownButton.SetItems(
                    new[] { Resx.Resources.Button_Uninstall, Resx.Resources.Button_UninstallPreview });
            }
            else
            {
                _dropdownButton.SetItems(
                    new[] { Resx.Resources.Button_Install, Resx.Resources.Button_InstallPreview });
            }
        }

        private void UpdatePackageStatus()
        {
            var model = (PackageDetailControlModel)DataContext;
            if (model == null)
            {
                return;
            }

            // This should only be called in cases where there is a single target
            Debug.Assert(Control.Target.TargetProjects.Count() == 1, "PackageDetailControl should only be used when there is only one target project!");

            UpdateInstallUninstallButton();
            var installedPackage = Control.Target.TargetProjects.Single().InstalledPackages.GetInstalledPackage(model.Package.Id);
            if (installedPackage != null)
            {
                var installedVersion = installedPackage.Identity.Version;
                model.CreateVersions(installedVersion);
            }
        }

        protected bool ShowLicenseAgreement(IEnumerable<PackageAction> operations)
        {
            var licensePackages = operations.Where(op =>
                op.ActionType == PackageActionType.Install &&
                op.Package.Value<bool>("requireLicenseAcceptance"));

            // display license window if necessary
            if (licensePackages.Any())
            {
                // Hacky distinct without writing a custom comparer
                var licenseModels = licensePackages
                    .GroupBy(a => Tuple.Create(a.Package["id"], a.Package["version"]))
                    .Select(g =>
                    {
                        dynamic p = g.First().Package;
                        var authors = String.Join(", ",
                            ((JArray)(p.authors)).Cast<JValue>()
                            .Select(author => author.Value as string));

                        return new PackageLicenseInfo(
                            p.id.Value,
                            p.licenseUrl.Value,
                            authors);
                    });

                bool accepted = Control.UI.PromptForLicenseAcceptance(licenseModels);
                if (!accepted)
                {
                    return false;
                }
            }

            return true;
        }

        private void ExecuteOpenLicenseLink(object sender, ExecutedRoutedEventArgs e)
        {
            Hyperlink hyperlink = e.OriginalSource as Hyperlink;
            if (hyperlink != null && hyperlink.NavigateUri != null)
            {
                Control.UI.LaunchExternalLink(hyperlink.NavigateUri);
                e.Handled = true;
            }
        }

        private void _dropdownButton_Clicked(object sender, DropdownButtonClickEventArgs e)
        {
            if (e.ButtonText == Resx.Resources.Button_Install)
            {
                PerformPackageAction(PackageActionType.Install);
            }
            else if (e.ButtonText == Resx.Resources.Button_InstallPreview)
            {
                Preview(PackageActionType.Install);
            }
            else if (e.ButtonText == Resx.Resources.Button_Uninstall)
            {
                PerformPackageAction(PackageActionType.Uninstall);
            }
            else if (e.ButtonText == Resx.Resources.Button_UninstallPreview)
            {
                Preview(PackageActionType.Uninstall);
            }
        }
    }
}