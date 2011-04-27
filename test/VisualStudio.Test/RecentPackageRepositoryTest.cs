﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet.Test;

namespace NuGet.VisualStudio.Test {

    using PackageUtility = NuGet.Test.PackageUtility;

    [TestClass]
    public class RecentPackageRepositoryTest {

        [TestMethod]
        public void RemovePackageMethodThrow() {
            // Arrange
            var repository = CreateRecentPackageRepository();

            // Act & Assert
            ExceptionAssert.Throws<NotSupportedException>(() => repository.RemovePackage(null));
        }

        [TestMethod]
        public void TestGetPackagesReturnNoPackageIfThereIsNoPackageMetadata() {
            // Arrange
            var repository = CreateRecentPackageRepository(null, new IPersistencePackageMetadata[0]);

            // Act
            var packages = repository.GetPackages().ToList();

            // Assert
            Assert.AreEqual(0, packages.Count);
        }

        [TestMethod]
        public void TestGetPackagesReturnCorrectNumberOfPackages() {
            // Scenario: The remote repository contains package A and C
            // Recent settings store contains metadata for A
            // Calling GetPackages() should return package A.

            // Arrange
            var repository = CreateRecentPackageRepository(null, new[] { new PersistencePackageMetadata("A", "1.0") });

            // Act
            var packages = repository.GetPackages().ToList();

            // Assert
            Assert.AreEqual(1, packages.Count);
            Assert.AreEqual("A", packages[0].Id);
            Assert.AreEqual(new Version("1.0"), packages[0].Version);
        }

        [TestMethod]
        public void TestGetPackagesReturnNothingAfterCallingClear() {
            // Arrange
            var repository = CreateRecentPackageRepository();

            // Act
            repository.Clear();

            // Assert
            var packages = repository.GetPackages();
            Assert.IsFalse(packages.Any());
        }

        [TestMethod]
        public void TestGetPackagesReturnPackagesSortedByDateByDefault() {
            // Scenario: The remote repository contains package A, B and C
            // Recent settings store contains metadata for A and B
            // Calling GetPackages() should return package A and B, sorted by date (B goes before A)

            // Arrange
            var packageA = PackageUtility.CreatePackage("A", "1.0");
            var packageB = PackageUtility.CreatePackage("B", "2.0");
            var packageC = PackageUtility.CreatePackage("C", "2.0");

            var packagesList = new[] { packageA, packageB, packageC };
            var repository = CreateRecentPackageRepository(packagesList);

            // Act
            var packages = repository.GetPackages().ToList();

            // Assert
            Assert.AreEqual(2, packages.Count);
            AssertPackage(packages[0], "B", "2.0");
            AssertPackage(packages[1], "A", "1.0");
        }

        [TestMethod]
        public void TestGetPackagesReturnCorrectNumberOfPackagesAfterAddingPackage() {
            // Scenario: The remote repository contains package A and C
            // Recent settings store contains metadata for A and B
            // Calling AddPackage(packageC)
            // Now GetPackages() should return A and C

            // Arrange
            var repository = CreateRecentPackageRepository();
            var packageC = PackageUtility.CreatePackage("C", "2.0");
            var recentPackageC = packageC;

            repository.AddPackage(recentPackageC);

            // Act
            var packages = repository.GetPackages().ToList();

            // Assert
            Assert.AreEqual(2, packages.Count);
            AssertPackage(packages[0], "C", "2.0");
            AssertPackage(packages[1], "A", "1.0");
        }

        [TestMethod]
        public void TestGetPackagesReturnCorrectNumberOfPackagesAfterAddingPackageThatAlreadyExists() {
            // Scenario: The remote repository contains package A and B
            // Recent settings store contains metadata for A and B
            // Calling AddPackage(packageA)
            // Now GetPackages() should return A and B in that order

            // Arrange
            var packageA = PackageUtility.CreatePackage("A", "1.0");
            var packageB = PackageUtility.CreatePackage("B", "2.0");

            var packagesList = new[] { packageA, packageB };

            var repository = CreateRecentPackageRepository(packagesList: packagesList);

            // Assert
            var packages = repository.GetPackages().ToList();
            Assert.AreEqual(2, packages.Count);
            AssertPackage(packages[0], "B", "2.0");
            AssertPackage(packages[1], "A", "1.0");

            // Act
            var newPackage = PackageUtility.CreatePackage("A", "1.0");
            repository.AddPackage(newPackage);

            // Assert
            packages = repository.GetPackages().ToList();
            Assert.AreEqual(2, packages.Count);
            AssertPackage(packages[0], "A", "1.0");
            AssertPackage(packages[1], "B", "2.0");
        }

        [TestMethod]
        public void GetPackagesReturnCorrectPackagesAfterAddingManyPackages() {
            // Arrange
            var repository = CreateRecentPackageRepository();

            var package1 = PackageUtility.CreatePackage("B", "1.0");
            var package2 = PackageUtility.CreatePackage("A", "1.0");
            var package3 = PackageUtility.CreatePackage("C", "2.0");
            var package4 = PackageUtility.CreatePackage("B", "2.0");
            var package5 = PackageUtility.CreatePackage("A", "1.0");
            var package6 = PackageUtility.CreatePackage("C", "2.0");

            // Act
            repository.AddPackage(package1);
            repository.AddPackage(package2);
            repository.AddPackage(package3);
            repository.AddPackage(package4);
            repository.AddPackage(package5);
            repository.AddPackage(package6);

            // Assert
            var packages = repository.GetPackages().ToList();
            Assert.AreEqual(3, packages.Count);

            AssertPackage(packages[0], "C", "2.0");
            AssertPackage(packages[1], "A", "1.0");
            AssertPackage(packages[2], "B", "2.0");
        }

        [TestMethod]
        public void RecentPackageRepositoryStoresLatestPackageVersions() {
            // Arrange
            var repository = CreateRecentPackageRepository();

            var packageA1 = PackageUtility.CreatePackage("A", "1.0");
            var packageB1 = PackageUtility.CreatePackage("B", "1.0");
            var packageA2 = PackageUtility.CreatePackage("A", "2.0");
            var packageB2 = PackageUtility.CreatePackage("B", "2.0");
            var packageB3 = PackageUtility.CreatePackage("B", "3.0");

            // Act
            repository.AddPackage(packageA1);
            repository.AddPackage(packageB1);
            repository.AddPackage(packageB3);
            repository.AddPackage(packageA2);
            repository.AddPackage(packageB2);

            // Assert
            var packages = repository.GetPackages().ToList();
            Assert.AreEqual(2, packages.Count);

            AssertPackage(packages[0], "B", "3.0");
            AssertPackage(packages[1], "A", "2.0");
        }

        [TestMethod]
        public void RecentPackageRepositoryUsesLatestVersionFromStore() {
            // Arrange
            var repository = CreateRecentPackageRepository();

            var packageA1 = PackageUtility.CreatePackage("A", "0.5");
            var packageB1 = PackageUtility.CreatePackage("B", "1.0");
            var packageA2 = PackageUtility.CreatePackage("A", "0.6");
            var packageB2 = PackageUtility.CreatePackage("B", "2.0");
            var packageB3 = PackageUtility.CreatePackage("B", "3.0");


            // Act
            repository.AddPackage(packageA1);
            repository.AddPackage(packageB1);
            repository.AddPackage(packageB3);
            repository.AddPackage(packageA2);
            repository.AddPackage(packageB2);

            // Assert
            var packages = repository.GetPackages().ToList();
            Assert.AreEqual(2, packages.Count);

            AssertPackage(packages[0], "B", "3.0");
            AssertPackage(packages[1], "A", "1.0");
        }

        [TestMethod]
        public void RecentPackageRepositoryCollapsesVersionsInStore() {
            // Arrange
            var storePackages = new[] {
                new PersistencePackageMetadata("A", "1.0", new DateTime(2037, 01, 01)),
                new PersistencePackageMetadata("C", "2.0", new DateTime(2011, 01, 01)),
                new PersistencePackageMetadata("A", "2.5", new DateTime(2011, 01, 01)),
                new PersistencePackageMetadata("C", "1.7", new DateTime(2010, 01, 01)),
                new PersistencePackageMetadata("C", "1.9", new DateTime(2011, 02, 01)),
            };

            var remotePackages = storePackages.Select(c => PackageUtility.CreatePackage(c.Id, c.Version.ToString()));
            var repository = CreateRecentPackageRepository(packagesList: remotePackages, settingsMetadata: storePackages);

            // Act and Assert
            var packages = repository.GetPackages().OfType<RecentPackage>().ToList();
            Assert.AreEqual(2, packages.Count);

            AssertPackage(packages[0], "A", "2.5");
            Assert.AreEqual(packages[0].LastUsedDate, new DateTime(2037, 01, 01));
            AssertPackage(packages[1], "C", "2.0");
            Assert.AreEqual(packages[1].LastUsedDate, new DateTime(2011, 02, 01));
        }

        [TestMethod]
        public void CallingClearMethodClearsAllPackagesFromSettingsStore() {
            // Arrange
            var repository = CreateRecentPackageRepository();

            // Act
            repository.Clear();
            var packages = repository.GetPackages().ToList();

            // Assert
            Assert.AreEqual(0, packages.Count);
        }

        private RecentPackagesRepository CreateRecentPackageRepository(IEnumerable<IPackage> packagesList = null, IEnumerable<IPersistencePackageMetadata> settingsMetadata = null) {
            if (packagesList == null) {
                var packageA = PackageUtility.CreatePackage("A", "1.0");
                var packageC = PackageUtility.CreatePackage("C", "2.0");

                packagesList = new[] { packageA, packageC };
            }

            var mockRepository = new Mock<IPackageRepository>();
            mockRepository.Setup(p => p.GetPackages()).Returns(packagesList.AsQueryable());

            var mockRepositoryFactory = new Mock<IPackageRepositoryFactory>();
            mockRepositoryFactory.Setup(f => f.CreateRepository(It.IsAny<string>())).Returns(mockRepository.Object);

            var mockSettingsManager = new MockSettingsManager();

            if (settingsMetadata == null) {
                var A = new PersistencePackageMetadata("A", "1.0", new DateTime(2010, 8, 12));
                var B = new PersistencePackageMetadata("B", "2.0", new DateTime(2011, 3, 2));
                settingsMetadata = new[] { A, B };
            }

            mockSettingsManager.SavePackageMetadata(settingsMetadata);

            var mockPackageSourceProvider = new MockPackageSourceProvider();
            mockPackageSourceProvider.SavePackageSources(new[] {new PackageSource("source")});
            return new RecentPackagesRepository(null, mockRepositoryFactory.Object, mockPackageSourceProvider, mockSettingsManager);
        }

        private void AssertPackage(IPackage package, string expectedId, string expectedVersion) {
            Assert.AreEqual(expectedId, package.Id);
            Assert.AreEqual(new Version(expectedVersion), package.Version);
        }

        private class MockSettingsManager : IPersistencePackageSettingsManager {

            List<IPersistencePackageMetadata> _items = new List<IPersistencePackageMetadata>();

            public System.Collections.Generic.IEnumerable<IPersistencePackageMetadata> LoadPackageMetadata(int maximumCount) {
                return _items.Take(maximumCount);
            }

            public void SavePackageMetadata(System.Collections.Generic.IEnumerable<IPersistencePackageMetadata> packageMetadata) {
                _items.Clear();
                _items.AddRange(packageMetadata);
            }

            public void ClearPackageMetadata() {
                _items.Clear();
            }
        }
    }
}