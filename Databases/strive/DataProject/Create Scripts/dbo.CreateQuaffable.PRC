SET QUOTED_IDENTIFIER ON 
GO
SET ANSI_NULLS ON 
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[CreateQuaffable]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[CreateQuaffable]
GO


CREATE PROCEDURE dbo.CreateQuaffable
	(
@AreaID int, @Name nvarchar(16), @ModelID int,
@Value int, @Weight int,
@LiquidTypeID int, @Capacity int,
@X int, @Y int, @Z int,
@HeadingX int, @HeadingY int, @HeadingZ int
	)
AS
	declare @PhysicalObjectID int
	SET NOCOUNT ON
	SET XACT_ABORT ON
	begin transaction
insert into PhysicalObject
values ( @AreaID, @Name, @ModelID )
set @PhysicalObjectID = SCOPE_IDENTITY()
insert into ItemPhysicalObject
values ( @PhysicalObjectID, @Value, @Weight )
insert into QuaffableItem
values ( @PhysicalObjectID, @LiquidTypeID, @Capacity )
insert into RespawnPoint
values ( @PhysicalObjectID, @X, @Y, @Z, @HeadingX, @HeadingY, @HeadingZ )
	commit
	RETURN 


GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

