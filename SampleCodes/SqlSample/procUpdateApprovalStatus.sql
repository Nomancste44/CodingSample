IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('procUpdateApprovalStatus'))
	DROP PROCEDURE procUpdateApprovalStatus
GO
--EXEC procUpdateApprovalStatus @pTreeId ='0244ECB4-F806-4DCF-8426-50C69E69C24D' ,@pEmployeeId = 'iplanlion'
CREATE PROCEDURE procUpdateApprovalStatus
   	@pTreeId UNIQUEIDENTIFIER,
	@pEmployeeId  VARCHAR(255) 
AS

BEGIN
	SET NOCOUNT ON

	BEGIN TRAN 
		BEGIN TRY
			DELETE APS 
			FROM ApprovalTree.Trees T
			INNER JOIN ApprovalTree.SubTrees ST ON T.Id = ST.TreeId
			INNER JOIN ApprovalTree.ApprovalStatus APS ON ST.Id = APS.SubtreeId
			WHERE T.Id = @pTreeId

			INSERT INTO [ApprovalTree].[ApprovalStatus]
					   ([ApprovalLevel],[NodeMappingLevel],[NodeName],[DataNodeName],[ApprovalStatusLabel],[RejectStatusLabel]
					   ,[IsSubmitNode],[EmployeeId],[UpdateOn],[NodeId],[SubtreeId],[SubTreeName])
			SELECT [NodeLevel] AS [ApprovalLevel]
				  ,[OuterND].NodeMappingLevel
				  ,[OuterND].[Name] AS [NodeName]
				  ,(
						SELECT ADN.cost_center_hierarchy_column 
						FROM (
								SELECT MIN(ND.NodeIndex) AS NodeIndex
								FROM [ApprovalTree].[Nodes] ND
								WHERE ND.SubTreeId = OuterND.SubTreeId
										AND ND.IsAuthorNode = 0
										AND ND.IsPublish = 1
										AND ND.NodeIndex > OuterND.NodeIndex

								) AS DN INNER JOIN [ApprovalTree].Nodes ND ON DN.NodeIndex = ND.NodeIndex
											AND ND.SubTreeId = OuterND.SubTreeId 
											AND ND.IsAuthorNode = 0 
											AND ND.IsPublish = 1
									INNER JOIN [AvailableDataNode] ADN ON ND.Name = ADN.data_node_name
					) AS [DataNodeName]
				   ,[ApprovalLabel] AS [ApprovalStatusLabel]
				   ,[RejectLabel] AS [RejectStatusLabel]
				   ,CASE 
					WHEN (
							SELECT MAX(NodeLevel) 
							FROM ApprovalTree.Nodes N
							INNER JOIN ApprovalTree.SubTrees ST ON N.SubTreeId = ST.Id
							WHERE SubTreeId = OuterND.SubTreeId
								AND IsAuthorNode = 1
								AND IsPublish = 1
								AND ST.Name ! = 'TAFE Commission'
							GROUP BY SubTreeId
						 ) = OuterND.NodeLevel
					THEN 1 ELSE 0 END AS [IsSubmitNode]
				  ,@pEmployeeId AS [EmployeeId]
				  ,GETDATE() AS [UpdateOn]
				  ,[OuterND].[Id] AS [NodeId]
				  ,[SubTreeId] AS [SubtreeId]
				  ,[OuterST].[Name] AS [SubTreeName]
			FROM [ApprovalTree].[Nodes] OuterND
			INNER JOIN [ApprovalTree].[SubTrees] OuterST ON OuterND.SubTreeId = OuterST.Id
			INNER JOIN [ApprovalTree].[Trees] OuterT ON OuterST.TreeId = OuterT.Id
			WHERE OuterT.Id = @pTreeId
				AND OuterND.IsAuthorNode = 1
				AND OuterND.IsPublish = 1

			EXEC procSetAuthorizationPolicies @pTreeId 
			COMMIT TRAN
		END TRY
		BEGIN CATCH
			ROLLBACK;
		END CATCH
END

