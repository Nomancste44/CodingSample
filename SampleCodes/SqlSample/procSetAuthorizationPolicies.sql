IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('procSetAuthorizationPolicies'))
	DROP PROCEDURE procSetAuthorizationPolicies
GO
--EXEC procSetAuthorizationPolicies @pTreeId ='4D7884FA-0E12-4A8A-84E2-8C84F41BAC8F' 
CREATE PROCEDURE procSetAuthorizationPolicies
   	@pTreeId UNIQUEIDENTIFIER
AS
BEGIN
	SET NOCOUNT ON
	/***Set Policies for Planning Overview Roles***/
	INSERT INTO ApprovalRoleAuthorizationPolicies(ApprovalRoleId,AuthorizationPoliciesId)
	SELECT NR.Id, AP.Id
	FROM ApprovalTree.Trees T 
	INNER JOIN AuthorizationPolicies AP ON T.Id = AP.TreeId AND T.Id = @pTreeId
		AND AP.IsSubmit = 0
	INNER JOIN ApprovalTree.SubTrees st ON AP.TreeId = st.TreeId  
	INNER JOIN ApprovalTree.Nodes N ON st.Id = N.SubTreeId
		AND N.IsAuthorNode = 1 
		AND N.IsPublish = 1
	INNER JOIN ApprovalTree.NodeRoles NR ON N.Id = NR.NodeId AND NR.IsViewOnly = AP.IsViewOnly
	INNER JOIN ApprovalStatus APS ON 
		N.Id = APS.node_id
		AND N.SubTreeId = APS.subtree_id
		AND APS.is_submit = 0
	LEFT JOIN ApprovalRoleAuthorizationPolicies ARAP ON AP.Id = ARAP.AuthorizationPoliciesId
		AND NR.Id = ARAP.ApprovalRoleId
	WHERE ARAP.Id IS NULL

	/***Set Policies for Planning Roles***/
	INSERT INTO ApprovalRoleAuthorizationPolicies(ApprovalRoleId,AuthorizationPoliciesId)
	SELECT NR.Id, AP.Id
	FROM ApprovalTree.Trees T 
	INNER JOIN AuthorizatiONPolicies AP ON T.Id = @pTreeId
		AND AP.IsSubmit = 1 
	INNER JOIN ApprovalTree.SubTrees ST ON AP.TreeId = ST.TreeId  
		AND AP.Name = CASE WHEN ST.Name = 'Delivery' THEN 'EducationalPlanning'
							WHEN ST.Name = 'NonDelivery' THEN 'NonDeliveryPlanning' 
							WHEN ST.Name = 'TAFE Corporate' THEN 'NonDeliveryPlanning' 
							WHEN ST.Name = 'Capacity Management' THEN 'CapacityManagementPlanning' 
							WHEN ST.Name = 'Product Builder' THEN 'ProductBuilderPlanning' 
							ELSE '' END
	INNER JOIN ApprovalTree.Nodes N ON ST.Id = N.SubTreeId
		AND N.IsAuthorNode = 1 
		AND N.IsPublish = 1
	INNER JOIN ApprovalTree.NodeRoles NR ON N.Id = NR.NodeId 
		AND NR.IsViewONly = AP.IsViewONly
	INNER JOIN ApprovalStatus APS ON N.Id = APS.node_id
		AND N.SubTreeId = APS.subtree_id
		AND APS.is_submit = 1
	LEFT JOIN ApprovalRoleAuthorizationPolicies ARAP ON AP.Id = ARAP.AuthorizationPoliciesId
		AND NR.Id = ARAP.ApprovalRoleId
	WHERE ARAP.Id IS NULL

	/***Set Capacity Planning policies for Delivery and NonDelivery Role from Budget Approval Tree***/
	INSERT INTO ApprovalRoleAuthorizationPolicies(ApprovalRoleId,AuthorizationPoliciesId)
	SELECT NR.Id, AP.Id
	FROM ApprovalTree.Trees T 
	INNER JOIN AuthorizationPolicies AP ON T.Id = AP.TreeId 
		AND T.Name = 'Budget approval'
		AND AP.IsSubmit = 1 
	INNER JOIN ApprovalTree.SubTrees ST ON AP.TreeId = ST.TreeId  
		AND AP.Name = CASE WHEN ST.Name != 'TAFE Commission' THEN 'CapacityPlanning' 
							ELSE '' END
	INNER JOIN ApprovalTree.Nodes N ON ST.Id = N.SubTreeId
		AND N.IsAuthorNode = 1 
		AND N.IsPublish = 1
	INNER JOIN ApprovalTree.NodeRoles NR ON N.Id = NR.NodeId AND NR.IsViewOnly = AP.IsViewOnly
	INNER JOIN ApprovalStatus APS ON 
		N.Id = APS.node_id
		AND N.SubTreeId = APS.subtree_id
		AND APS.is_submit = 1
	LEFT JOIN ApprovalRoleAuthorizationPolicies ARAP ON AP.Id = ARAP.AuthorizationPoliciesId
		AND NR.Id = ARAP.ApprovalRoleId
	WHERE ARAP.Id IS NULL
	
END

