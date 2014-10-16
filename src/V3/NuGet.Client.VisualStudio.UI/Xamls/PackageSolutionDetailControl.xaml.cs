using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using NuGet.Client.Resolution;
using Resx = NuGet.Client.VisualStudio.UI.Resources;
using NuGet.Client.Installation;
using NuGet.Client.ProjectSystem;
using System.Diagnostics;
using System.Threading;

namespace NuGet.Client.VisualStudio.UI
{
    public partial class PackageSolutionDetailControl : UserControl
    {
        public PackageManagerControl Control { get; set; }
        
        private Solution Solution
        {
            get
            {
                var solution = Control.Target as Solution;
                Debug.Assert(solution != null, "Expected that the target would be a solution!");
                return solution;
            }
        }

        public PackageSolutionDetailControl()
        {
            InitializeComponent();
            this.DataContextChanged += PackageSolutionDetailControl_DataContextChanged;
        }

        private void PackageSolutionDetailControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is PackageSolutionDetailControlModel)
            {
                _root.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                _root.Visibility = System.Windows.Visibility.Collapsed;
            }
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

        private async Task<IEnumerable<PackageAction>> ResolveActions()
        {
            var model = (PackageSolutionDetailControlModel)DataContext;
            var repo = Control.CreateActiveRepository();
            if (repo == null)
            {
                throw new InvalidOperationException(Resx.Resources.Error_NoActiveRepository);
            }
            var resolver = new ActionResolver(
                repo,
                new ResolutionContext()
                {
                    DependencyBehavior = model.SelectedDependencyBehavior.Behavior,
                    AllowPrerelease = false
                });

            var action = model.SelectedAction == Resx.Resources.Action_Uninstall ?
                PackageActionType.Uninstall :
                PackageActionType.Install;

            var targetProjects = model.Projects
                .Where(p => p.Selected)
                .Select(p => p.Project);
            return await resolver.ResolveActionsAsync(
                new PackageIdentity(model.Package.Id, model.SelectedVersion.Version),
                action,
                targetProjects,
                Solution);
        }

        private async void ActionButtonClicked(object sender, RoutedEventArgs e)
        {
            var model = (PackageSolutionDetailControlModel)DataContext;

            Control.SetBusy(true);
            var progressDialog = new ProgressDialog(
                model.SelectedFileConflictAction.Action);
            try
            {
                IEnumerable<PackageAction> actions = await ResolveActions();
                
                // show license agreeement
                bool acceptLicense = Control.ShowLicenseAgreement(actions);
                if (!acceptLicense)
                {
                    return;
                }

                var executor = new ActionExecutor();
                progressDialog.Owner = Window.GetWindow(Control);
                progressDialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                progressDialog.Show();

                await executor.ExecuteActionsAsync(actions, logger: progressDialog, cancelToken: CancellationToken.None);

                Control.UpdatePackageStatus();
                model.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    Window.GetWindow(Control),
                    ex.Message,
                    Resx.Resources.WindowTitle_Error,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                progressDialog.RequestToClose();
                Control.SetBusy(false);
            }
        }

        private async void PreviewButtonClicked(object sender, RoutedEventArgs e)
        {
            var model = (PackageSolutionDetailControlModel)DataContext;

            Control.SetBusy(true);
            var progressDialog = new ProgressDialog(
                model.SelectedFileConflictAction.Action);
            try
            {
                IEnumerable<PackageAction> actions = await ResolveActions();
                Control.PreviewActions(actions);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                progressDialog.RequestToClose();
                Control.SetBusy(false);
            }
        }
    }
}