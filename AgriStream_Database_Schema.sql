-- ============================================================================
-- CLDV7112 Practicum: AgriStream IoT
-- Azure SQL Database Schema & Initial Data Seed
-- Tasks b & d Support Script
-- ============================================================================

-- 1. Create Roles Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Roles')
BEGIN
    CREATE TABLE Roles (
        RoleId INT IDENTITY(1,1) PRIMARY KEY,
        RoleName NVARCHAR(50) NOT NULL UNIQUE,
        Description NVARCHAR(255) NULL
    );
END
GO

-- 2. Create Users Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE Users (
        UserId INT IDENTITY(1,1) PRIMARY KEY,
        Username NVARCHAR(100) NOT NULL UNIQUE,
        Password NVARCHAR(255) NOT NULL,
        Email NVARCHAR(150) NULL,
        RoleId INT NOT NULL,
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) 
            REFERENCES Roles(RoleId)
            ON DELETE NO ACTION
    );
END
GO

-- 3. Create Farms Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Farms')
BEGIN
    CREATE TABLE Farms (
        FarmId INT IDENTITY(1,1) PRIMARY KEY,
        FarmName NVARCHAR(150) NOT NULL,
        Location NVARCHAR(200) NOT NULL,
        TotalHectares DECIMAL(10, 2) NOT NULL,
        OwnerUserId INT NOT NULL,
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        CONSTRAINT FK_Farms_Users FOREIGN KEY (OwnerUserId) 
            REFERENCES Users(UserId)
    );
END
GO

-- 4. Create Fields Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Fields')
BEGIN
    CREATE TABLE Fields (
        FieldId INT IDENTITY(1,1) PRIMARY KEY,
        FarmId INT NOT NULL,
        FieldName NVARCHAR(100) NOT NULL,
        CropType NVARCHAR(100) NOT NULL,
        SoilType NVARCHAR(100) NULL,
        AreaHectares DECIMAL(10, 2) NOT NULL,
        Status NVARCHAR(50) DEFAULT 'Active',
        CONSTRAINT FK_Fields_Farms FOREIGN KEY (FarmId) 
            REFERENCES Farms(FarmId) ON DELETE CASCADE
    );
END
GO

-- 5. Create IoTDevices Table (Master Registry for Task b & d)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'IoTDevices')
BEGIN
    CREATE TABLE IoTDevices (
        DeviceId INT IDENTITY(1,1) PRIMARY KEY,
        DeviceSerialNumber NVARCHAR(100) NOT NULL UNIQUE,
        DeviceType NVARCHAR(100) NOT NULL,
        FieldId INT NOT NULL,
        Status NVARCHAR(50) DEFAULT 'Online', -- Updated to 'Active - Irrigating' by Queue Trigger
        BatteryLevelPercentage INT NULL,
        InstallationDate DATE NOT NULL,
        CONSTRAINT FK_IoTDevices_Fields FOREIGN KEY (FieldId) 
            REFERENCES Fields(FieldId)
    );
END
GO

-- 6. Create DroneMedia Table (Metadata linking to Azure Blob Storage URLs)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DroneMedia')
BEGIN
    CREATE TABLE DroneMedia (
        MediaId INT IDENTITY(1,1) PRIMARY KEY,
        FieldId INT NOT NULL,
        UploadedByUserId INT NOT NULL,
        FileName NVARCHAR(255) NOT NULL,
        BlobUrl NVARCHAR(1000) NOT NULL,
        FlightAltitudeMeters INT NULL,
        ImageResolution NVARCHAR(50) NULL,
        CapturedAt DATETIME2 NOT NULL,
        UploadedAt DATETIME2 DEFAULT GETUTCDATE(),
        CONSTRAINT FK_DroneMedia_Fields FOREIGN KEY (FieldId) 
            REFERENCES Fields(FieldId),
        CONSTRAINT FK_DroneMedia_Users FOREIGN KEY (UploadedByUserId) 
            REFERENCES Users(UserId)
    );
END
GO

-- ============================================================================
-- Seed Data
-- ============================================================================

-- Roles
IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = 'Admin')
    INSERT INTO Roles (RoleName, Description) VALUES ('Admin', 'System administrator with full management access');
IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = 'Farmer')
    INSERT INTO Roles (RoleName, Description) VALUES ('Farmer', 'Farm owner/manager monitoring telemetry and drone media');
IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = 'Agronomist')
    INSERT INTO Roles (RoleName, Description) VALUES ('Agronomist', 'Agricultural expert analyzing crop and soil telemetry');
GO

-- Initial Users
IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'admin_agristream')
    INSERT INTO Users (Username, Password, Email, RoleId) VALUES ('admin_agristream', 'AdminPassword123!', 'admin@agristream.com', 1);
IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'john_farmer')
    INSERT INTO Users (Username, Password, Email, RoleId) VALUES ('john_farmer', 'FarmerPassword123!', 'john@greenvalley.com', 2);
IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'dr_sarah_agro')
    INSERT INTO Users (Username, Password, Email, RoleId) VALUES ('dr_sarah_agro', 'AgroPassword123!', 'sarah@agristream.com', 3);
GO

-- Sample Farm
IF NOT EXISTS (SELECT 1 FROM Farms WHERE FarmName = 'Green Valley Agricultural Hub')
    INSERT INTO Farms (FarmName, Location, TotalHectares, OwnerUserId)
    VALUES ('Green Valley Agricultural Hub', 'Free State, South Africa', 1250.50, (SELECT UserId FROM Users WHERE Username = 'john_farmer'));
GO

-- Sample Fields
IF NOT EXISTS (SELECT 1 FROM Fields WHERE FieldName = 'North Parcel Alpha')
    INSERT INTO Fields (FarmId, FieldName, CropType, SoilType, AreaHectares, Status)
    VALUES (1, 'North Parcel Alpha', 'Maize', 'Sandy Loam', 450.00, 'Active');

IF NOT EXISTS (SELECT 1 FROM Fields WHERE FieldName = 'South Parcel Beta')
    INSERT INTO Fields (FarmId, FieldName, CropType, SoilType, AreaHectares, Status)
    VALUES (1, 'South Parcel Beta', 'Soybeans', 'Clay Loam', 320.00, 'Active');
GO

-- Sample IoT Devices
IF NOT EXISTS (SELECT 1 FROM IoTDevices WHERE DeviceSerialNumber = 'SN-SOIL-001')
    INSERT INTO IoTDevices (DeviceSerialNumber, DeviceType, FieldId, Status, BatteryLevelPercentage, InstallationDate)
    VALUES ('SN-SOIL-001', 'Multi-Depth Soil Probe', 1, 'Online', 94, '2026-01-15');

IF NOT EXISTS (SELECT 1 FROM IoTDevices WHERE DeviceSerialNumber = 'SN-WTH-002')
    INSERT INTO IoTDevices (DeviceSerialNumber, DeviceType, FieldId, Status, BatteryLevelPercentage, InstallationDate)
    VALUES ('SN-WTH-002', 'Solar Weather Station', 1, 'Online', 98, '2026-02-10');

IF NOT EXISTS (SELECT 1 FROM IoTDevices WHERE DeviceSerialNumber = 'SN-SOIL-003')
    INSERT INTO IoTDevices (DeviceSerialNumber, DeviceType, FieldId, Status, BatteryLevelPercentage, InstallationDate)
    VALUES ('SN-SOIL-003', 'Multi-Depth Soil Probe', 2, 'Online', 88, '2026-02-12');
GO
