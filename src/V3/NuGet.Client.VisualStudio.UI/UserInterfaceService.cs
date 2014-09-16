﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Windows.Threading;
using System.Windows;

namespace NuGet.Client.VisualStudio.UI
{
    [Export(typeof(IUserInterfaceService))]
    public class UserInterfaceService : IUserInterfaceService
    {
        private readonly Dispatcher _uiDispatcher;

        public UserInterfaceService()
        {
            _uiDispatcher = Dispatcher.CurrentDispatcher;
        }

        public bool PromptForLicenseAcceptance(IEnumerable<PackageLicenseInfo> packages)
        {
            if (_uiDispatcher.CheckAccess())
            {
                return PromptForLicenseAcceptanceImpl(packages);
            }
            else
            {
                // Use Invoke() here to block the worker thread
                object result = _uiDispatcher.Invoke(
                    new Func<IEnumerable<PackageLicenseInfo>, bool>(PromptForLicenseAcceptanceImpl), packages);
                return (bool)result;
            }
        }

        private bool PromptForLicenseAcceptanceImpl(IEnumerable<PackageLicenseInfo> packages)
        {
            MessageBox.Show("TODO: Fix license dialog");
            return true;
            //var licenseWindow = new LicenseAcceptanceWindow()
            //{
            //    DataContext = packages
            //};

            ///* !!!
            //using (NuGetEventTrigger.Instance.TriggerEventBeginEnd(
            //    NuGetEvent.LicenseWindowBegin,
            //    NuGetEvent.LicenseWindowEnd))
            //{ */
            //bool? dialogResult = licenseWindow.ShowDialog();
            //return dialogResult ?? false;
        }

        public void LaunchExternalLink(Uri url)
        {
            throw new NotImplementedException();
        }


        public void LaunchNuGetOptionsDialog()
        {
            System.Windows.MessageBox.Show("Not implemented yet!!!");
        }
    }
}
