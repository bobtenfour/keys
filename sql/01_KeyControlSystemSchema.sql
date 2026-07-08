USE [KeyControlSystemDev];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE TABLE dbo.Organization (
    OrganizationId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Organization PRIMARY KEY,
    OrganizationCode nvarchar(50) NOT NULL,
    OrganizationName nvarchar(200) NOT NULL,
    IsActive bit NOT NULL CONSTRAINT DF_Organization_IsActive DEFAULT (1),
    CreatedUtc datetime2(7) NOT NULL CONSTRAINT DF_Organization_CreatedUtc DEFAULT (sysutcdatetime()),
    CONSTRAINT UQ_Organization_Code UNIQUE (OrganizationCode)
);
GO

CREATE TABLE dbo.RiskLevel (
    RiskLevelId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_RiskLevel PRIMARY KEY,
    RiskLevelCode nvarchar(50) NOT NULL,
    RiskLevelName nvarchar(100) NOT NULL,
    RiskRank int NOT NULL,
    CONSTRAINT UQ_RiskLevel_Code UNIQUE (RiskLevelCode),
    CONSTRAINT UQ_RiskLevel_Rank UNIQUE (RiskRank)
);
GO

CREATE TABLE dbo.Site (
    SiteId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Site PRIMARY KEY,
    OrganizationId bigint NOT NULL,
    SiteCode nvarchar(50) NOT NULL,
    SiteName nvarchar(200) NOT NULL,
    TimeZoneId nvarchar(100) NOT NULL,
    IsActive bit NOT NULL CONSTRAINT DF_Site_IsActive DEFAULT (1),
    CONSTRAINT UQ_Site_Organization_Code UNIQUE (OrganizationId, SiteCode),
    CONSTRAINT FK_Site_Organization FOREIGN KEY (OrganizationId) REFERENCES dbo.Organization(OrganizationId)
);
GO

CREATE TABLE dbo.Facility (
    FacilityId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Facility PRIMARY KEY,
    SiteId bigint NOT NULL,
    FacilityCode nvarchar(50) NOT NULL,
    FacilityName nvarchar(200) NOT NULL,
    IsActive bit NOT NULL CONSTRAINT DF_Facility_IsActive DEFAULT (1),
    CONSTRAINT UQ_Facility_Site_Code UNIQUE (SiteId, FacilityCode),
    CONSTRAINT FK_Facility_Site FOREIGN KEY (SiteId) REFERENCES dbo.Site(SiteId)
);
GO

CREATE TABLE dbo.Area (
    AreaId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Area PRIMARY KEY,
    FacilityId bigint NOT NULL,
    ParentAreaId bigint NULL,
    AreaCode nvarchar(50) NOT NULL,
    AreaName nvarchar(200) NOT NULL,
    RiskLevelId int NOT NULL,
    IsActive bit NOT NULL CONSTRAINT DF_Area_IsActive DEFAULT (1),
    CONSTRAINT UQ_Area_Facility_Code UNIQUE (FacilityId, AreaCode),
    CONSTRAINT CK_Area_NotSelfParent CHECK (ParentAreaId IS NULL OR ParentAreaId <> AreaId),
    CONSTRAINT FK_Area_Facility FOREIGN KEY (FacilityId) REFERENCES dbo.Facility(FacilityId),
    CONSTRAINT FK_Area_ParentArea FOREIGN KEY (ParentAreaId) REFERENCES dbo.Area(AreaId),
    CONSTRAINT FK_Area_RiskLevel FOREIGN KEY (RiskLevelId) REFERENCES dbo.RiskLevel(RiskLevelId)
);
GO

CREATE TABLE dbo.PartyType (
    PartyTypeId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PartyType PRIMARY KEY,
    PartyTypeCode nvarchar(50) NOT NULL,
    PartyTypeName nvarchar(100) NOT NULL,
    CONSTRAINT UQ_PartyType_Code UNIQUE (PartyTypeCode)
);
GO

CREATE TABLE dbo.Party (
    PartyId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Party PRIMARY KEY,
    OrganizationId bigint NOT NULL,
    PartyTypeId int NOT NULL,
    DisplayName nvarchar(200) NOT NULL,
    LegalName nvarchar(200) NULL,
    ExternalReference nvarchar(100) NULL,
    IsActive bit NOT NULL CONSTRAINT DF_Party_IsActive DEFAULT (1),
    CreatedUtc datetime2(7) NOT NULL CONSTRAINT DF_Party_CreatedUtc DEFAULT (sysutcdatetime()),
    CONSTRAINT FK_Party_Organization FOREIGN KEY (OrganizationId) REFERENCES dbo.Organization(OrganizationId),
    CONSTRAINT FK_Party_PartyType FOREIGN KEY (PartyTypeId) REFERENCES dbo.PartyType(PartyTypeId)
);
GO
CREATE UNIQUE INDEX UX_Party_Organization_ExternalReference ON dbo.Party(OrganizationId, ExternalReference) WHERE ExternalReference IS NOT NULL;
GO

CREATE TABLE dbo.EmployeeProfile (
    PartyId bigint NOT NULL CONSTRAINT PK_EmployeeProfile PRIMARY KEY,
    EmployeeNumber nvarchar(50) NOT NULL,
    HireDate date NULL,
    TerminationDate date NULL,
    SupervisorPartyId bigint NULL,
    CONSTRAINT FK_EmployeeProfile_Party FOREIGN KEY (PartyId) REFERENCES dbo.Party(PartyId),
    CONSTRAINT FK_EmployeeProfile_SupervisorParty FOREIGN KEY (SupervisorPartyId) REFERENCES dbo.Party(PartyId),
    CONSTRAINT CK_EmployeeProfile_Dates CHECK (TerminationDate IS NULL OR HireDate IS NULL OR TerminationDate >= HireDate)
);
GO
CREATE UNIQUE INDEX UX_EmployeeProfile_EmployeeNumber ON dbo.EmployeeProfile(EmployeeNumber);
GO

CREATE TABLE dbo.ContractorProfile (
    PartyId bigint NOT NULL CONSTRAINT PK_ContractorProfile PRIMARY KEY,
    ContractorNumber nvarchar(50) NOT NULL,
    CompanyPartyId bigint NOT NULL,
    ContractStartDate date NULL,
    ContractEndDate date NULL,
    CONSTRAINT FK_ContractorProfile_Party FOREIGN KEY (PartyId) REFERENCES dbo.Party(PartyId),
    CONSTRAINT FK_ContractorProfile_CompanyParty FOREIGN KEY (CompanyPartyId) REFERENCES dbo.Party(PartyId),
    CONSTRAINT CK_ContractorProfile_Dates CHECK (ContractEndDate IS NULL OR ContractStartDate IS NULL OR ContractEndDate >= ContractStartDate)
);
GO
CREATE UNIQUE INDEX UX_ContractorProfile_Number ON dbo.ContractorProfile(ContractorNumber);
GO

CREATE TABLE dbo.VisitorProfile (
    PartyId bigint NOT NULL CONSTRAINT PK_VisitorProfile PRIMARY KEY,
    HostPartyId bigint NOT NULL,
    VisitStartUtc datetime2(7) NOT NULL,
    VisitEndUtc datetime2(7) NOT NULL,
    CONSTRAINT FK_VisitorProfile_Party FOREIGN KEY (PartyId) REFERENCES dbo.Party(PartyId),
    CONSTRAINT FK_VisitorProfile_HostParty FOREIGN KEY (HostPartyId) REFERENCES dbo.Party(PartyId),
    CONSTRAINT CK_VisitorProfile_VisitRange CHECK (VisitEndUtc > VisitStartUtc)
);
GO

CREATE TABLE dbo.VendorProfile (
    PartyId bigint NOT NULL CONSTRAINT PK_VendorProfile PRIMARY KEY,
    VendorNumber nvarchar(50) NOT NULL,
    TaxIdentifierHash varbinary(64) NULL,
    PrimaryContactPartyId bigint NULL,
    CONSTRAINT FK_VendorProfile_Party FOREIGN KEY (PartyId) REFERENCES dbo.Party(PartyId),
    CONSTRAINT FK_VendorProfile_PrimaryContactParty FOREIGN KEY (PrimaryContactPartyId) REFERENCES dbo.Party(PartyId),
    CONSTRAINT UQ_VendorProfile_Number UNIQUE (VendorNumber)
);
GO

CREATE TABLE dbo.ExternalCompanyProfile (
    PartyId bigint NOT NULL CONSTRAINT PK_ExternalCompanyProfile PRIMARY KEY,
    CompanyNumber nvarchar(50) NOT NULL,
    PrimaryContactPartyId bigint NULL,
    CONSTRAINT FK_ExternalCompanyProfile_Party FOREIGN KEY (PartyId) REFERENCES dbo.Party(PartyId),
    CONSTRAINT FK_ExternalCompanyProfile_PrimaryContactParty FOREIGN KEY (PrimaryContactPartyId) REFERENCES dbo.Party(PartyId),
    CONSTRAINT UQ_ExternalCompanyProfile_Number UNIQUE (CompanyNumber)
);
GO

CREATE TABLE dbo.Department (
    DepartmentId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Department PRIMARY KEY,
    OrganizationId bigint NOT NULL,
    ParentDepartmentId bigint NULL,
    DepartmentCode nvarchar(50) NOT NULL,
    DepartmentName nvarchar(200) NOT NULL,
    IsActive bit NOT NULL CONSTRAINT DF_Department_IsActive DEFAULT (1),
    CONSTRAINT UQ_Department_Organization_Code UNIQUE (OrganizationId, DepartmentCode),
    CONSTRAINT CK_Department_NotSelfParent CHECK (ParentDepartmentId IS NULL OR ParentDepartmentId <> DepartmentId),
    CONSTRAINT FK_Department_Organization FOREIGN KEY (OrganizationId) REFERENCES dbo.Organization(OrganizationId),
    CONSTRAINT FK_Department_Parent FOREIGN KEY (ParentDepartmentId) REFERENCES dbo.Department(DepartmentId)
);
GO

CREATE TABLE dbo.PartyDepartmentAssignment (
    PartyDepartmentAssignmentId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_PartyDepartmentAssignment PRIMARY KEY,
    PartyId bigint NOT NULL,
    DepartmentId bigint NOT NULL,
    EffectiveFromUtc datetime2(7) NOT NULL,
    EffectiveToUtc datetime2(7) NULL,
    IsPrimary bit NOT NULL CONSTRAINT DF_PartyDepartmentAssignment_IsPrimary DEFAULT (0),
    CONSTRAINT FK_PartyDepartmentAssignment_Party FOREIGN KEY (PartyId) REFERENCES dbo.Party(PartyId),
    CONSTRAINT FK_PartyDepartmentAssignment_Department FOREIGN KEY (DepartmentId) REFERENCES dbo.Department(DepartmentId),
    CONSTRAINT CK_PartyDepartmentAssignment_Range CHECK (EffectiveToUtc IS NULL OR EffectiveToUtc > EffectiveFromUtc)
);
GO
CREATE UNIQUE INDEX UX_PartyDepartmentAssignment_OneActivePrimary ON dbo.PartyDepartmentAssignment(PartyId) WHERE IsPrimary = 1 AND EffectiveToUtc IS NULL;
CREATE INDEX IX_PartyDepartmentAssignment_Department ON dbo.PartyDepartmentAssignment(DepartmentId);
GO

CREATE TABLE dbo.SecurityPrincipalType (
    SecurityPrincipalTypeId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_SecurityPrincipalType PRIMARY KEY,
    PrincipalTypeCode nvarchar(50) NOT NULL,
    PrincipalTypeName nvarchar(100) NOT NULL,
    CONSTRAINT UQ_SecurityPrincipalType_Code UNIQUE (PrincipalTypeCode)
);
GO

CREATE TABLE dbo.SecurityPrincipal (
    SecurityPrincipalId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_SecurityPrincipal PRIMARY KEY,
    PartyId bigint NULL,
    PrincipalName nvarchar(256) NOT NULL,
    SecurityPrincipalTypeId int NOT NULL,
    IsActive bit NOT NULL CONSTRAINT DF_SecurityPrincipal_IsActive DEFAULT (1),
    CreatedUtc datetime2(7) NOT NULL CONSTRAINT DF_SecurityPrincipal_CreatedUtc DEFAULT (sysutcdatetime()),
    CONSTRAINT UQ_SecurityPrincipal_PrincipalName UNIQUE (PrincipalName),
    CONSTRAINT FK_SecurityPrincipal_Party FOREIGN KEY (PartyId) REFERENCES dbo.Party(PartyId),
    CONSTRAINT FK_SecurityPrincipal_Type FOREIGN KEY (SecurityPrincipalTypeId) REFERENCES dbo.SecurityPrincipalType(SecurityPrincipalTypeId)
);
GO

CREATE TABLE dbo.Role (
    RoleId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Role PRIMARY KEY,
    OrganizationId bigint NOT NULL,
    RoleCode nvarchar(100) NOT NULL,
    RoleName nvarchar(200) NOT NULL,
    Description nvarchar(1000) NULL,
    IsSystemRole bit NOT NULL CONSTRAINT DF_Role_IsSystemRole DEFAULT (0),
    IsActive bit NOT NULL CONSTRAINT DF_Role_IsActive DEFAULT (1),
    CONSTRAINT UQ_Role_Organization_Code UNIQUE (OrganizationId, RoleCode),
    CONSTRAINT FK_Role_Organization FOREIGN KEY (OrganizationId) REFERENCES dbo.Organization(OrganizationId)
);
GO

CREATE TABLE dbo.Permission (
    PermissionId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Permission PRIMARY KEY,
    PermissionCode nvarchar(150) NOT NULL,
    PermissionName nvarchar(200) NOT NULL,
    PermissionDescription nvarchar(1000) NULL,
    CONSTRAINT UQ_Permission_Code UNIQUE (PermissionCode)
);
GO

CREATE TABLE dbo.RolePermission (
    RoleId bigint NOT NULL,
    PermissionId int NOT NULL,
    CONSTRAINT PK_RolePermission PRIMARY KEY (RoleId, PermissionId),
    CONSTRAINT FK_RolePermission_Role FOREIGN KEY (RoleId) REFERENCES dbo.Role(RoleId),
    CONSTRAINT FK_RolePermission_Permission FOREIGN KEY (PermissionId) REFERENCES dbo.Permission(PermissionId)
);
GO

CREATE TABLE dbo.AuthorizationScopeType (
    AuthorizationScopeTypeId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_AuthorizationScopeType PRIMARY KEY,
    ScopeTypeCode nvarchar(50) NOT NULL,
    ScopeTypeName nvarchar(100) NOT NULL,
    CONSTRAINT UQ_AuthorizationScopeType_Code UNIQUE (ScopeTypeCode)
);
GO

CREATE TABLE dbo.PrincipalRoleAssignment (
    PrincipalRoleAssignmentId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_PrincipalRoleAssignment PRIMARY KEY,
    SecurityPrincipalId bigint NOT NULL,
    RoleId bigint NOT NULL,
    ScopeTypeId int NOT NULL,
    ScopeEntityId bigint NOT NULL,
    EffectiveFromUtc datetime2(7) NOT NULL,
    EffectiveToUtc datetime2(7) NULL,
    AssignedByPrincipalId bigint NOT NULL,
    CONSTRAINT FK_PrincipalRoleAssignment_Principal FOREIGN KEY (SecurityPrincipalId) REFERENCES dbo.SecurityPrincipal(SecurityPrincipalId),
    CONSTRAINT FK_PrincipalRoleAssignment_Role FOREIGN KEY (RoleId) REFERENCES dbo.Role(RoleId),
    CONSTRAINT FK_PrincipalRoleAssignment_ScopeType FOREIGN KEY (ScopeTypeId) REFERENCES dbo.AuthorizationScopeType(AuthorizationScopeTypeId),
    CONSTRAINT FK_PrincipalRoleAssignment_AssignedBy FOREIGN KEY (AssignedByPrincipalId) REFERENCES dbo.SecurityPrincipal(SecurityPrincipalId),
    CONSTRAINT CK_PrincipalRoleAssignment_Range CHECK (EffectiveToUtc IS NULL OR EffectiveToUtc > EffectiveFromUtc)
);
GO
CREATE UNIQUE INDEX UX_PrincipalRoleAssignment_Active ON dbo.PrincipalRoleAssignment(SecurityPrincipalId, RoleId, ScopeTypeId, ScopeEntityId) WHERE EffectiveToUtc IS NULL;
GO

CREATE TABLE dbo.KeyType (
    KeyTypeId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_KeyType PRIMARY KEY,
    KeyTypeCode nvarchar(50) NOT NULL,
    KeyTypeName nvarchar(100) NOT NULL,
    IsPhysical bit NOT NULL,
    IsElectronic bit NOT NULL,
    CONSTRAINT UQ_KeyType_Code UNIQUE (KeyTypeCode)
);
GO

CREATE TABLE dbo.AggregateType (
    AggregateTypeId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_AggregateType PRIMARY KEY,
    AggregateTypeCode nvarchar(100) NOT NULL,
    AggregateTypeName nvarchar(200) NOT NULL,
    CONSTRAINT UQ_AggregateType_Code UNIQUE (AggregateTypeCode)
);
GO

CREATE TABLE dbo.EventType (
    EventTypeId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_EventType PRIMARY KEY,
    EventTypeCode nvarchar(100) NOT NULL,
    EventTypeName nvarchar(200) NOT NULL,
    AggregateTypeId int NOT NULL,
    IsLifecycleEvent bit NOT NULL CONSTRAINT DF_EventType_IsLifecycleEvent DEFAULT (0),
    IsCustodyEvent bit NOT NULL CONSTRAINT DF_EventType_IsCustodyEvent DEFAULT (0),
    RequiresSignature bit NOT NULL CONSTRAINT DF_EventType_RequiresSignature DEFAULT (0),
    IsActive bit NOT NULL CONSTRAINT DF_EventType_IsActive DEFAULT (1),
    CONSTRAINT UQ_EventType_Code UNIQUE (EventTypeCode),
    CONSTRAINT FK_EventType_AggregateType FOREIGN KEY (AggregateTypeId) REFERENCES dbo.AggregateType(AggregateTypeId)
);
GO

CREATE TABLE dbo.IntegrityAlgorithm (
    IntegrityAlgorithmId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_IntegrityAlgorithm PRIMARY KEY,
    AlgorithmCode nvarchar(50) NOT NULL,
    AlgorithmName nvarchar(100) NOT NULL,
    CONSTRAINT UQ_IntegrityAlgorithm_Code UNIQUE (AlgorithmCode)
);
GO

CREATE TABLE dbo.EventStream (
    EventStreamId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_EventStream PRIMARY KEY,
    OrganizationId bigint NOT NULL,
    AggregateTypeId int NOT NULL,
    AggregateId bigint NOT NULL,
    StreamKey nvarchar(200) NOT NULL,
    CreatedUtc datetime2(7) NOT NULL CONSTRAINT DF_EventStream_CreatedUtc DEFAULT (sysutcdatetime()),
    CONSTRAINT UQ_EventStream_Aggregate UNIQUE (AggregateTypeId, AggregateId),
    CONSTRAINT UQ_EventStream_StreamKey UNIQUE (StreamKey),
    CONSTRAINT FK_EventStream_Organization FOREIGN KEY (OrganizationId) REFERENCES dbo.Organization(OrganizationId),
    CONSTRAINT FK_EventStream_AggregateType FOREIGN KEY (AggregateTypeId) REFERENCES dbo.AggregateType(AggregateTypeId)
);
GO

CREATE TABLE dbo.Event (
    EventId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Event PRIMARY KEY,
    EventStreamId bigint NOT NULL,
    EventTypeId int NOT NULL,
    EventSequenceNumber bigint NOT NULL,
    OccurredUtc datetime2(7) NOT NULL,
    RecordedUtc datetime2(7) NOT NULL CONSTRAINT DF_Event_RecordedUtc DEFAULT (sysutcdatetime()),
    ActorPartyId bigint NULL,
    ActorPrincipalId bigint NULL,
    CorrelationId uniqueidentifier NOT NULL,
    CausationEventId bigint NULL,
    EventPayloadJson nvarchar(max) NOT NULL,
    EventSchemaVersion int NOT NULL,
    PreviousEventHash varbinary(64) NULL,
    EventHash varbinary(64) NOT NULL,
    IntegrityAlgorithmId int NOT NULL,
    IsCompensatingEvent bit NOT NULL CONSTRAINT DF_Event_IsCompensatingEvent DEFAULT (0),
    CONSTRAINT UQ_Event_Stream_Sequence UNIQUE (EventStreamId, EventSequenceNumber),
    CONSTRAINT UQ_Event_Hash UNIQUE (EventHash),
    CONSTRAINT CK_Event_RecordedAfterOccurred CHECK (RecordedUtc >= OccurredUtc),
    CONSTRAINT FK_Event_Stream FOREIGN KEY (EventStreamId) REFERENCES dbo.EventStream(EventStreamId),
    CONSTRAINT FK_Event_Type FOREIGN KEY (EventTypeId) REFERENCES dbo.EventType(EventTypeId),
    CONSTRAINT FK_Event_ActorParty FOREIGN KEY (ActorPartyId) REFERENCES dbo.Party(PartyId),
    CONSTRAINT FK_Event_ActorPrincipal FOREIGN KEY (ActorPrincipalId) REFERENCES dbo.SecurityPrincipal(SecurityPrincipalId),
    CONSTRAINT FK_Event_CausationEvent FOREIGN KEY (CausationEventId) REFERENCES dbo.Event(EventId),
    CONSTRAINT FK_Event_IntegrityAlgorithm FOREIGN KEY (IntegrityAlgorithmId) REFERENCES dbo.IntegrityAlgorithm(IntegrityAlgorithmId)
);
GO
CREATE INDEX IX_Event_CorrelationId ON dbo.Event(CorrelationId);
CREATE INDEX IX_Event_EventType_OccurredUtc ON dbo.Event(EventTypeId, OccurredUtc);
GO

CREATE TABLE dbo.EventSchema (
    EventSchemaId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_EventSchema PRIMARY KEY,
    EventTypeId int NOT NULL,
    SchemaVersion int NOT NULL,
    JsonSchemaDefinition nvarchar(max) NOT NULL,
    EffectiveFromUtc datetime2(7) NOT NULL,
    EffectiveToUtc datetime2(7) NULL,
    CONSTRAINT UQ_EventSchema_Type_Version UNIQUE (EventTypeId, SchemaVersion),
    CONSTRAINT CK_EventSchema_Range CHECK (EffectiveToUtc IS NULL OR EffectiveToUtc > EffectiveFromUtc),
    CONSTRAINT FK_EventSchema_EventType FOREIGN KEY (EventTypeId) REFERENCES dbo.EventType(EventTypeId)
);
GO

CREATE TABLE dbo.KeyAsset (
    KeyAssetId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_KeyAsset PRIMARY KEY,
    OrganizationId bigint NOT NULL,
    SiteId bigint NOT NULL,
    KeyTypeId int NOT NULL,
    KeyCode nvarchar(100) NOT NULL,
    SerialNumber nvarchar(100) NULL,
    Manufacturer nvarchar(100) NULL,
    Model nvarchar(100) NULL,
    RiskLevelId int NOT NULL,
    CreatedEventId bigint NULL,
    CreatedUtc datetime2(7) NOT NULL CONSTRAINT DF_KeyAsset_CreatedUtc DEFAULT (sysutcdatetime()),
    CONSTRAINT UQ_KeyAsset_Organization_KeyCode UNIQUE (OrganizationId, KeyCode),
    CONSTRAINT FK_KeyAsset_Organization FOREIGN KEY (OrganizationId) REFERENCES dbo.Organization(OrganizationId),
    CONSTRAINT FK_KeyAsset_Site FOREIGN KEY (SiteId) REFERENCES dbo.Site(SiteId),
    CONSTRAINT FK_KeyAsset_KeyType FOREIGN KEY (KeyTypeId) REFERENCES dbo.KeyType(KeyTypeId),
    CONSTRAINT FK_KeyAsset_RiskLevel FOREIGN KEY (RiskLevelId) REFERENCES dbo.RiskLevel(RiskLevelId),
    CONSTRAINT FK_KeyAsset_CreatedEvent FOREIGN KEY (CreatedEventId) REFERENCES dbo.Event(EventId)
);
GO
CREATE UNIQUE INDEX UX_KeyAsset_Organization_SerialNumber ON dbo.KeyAsset(OrganizationId, SerialNumber) WHERE SerialNumber IS NOT NULL;
GO

CREATE TABLE dbo.AccessLevel (
    AccessLevelId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_AccessLevel PRIMARY KEY,
    AccessLevelCode nvarchar(50) NOT NULL,
    AccessLevelName nvarchar(100) NOT NULL,
    CONSTRAINT UQ_AccessLevel_Code UNIQUE (AccessLevelCode)
);
GO

CREATE TABLE dbo.KeyAreaAccess (
    KeyAssetId bigint NOT NULL,
    AreaId bigint NOT NULL,
    AccessLevelId int NOT NULL,
    EffectiveFromUtc datetime2(7) NOT NULL,
    EffectiveToUtc datetime2(7) NULL,
    CONSTRAINT PK_KeyAreaAccess PRIMARY KEY (KeyAssetId, AreaId, AccessLevelId, EffectiveFromUtc),
    CONSTRAINT CK_KeyAreaAccess_Range CHECK (EffectiveToUtc IS NULL OR EffectiveToUtc > EffectiveFromUtc),
    CONSTRAINT FK_KeyAreaAccess_KeyAsset FOREIGN KEY (KeyAssetId) REFERENCES dbo.KeyAsset(KeyAssetId),
    CONSTRAINT FK_KeyAreaAccess_Area FOREIGN KEY (AreaId) REFERENCES dbo.Area(AreaId),
    CONSTRAINT FK_KeyAreaAccess_AccessLevel FOREIGN KEY (AccessLevelId) REFERENCES dbo.AccessLevel(AccessLevelId)
);
GO

CREATE TABLE dbo.KeyGroup (
    KeyGroupId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_KeyGroup PRIMARY KEY,
    OrganizationId bigint NOT NULL,
    KeyGroupCode nvarchar(50) NOT NULL,
    KeyGroupName nvarchar(200) NOT NULL,
    Description nvarchar(1000) NULL,
    CONSTRAINT UQ_KeyGroup_Organization_Code UNIQUE (OrganizationId, KeyGroupCode),
    CONSTRAINT FK_KeyGroup_Organization FOREIGN KEY (OrganizationId) REFERENCES dbo.Organization(OrganizationId)
);
GO

CREATE TABLE dbo.KeyGroupMember (
    KeyGroupId bigint NOT NULL,
    KeyAssetId bigint NOT NULL,
    EffectiveFromUtc datetime2(7) NOT NULL,
    EffectiveToUtc datetime2(7) NULL,
    CONSTRAINT PK_KeyGroupMember PRIMARY KEY (KeyGroupId, KeyAssetId, EffectiveFromUtc),
    CONSTRAINT CK_KeyGroupMember_Range CHECK (EffectiveToUtc IS NULL OR EffectiveToUtc > EffectiveFromUtc),
    CONSTRAINT FK_KeyGroupMember_KeyGroup FOREIGN KEY (KeyGroupId) REFERENCES dbo.KeyGroup(KeyGroupId),
    CONSTRAINT FK_KeyGroupMember_KeyAsset FOREIGN KEY (KeyAssetId) REFERENCES dbo.KeyAsset(KeyAssetId)
);
GO

CREATE TABLE dbo.EntityType (
    EntityTypeId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_EntityType PRIMARY KEY,
    EntityTypeCode nvarchar(100) NOT NULL,
    EntityTypeName nvarchar(200) NOT NULL,
    CONSTRAINT UQ_EntityType_Code UNIQUE (EntityTypeCode)
);
GO

CREATE TABLE dbo.AuditActionType (
    AuditActionTypeId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_AuditActionType PRIMARY KEY,
    AuditActionCode nvarchar(100) NOT NULL,
    AuditActionName nvarchar(200) NOT NULL,
    CONSTRAINT UQ_AuditActionType_Code UNIQUE (AuditActionCode)
);
GO

CREATE TABLE dbo.AuditRecord (
    AuditRecordId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_AuditRecord PRIMARY KEY,
    OrganizationId bigint NOT NULL,
    AuditActionTypeId int NOT NULL,
    ActorPrincipalId bigint NULL,
    ActorPartyId bigint NULL,
    TargetEntityTypeId int NOT NULL,
    TargetEntityId bigint NOT NULL,
    OccurredUtc datetime2(7) NOT NULL,
    RecordedUtc datetime2(7) NOT NULL CONSTRAINT DF_AuditRecord_RecordedUtc DEFAULT (sysutcdatetime()),
    SourceIpAddress nvarchar(45) NULL,
    UserAgent nvarchar(512) NULL,
    CorrelationId uniqueidentifier NOT NULL,
    AuditPayloadJson nvarchar(max) NOT NULL,
    PreviousAuditHash varbinary(64) NULL,
    AuditHash varbinary(64) NOT NULL,
    IntegrityAlgorithmId int NOT NULL,
    CONSTRAINT UQ_AuditRecord_Hash UNIQUE (AuditHash),
    CONSTRAINT CK_AuditRecord_RecordedAfterOccurred CHECK (RecordedUtc >= OccurredUtc),
    CONSTRAINT FK_AuditRecord_Organization FOREIGN KEY (OrganizationId) REFERENCES dbo.Organization(OrganizationId),
    CONSTRAINT FK_AuditRecord_ActionType FOREIGN KEY (AuditActionTypeId) REFERENCES dbo.AuditActionType(AuditActionTypeId),
    CONSTRAINT FK_AuditRecord_ActorPrincipal FOREIGN KEY (ActorPrincipalId) REFERENCES dbo.SecurityPrincipal(SecurityPrincipalId),
    CONSTRAINT FK_AuditRecord_ActorParty FOREIGN KEY (ActorPartyId) REFERENCES dbo.Party(PartyId),
    CONSTRAINT FK_AuditRecord_TargetEntityType FOREIGN KEY (TargetEntityTypeId) REFERENCES dbo.EntityType(EntityTypeId),
    CONSTRAINT FK_AuditRecord_IntegrityAlgorithm FOREIGN KEY (IntegrityAlgorithmId) REFERENCES dbo.IntegrityAlgorithm(IntegrityAlgorithmId)
);
GO

CREATE TABLE dbo.LifecycleState (
    LifecycleStateId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_LifecycleState PRIMARY KEY,
    LifecycleStateCode nvarchar(100) NOT NULL,
    LifecycleStateName nvarchar(200) NOT NULL,
    IsTerminal bit NOT NULL CONSTRAINT DF_LifecycleState_IsTerminal DEFAULT (0),
    CONSTRAINT UQ_LifecycleState_Code UNIQUE (LifecycleStateCode)
);
GO

CREATE TABLE dbo.LifecycleTransition (
    LifecycleTransitionId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_LifecycleTransition PRIMARY KEY,
    FromLifecycleStateId int NULL,
    ToLifecycleStateId int NOT NULL,
    EventTypeId int NOT NULL,
    RequiresAuthorization bit NOT NULL,
    RequiresInspection bit NOT NULL,
    IsEmergencyOnly bit NOT NULL,
    IsActive bit NOT NULL CONSTRAINT DF_LifecycleTransition_IsActive DEFAULT (1),
    CONSTRAINT UQ_LifecycleTransition UNIQUE (FromLifecycleStateId, ToLifecycleStateId, EventTypeId),
    CONSTRAINT CK_LifecycleTransition_NotSame CHECK (FromLifecycleStateId IS NULL OR FromLifecycleStateId <> ToLifecycleStateId),
    CONSTRAINT FK_LifecycleTransition_From FOREIGN KEY (FromLifecycleStateId) REFERENCES dbo.LifecycleState(LifecycleStateId),
    CONSTRAINT FK_LifecycleTransition_To FOREIGN KEY (ToLifecycleStateId) REFERENCES dbo.LifecycleState(LifecycleStateId),
    CONSTRAINT FK_LifecycleTransition_EventType FOREIGN KEY (EventTypeId) REFERENCES dbo.EventType(EventTypeId)
);
GO

CREATE TABLE dbo.KeyLifecycleProjection (
    KeyAssetId bigint NOT NULL CONSTRAINT PK_KeyLifecycleProjection PRIMARY KEY,
    LifecycleStateId int NOT NULL,
    DerivedFromEventId bigint NOT NULL,
    DerivedUtc datetime2(7) NOT NULL,
    CONSTRAINT FK_KeyLifecycleProjection_KeyAsset FOREIGN KEY (KeyAssetId) REFERENCES dbo.KeyAsset(KeyAssetId),
    CONSTRAINT FK_KeyLifecycleProjection_State FOREIGN KEY (LifecycleStateId) REFERENCES dbo.LifecycleState(LifecycleStateId),
    CONSTRAINT FK_KeyLifecycleProjection_Event FOREIGN KEY (DerivedFromEventId) REFERENCES dbo.Event(EventId)
);
GO

CREATE TABLE dbo.LoanState (
    LoanStateId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_LoanState PRIMARY KEY,
    LoanStateCode nvarchar(100) NOT NULL,
    LoanStateName nvarchar(200) NOT NULL,
    IsTerminal bit NOT NULL,
    CONSTRAINT UQ_LoanState_Code UNIQUE (LoanStateCode)
);
GO

CREATE TABLE dbo.Loan (
    LoanId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Loan PRIMARY KEY,
    OrganizationId bigint NOT NULL,
    KeyAssetId bigint NOT NULL,
    BorrowerPartyId bigint NOT NULL,
    RequestedByPartyId bigint NOT NULL,
    ExpectedReturnUtc datetime2(7) NOT NULL,
    Purpose nvarchar(1000) NULL,
    CreatedEventId bigint NULL,
    CreatedUtc datetime2(7) NOT NULL CONSTRAINT DF_Loan_CreatedUtc DEFAULT (sysutcdatetime()),
    CONSTRAINT FK_Loan_Organization FOREIGN KEY (OrganizationId) REFERENCES dbo.Organization(OrganizationId),
    CONSTRAINT FK_Loan_KeyAsset FOREIGN KEY (KeyAssetId) REFERENCES dbo.KeyAsset(KeyAssetId),
    CONSTRAINT FK_Loan_BorrowerParty FOREIGN KEY (BorrowerPartyId) REFERENCES dbo.Party(PartyId),
    CONSTRAINT FK_Loan_RequestedByParty FOREIGN KEY (RequestedByPartyId) REFERENCES dbo.Party(PartyId),
    CONSTRAINT FK_Loan_CreatedEvent FOREIGN KEY (CreatedEventId) REFERENCES dbo.Event(EventId)
);
GO

CREATE TABLE dbo.LoanTerm (
    LoanTermId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_LoanTerm PRIMARY KEY,
    LoanId bigint NOT NULL,
    EffectiveFromUtc datetime2(7) NOT NULL,
    EffectiveToUtc datetime2(7) NOT NULL,
    MaxDurationMinutes int NOT NULL,
    BusinessHoursOnly bit NOT NULL,
    ReturnGraceMinutes int NOT NULL,
    CONSTRAINT CK_LoanTerm_Range CHECK (EffectiveToUtc > EffectiveFromUtc),
    CONSTRAINT CK_LoanTerm_Duration CHECK (MaxDurationMinutes > 0 AND ReturnGraceMinutes >= 0),
    CONSTRAINT FK_LoanTerm_Loan FOREIGN KEY (LoanId) REFERENCES dbo.Loan(LoanId)
);
GO

CREATE TABLE dbo.LoanProjection (
    LoanId bigint NOT NULL CONSTRAINT PK_LoanProjection PRIMARY KEY,
    LoanStateId int NOT NULL,
    IssuedEventId bigint NULL,
    ReturnedEventId bigint NULL,
    CurrentCustodianPartyId bigint NULL,
    DerivedFromEventId bigint NOT NULL,
    DerivedUtc datetime2(7) NOT NULL,
    CONSTRAINT FK_LoanProjection_Loan FOREIGN KEY (LoanId) REFERENCES dbo.Loan(LoanId),
    CONSTRAINT FK_LoanProjection_State FOREIGN KEY (LoanStateId) REFERENCES dbo.LoanState(LoanStateId),
    CONSTRAINT FK_LoanProjection_IssuedEvent FOREIGN KEY (IssuedEventId) REFERENCES dbo.Event(EventId),
    CONSTRAINT FK_LoanProjection_ReturnedEvent FOREIGN KEY (ReturnedEventId) REFERENCES dbo.Event(EventId),
    CONSTRAINT FK_LoanProjection_CurrentCustodian FOREIGN KEY (CurrentCustodianPartyId) REFERENCES dbo.Party(PartyId),
    CONSTRAINT FK_LoanProjection_DerivedEvent FOREIGN KEY (DerivedFromEventId) REFERENCES dbo.Event(EventId)
);
GO

CREATE TABLE dbo.StorageDeviceType (
    StorageDeviceTypeId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_StorageDeviceType PRIMARY KEY,
    DeviceTypeCode nvarchar(100) NOT NULL,
    DeviceTypeName nvarchar(200) NOT NULL,
    IsElectronic bit NOT NULL,
    SupportsSlotTelemetry bit NOT NULL,
    SupportsRfid bit NOT NULL,
    CONSTRAINT UQ_StorageDeviceType_Code UNIQUE (DeviceTypeCode)
);
GO

CREATE TABLE dbo.StorageDevice (
    StorageDeviceId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_StorageDevice PRIMARY KEY,
    OrganizationId bigint NOT NULL,
    SiteId bigint NOT NULL,
    StorageDeviceTypeId int NOT NULL,
    DeviceCode nvarchar(100) NOT NULL,
    DeviceName nvarchar(200) NOT NULL,
    Manufacturer nvarchar(100) NULL,
    Model nvarchar(100) NULL,
    SerialNumber nvarchar(100) NULL,
    NetworkIdentifier nvarchar(200) NULL,
    IsActive bit NOT NULL CONSTRAINT DF_StorageDevice_IsActive DEFAULT (1),
    CONSTRAINT UQ_StorageDevice_Organization_DeviceCode UNIQUE (OrganizationId, DeviceCode),
    CONSTRAINT FK_StorageDevice_Organization FOREIGN KEY (OrganizationId) REFERENCES dbo.Organization(OrganizationId),
    CONSTRAINT FK_StorageDevice_Site FOREIGN KEY (SiteId) REFERENCES dbo.Site(SiteId),
    CONSTRAINT FK_StorageDevice_Type FOREIGN KEY (StorageDeviceTypeId) REFERENCES dbo.StorageDeviceType(StorageDeviceTypeId)
);
GO
CREATE UNIQUE INDEX UX_StorageDevice_Organization_SerialNumber ON dbo.StorageDevice(OrganizationId, SerialNumber) WHERE SerialNumber IS NOT NULL;
GO

CREATE TABLE dbo.CabinetProfile (
    StorageDeviceId bigint NOT NULL CONSTRAINT PK_CabinetProfile PRIMARY KEY,
    CabinetFirmwareVersion nvarchar(100) NULL,
    LastHeartbeatUtc datetime2(7) NULL,
    CONSTRAINT FK_CabinetProfile_Device FOREIGN KEY (StorageDeviceId) REFERENCES dbo.StorageDevice(StorageDeviceId)
);
GO

CREATE TABLE dbo.LockerProfile (
    StorageDeviceId bigint NOT NULL CONSTRAINT PK_LockerProfile PRIMARY KEY,
    LockerBankIdentifier nvarchar(100) NOT NULL,
    CONSTRAINT FK_LockerProfile_Device FOREIGN KEY (StorageDeviceId) REFERENCES dbo.StorageDevice(StorageDeviceId)
);
GO

CREATE TABLE dbo.RfidCabinetProfile (
    StorageDeviceId bigint NOT NULL CONSTRAINT PK_RfidCabinetProfile PRIMARY KEY,
    RfidReaderIdentifier nvarchar(100) NOT NULL,
    FirmwareVersion nvarchar(100) NULL,
    LastSyncUtc datetime2(7) NULL,
    CONSTRAINT FK_RfidCabinetProfile_Device FOREIGN KEY (StorageDeviceId) REFERENCES dbo.StorageDevice(StorageDeviceId)
);
GO

CREATE TABLE dbo.StorageSlot (
    StorageSlotId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_StorageSlot PRIMARY KEY,
    StorageDeviceId bigint NOT NULL,
    SlotCode nvarchar(100) NOT NULL,
    SlotName nvarchar(200) NULL,
    IsActive bit NOT NULL CONSTRAINT DF_StorageSlot_IsActive DEFAULT (1),
    CONSTRAINT UQ_StorageSlot_Device_Code UNIQUE (StorageDeviceId, SlotCode),
    CONSTRAINT FK_StorageSlot_Device FOREIGN KEY (StorageDeviceId) REFERENCES dbo.StorageDevice(StorageDeviceId)
);
GO

CREATE TABLE dbo.StorageLocationType (
    StorageLocationTypeId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_StorageLocationType PRIMARY KEY,
    LocationTypeCode nvarchar(100) NOT NULL,
    LocationTypeName nvarchar(200) NOT NULL,
    CONSTRAINT UQ_StorageLocationType_Code UNIQUE (LocationTypeCode)
);
GO

CREATE TABLE dbo.StorageLocation (
    StorageLocationId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_StorageLocation PRIMARY KEY,
    StorageLocationTypeId int NOT NULL,
    StorageDeviceId bigint NULL,
    StorageSlotId bigint NULL,
    SiteId bigint NOT NULL,
    LocationCode nvarchar(100) NOT NULL,
    CONSTRAINT UQ_StorageLocation_Site_Code UNIQUE (SiteId, LocationCode),
    CONSTRAINT FK_StorageLocation_Type FOREIGN KEY (StorageLocationTypeId) REFERENCES dbo.StorageLocationType(StorageLocationTypeId),
    CONSTRAINT FK_StorageLocation_Device FOREIGN KEY (StorageDeviceId) REFERENCES dbo.StorageDevice(StorageDeviceId),
    CONSTRAINT FK_StorageLocation_Slot FOREIGN KEY (StorageSlotId) REFERENCES dbo.StorageSlot(StorageSlotId),
    CONSTRAINT FK_StorageLocation_Site FOREIGN KEY (SiteId) REFERENCES dbo.Site(SiteId)
);
GO

CREATE TABLE dbo.CustodyTransferReason (
    CustodyTransferReasonId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_CustodyTransferReason PRIMARY KEY,
    ReasonCode nvarchar(100) NOT NULL,
    ReasonName nvarchar(200) NOT NULL,
    CONSTRAINT UQ_CustodyTransferReason_Code UNIQUE (ReasonCode)
);
GO

CREATE TABLE dbo.CustodyEvent (
    CustodyEventId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_CustodyEvent PRIMARY KEY,
    EventId bigint NOT NULL,
    KeyAssetId bigint NOT NULL,
    FromPartyId bigint NULL,
    ToPartyId bigint NULL,
    FromStorageLocationId bigint NULL,
    ToStorageLocationId bigint NULL,
    TransferReasonId int NOT NULL,
    WitnessPartyId bigint NULL,
    AcceptedUtc datetime2(7) NULL,
    DueBackUtc datetime2(7) NULL,
    CONSTRAINT UQ_CustodyEvent_Event UNIQUE (EventId),
    CONSTRAINT CK_CustodyEvent_Source CHECK (FromPartyId IS NOT NULL OR FromStorageLocationId IS NOT NULL),
    CONSTRAINT CK_CustodyEvent_Destination CHECK (ToPartyId IS NOT NULL OR ToStorageLocationId IS NOT NULL),
    CONSTRAINT CK_CustodyEvent_PartyNotSame CHECK (FromPartyId IS NULL OR ToPartyId IS NULL OR FromPartyId <> ToPartyId),
    CONSTRAINT CK_CustodyEvent_StorageNotSame CHECK (FromStorageLocationId IS NULL OR ToStorageLocationId IS NULL OR FromStorageLocationId <> ToStorageLocationId),
    CONSTRAINT FK_CustodyEvent_Event FOREIGN KEY (EventId) REFERENCES dbo.Event(EventId),
    CONSTRAINT FK_CustodyEvent_KeyAsset FOREIGN KEY (KeyAssetId) REFERENCES dbo.KeyAsset(KeyAssetId),
    CONSTRAINT FK_CustodyEvent_FromParty FOREIGN KEY (FromPartyId) REFERENCES dbo.Party(PartyId),
    CONSTRAINT FK_CustodyEvent_ToParty FOREIGN KEY (ToPartyId) REFERENCES dbo.Party(PartyId),
    CONSTRAINT FK_CustodyEvent_FromStorage FOREIGN KEY (FromStorageLocationId) REFERENCES dbo.StorageLocation(StorageLocationId),
    CONSTRAINT FK_CustodyEvent_ToStorage FOREIGN KEY (ToStorageLocationId) REFERENCES dbo.StorageLocation(StorageLocationId),
    CONSTRAINT FK_CustodyEvent_Reason FOREIGN KEY (TransferReasonId) REFERENCES dbo.CustodyTransferReason(CustodyTransferReasonId),
    CONSTRAINT FK_CustodyEvent_WitnessParty FOREIGN KEY (WitnessPartyId) REFERENCES dbo.Party(PartyId)
);
GO

CREATE TABLE dbo.KeyCustodyProjection (
    KeyAssetId bigint NOT NULL CONSTRAINT PK_KeyCustodyProjection PRIMARY KEY,
    CurrentCustodianPartyId bigint NULL,
    CurrentStorageLocationId bigint NULL,
    DerivedFromCustodyEventId bigint NOT NULL,
    DerivedUtc datetime2(7) NOT NULL,
    CONSTRAINT CK_KeyCustodyProjection_OneEndpoint CHECK ((CurrentCustodianPartyId IS NOT NULL AND CurrentStorageLocationId IS NULL) OR (CurrentCustodianPartyId IS NULL AND CurrentStorageLocationId IS NOT NULL) OR (CurrentCustodianPartyId IS NULL AND CurrentStorageLocationId IS NULL)),
    CONSTRAINT FK_KeyCustodyProjection_KeyAsset FOREIGN KEY (KeyAssetId) REFERENCES dbo.KeyAsset(KeyAssetId),
    CONSTRAINT FK_KeyCustodyProjection_Party FOREIGN KEY (CurrentCustodianPartyId) REFERENCES dbo.Party(PartyId),
    CONSTRAINT FK_KeyCustodyProjection_Storage FOREIGN KEY (CurrentStorageLocationId) REFERENCES dbo.StorageLocation(StorageLocationId),
    CONSTRAINT FK_KeyCustodyProjection_CustodyEvent FOREIGN KEY (DerivedFromCustodyEventId) REFERENCES dbo.CustodyEvent(CustodyEventId)
);
GO

CREATE TABLE dbo.AuthorizationPurpose (
    AuthorizationPurposeId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_AuthorizationPurpose PRIMARY KEY,
    PurposeCode nvarchar(100) NOT NULL,
    PurposeName nvarchar(200) NOT NULL,
    CONSTRAINT UQ_AuthorizationPurpose_Code UNIQUE (PurposeCode)
);
GO

CREATE TABLE dbo.PolicyType (
    PolicyTypeId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PolicyType PRIMARY KEY,
    PolicyTypeCode nvarchar(100) NOT NULL,
    PolicyTypeName nvarchar(200) NOT NULL,
    CONSTRAINT UQ_PolicyType_Code UNIQUE (PolicyTypeCode)
);
GO

CREATE TABLE dbo.Policy (
    PolicyId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Policy PRIMARY KEY,
    OrganizationId bigint NOT NULL,
    PolicyCode nvarchar(100) NOT NULL,
    PolicyName nvarchar(200) NOT NULL,
    PolicyTypeId int NOT NULL,
    Priority int NOT NULL,
    EffectiveFromUtc datetime2(7) NOT NULL,
    EffectiveToUtc datetime2(7) NULL,
    IsActive bit NOT NULL CONSTRAINT DF_Policy_IsActive DEFAULT (1),
    CreatedEventId bigint NULL,
    CONSTRAINT UQ_Policy_Organization_Code UNIQUE (OrganizationId, PolicyCode),
    CONSTRAINT CK_Policy_Range CHECK (EffectiveToUtc IS NULL OR EffectiveToUtc > EffectiveFromUtc),
    CONSTRAINT FK_Policy_Organization FOREIGN KEY (OrganizationId) REFERENCES dbo.Organization(OrganizationId),
    CONSTRAINT FK_Policy_Type FOREIGN KEY (PolicyTypeId) REFERENCES dbo.PolicyType(PolicyTypeId),
    CONSTRAINT FK_Policy_CreatedEvent FOREIGN KEY (CreatedEventId) REFERENCES dbo.Event(EventId)
);
GO

CREATE TABLE dbo.PolicyEvaluationResult (
    PolicyEvaluationResultId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PolicyEvaluationResult PRIMARY KEY,
    ResultCode nvarchar(100) NOT NULL,
    ResultName nvarchar(200) NOT NULL,
    CONSTRAINT UQ_PolicyEvaluationResult_Code UNIQUE (ResultCode)
);
GO

CREATE TABLE dbo.PolicyEvaluation (
    PolicyEvaluationId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_PolicyEvaluation PRIMARY KEY,
    OrganizationId bigint NOT NULL,
    EvaluatedAtUtc datetime2(7) NOT NULL,
    EvaluatedByPrincipalId bigint NULL,
    TargetEntityTypeId int NOT NULL,
    TargetEntityId bigint NOT NULL,
    InputContextJson nvarchar(max) NOT NULL,
    EvaluationResultId int NOT NULL,
    ResultPayloadJson nvarchar(max) NOT NULL,
    CONSTRAINT FK_PolicyEvaluation_Organization FOREIGN KEY (OrganizationId) REFERENCES dbo.Organization(OrganizationId),
    CONSTRAINT FK_PolicyEvaluation_Principal FOREIGN KEY (EvaluatedByPrincipalId) REFERENCES dbo.SecurityPrincipal(SecurityPrincipalId),
    CONSTRAINT FK_PolicyEvaluation_TargetEntityType FOREIGN KEY (TargetEntityTypeId) REFERENCES dbo.EntityType(EntityTypeId),
    CONSTRAINT FK_PolicyEvaluation_Result FOREIGN KEY (EvaluationResultId) REFERENCES dbo.PolicyEvaluationResult(PolicyEvaluationResultId)
);
GO

CREATE TABLE dbo.PolicyEvaluationPolicy (
    PolicyEvaluationId bigint NOT NULL,
    PolicyId bigint NOT NULL,
    Matched bit NOT NULL,
    EvaluationDetailJson nvarchar(max) NOT NULL,
    CONSTRAINT PK_PolicyEvaluationPolicy PRIMARY KEY (PolicyEvaluationId, PolicyId),
    CONSTRAINT FK_PolicyEvaluationPolicy_Evaluation FOREIGN KEY (PolicyEvaluationId) REFERENCES dbo.PolicyEvaluation(PolicyEvaluationId),
    CONSTRAINT FK_PolicyEvaluationPolicy_Policy FOREIGN KEY (PolicyId) REFERENCES dbo.Policy(PolicyId)
);
GO

CREATE TABLE dbo.AuthorizationRequest (
    AuthorizationRequestId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_AuthorizationRequest PRIMARY KEY,
    OrganizationId bigint NOT NULL,
    RequestedByPartyId bigint NOT NULL,
    SubjectPartyId bigint NULL,
    TargetEntityTypeId int NOT NULL,
    TargetEntityId bigint NOT NULL,
    AuthorizationPurposeId int NOT NULL,
    PolicyEvaluationId bigint NULL,
    ExpiresUtc datetime2(7) NULL,
    CreatedEventId bigint NULL,
    CreatedUtc datetime2(7) NOT NULL CONSTRAINT DF_AuthorizationRequest_CreatedUtc DEFAULT (sysutcdatetime()),
    CONSTRAINT FK_AuthorizationRequest_Organization FOREIGN KEY (OrganizationId) REFERENCES dbo.Organization(OrganizationId),
    CONSTRAINT FK_AuthorizationRequest_RequestedBy FOREIGN KEY (RequestedByPartyId) REFERENCES dbo.Party(PartyId),
    CONSTRAINT FK_AuthorizationRequest_Subject FOREIGN KEY (SubjectPartyId) REFERENCES dbo.Party(PartyId),
    CONSTRAINT FK_AuthorizationRequest_TargetEntityType FOREIGN KEY (TargetEntityTypeId) REFERENCES dbo.EntityType(EntityTypeId),
    CONSTRAINT FK_AuthorizationRequest_Purpose FOREIGN KEY (AuthorizationPurposeId) REFERENCES dbo.AuthorizationPurpose(AuthorizationPurposeId),
    CONSTRAINT FK_AuthorizationRequest_PolicyEvaluation FOREIGN KEY (PolicyEvaluationId) REFERENCES dbo.PolicyEvaluation(PolicyEvaluationId),
    CONSTRAINT FK_AuthorizationRequest_CreatedEvent FOREIGN KEY (CreatedEventId) REFERENCES dbo.Event(EventId)
);
GO

CREATE TABLE dbo.ApprovalRequirementType (
    ApprovalRequirementTypeId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ApprovalRequirementType PRIMARY KEY,
    RequirementTypeCode nvarchar(100) NOT NULL,
    RequirementTypeName nvarchar(200) NOT NULL,
    CONSTRAINT UQ_ApprovalRequirementType_Code UNIQUE (RequirementTypeCode)
);
GO

CREATE TABLE dbo.ApprovalRequirement (
    ApprovalRequirementId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_ApprovalRequirement PRIMARY KEY,
    AuthorizationRequestId bigint NOT NULL,
    ApprovalRequirementTypeId int NOT NULL,
    RequiredRoleId bigint NULL,
    RequiredPartyId bigint NULL,
    RequiredCount int NOT NULL,
    CandidatePoolPolicyId bigint NULL,
    IsSequential bit NOT NULL,
    SequenceNumber int NULL,
    CONSTRAINT CK_ApprovalRequirement_Count CHECK (RequiredCount > 0),
    CONSTRAINT CK_ApprovalRequirement_Target CHECK (RequiredRoleId IS NOT NULL OR RequiredPartyId IS NOT NULL OR CandidatePoolPolicyId IS NOT NULL),
    CONSTRAINT FK_ApprovalRequirement_Request FOREIGN KEY (AuthorizationRequestId) REFERENCES dbo.AuthorizationRequest(AuthorizationRequestId),
    CONSTRAINT FK_ApprovalRequirement_Type FOREIGN KEY (ApprovalRequirementTypeId) REFERENCES dbo.ApprovalRequirementType(ApprovalRequirementTypeId),
    CONSTRAINT FK_ApprovalRequirement_Role FOREIGN KEY (RequiredRoleId) REFERENCES dbo.Role(RoleId),
    CONSTRAINT FK_ApprovalRequirement_Party FOREIGN KEY (RequiredPartyId) REFERENCES dbo.Party(PartyId),
    CONSTRAINT FK_ApprovalRequirement_CandidatePolicy FOREIGN KEY (CandidatePoolPolicyId) REFERENCES dbo.Policy(PolicyId)
);
GO

CREATE TABLE dbo.ApprovalDecisionType (
    ApprovalDecisionTypeId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ApprovalDecisionType PRIMARY KEY,
    DecisionTypeCode nvarchar(100) NOT NULL,
    DecisionTypeName nvarchar(200) NOT NULL,
    CONSTRAINT UQ_ApprovalDecisionType_Code UNIQUE (DecisionTypeCode)
);
GO

CREATE TABLE dbo.ApprovalDecision (
    ApprovalDecisionId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_ApprovalDecision PRIMARY KEY,
    AuthorizationRequestId bigint NOT NULL,
    ApprovalRequirementId bigint NOT NULL,
    DecisionEventId bigint NOT NULL,
    ApproverPartyId bigint NOT NULL,
    ApprovalDecisionTypeId int NOT NULL,
    DecisionUtc datetime2(7) NOT NULL,
    DecisionReason nvarchar(1000) NULL,
    ExpiresUtc datetime2(7) NULL,
    CONSTRAINT UQ_ApprovalDecision_Event UNIQUE (DecisionEventId),
    CONSTRAINT UQ_ApprovalDecision_ApproverRequirement UNIQUE (ApprovalRequirementId, ApproverPartyId),
    CONSTRAINT FK_ApprovalDecision_Request FOREIGN KEY (AuthorizationRequestId) REFERENCES dbo.AuthorizationRequest(AuthorizationRequestId),
    CONSTRAINT FK_ApprovalDecision_Requirement FOREIGN KEY (ApprovalRequirementId) REFERENCES dbo.ApprovalRequirement(ApprovalRequirementId),
    CONSTRAINT FK_ApprovalDecision_Event FOREIGN KEY (DecisionEventId) REFERENCES dbo.Event(EventId),
    CONSTRAINT FK_ApprovalDecision_Approver FOREIGN KEY (ApproverPartyId) REFERENCES dbo.Party(PartyId),
    CONSTRAINT FK_ApprovalDecision_Type FOREIGN KEY (ApprovalDecisionTypeId) REFERENCES dbo.ApprovalDecisionType(ApprovalDecisionTypeId)
);
GO

CREATE TABLE dbo.AuthorizationState (
    AuthorizationStateId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_AuthorizationState PRIMARY KEY,
    AuthorizationStateCode nvarchar(100) NOT NULL,
    AuthorizationStateName nvarchar(200) NOT NULL,
    IsTerminal bit NOT NULL,
    CONSTRAINT UQ_AuthorizationState_Code UNIQUE (AuthorizationStateCode)
);
GO

CREATE TABLE dbo.AuthorizationProjection (
    AuthorizationRequestId bigint NOT NULL CONSTRAINT PK_AuthorizationProjection PRIMARY KEY,
    AuthorizationStateId int NOT NULL,
    DerivedFromEventId bigint NOT NULL,
    DerivedUtc datetime2(7) NOT NULL,
    CONSTRAINT FK_AuthorizationProjection_Request FOREIGN KEY (AuthorizationRequestId) REFERENCES dbo.AuthorizationRequest(AuthorizationRequestId),
    CONSTRAINT FK_AuthorizationProjection_State FOREIGN KEY (AuthorizationStateId) REFERENCES dbo.AuthorizationState(AuthorizationStateId),
    CONSTRAINT FK_AuthorizationProjection_Event FOREIGN KEY (DerivedFromEventId) REFERENCES dbo.Event(EventId)
);
GO

CREATE TABLE dbo.EscalationRule (
    EscalationRuleId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_EscalationRule PRIMARY KEY,
    PolicyId bigint NOT NULL,
    AfterMinutes int NOT NULL,
    EscalateToRoleId bigint NULL,
    EscalateToPartyId bigint NULL,
    IsActive bit NOT NULL CONSTRAINT DF_EscalationRule_IsActive DEFAULT (1),
    CONSTRAINT CK_EscalationRule_Minutes CHECK (AfterMinutes > 0),
    CONSTRAINT CK_EscalationRule_Target CHECK (EscalateToRoleId IS NOT NULL OR EscalateToPartyId IS NOT NULL),
    CONSTRAINT FK_EscalationRule_Policy FOREIGN KEY (PolicyId) REFERENCES dbo.Policy(PolicyId),
    CONSTRAINT FK_EscalationRule_Role FOREIGN KEY (EscalateToRoleId) REFERENCES dbo.Role(RoleId),
    CONSTRAINT FK_EscalationRule_Party FOREIGN KEY (EscalateToPartyId) REFERENCES dbo.Party(PartyId)
);
GO

CREATE TABLE dbo.EmergencyOverride (
    EmergencyOverrideId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_EmergencyOverride PRIMARY KEY,
    AuthorizationRequestId bigint NULL,
    OverrideEventId bigint NOT NULL,
    OverrideByPartyId bigint NOT NULL,
    OverrideReason nvarchar(1000) NOT NULL,
    RequiresPostReview bit NOT NULL,
    PostReviewDueUtc datetime2(7) NULL,
    CONSTRAINT UQ_EmergencyOverride_Event UNIQUE (OverrideEventId),
    CONSTRAINT FK_EmergencyOverride_Request FOREIGN KEY (AuthorizationRequestId) REFERENCES dbo.AuthorizationRequest(AuthorizationRequestId),
    CONSTRAINT FK_EmergencyOverride_Event FOREIGN KEY (OverrideEventId) REFERENCES dbo.Event(EventId),
    CONSTRAINT FK_EmergencyOverride_Party FOREIGN KEY (OverrideByPartyId) REFERENCES dbo.Party(PartyId)
);
GO

CREATE TABLE dbo.BusinessCalendar (
    BusinessCalendarId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_BusinessCalendar PRIMARY KEY,
    OrganizationId bigint NOT NULL,
    SiteId bigint NULL,
    CalendarCode nvarchar(100) NOT NULL,
    CalendarName nvarchar(200) NOT NULL,
    TimeZoneId nvarchar(100) NOT NULL,
    IsActive bit NOT NULL CONSTRAINT DF_BusinessCalendar_IsActive DEFAULT (1),
    CONSTRAINT UQ_BusinessCalendar_Organization_Code UNIQUE (OrganizationId, CalendarCode),
    CONSTRAINT FK_BusinessCalendar_Organization FOREIGN KEY (OrganizationId) REFERENCES dbo.Organization(OrganizationId),
    CONSTRAINT FK_BusinessCalendar_Site FOREIGN KEY (SiteId) REFERENCES dbo.Site(SiteId)
);
GO

CREATE TABLE dbo.BusinessHoursRule (
    BusinessHoursRuleId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_BusinessHoursRule PRIMARY KEY,
    BusinessCalendarId bigint NOT NULL,
    DayOfWeek tinyint NOT NULL,
    StartLocalTime time(0) NULL,
    EndLocalTime time(0) NULL,
    IsClosed bit NOT NULL,
    CONSTRAINT CK_BusinessHoursRule_Day CHECK (DayOfWeek BETWEEN 1 AND 7),
    CONSTRAINT CK_BusinessHoursRule_Time CHECK (IsClosed = 1 OR (StartLocalTime IS NOT NULL AND EndLocalTime IS NOT NULL AND EndLocalTime > StartLocalTime)),
    CONSTRAINT FK_BusinessHoursRule_Calendar FOREIGN KEY (BusinessCalendarId) REFERENCES dbo.BusinessCalendar(BusinessCalendarId)
);
GO

CREATE TABLE dbo.CalendarExceptionType (
    CalendarExceptionTypeId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_CalendarExceptionType PRIMARY KEY,
    ExceptionTypeCode nvarchar(100) NOT NULL,
    ExceptionTypeName nvarchar(200) NOT NULL,
    CONSTRAINT UQ_CalendarExceptionType_Code UNIQUE (ExceptionTypeCode)
);
GO

CREATE TABLE dbo.CalendarException (
    CalendarExceptionId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_CalendarException PRIMARY KEY,
    BusinessCalendarId bigint NOT NULL,
    ExceptionDate date NOT NULL,
    CalendarExceptionTypeId int NOT NULL,
    StartLocalTime time(0) NULL,
    EndLocalTime time(0) NULL,
    Description nvarchar(500) NULL,
    CONSTRAINT UQ_CalendarException UNIQUE (BusinessCalendarId, ExceptionDate, StartLocalTime, EndLocalTime),
    CONSTRAINT FK_CalendarException_Calendar FOREIGN KEY (BusinessCalendarId) REFERENCES dbo.BusinessCalendar(BusinessCalendarId),
    CONSTRAINT FK_CalendarException_Type FOREIGN KEY (CalendarExceptionTypeId) REFERENCES dbo.CalendarExceptionType(CalendarExceptionTypeId)
);
GO

CREATE TABLE dbo.AlertType (
    AlertTypeId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_AlertType PRIMARY KEY,
    AlertTypeCode nvarchar(100) NOT NULL,
    AlertTypeName nvarchar(200) NOT NULL,
    CONSTRAINT UQ_AlertType_Code UNIQUE (AlertTypeCode)
);
GO

CREATE TABLE dbo.AlertSeverity (
    AlertSeverityId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_AlertSeverity PRIMARY KEY,
    SeverityCode nvarchar(100) NOT NULL,
    SeverityRank int NOT NULL,
    CONSTRAINT UQ_AlertSeverity_Code UNIQUE (SeverityCode),
    CONSTRAINT UQ_AlertSeverity_Rank UNIQUE (SeverityRank)
);
GO

CREATE TABLE dbo.AlertRule (
    AlertRuleId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_AlertRule PRIMARY KEY,
    OrganizationId bigint NOT NULL,
    AlertRuleCode nvarchar(100) NOT NULL,
    AlertTypeId int NOT NULL,
    PolicyId bigint NULL,
    IsActive bit NOT NULL CONSTRAINT DF_AlertRule_IsActive DEFAULT (1),
    CONSTRAINT UQ_AlertRule_Organization_Code UNIQUE (OrganizationId, AlertRuleCode),
    CONSTRAINT FK_AlertRule_Organization FOREIGN KEY (OrganizationId) REFERENCES dbo.Organization(OrganizationId),
    CONSTRAINT FK_AlertRule_Type FOREIGN KEY (AlertTypeId) REFERENCES dbo.AlertType(AlertTypeId),
    CONSTRAINT FK_AlertRule_Policy FOREIGN KEY (PolicyId) REFERENCES dbo.Policy(PolicyId)
);
GO

CREATE TABLE dbo.Alert (
    AlertId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Alert PRIMARY KEY,
    OrganizationId bigint NOT NULL,
    AlertTypeId int NOT NULL,
    TriggeredByEventId bigint NULL,
    TargetEntityTypeId int NOT NULL,
    TargetEntityId bigint NOT NULL,
    SeverityId int NOT NULL,
    CreatedUtc datetime2(7) NOT NULL,
    ResolvedUtc datetime2(7) NULL,
    ResolvedByPartyId bigint NULL,
    ResolutionNote nvarchar(1000) NULL,
    CONSTRAINT CK_Alert_ResolvedAfterCreated CHECK (ResolvedUtc IS NULL OR ResolvedUtc > CreatedUtc),
    CONSTRAINT FK_Alert_Organization FOREIGN KEY (OrganizationId) REFERENCES dbo.Organization(OrganizationId),
    CONSTRAINT FK_Alert_Type FOREIGN KEY (AlertTypeId) REFERENCES dbo.AlertType(AlertTypeId),
    CONSTRAINT FK_Alert_Event FOREIGN KEY (TriggeredByEventId) REFERENCES dbo.Event(EventId),
    CONSTRAINT FK_Alert_TargetEntityType FOREIGN KEY (TargetEntityTypeId) REFERENCES dbo.EntityType(EntityTypeId),
    CONSTRAINT FK_Alert_Severity FOREIGN KEY (SeverityId) REFERENCES dbo.AlertSeverity(AlertSeverityId),
    CONSTRAINT FK_Alert_ResolvedBy FOREIGN KEY (ResolvedByPartyId) REFERENCES dbo.Party(PartyId)
);
GO

CREATE TABLE dbo.ContactMethodType (
    ContactMethodTypeId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ContactMethodType PRIMARY KEY,
    ContactMethodTypeCode nvarchar(100) NOT NULL,
    ContactMethodTypeName nvarchar(200) NOT NULL,
    CONSTRAINT UQ_ContactMethodType_Code UNIQUE (ContactMethodTypeCode)
);
GO

CREATE TABLE dbo.PartyContactMethod (
    PartyContactMethodId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_PartyContactMethod PRIMARY KEY,
    PartyId bigint NOT NULL,
    ContactMethodTypeId int NOT NULL,
    ContactValue nvarchar(512) NOT NULL,
    IsPrimary bit NOT NULL,
    VerifiedUtc datetime2(7) NULL,
    IsActive bit NOT NULL CONSTRAINT DF_PartyContactMethod_IsActive DEFAULT (1),
    CONSTRAINT FK_PartyContactMethod_Party FOREIGN KEY (PartyId) REFERENCES dbo.Party(PartyId),
    CONSTRAINT FK_PartyContactMethod_Type FOREIGN KEY (ContactMethodTypeId) REFERENCES dbo.ContactMethodType(ContactMethodTypeId)
);
GO
CREATE UNIQUE INDEX UX_PartyContactMethod_ActiveValue ON dbo.PartyContactMethod(PartyId, ContactMethodTypeId, ContactValue) WHERE IsActive = 1;
CREATE UNIQUE INDEX UX_PartyContactMethod_Primary ON dbo.PartyContactMethod(PartyId, ContactMethodTypeId) WHERE IsPrimary = 1 AND IsActive = 1;
GO

CREATE TABLE dbo.NotificationType (
    NotificationTypeId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_NotificationType PRIMARY KEY,
    NotificationTypeCode nvarchar(100) NOT NULL,
    NotificationTypeName nvarchar(200) NOT NULL,
    CONSTRAINT UQ_NotificationType_Code UNIQUE (NotificationTypeCode)
);
GO

CREATE TABLE dbo.NotificationTemplate (
    NotificationTemplateId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_NotificationTemplate PRIMARY KEY,
    OrganizationId bigint NOT NULL,
    NotificationTypeId int NOT NULL,
    TemplateCode nvarchar(100) NOT NULL,
    SubjectTemplate nvarchar(500) NOT NULL,
    BodyTemplate nvarchar(max) NOT NULL,
    IsActive bit NOT NULL CONSTRAINT DF_NotificationTemplate_IsActive DEFAULT (1),
    CONSTRAINT UQ_NotificationTemplate_Organization_Code UNIQUE (OrganizationId, TemplateCode),
    CONSTRAINT FK_NotificationTemplate_Organization FOREIGN KEY (OrganizationId) REFERENCES dbo.Organization(OrganizationId),
    CONSTRAINT FK_NotificationTemplate_Type FOREIGN KEY (NotificationTypeId) REFERENCES dbo.NotificationType(NotificationTypeId)
);
GO

CREATE TABLE dbo.NotificationStatus (
    NotificationStatusId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_NotificationStatus PRIMARY KEY,
    StatusCode nvarchar(100) NOT NULL,
    StatusName nvarchar(200) NOT NULL,
    CONSTRAINT UQ_NotificationStatus_Code UNIQUE (StatusCode)
);
GO

CREATE TABLE dbo.Notification (
    NotificationId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Notification PRIMARY KEY,
    OrganizationId bigint NOT NULL,
    AlertId bigint NULL,
    RecipientPartyId bigint NULL,
    RecipientEndpointId bigint NULL,
    NotificationTypeId int NOT NULL,
    NotificationStatusId int NOT NULL,
    CreatedUtc datetime2(7) NOT NULL,
    SentUtc datetime2(7) NULL,
    DeliveryPayloadJson nvarchar(max) NULL,
    CONSTRAINT CK_Notification_Recipient CHECK (RecipientPartyId IS NOT NULL OR RecipientEndpointId IS NOT NULL),
    CONSTRAINT FK_Notification_Organization FOREIGN KEY (OrganizationId) REFERENCES dbo.Organization(OrganizationId),
    CONSTRAINT FK_Notification_Alert FOREIGN KEY (AlertId) REFERENCES dbo.Alert(AlertId),
    CONSTRAINT FK_Notification_RecipientParty FOREIGN KEY (RecipientPartyId) REFERENCES dbo.Party(PartyId),
    CONSTRAINT FK_Notification_Endpoint FOREIGN KEY (RecipientEndpointId) REFERENCES dbo.PartyContactMethod(PartyContactMethodId),
    CONSTRAINT FK_Notification_Type FOREIGN KEY (NotificationTypeId) REFERENCES dbo.NotificationType(NotificationTypeId),
    CONSTRAINT FK_Notification_Status FOREIGN KEY (NotificationStatusId) REFERENCES dbo.NotificationStatus(NotificationStatusId)
);
GO

CREATE TABLE dbo.InventorySessionType (
    InventorySessionTypeId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_InventorySessionType PRIMARY KEY,
    SessionTypeCode nvarchar(100) NOT NULL,
    SessionTypeName nvarchar(200) NOT NULL,
    CONSTRAINT UQ_InventorySessionType_Code UNIQUE (SessionTypeCode)
);
GO

CREATE TABLE dbo.InventorySession (
    InventorySessionId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_InventorySession PRIMARY KEY,
    OrganizationId bigint NOT NULL,
    SiteId bigint NOT NULL,
    InventorySessionTypeId int NOT NULL,
    StartedByPartyId bigint NOT NULL,
    StartedEventId bigint NOT NULL,
    StartedUtc datetime2(7) NOT NULL,
    CompletedEventId bigint NULL,
    CompletedUtc datetime2(7) NULL,
    CONSTRAINT CK_InventorySession_CompletedAfterStarted CHECK (CompletedUtc IS NULL OR CompletedUtc > StartedUtc),
    CONSTRAINT FK_InventorySession_Organization FOREIGN KEY (OrganizationId) REFERENCES dbo.Organization(OrganizationId),
    CONSTRAINT FK_InventorySession_Site FOREIGN KEY (SiteId) REFERENCES dbo.Site(SiteId),
    CONSTRAINT FK_InventorySession_Type FOREIGN KEY (InventorySessionTypeId) REFERENCES dbo.InventorySessionType(InventorySessionTypeId),
    CONSTRAINT FK_InventorySession_StartedBy FOREIGN KEY (StartedByPartyId) REFERENCES dbo.Party(PartyId),
    CONSTRAINT FK_InventorySession_StartedEvent FOREIGN KEY (StartedEventId) REFERENCES dbo.Event(EventId),
    CONSTRAINT FK_InventorySession_CompletedEvent FOREIGN KEY (CompletedEventId) REFERENCES dbo.Event(EventId)
);
GO

CREATE TABLE dbo.InventoryScope (
    InventoryScopeId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_InventoryScope PRIMARY KEY,
    InventorySessionId bigint NOT NULL,
    ScopeTypeId int NOT NULL,
    ScopeEntityId bigint NOT NULL,
    CONSTRAINT FK_InventoryScope_Session FOREIGN KEY (InventorySessionId) REFERENCES dbo.InventorySession(InventorySessionId),
    CONSTRAINT FK_InventoryScope_ScopeType FOREIGN KEY (ScopeTypeId) REFERENCES dbo.AuthorizationScopeType(AuthorizationScopeTypeId)
);
GO

CREATE TABLE dbo.InventoryCountResult (
    InventoryCountResultId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_InventoryCountResult PRIMARY KEY,
    ResultCode nvarchar(100) NOT NULL,
    ResultName nvarchar(200) NOT NULL,
    CONSTRAINT UQ_InventoryCountResult_Code UNIQUE (ResultCode)
);
GO

CREATE TABLE dbo.InventoryCount (
    InventoryCountId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_InventoryCount PRIMARY KEY,
    InventorySessionId bigint NOT NULL,
    KeyAssetId bigint NOT NULL,
    CountedByPartyId bigint NOT NULL,
    CountEventId bigint NOT NULL,
    ObservedStorageLocationId bigint NULL,
    ObservedCustodianPartyId bigint NULL,
    ObservedUtc datetime2(7) NOT NULL,
    InventoryCountResultId int NOT NULL,
    CONSTRAINT UQ_InventoryCount_Event UNIQUE (CountEventId),
    CONSTRAINT FK_InventoryCount_Session FOREIGN KEY (InventorySessionId) REFERENCES dbo.InventorySession(InventorySessionId),
    CONSTRAINT FK_InventoryCount_KeyAsset FOREIGN KEY (KeyAssetId) REFERENCES dbo.KeyAsset(KeyAssetId),
    CONSTRAINT FK_InventoryCount_CountedBy FOREIGN KEY (CountedByPartyId) REFERENCES dbo.Party(PartyId),
    CONSTRAINT FK_InventoryCount_Event FOREIGN KEY (CountEventId) REFERENCES dbo.Event(EventId),
    CONSTRAINT FK_InventoryCount_Storage FOREIGN KEY (ObservedStorageLocationId) REFERENCES dbo.StorageLocation(StorageLocationId),
    CONSTRAINT FK_InventoryCount_Custodian FOREIGN KEY (ObservedCustodianPartyId) REFERENCES dbo.Party(PartyId),
    CONSTRAINT FK_InventoryCount_Result FOREIGN KEY (InventoryCountResultId) REFERENCES dbo.InventoryCountResult(InventoryCountResultId)
);
GO

CREATE TABLE dbo.InventoryDiscrepancyType (
    InventoryDiscrepancyTypeId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_InventoryDiscrepancyType PRIMARY KEY,
    DiscrepancyTypeCode nvarchar(100) NOT NULL,
    DiscrepancyTypeName nvarchar(200) NOT NULL,
    CONSTRAINT UQ_InventoryDiscrepancyType_Code UNIQUE (DiscrepancyTypeCode)
);
GO

CREATE TABLE dbo.InventoryDiscrepancy (
    InventoryDiscrepancyId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_InventoryDiscrepancy PRIMARY KEY,
    InventorySessionId bigint NOT NULL,
    KeyAssetId bigint NOT NULL,
    DetectedByInventoryCountId bigint NOT NULL,
    InventoryDiscrepancyTypeId int NOT NULL,
    DetectedEventId bigint NOT NULL,
    DetectedUtc datetime2(7) NOT NULL,
    ResolvedEventId bigint NULL,
    ResolvedUtc datetime2(7) NULL,
    CONSTRAINT CK_InventoryDiscrepancy_ResolvedAfterDetected CHECK (ResolvedUtc IS NULL OR ResolvedUtc > DetectedUtc),
    CONSTRAINT FK_InventoryDiscrepancy_Session FOREIGN KEY (InventorySessionId) REFERENCES dbo.InventorySession(InventorySessionId),
    CONSTRAINT FK_InventoryDiscrepancy_KeyAsset FOREIGN KEY (KeyAssetId) REFERENCES dbo.KeyAsset(KeyAssetId),
    CONSTRAINT FK_InventoryDiscrepancy_Count FOREIGN KEY (DetectedByInventoryCountId) REFERENCES dbo.InventoryCount(InventoryCountId),
    CONSTRAINT FK_InventoryDiscrepancy_Type FOREIGN KEY (InventoryDiscrepancyTypeId) REFERENCES dbo.InventoryDiscrepancyType(InventoryDiscrepancyTypeId),
    CONSTRAINT FK_InventoryDiscrepancy_DetectedEvent FOREIGN KEY (DetectedEventId) REFERENCES dbo.Event(EventId),
    CONSTRAINT FK_InventoryDiscrepancy_ResolvedEvent FOREIGN KEY (ResolvedEventId) REFERENCES dbo.Event(EventId)
);
GO

CREATE TABLE dbo.InvestigationOutcome (
    InvestigationOutcomeId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_InvestigationOutcome PRIMARY KEY,
    OutcomeCode nvarchar(100) NOT NULL,
    OutcomeName nvarchar(200) NOT NULL,
    CONSTRAINT UQ_InvestigationOutcome_Code UNIQUE (OutcomeCode)
);
GO

CREATE TABLE dbo.Investigation (
    InvestigationId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Investigation PRIMARY KEY,
    OrganizationId bigint NOT NULL,
    InventoryDiscrepancyId bigint NULL,
    AlertId bigint NULL,
    OpenedByPartyId bigint NOT NULL,
    OpenedEventId bigint NOT NULL,
    OpenedUtc datetime2(7) NOT NULL,
    ClosedEventId bigint NULL,
    ClosedUtc datetime2(7) NULL,
    InvestigationOutcomeId int NULL,
    CONSTRAINT CK_Investigation_Source CHECK (InventoryDiscrepancyId IS NOT NULL OR AlertId IS NOT NULL),
    CONSTRAINT CK_Investigation_ClosedAfterOpened CHECK (ClosedUtc IS NULL OR ClosedUtc > OpenedUtc),
    CONSTRAINT FK_Investigation_Organization FOREIGN KEY (OrganizationId) REFERENCES dbo.Organization(OrganizationId),
    CONSTRAINT FK_Investigation_Discrepancy FOREIGN KEY (InventoryDiscrepancyId) REFERENCES dbo.InventoryDiscrepancy(InventoryDiscrepancyId),
    CONSTRAINT FK_Investigation_Alert FOREIGN KEY (AlertId) REFERENCES dbo.Alert(AlertId),
    CONSTRAINT FK_Investigation_OpenedBy FOREIGN KEY (OpenedByPartyId) REFERENCES dbo.Party(PartyId),
    CONSTRAINT FK_Investigation_OpenedEvent FOREIGN KEY (OpenedEventId) REFERENCES dbo.Event(EventId),
    CONSTRAINT FK_Investigation_ClosedEvent FOREIGN KEY (ClosedEventId) REFERENCES dbo.Event(EventId),
    CONSTRAINT FK_Investigation_Outcome FOREIGN KEY (InvestigationOutcomeId) REFERENCES dbo.InvestigationOutcome(InvestigationOutcomeId)
);
GO

CREATE TABLE dbo.MaintenanceType (
    MaintenanceTypeId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_MaintenanceType PRIMARY KEY,
    MaintenanceTypeCode nvarchar(100) NOT NULL,
    MaintenanceTypeName nvarchar(200) NOT NULL,
    CONSTRAINT UQ_MaintenanceType_Code UNIQUE (MaintenanceTypeCode)
);
GO

CREATE TABLE dbo.MaintenancePriority (
    MaintenancePriorityId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_MaintenancePriority PRIMARY KEY,
    PriorityCode nvarchar(100) NOT NULL,
    PriorityRank int NOT NULL,
    CONSTRAINT UQ_MaintenancePriority_Code UNIQUE (PriorityCode),
    CONSTRAINT UQ_MaintenancePriority_Rank UNIQUE (PriorityRank)
);
GO

CREATE TABLE dbo.MaintenanceOutcome (
    MaintenanceOutcomeId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_MaintenanceOutcome PRIMARY KEY,
    OutcomeCode nvarchar(100) NOT NULL,
    OutcomeName nvarchar(200) NOT NULL,
    CONSTRAINT UQ_MaintenanceOutcome_Code UNIQUE (OutcomeCode)
);
GO

CREATE TABLE dbo.MaintenanceRequest (
    MaintenanceRequestId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_MaintenanceRequest PRIMARY KEY,
    OrganizationId bigint NOT NULL,
    KeyAssetId bigint NOT NULL,
    RequestedByPartyId bigint NOT NULL,
    MaintenanceTypeId int NOT NULL,
    MaintenancePriorityId int NOT NULL,
    RequestEventId bigint NOT NULL,
    RequestedUtc datetime2(7) NOT NULL,
    Description nvarchar(1000) NULL,
    CONSTRAINT FK_MaintenanceRequest_Organization FOREIGN KEY (OrganizationId) REFERENCES dbo.Organization(OrganizationId),
    CONSTRAINT FK_MaintenanceRequest_KeyAsset FOREIGN KEY (KeyAssetId) REFERENCES dbo.KeyAsset(KeyAssetId),
    CONSTRAINT FK_MaintenanceRequest_RequestedBy FOREIGN KEY (RequestedByPartyId) REFERENCES dbo.Party(PartyId),
    CONSTRAINT FK_MaintenanceRequest_Type FOREIGN KEY (MaintenanceTypeId) REFERENCES dbo.MaintenanceType(MaintenanceTypeId),
    CONSTRAINT FK_MaintenanceRequest_Priority FOREIGN KEY (MaintenancePriorityId) REFERENCES dbo.MaintenancePriority(MaintenancePriorityId),
    CONSTRAINT FK_MaintenanceRequest_Event FOREIGN KEY (RequestEventId) REFERENCES dbo.Event(EventId)
);
GO

CREATE TABLE dbo.MaintenanceExecution (
    MaintenanceExecutionId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_MaintenanceExecution PRIMARY KEY,
    MaintenanceRequestId bigint NOT NULL,
    PerformedByPartyId bigint NOT NULL,
    ExecutionEventId bigint NOT NULL,
    StartedUtc datetime2(7) NOT NULL,
    CompletedUtc datetime2(7) NULL,
    MaintenanceOutcomeId int NULL,
    CostAmount decimal(19,4) NULL,
    CurrencyCode char(3) NULL,
    CONSTRAINT UQ_MaintenanceExecution_Event UNIQUE (ExecutionEventId),
    CONSTRAINT CK_MaintenanceExecution_CompletedAfterStarted CHECK (CompletedUtc IS NULL OR CompletedUtc > StartedUtc),
    CONSTRAINT CK_MaintenanceExecution_Cost CHECK (CostAmount IS NULL OR CostAmount >= 0),
    CONSTRAINT FK_MaintenanceExecution_Request FOREIGN KEY (MaintenanceRequestId) REFERENCES dbo.MaintenanceRequest(MaintenanceRequestId),
    CONSTRAINT FK_MaintenanceExecution_PerformedBy FOREIGN KEY (PerformedByPartyId) REFERENCES dbo.Party(PartyId),
    CONSTRAINT FK_MaintenanceExecution_Event FOREIGN KEY (ExecutionEventId) REFERENCES dbo.Event(EventId),
    CONSTRAINT FK_MaintenanceExecution_Outcome FOREIGN KEY (MaintenanceOutcomeId) REFERENCES dbo.MaintenanceOutcome(MaintenanceOutcomeId)
);
GO

CREATE TABLE dbo.CylinderReplacement (
    CylinderReplacementId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_CylinderReplacement PRIMARY KEY,
    MaintenanceExecutionId bigint NOT NULL,
    OldCylinderIdentifier nvarchar(100) NOT NULL,
    NewCylinderIdentifier nvarchar(100) NOT NULL,
    ReplacementReason nvarchar(1000) NULL,
    CONSTRAINT UQ_CylinderReplacement_Execution UNIQUE (MaintenanceExecutionId),
    CONSTRAINT CK_CylinderReplacement_Different CHECK (OldCylinderIdentifier <> NewCylinderIdentifier),
    CONSTRAINT FK_CylinderReplacement_Execution FOREIGN KEY (MaintenanceExecutionId) REFERENCES dbo.MaintenanceExecution(MaintenanceExecutionId)
);
GO

CREATE TABLE dbo.RekeyAction (
    RekeyActionId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_RekeyAction PRIMARY KEY,
    MaintenanceExecutionId bigint NOT NULL,
    OldBittingHash varbinary(64) NULL,
    NewBittingHash varbinary(64) NULL,
    RekeyReason nvarchar(1000) NULL,
    CONSTRAINT UQ_RekeyAction_Execution UNIQUE (MaintenanceExecutionId),
    CONSTRAINT CK_RekeyAction_Different CHECK (OldBittingHash IS NULL OR NewBittingHash IS NULL OR OldBittingHash <> NewBittingHash),
    CONSTRAINT FK_RekeyAction_Execution FOREIGN KEY (MaintenanceExecutionId) REFERENCES dbo.MaintenanceExecution(MaintenanceExecutionId)
);
GO

CREATE TABLE dbo.DuplicateCreation (
    DuplicateCreationId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_DuplicateCreation PRIMARY KEY,
    MaintenanceExecutionId bigint NOT NULL,
    SourceKeyAssetId bigint NOT NULL,
    DuplicateKeyAssetId bigint NOT NULL,
    DuplicateCreatedEventId bigint NOT NULL,
    CONSTRAINT CK_DuplicateCreation_DifferentKeys CHECK (SourceKeyAssetId <> DuplicateKeyAssetId),
    CONSTRAINT FK_DuplicateCreation_Execution FOREIGN KEY (MaintenanceExecutionId) REFERENCES dbo.MaintenanceExecution(MaintenanceExecutionId),
    CONSTRAINT FK_DuplicateCreation_SourceKey FOREIGN KEY (SourceKeyAssetId) REFERENCES dbo.KeyAsset(KeyAssetId),
    CONSTRAINT FK_DuplicateCreation_DuplicateKey FOREIGN KEY (DuplicateKeyAssetId) REFERENCES dbo.KeyAsset(KeyAssetId),
    CONSTRAINT FK_DuplicateCreation_Event FOREIGN KEY (DuplicateCreatedEventId) REFERENCES dbo.Event(EventId)
);
GO

CREATE TABLE dbo.Retirement (
    RetirementId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Retirement PRIMARY KEY,
    MaintenanceExecutionId bigint NOT NULL,
    RetirementEventId bigint NOT NULL,
    RetirementReason nvarchar(1000) NOT NULL,
    CONSTRAINT UQ_Retirement_Execution UNIQUE (MaintenanceExecutionId),
    CONSTRAINT FK_Retirement_Execution FOREIGN KEY (MaintenanceExecutionId) REFERENCES dbo.MaintenanceExecution(MaintenanceExecutionId),
    CONSTRAINT FK_Retirement_Event FOREIGN KEY (RetirementEventId) REFERENCES dbo.Event(EventId)
);
GO

CREATE TABLE dbo.DestructionMethod (
    DestructionMethodId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_DestructionMethod PRIMARY KEY,
    MethodCode nvarchar(100) NOT NULL,
    MethodName nvarchar(200) NOT NULL,
    CONSTRAINT UQ_DestructionMethod_Code UNIQUE (MethodCode)
);
GO

CREATE TABLE dbo.Destruction (
    DestructionId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Destruction PRIMARY KEY,
    MaintenanceExecutionId bigint NOT NULL,
    DestructionEventId bigint NOT NULL,
    DestroyedByPartyId bigint NOT NULL,
    WitnessPartyId bigint NULL,
    DestructionMethodId int NOT NULL,
    DestroyedUtc datetime2(7) NOT NULL,
    CONSTRAINT UQ_Destruction_Execution UNIQUE (MaintenanceExecutionId),
    CONSTRAINT FK_Destruction_Execution FOREIGN KEY (MaintenanceExecutionId) REFERENCES dbo.MaintenanceExecution(MaintenanceExecutionId),
    CONSTRAINT FK_Destruction_Event FOREIGN KEY (DestructionEventId) REFERENCES dbo.Event(EventId),
    CONSTRAINT FK_Destruction_DestroyedBy FOREIGN KEY (DestroyedByPartyId) REFERENCES dbo.Party(PartyId),
    CONSTRAINT FK_Destruction_Witness FOREIGN KEY (WitnessPartyId) REFERENCES dbo.Party(PartyId),
    CONSTRAINT FK_Destruction_Method FOREIGN KEY (DestructionMethodId) REFERENCES dbo.DestructionMethod(DestructionMethodId)
);
GO

CREATE TABLE dbo.DeviceEventType (
    DeviceEventTypeId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_DeviceEventType PRIMARY KEY,
    DeviceEventTypeCode nvarchar(100) NOT NULL,
    DeviceEventTypeName nvarchar(200) NOT NULL,
    CONSTRAINT UQ_DeviceEventType_Code UNIQUE (DeviceEventTypeCode)
);
GO

CREATE TABLE dbo.DeviceEvent (
    DeviceEventId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_DeviceEvent PRIMARY KEY,
    EventId bigint NOT NULL,
    StorageDeviceId bigint NOT NULL,
    StorageSlotId bigint NULL,
    DeviceEventTypeId int NOT NULL,
    KeyAssetId bigint NULL,
    PartyId bigint NULL,
    DeviceTimestampUtc datetime2(7) NOT NULL,
    ReceivedUtc datetime2(7) NOT NULL,
    RawPayloadJson nvarchar(max) NOT NULL,
    CONSTRAINT UQ_DeviceEvent_Event UNIQUE (EventId),
    CONSTRAINT FK_DeviceEvent_Event FOREIGN KEY (EventId) REFERENCES dbo.Event(EventId),
    CONSTRAINT FK_DeviceEvent_Device FOREIGN KEY (StorageDeviceId) REFERENCES dbo.StorageDevice(StorageDeviceId),
    CONSTRAINT FK_DeviceEvent_Slot FOREIGN KEY (StorageSlotId) REFERENCES dbo.StorageSlot(StorageSlotId),
    CONSTRAINT FK_DeviceEvent_Type FOREIGN KEY (DeviceEventTypeId) REFERENCES dbo.DeviceEventType(DeviceEventTypeId),
    CONSTRAINT FK_DeviceEvent_KeyAsset FOREIGN KEY (KeyAssetId) REFERENCES dbo.KeyAsset(KeyAssetId),
    CONSTRAINT FK_DeviceEvent_Party FOREIGN KEY (PartyId) REFERENCES dbo.Party(PartyId)
);
GO

CREATE TABLE dbo.LogicalOperator (
    LogicalOperatorId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_LogicalOperator PRIMARY KEY,
    OperatorCode nvarchar(20) NOT NULL,
    OperatorName nvarchar(100) NOT NULL,
    CONSTRAINT UQ_LogicalOperator_Code UNIQUE (OperatorCode)
);
GO

CREATE TABLE dbo.ConditionValueType (
    ConditionValueTypeId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ConditionValueType PRIMARY KEY,
    ValueTypeCode nvarchar(100) NOT NULL,
    ValueTypeName nvarchar(200) NOT NULL,
    CONSTRAINT UQ_ConditionValueType_Code UNIQUE (ValueTypeCode)
);
GO

CREATE TABLE dbo.ConditionAttribute (
    ConditionAttributeId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ConditionAttribute PRIMARY KEY,
    AttributeCode nvarchar(150) NOT NULL,
    AttributeName nvarchar(200) NOT NULL,
    ConditionValueTypeId int NOT NULL,
    CONSTRAINT UQ_ConditionAttribute_Code UNIQUE (AttributeCode),
    CONSTRAINT FK_ConditionAttribute_ValueType FOREIGN KEY (ConditionValueTypeId) REFERENCES dbo.ConditionValueType(ConditionValueTypeId)
);
GO

CREATE TABLE dbo.ComparisonOperator (
    ComparisonOperatorId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ComparisonOperator PRIMARY KEY,
    OperatorCode nvarchar(100) NOT NULL,
    OperatorName nvarchar(200) NOT NULL,
    CONSTRAINT UQ_ComparisonOperator_Code UNIQUE (OperatorCode)
);
GO

CREATE TABLE dbo.PolicyConditionGroup (
    PolicyConditionGroupId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_PolicyConditionGroup PRIMARY KEY,
    PolicyId bigint NOT NULL,
    ParentConditionGroupId bigint NULL,
    LogicalOperatorId int NOT NULL,
    SequenceNumber int NOT NULL,
    CONSTRAINT FK_PolicyConditionGroup_Policy FOREIGN KEY (PolicyId) REFERENCES dbo.Policy(PolicyId),
    CONSTRAINT FK_PolicyConditionGroup_Parent FOREIGN KEY (ParentConditionGroupId) REFERENCES dbo.PolicyConditionGroup(PolicyConditionGroupId),
    CONSTRAINT FK_PolicyConditionGroup_Operator FOREIGN KEY (LogicalOperatorId) REFERENCES dbo.LogicalOperator(LogicalOperatorId)
);
GO

CREATE TABLE dbo.PolicyCondition (
    PolicyConditionId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_PolicyCondition PRIMARY KEY,
    PolicyConditionGroupId bigint NOT NULL,
    ConditionAttributeId int NOT NULL,
    ComparisonOperatorId int NOT NULL,
    ConditionValueTypeId int NOT NULL,
    ConditionValue nvarchar(1000) NOT NULL,
    SequenceNumber int NOT NULL,
    CONSTRAINT FK_PolicyCondition_Group FOREIGN KEY (PolicyConditionGroupId) REFERENCES dbo.PolicyConditionGroup(PolicyConditionGroupId),
    CONSTRAINT FK_PolicyCondition_Attribute FOREIGN KEY (ConditionAttributeId) REFERENCES dbo.ConditionAttribute(ConditionAttributeId),
    CONSTRAINT FK_PolicyCondition_ComparisonOperator FOREIGN KEY (ComparisonOperatorId) REFERENCES dbo.ComparisonOperator(ComparisonOperatorId),
    CONSTRAINT FK_PolicyCondition_ValueType FOREIGN KEY (ConditionValueTypeId) REFERENCES dbo.ConditionValueType(ConditionValueTypeId)
);
GO

CREATE TABLE dbo.PolicyActionType (
    PolicyActionTypeId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PolicyActionType PRIMARY KEY,
    ActionTypeCode nvarchar(100) NOT NULL,
    ActionTypeName nvarchar(200) NOT NULL,
    CONSTRAINT UQ_PolicyActionType_Code UNIQUE (ActionTypeCode)
);
GO

CREATE TABLE dbo.PolicyAction (
    PolicyActionId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_PolicyAction PRIMARY KEY,
    PolicyId bigint NOT NULL,
    PolicyActionTypeId int NOT NULL,
    ActionParameterJson nvarchar(max) NOT NULL,
    SequenceNumber int NOT NULL,
    CONSTRAINT FK_PolicyAction_Policy FOREIGN KEY (PolicyId) REFERENCES dbo.Policy(PolicyId),
    CONSTRAINT FK_PolicyAction_Type FOREIGN KEY (PolicyActionTypeId) REFERENCES dbo.PolicyActionType(PolicyActionTypeId)
);
GO

CREATE TABLE dbo.SignatureMethod (
    SignatureMethodId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_SignatureMethod PRIMARY KEY,
    SignatureMethodCode nvarchar(100) NOT NULL,
    SignatureMethodName nvarchar(200) NOT NULL,
    IsAuthenticationMethod bit NOT NULL,
    IsNonRepudiationMethod bit NOT NULL,
    CONSTRAINT UQ_SignatureMethod_Code UNIQUE (SignatureMethodCode)
);
GO

CREATE TABLE dbo.PartyCredential (
    PartyCredentialId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_PartyCredential PRIMARY KEY,
    PartyId bigint NOT NULL,
    SignatureMethodId int NOT NULL,
    CredentialIdentifierHash varbinary(64) NOT NULL,
    CredentialPublicKey nvarchar(max) NULL,
    IssuedUtc datetime2(7) NOT NULL,
    ExpiresUtc datetime2(7) NULL,
    RevokedUtc datetime2(7) NULL,
    IsActive bit NOT NULL CONSTRAINT DF_PartyCredential_IsActive DEFAULT (1),
    CONSTRAINT CK_PartyCredential_ExpiresAfterIssued CHECK (ExpiresUtc IS NULL OR ExpiresUtc > IssuedUtc),
    CONSTRAINT FK_PartyCredential_Party FOREIGN KEY (PartyId) REFERENCES dbo.Party(PartyId),
    CONSTRAINT FK_PartyCredential_SignatureMethod FOREIGN KEY (SignatureMethodId) REFERENCES dbo.SignatureMethod(SignatureMethodId)
);
GO

CREATE TABLE dbo.SignatureVerificationStatus (
    SignatureVerificationStatusId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_SignatureVerificationStatus PRIMARY KEY,
    StatusCode nvarchar(100) NOT NULL,
    StatusName nvarchar(200) NOT NULL,
    CONSTRAINT UQ_SignatureVerificationStatus_Code UNIQUE (StatusCode)
);
GO

CREATE TABLE dbo.EventSignature (
    EventSignatureId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_EventSignature PRIMARY KEY,
    EventId bigint NOT NULL,
    SignedByPartyId bigint NOT NULL,
    PartyCredentialId bigint NULL,
    SignatureMethodId int NOT NULL,
    SignedUtc datetime2(7) NOT NULL,
    SignatureValue varbinary(max) NOT NULL,
    SignaturePayloadHash varbinary(64) NOT NULL,
    SignatureVerificationStatusId int NOT NULL,
    CONSTRAINT UQ_EventSignature_Event_Party_Method UNIQUE (EventId, SignedByPartyId, SignatureMethodId),
    CONSTRAINT FK_EventSignature_Event FOREIGN KEY (EventId) REFERENCES dbo.Event(EventId),
    CONSTRAINT FK_EventSignature_Party FOREIGN KEY (SignedByPartyId) REFERENCES dbo.Party(PartyId),
    CONSTRAINT FK_EventSignature_Credential FOREIGN KEY (PartyCredentialId) REFERENCES dbo.PartyCredential(PartyCredentialId),
    CONSTRAINT FK_EventSignature_Method FOREIGN KEY (SignatureMethodId) REFERENCES dbo.SignatureMethod(SignatureMethodId),
    CONSTRAINT FK_EventSignature_Status FOREIGN KEY (SignatureVerificationStatusId) REFERENCES dbo.SignatureVerificationStatus(SignatureVerificationStatusId)
);
GO

CREATE TABLE dbo.KpiCategory (
    KpiCategoryId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_KpiCategory PRIMARY KEY,
    KpiCategoryCode nvarchar(100) NOT NULL,
    KpiCategoryName nvarchar(200) NOT NULL,
    CONSTRAINT UQ_KpiCategory_Code UNIQUE (KpiCategoryCode)
);
GO

CREATE TABLE dbo.KpiDefinition (
    KpiDefinitionId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_KpiDefinition PRIMARY KEY,
    KpiCode nvarchar(100) NOT NULL,
    KpiName nvarchar(200) NOT NULL,
    KpiCategoryId int NOT NULL,
    Description nvarchar(1000) NULL,
    IsActive bit NOT NULL CONSTRAINT DF_KpiDefinition_IsActive DEFAULT (1),
    CONSTRAINT UQ_KpiDefinition_Code UNIQUE (KpiCode),
    CONSTRAINT FK_KpiDefinition_Category FOREIGN KEY (KpiCategoryId) REFERENCES dbo.KpiCategory(KpiCategoryId)
);
GO

CREATE TABLE dbo.KpiSnapshot (
    KpiSnapshotId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_KpiSnapshot PRIMARY KEY,
    OrganizationId bigint NOT NULL,
    SiteId bigint NULL,
    KpiDefinitionId int NOT NULL,
    SnapshotUtc datetime2(7) NOT NULL,
    MetricValueDecimal decimal(19,4) NOT NULL,
    MetricValueJson nvarchar(max) NULL,
    DerivedThroughEventId bigint NULL,
    CONSTRAINT UQ_KpiSnapshot UNIQUE (OrganizationId, SiteId, KpiDefinitionId, SnapshotUtc),
    CONSTRAINT FK_KpiSnapshot_Organization FOREIGN KEY (OrganizationId) REFERENCES dbo.Organization(OrganizationId),
    CONSTRAINT FK_KpiSnapshot_Site FOREIGN KEY (SiteId) REFERENCES dbo.Site(SiteId),
    CONSTRAINT FK_KpiSnapshot_Definition FOREIGN KEY (KpiDefinitionId) REFERENCES dbo.KpiDefinition(KpiDefinitionId),
    CONSTRAINT FK_KpiSnapshot_Event FOREIGN KEY (DerivedThroughEventId) REFERENCES dbo.Event(EventId)
);
GO

CREATE TABLE dbo.ReportCategory (
    ReportCategoryId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ReportCategory PRIMARY KEY,
    ReportCategoryCode nvarchar(100) NOT NULL,
    ReportCategoryName nvarchar(200) NOT NULL,
    CONSTRAINT UQ_ReportCategory_Code UNIQUE (ReportCategoryCode)
);
GO

CREATE TABLE dbo.ReportDefinition (
    ReportDefinitionId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_ReportDefinition PRIMARY KEY,
    OrganizationId bigint NOT NULL,
    ReportCode nvarchar(100) NOT NULL,
    ReportName nvarchar(200) NOT NULL,
    ReportCategoryId int NOT NULL,
    DefinitionJson nvarchar(max) NOT NULL,
    IsActive bit NOT NULL CONSTRAINT DF_ReportDefinition_IsActive DEFAULT (1),
    CONSTRAINT UQ_ReportDefinition_Organization_Code UNIQUE (OrganizationId, ReportCode),
    CONSTRAINT FK_ReportDefinition_Organization FOREIGN KEY (OrganizationId) REFERENCES dbo.Organization(OrganizationId),
    CONSTRAINT FK_ReportDefinition_Category FOREIGN KEY (ReportCategoryId) REFERENCES dbo.ReportCategory(ReportCategoryId)
);
GO

CREATE TABLE dbo.DashboardDefinition (
    DashboardDefinitionId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_DashboardDefinition PRIMARY KEY,
    OrganizationId bigint NOT NULL,
    DashboardCode nvarchar(100) NOT NULL,
    DashboardName nvarchar(200) NOT NULL,
    DefinitionJson nvarchar(max) NOT NULL,
    IsActive bit NOT NULL CONSTRAINT DF_DashboardDefinition_IsActive DEFAULT (1),
    CONSTRAINT UQ_DashboardDefinition_Organization_Code UNIQUE (OrganizationId, DashboardCode),
    CONSTRAINT FK_DashboardDefinition_Organization FOREIGN KEY (OrganizationId) REFERENCES dbo.Organization(OrganizationId)
);
GO

CREATE TABLE dbo.IntegrationEndpointType (
    IntegrationEndpointTypeId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_IntegrationEndpointType PRIMARY KEY,
    EndpointTypeCode nvarchar(100) NOT NULL,
    EndpointTypeName nvarchar(200) NOT NULL,
    CONSTRAINT UQ_IntegrationEndpointType_Code UNIQUE (EndpointTypeCode)
);
GO

CREATE TABLE dbo.IntegrationEndpoint (
    IntegrationEndpointId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_IntegrationEndpoint PRIMARY KEY,
    OrganizationId bigint NOT NULL,
    EndpointCode nvarchar(100) NOT NULL,
    IntegrationEndpointTypeId int NOT NULL,
    EndpointName nvarchar(200) NOT NULL,
    ConfigurationJson nvarchar(max) NOT NULL,
    IsActive bit NOT NULL CONSTRAINT DF_IntegrationEndpoint_IsActive DEFAULT (1),
    CONSTRAINT UQ_IntegrationEndpoint_Organization_Code UNIQUE (OrganizationId, EndpointCode),
    CONSTRAINT FK_IntegrationEndpoint_Organization FOREIGN KEY (OrganizationId) REFERENCES dbo.Organization(OrganizationId),
    CONSTRAINT FK_IntegrationEndpoint_Type FOREIGN KEY (IntegrationEndpointTypeId) REFERENCES dbo.IntegrationEndpointType(IntegrationEndpointTypeId)
);
GO

CREATE TABLE dbo.OutboxMessage (
    OutboxMessageId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_OutboxMessage PRIMARY KEY,
    EventId bigint NOT NULL,
    IntegrationEndpointId bigint NULL,
    MessageType nvarchar(200) NOT NULL,
    PayloadJson nvarchar(max) NOT NULL,
    CreatedUtc datetime2(7) NOT NULL,
    PublishedUtc datetime2(7) NULL,
    PublishAttemptCount int NOT NULL CONSTRAINT DF_OutboxMessage_Attempts DEFAULT (0),
    LastError nvarchar(max) NULL,
    CONSTRAINT CK_OutboxMessage_Attempts CHECK (PublishAttemptCount >= 0),
    CONSTRAINT FK_OutboxMessage_Event FOREIGN KEY (EventId) REFERENCES dbo.Event(EventId),
    CONSTRAINT FK_OutboxMessage_Endpoint FOREIGN KEY (IntegrationEndpointId) REFERENCES dbo.IntegrationEndpoint(IntegrationEndpointId)
);
GO

CREATE TABLE dbo.HealthCheckType (
    HealthCheckTypeId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_HealthCheckType PRIMARY KEY,
    HealthCheckTypeCode nvarchar(100) NOT NULL,
    HealthCheckTypeName nvarchar(200) NOT NULL,
    CONSTRAINT UQ_HealthCheckType_Code UNIQUE (HealthCheckTypeCode)
);
GO

CREATE TABLE dbo.HealthStatus (
    HealthStatusId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_HealthStatus PRIMARY KEY,
    HealthStatusCode nvarchar(100) NOT NULL,
    HealthStatusName nvarchar(200) NOT NULL,
    CONSTRAINT UQ_HealthStatus_Code UNIQUE (HealthStatusCode)
);
GO

CREATE TABLE dbo.SystemHealthCheck (
    SystemHealthCheckId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_SystemHealthCheck PRIMARY KEY,
    OrganizationId bigint NULL,
    HealthCheckTypeId int NOT NULL,
    CheckedUtc datetime2(7) NOT NULL,
    HealthStatusId int NOT NULL,
    DetailJson nvarchar(max) NOT NULL,
    CONSTRAINT FK_SystemHealthCheck_Organization FOREIGN KEY (OrganizationId) REFERENCES dbo.Organization(OrganizationId),
    CONSTRAINT FK_SystemHealthCheck_Type FOREIGN KEY (HealthCheckTypeId) REFERENCES dbo.HealthCheckType(HealthCheckTypeId),
    CONSTRAINT FK_SystemHealthCheck_Status FOREIGN KEY (HealthStatusId) REFERENCES dbo.HealthStatus(HealthStatusId)
);
GO

CREATE TABLE dbo.RunbookCategory (
    RunbookCategoryId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_RunbookCategory PRIMARY KEY,
    RunbookCategoryCode nvarchar(100) NOT NULL,
    RunbookCategoryName nvarchar(200) NOT NULL,
    CONSTRAINT UQ_RunbookCategory_Code UNIQUE (RunbookCategoryCode)
);
GO

CREATE TABLE dbo.Runbook (
    RunbookId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Runbook PRIMARY KEY,
    OrganizationId bigint NULL,
    RunbookCode nvarchar(100) NOT NULL,
    RunbookName nvarchar(200) NOT NULL,
    RunbookCategoryId int NOT NULL,
    DocumentLocation nvarchar(1000) NOT NULL,
    IsActive bit NOT NULL CONSTRAINT DF_Runbook_IsActive DEFAULT (1),
    CONSTRAINT FK_Runbook_Organization FOREIGN KEY (OrganizationId) REFERENCES dbo.Organization(OrganizationId),
    CONSTRAINT FK_Runbook_Category FOREIGN KEY (RunbookCategoryId) REFERENCES dbo.RunbookCategory(RunbookCategoryId)
);
GO
CREATE UNIQUE INDEX UX_Runbook_Organization_Code ON dbo.Runbook(OrganizationId, RunbookCode) WHERE OrganizationId IS NOT NULL;
GO
