﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Client.Diagnostics;

namespace NuGet.Client.Interop
{
    internal class CoreInteropProjectManager : IProjectManager
    {
        private readonly SourceRepository _source;
        private readonly InstallationTarget _target;
        private readonly CoreInteropSharedRepository _sharedRepo;
        private readonly CoreInteropPackageManager _pacman;
        private readonly CoreInteropSourceRepository _sourceRepo;

        public IPackageRepository LocalRepository
        {
            get { return _sharedRepo; }
        }

        public IPackageManager PackageManager
        {
            get { return _pacman; }
        }

        public IProjectSystem Project
        {
            get { return _target.ProjectSystem; }
        }

        public IPackageConstraintProvider ConstraintProvider
        {
            get
            {
                return new CoreInteropConstraintProvider(_target.Installed);
            }
            set
            {
                NuGetTraceSources.CoreInterop.Error("setconstraintprovider", "Someone tried to set the constraint provider for an interop Project Manager! It won't work!!");
            }
        }

        public CoreInteropProjectManager(InstallationTarget target, SourceRepository activeSource)
        {
            _target = target;
            _source = activeSource;

            _sharedRepo = new CoreInteropSharedRepository(target);
            _sourceRepo = new CoreInteropSourceRepository(activeSource);
            _pacman = new CoreInteropPackageManager(_sharedRepo, _sourceRepo);
        }

        #region Unimplemented stuff.

        public ILogger Logger
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public void Execute(PackageOperation operation)
        {
            throw new NotImplementedException();
        }

        // Suppress 'The event ... is never used' warning
#pragma warning disable 0067
        public event EventHandler<PackageOperationEventArgs> PackageReferenceAdded;

        public event EventHandler<PackageOperationEventArgs> PackageReferenceAdding;

        public event EventHandler<PackageOperationEventArgs> PackageReferenceRemoved;

        public event EventHandler<PackageOperationEventArgs> PackageReferenceRemoving;
#pragma warning restore 0067
        #endregion
    }
}
