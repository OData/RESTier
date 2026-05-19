// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Microsoft.Restier.Core.Query;
using Xunit;

namespace Microsoft.Restier.Tests.Core.Query
{
    /// <summary>
    /// Tests for <see cref="DefaultExpandCycleDetector"/>.
    ///
    /// EDM topology used by these tests:
    ///   Employee  (entity type)
    ///     Manager       : Employee     (single nav, self-referential)
    ///     Reports       : Employee[]   (collection nav, self-referential)
    ///     Department    : Department   (single nav)
    ///     Customer      : Customer     (single nav)
    ///   Department
    ///     Employees     : Employee[]   (collection nav — back to Employee)
    ///     Parent        : Department   (single nav, self-referential)
    ///     Location      : Address      (single nav — terminal, no further navs)
    ///     HeadManager   : Manager      (single nav — declared target is the
    ///                                   derived Manager type, used to exercise
    ///                                   the BaseEntityType() inheritance walk)
    ///   Manager : Employee             (derived type)
    ///   Customer (no nav back to Employee)
    ///   Address  (terminal — no navs)
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class DefaultExpandCycleDetectorTests
    {
        private readonly TestEdm edm = new();
        private readonly DefaultExpandCycleDetector detector = new();

        [Fact]
        public void NullClause_ReturnsFalse()
        {
            detector.HasCycle(edm.EmployeeType, null).Should().BeFalse();
        }

        [Fact]
        public void NoExpand_ReturnsFalse()
        {
            var clause = new SelectExpandClause(Array.Empty<SelectItem>(), allSelected: true);
            detector.HasCycle(edm.EmployeeType, clause).Should().BeFalse();
        }

        [Fact]
        public void NonRecursiveExpand_ReturnsFalse()
        {
            // /Employees?$expand=Department
            var clause = edm.Expand(edm.EmployeeType, "Department");
            detector.HasCycle(edm.EmployeeType, clause).Should().BeFalse();
        }

        [Fact]
        public void SelfCycleViaSingleNav_ReturnsTrue()
        {
            // /Employees?$expand=Manager  (Manager : Employee)
            var clause = edm.Expand(edm.EmployeeType, "Manager");
            detector.HasCycle(edm.EmployeeType, clause).Should().BeTrue();
        }

        [Fact]
        public void SelfCycleViaCollectionNav_ReturnsTrue()
        {
            // /Employees?$expand=Reports
            var clause = edm.Expand(edm.EmployeeType, "Reports");
            detector.HasCycle(edm.EmployeeType, clause).Should().BeTrue();
        }

        [Fact]
        public void CrossTypeCycle_ReturnsTrue()
        {
            // /Departments?$expand=Employees($expand=Department)
            var inner = edm.Expand(edm.EmployeeType, "Department");
            var clause = edm.Expand(edm.DepartmentType, "Employees", inner);
            detector.HasCycle(edm.DepartmentType, clause).Should().BeTrue();
        }

        [Fact]
        public void NestedNonCycle_ReturnsFalse()
        {
            // /Employees?$expand=Department($expand=Location)  — terminal Address, no cycle
            var inner = edm.Expand(edm.DepartmentType, "Location");
            var clause = edm.Expand(edm.EmployeeType, "Department", inner);
            detector.HasCycle(edm.EmployeeType, clause).Should().BeFalse();
        }

        [Fact]
        public void SiblingExpandsNoCycle_ReturnsFalse()
        {
            // /Employees?$expand=Department,Customer  (Customer has no nav back)
            var clause = edm.Expand(
                edm.EmployeeType,
                ("Department", null),
                ("Customer", null));
            detector.HasCycle(edm.EmployeeType, clause).Should().BeFalse();
        }

        [Fact]
        public void InheritanceCounts_DerivedTypeRevisitsBase_ReturnsTrue()
        {
            // /Departments?$expand=HeadManager($expand=Reports)
            // Path: [Department, Manager] -> target Employee.
            // Manager and Employee share an inheritance hierarchy (Manager : Employee),
            // so the algorithm should detect a cycle via the BaseEntityType() walk —
            // this is what type-equality alone wouldn't catch.
            var inner = edm.Expand(edm.ManagerType, "Reports");
            var clause = edm.Expand(edm.DepartmentType, "HeadManager", inner);
            detector.HasCycle(edm.DepartmentType, clause).Should().BeTrue();
        }

        [Fact]
        public void DeepCrossTypeCycle_ReturnsTrue()
        {
            // /Employees?$expand=Department($expand=Employees($expand=Department))
            var innermost = edm.Expand(edm.EmployeeType, "Department");
            var middle = edm.Expand(edm.DepartmentType, "Employees", innermost);
            var clause = edm.Expand(edm.EmployeeType, "Department", middle);
            detector.HasCycle(edm.EmployeeType, clause).Should().BeTrue();
        }
    }

    /// <summary>
    /// Hand-built EDM model exposing exactly the topology described in the test
    /// summary. Kept inside the test assembly so it can evolve with the tests.
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal sealed class TestEdm
    {
        public EdmModel Model { get; }
        public EdmEntityType EmployeeType { get; }
        public EdmEntityType ManagerType { get; }
        public EdmEntityType DepartmentType { get; }
        public EdmEntityType CustomerType { get; }
        public EdmEntityType AddressType { get; }
        public EdmEntityContainer Container { get; }

        public TestEdm()
        {
            Model = new EdmModel();

            EmployeeType = new EdmEntityType("Test", "Employee");
            EmployeeType.AddKeys(EmployeeType.AddStructuralProperty("Id", EdmPrimitiveTypeKind.Int32));

            DepartmentType = new EdmEntityType("Test", "Department");
            DepartmentType.AddKeys(DepartmentType.AddStructuralProperty("Id", EdmPrimitiveTypeKind.Int32));

            ManagerType = new EdmEntityType("Test", "Manager", EmployeeType);

            CustomerType = new EdmEntityType("Test", "Customer");
            CustomerType.AddKeys(CustomerType.AddStructuralProperty("Id", EdmPrimitiveTypeKind.Int32));

            AddressType = new EdmEntityType("Test", "Address");
            AddressType.AddKeys(AddressType.AddStructuralProperty("Id", EdmPrimitiveTypeKind.Int32));

            EmployeeType.AddUnidirectionalNavigation(new EdmNavigationPropertyInfo
            {
                Name = "Manager",
                Target = EmployeeType,
                TargetMultiplicity = EdmMultiplicity.ZeroOrOne,
            });
            EmployeeType.AddUnidirectionalNavigation(new EdmNavigationPropertyInfo
            {
                Name = "Reports",
                Target = EmployeeType,
                TargetMultiplicity = EdmMultiplicity.Many,
            });
            EmployeeType.AddUnidirectionalNavigation(new EdmNavigationPropertyInfo
            {
                Name = "Department",
                Target = DepartmentType,
                TargetMultiplicity = EdmMultiplicity.ZeroOrOne,
            });
            EmployeeType.AddUnidirectionalNavigation(new EdmNavigationPropertyInfo
            {
                Name = "Customer",
                Target = CustomerType,
                TargetMultiplicity = EdmMultiplicity.ZeroOrOne,
            });

            DepartmentType.AddUnidirectionalNavigation(new EdmNavigationPropertyInfo
            {
                Name = "Employees",
                Target = EmployeeType,
                TargetMultiplicity = EdmMultiplicity.Many,
            });
            DepartmentType.AddUnidirectionalNavigation(new EdmNavigationPropertyInfo
            {
                Name = "Parent",
                Target = DepartmentType,
                TargetMultiplicity = EdmMultiplicity.ZeroOrOne,
            });
            DepartmentType.AddUnidirectionalNavigation(new EdmNavigationPropertyInfo
            {
                Name = "Location",
                Target = AddressType,
                TargetMultiplicity = EdmMultiplicity.ZeroOrOne,
            });
            DepartmentType.AddUnidirectionalNavigation(new EdmNavigationPropertyInfo
            {
                Name = "HeadManager",
                Target = ManagerType,
                TargetMultiplicity = EdmMultiplicity.ZeroOrOne,
            });

            Model.AddElement(EmployeeType);
            Model.AddElement(ManagerType);
            Model.AddElement(DepartmentType);
            Model.AddElement(CustomerType);
            Model.AddElement(AddressType);

            Container = new EdmEntityContainer("Test", "Container");
            Container.AddEntitySet("Employees", EmployeeType);
            Container.AddEntitySet("Departments", DepartmentType);
            Container.AddEntitySet("Customers", CustomerType);
            Container.AddEntitySet("Addresses", AddressType);
            Model.AddElement(Container);
        }

        /// <summary>Build a single-level <c>$expand=navName</c> clause.</summary>
        public SelectExpandClause Expand(IEdmEntityType source, string navName, SelectExpandClause inner = null)
            => Expand(source, (navName, inner));

        /// <summary>Build a <c>$expand</c> clause with multiple sibling expansions.</summary>
        public SelectExpandClause Expand(IEdmEntityType source, params (string Nav, SelectExpandClause Inner)[] expansions)
        {
            var items = new List<SelectItem>(expansions.Length);
            var entitySet = Container.FindEntitySet(source.Name + "s") ?? Container.FindEntitySet("Employees");

            foreach (var (navName, innerClause) in expansions)
            {
                var nav = source.FindProperty(navName) as IEdmNavigationProperty
                    ?? throw new InvalidOperationException($"Navigation '{navName}' not found on {source.Name}.");
                var navSegment = new NavigationPropertySegment(nav, entitySet);
                var path = new ODataExpandPath(navSegment);
                items.Add(new ExpandedNavigationSelectItem(
                    path,
                    entitySet,
                    innerClause ?? new SelectExpandClause(Array.Empty<SelectItem>(), allSelected: true)));
            }

            return new SelectExpandClause(items, allSelected: true);
        }
    }
}
