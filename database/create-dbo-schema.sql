USE [SIMPLETS]
GO

-- Drop all tables before creating them.
-- Execute this ~5 times to drop all tables (fk exceptions)
-- EXEC sp_MSforeachtable @command1 = "DROP TABLE ?" 
-- EXEC sp_MSforeachtable @command1 = "DBCC CHECKIDENT (?, reseed, 1)"

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO

IF (NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'clubs')) 
BEGIN
    EXEC ('CREATE SCHEMA [clubs] AUTHORIZATION [dbo]')
END

/****** Object:  Table [dbo].[Adresses]    Script Date: 12/12/2014 2:40:31 PM ******/
CREATE TABLE [dbo].[Adresses](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[NoCivique] [int] NULL,
	[Rue] [varchar](50) NULL,
	[Appartement] [varchar](10) NULL,
	[Ville] [varchar](150) NULL,
	[CodePostal] [varchar](16) NULL,
	[Version] [rowversion] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)) ON [PRIMARY]
GO

/****** Object:  Table [dbo].[Concentrations]    Script Date: 12/12/2014 2:40:31 PM ******/
CREATE TABLE [dbo].[Concentrations](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Acronyme] [varchar](10) NOT NULL,
	[Description] [varchar](150) NULL
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)) ON [PRIMARY]
GO

/****** Object:  Table [dbo].[Liens_Parente]    Script Date: 12/12/2014 2:40:31 PM ******/
CREATE TABLE [dbo].[TypesContact](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Nom] [varchar](50) NOT NULL,
	[Description] [varchar](150) NULL
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)) ON [PRIMARY]
GO

/****** Object:  Table [dbo].[Units]    Script Date: 12/12/2014 2:40:31 PM ******/
CREATE TABLE [dbo].[Unites](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Code] [varchar](10) NOT NULL,
	[Systeme] [varchar](10) NOT NULL
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)) ON [PRIMARY]
GO

/****** Object:  Table [dbo].[Units]    Script Date: 12/12/2014 2:40:31 PM ******/
CREATE TABLE [dbo].[Contacts](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[TypeContactId] [int] NOT NULL,
	[Nom] [varchar](50) NOT NULL,
	[Prenom] [varchar](50) NOT NULL,
	[Telephone] [varchar](20) NULL,
	[Courriel] [varchar](250) NULL,
	[Version] [rowversion] NOT NULL
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)) ON [PRIMARY]
GO

/****** Foreign keys ******/
ALTER TABLE [dbo].[Contacts]  WITH CHECK ADD CONSTRAINT [FKContactsTypesContact] FOREIGN KEY([TypeContactId])
REFERENCES [dbo].[TypesContact] ([Id])
GO
ALTER TABLE [dbo].[Contacts] CHECK CONSTRAINT [FKContactsTypesContact]
GO

SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER OFF
GO
SET ANSI_PADDING OFF
GO