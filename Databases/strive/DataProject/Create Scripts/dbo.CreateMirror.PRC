SET QUOTED_IDENTIFIER ON 
GO
SET ANSI_NULLS ON 
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[CreateMirror]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[CreateMirror]
GO



CREATE PROCEDURE CreateMirror
@PhysicalObjectID int,
@X int, @Y int, @Z int,
@HeadingX int, @HeadingY int, @HeadingZ int
AS
SET NOCOUNT ON
insert into RespawnPoint values (
    @PhysicalObjectID,
    @X, @Y, @Z,
    @HeadingX, @HeadingY, @HeadingZ
)



GO
SET QUOTED_IDENTIFIER OFF 
GO
SET ANSI_NULLS ON 
GO

