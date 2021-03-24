IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('procGetPushDownSubTreeAvailableMappings'))
	DROP PROCEDURE procGetPushDownSubTreeAvailableMappings
GO
--EXEC procGetPushDownSubTreeAvailableMappings @pNodeIndex = 0, @pIsFromAddNew = 1
CREATE PROCEDURE procGetPushDownSubTreeAvailableMappings
	@pNodeIndex INT,
	@pIsFromAddNew BIT
AS
BEGIN
	SET NOCOUNT ON

	IF(@pIsFromAddNew = 1)
	BEGIN
		SET @pNodeIndex -= 1
	END

	DECLARE @upperNodeLevel INT = 0
	DECLARE @lowerNodeLevel INT = 0

	DECLARE @tblMappingNodes TABLE(
		NodeMappingName VARCHAR(MAX),
		NodeMappingLevel INT
	)

	SELECT @upperNodeLevel = ISNULL(MAX(ND.NodeMappingLevel),0) 
	FROM ApprovalTree.Nodes ND 
	INNER JOIN ApprovalTree.SubTrees ST ON ND.SubTreeId = ST.Id
	WHERE ST.Name = 'Target Pushdown'
		AND ND.IsAuthorNode = 1 
		AND ND.IsPublish = 0
		AND ND.NodeIndex < @pNodeIndex
	
	SELECT @lowerNodeLevel = ISNULL(MIN(ND.NodeMappingLevel),0) 
	FROM ApprovalTree.Nodes ND 
	INNER JOIN ApprovalTree.SubTrees ST ON ND.SubTreeId = ST.Id
	WHERE ST.Name = 'Target Pushdown'
		AND ND.IsAuthorNode = 1 
		AND ND.IsPublish = 0
		AND ND.NodeIndex > @pNodeIndex

	INSERT INTO @tblMappingNodes(NodeMappingLevel, NodeMappingName)
	SELECT OuterND.NodeLevel AS NodeMappingLevel
		,STUFF(
				(
					SELECT '/ ' + CAST(ND.Name AS VARCHAR(255)) [MappingName]
					FROM ApprovalTree.Nodes ND 
					INNER JOIN ApprovalTree.ApprovalStatus APS ON ND.Id = APS.NodeId 
						AND APS.IsSubmitNode = 0
					INNER JOIN ApprovalTree.SubTrees ST ON ND.SubTreeId = ST.Id
					INNER JOIN ApprovalTree.Trees T ON ST.TreeId = T.Id
					WHERE T.Name = 'Budget Approval'
						AND ND.NodeLevel > @upperNodeLevel 
						AND (ND.NodeLevel <= @lowerNodeLevel 
								OR @lowerNodeLevel = 0)
								AND ND.IsAuthorNode = 1
								AND ND.IsPublish = 1
						AND	ND.NodeLevel = OuterND.NodeLevel
					FOR XML PATH(''), TYPE
				)
			.value('.','NVARCHAR(MAX)'),1,2,'') AS NodeMappingName
	FROM ApprovalTree.Nodes OuterND 
	INNER JOIN ApprovalTree.ApprovalStatus APS ON OuterND.Id = APS.NodeId 
		AND APS.IsSubmitNode = 0
	INNER JOIN ApprovalTree.SubTrees ST ON OuterND.SubTreeId = ST.Id
	INNER JOIN ApprovalTree.Trees T ON ST.TreeId = T.Id
	WHERE T.Name = 'Budget Approval'
		AND OuterND.NodeLevel > @upperNodeLevel 
		AND (OuterND.NodeLevel <= @lowerNodeLevel 
				OR @lowerNodeLevel = 0)
		AND OuterND.IsAuthorNode = 1
		AND OuterND.IsPublish = 1
	GROUP BY OuterND.NodeLevel

	SELECT MN.NodeMappingLevel,
		CONCAT(MN.NodeMappingLevel, '.', MN.NodeMappingName) NodeMappingName,
		ISNULL(Nodes.NodeMappingLevel, 0) MappedLevel
	FROM @tblMappingNodes MN
	LEFT JOIN (
		SELECT NodeMappingLevel, NodeIndex, ND.Id 
		FROM ApprovalTree.Nodes ND 
		INNER JOIN ApprovalTree.SubTrees ST ON ND.SubTreeId = ST.Id
		WHERE ST.Name = 'Target Pushdown'
			AND ND.IsAuthorNode = 1
			AND ND.IsPublish = 0) AS Nodes ON Nodes.NodeMappingLevel = MN.NodeMappingLevel
	WHERE Nodes.NodeIndex = @pNodeIndex OR Nodes.NodeIndex IS NULL
		
END

