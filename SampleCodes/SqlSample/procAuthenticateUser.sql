IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('procAuthenticateUser'))
	DROP PROCEDURE procAuthenticateUser
GO
--EXEC procAuthenticateUser @pUserId ='iplanlion' 
CREATE PROCEDURE procAuthenticateUser
   	@pUserId VARCHAR(64)
AS
BEGIN
	SET NOCOUNT ON
	DECLARE @Role VARCHAR(64)

	CREATE TABLE #UserInfo(
		UserId VARCHAR(64),
		Password VARCHAR(2000),
		Email VARCHAR(64),
		UserName VARCHAR(64),
		IsActive BIT,
		IsAdmin BIT,
		Role VARCHAR(64)
	)

	INSERT INTO #UserInfo(UserId,Password,Email,UserName,IsActive,IsAdmin,Role)
	SELECT TOP 1 u.[employee_id] AS UserId, 
		[password] AS Password,
		[email_address] AS Email, 
		[user_name] AS UserName,
		[is_active] AS IsActive,
		[is_admin] AS IsAdmin, 
		[role] AS [Role]
	FROM users.Users U
	INNER JOIN users.UserProfiles UP ON U.employee_id = UP.employee_id
	WHERE U.employee_id = @pUserId 
	ORDER BY UP.role 

	SELECT @Role = Role FROM #UserInfo

	SELECT * 
	FROM #UserInfo OUTER APPLY 
	(
		SELECT 
			AP.Name AS PolicyName,
			T.Name AS TreeName,
			APS.IsSubmitNode,
			APS.SubTreeName,
			AP.IsViewOnly
		FROM AuthorizationPolicies AP
		INNER JOIN ApprovalRoleAuthorizationPolicies ARAP ON AP.Id = ARAP.AuthorizationPoliciesId
		INNER JOIN ApprovalTree.NodeRoles NR ON NR.ID = ARAP.ApprovalRoleId
		INNER JOIN ApprovalTree.ApprovalStatus APS ON NR.NodeId = APS.NodeId
		INNER JOIN ApprovalTree.Trees T ON AP.TreeId = T.Id
		WHERE NR.Role = @Role
	) AS Result

	DROP TABLE #UserInfo
END

