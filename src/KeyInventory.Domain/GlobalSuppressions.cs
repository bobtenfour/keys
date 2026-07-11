using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Naming",
    "CA1711:Identifiers should not have incorrect suffix",
    Justification = "Permission is a contract-required RBAC entity name.",
    Scope = "type",
    Target = "~T:KeyInventory.Domain.Identity.Permission")]

[assembly: SuppressMessage(
    "Naming",
    "CA1711:Identifiers should not have incorrect suffix",
    Justification = "RolePermission is a contract-required RBAC entity name.",
    Scope = "type",
    Target = "~T:KeyInventory.Domain.Identity.RolePermission")]
