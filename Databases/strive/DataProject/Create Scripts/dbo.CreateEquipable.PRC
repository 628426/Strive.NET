SET QUOTED_IDENTIFIER ON 
GO
SET ANSI_NULLS ON 
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[CreateEquipable]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[CreateEquipable]
GO


CREATE PROCEDURE dbo.CreateEquipable
	(
@AreaID int, @Name nvarchar(16), @ModelID int,
@Value int, @Weight int,
@WearLocation int, @ArmorClass int,
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
insert into EquipableItem
values ( @PhysicalObjectID, @WearLocation, @ArmorClass )
insert into RespawnPoint
values ( @PhysicalObjectID, @X, @Y, @Z, @HeadingX, @HeadingY, @HeadingZ )
	commit
	RETURN 


GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

